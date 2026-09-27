#!/usr/bin/env python3
"""Extract the minimal Sidekick 1.2.6 runtime dependency closure from a Unity .unitypackage.

This keeps the HIGHFLY CI build lean: only the official demo humanoid prefab and
package-owned assets reachable through GUID references are reconstructed.
No Synty source assets are committed to this repository; they exist only in CI.
"""
from __future__ import annotations

import argparse
import re
import shutil
import tarfile
import tempfile
from pathlib import Path

GUID_RE = re.compile(r"\bguid:\s*([0-9a-fA-F]{32})\b")
META_GUID_RE = re.compile(r"(?m)^guid:\s*([0-9a-fA-F]{32})\s*$")

START_PATH = "Assets/Synty/SidekickCharacters/_Demos/Prefabs/SK_FacialDemoCharacter.prefab"
TARGET_RESOURCE = "Assets/Resources/HIGHFLY/Run0I/Sidekick126.prefab"
REQUIRED_SHADER_GUID = "db628544640279b41a4a7aa5d75c0322"


def read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8", errors="ignore")


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--package", required=True, type=Path)
    ap.add_argument("--project", required=True, type=Path)
    args = ap.parse_args()

    if not args.package.is_file():
        raise SystemExit(f"Missing unitypackage: {args.package}")
    if not (args.project / "Assets").is_dir():
        raise SystemExit(f"Not a Unity project: {args.project}")

    with tempfile.TemporaryDirectory(prefix="sidekick126_") as td:
        root = Path(td)
        with tarfile.open(args.package, "r:*") as tf:
            tf.extractall(root)

        path_to_dir: dict[str, Path] = {}
        guid_to_dir: dict[str, Path] = {}

        for pathname in root.rglob("pathname"):
            asset_dir = pathname.parent
            logical = read_text(pathname).strip()
            if not logical:
                continue
            path_to_dir[logical] = asset_dir

            meta = asset_dir / "asset.meta"
            if meta.is_file():
                m = META_GUID_RE.search(read_text(meta))
                if m:
                    guid_to_dir[m.group(1).lower()] = asset_dir

        start_dir = path_to_dir.get(START_PATH)
        if start_dir is None:
            raise SystemExit(f"Sidekick 1.2.6 package does not contain {START_PATH}")

        shader_dir = guid_to_dir.get(REQUIRED_SHADER_GUID)
        if shader_dir is None:
            raise SystemExit(
                "Sidekick shader GUID missing from package: " + REQUIRED_SHADER_GUID
            )

        queue = [start_dir]

        # Arsenal pass: include the official Sidekick axe and its dependency
        # closure as a same-style weapon candidate for the Hunter.
        sidekick_axe_dirs = [
            d for logical, d in path_to_dir.items()
            if logical.lower().endswith("sk_axe.fbx")
            and "goblin_axe" in logical.lower()
        ]
        if not sidekick_axe_dirs:
            raise SystemExit("Sidekick 1.2.6 package is missing Goblin_Axe/SK_Axe.fbx")
        queue.extend(sidekick_axe_dirs)

        selected: set[Path] = set()
        unresolved: set[str] = set()

        while queue:
            asset_dir = queue.pop()
            if asset_dir in selected:
                continue
            selected.add(asset_dir)

            asset = asset_dir / "asset"
            if not asset.is_file():
                continue

            data = asset.read_bytes()
            # GUID references live in YAML/text assets. Binary files can be copied
            # as leaves; decoding with ignore is safe for discovering textual refs.
            text = data.decode("utf-8", errors="ignore")
            for guid in GUID_RE.findall(text):
                key = guid.lower()
                dep = guid_to_dir.get(key)
                if dep is not None:
                    if dep not in selected:
                        queue.append(dep)
                elif key != "0000000000000000f000000000000000":
                    unresolved.add(key)

        # Force include the Sidekick shader even if a Unity serialization variant
        # obscures the material->shader GUID edge.
        selected.add(shader_dir)

        copied = []
        for asset_dir in sorted(selected, key=lambda p: str(p)):
            pathname = asset_dir / "pathname"
            asset = asset_dir / "asset"
            meta = asset_dir / "asset.meta"
            if not pathname.is_file() or not asset.is_file():
                continue

            logical = read_text(pathname).strip()
            if not logical.startswith("Assets/"):
                continue

            dest = args.project / logical
            dest.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(asset, dest)
            if meta.is_file():
                shutil.copy2(meta, Path(str(dest) + ".meta"))
            copied.append(logical)

        # Duplicate the known-good full humanoid prefab under Resources so runtime
        # code can load it without AssetDatabase.
        src_prefab = args.project / START_PATH
        dst_prefab = args.project / TARGET_RESOURCE
        dst_prefab.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(src_prefab, dst_prefab)
        # Deliberately do not copy the prefab meta: Unity assigns a fresh GUID.

        shader_path = None
        for logical, d in path_to_dir.items():
            if d == shader_dir:
                shader_path = logical
                break

        if not shader_path or not (args.project / shader_path).is_file():
            raise SystemExit("Sidekick shader was selected but not reconstructed")

        if len(copied) < 4:
            raise SystemExit(f"Sidekick dependency closure is unexpectedly small: {len(copied)}")

        print(f"SIDEKICK126_EXTRACT_OK=1")
        print(f"SIDEKICK126_SELECTED_ASSETS={len(copied)}")
        print(f"SIDEKICK126_RESOURCE={TARGET_RESOURCE}")
        print(f"SIDEKICK126_SHADER={shader_path}")
        if unresolved:
            print("SIDEKICK126_EXTERNAL_GUIDS=" + ",".join(sorted(unresolved)[:50]))


if __name__ == "__main__":
    main()
