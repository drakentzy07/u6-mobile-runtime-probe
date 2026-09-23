#!/usr/bin/env python3
import argparse
import hashlib
import os
import re
import tarfile
from pathlib import Path

TEXT_EXTS = {
    ".unity", ".prefab", ".mat", ".asset", ".shadergraph",
    ".shadersubgraph", ".terrainlayer", ".cs", ".controller",
    ".anim", ".lighting", ".rendertexture", ".physicmaterial"
}

GUID_RE = re.compile(r"guid:\s*([0-9a-fA-F]{32})")


def deterministic_guid(old_guid: str) -> str:
    return hashlib.md5(("HIGHFLY_WORLD_CITY01:" + old_guid).encode("utf-8")).hexdigest()


def read_existing_guids(project: Path):
    result = set()
    assets = project / "Assets"
    for meta in assets.rglob("*.meta"):
        try:
            text = meta.read_text("utf-8", errors="ignore")
        except Exception:
            continue
        m = GUID_RE.search(text)
        if m:
            result.add(m.group(1).lower())
    return result


def safe_path(raw: str) -> Path:
    raw = raw.replace("\\", "/").strip()
    p = Path(raw)
    if p.is_absolute() or ".." in p.parts:
        raise RuntimeError(f"Unsafe unitypackage path: {raw}")
    if not raw.startswith("Assets/"):
        raise RuntimeError(f"Package entry outside Assets/: {raw}")
    return p


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--package", required=True)
    ap.add_argument("--project", required=True)
    args = ap.parse_args()

    package = Path(args.package)
    project = Path(args.project)
    if not package.is_file():
        raise SystemExit(f"Package not found: {package}")
    if not (project / "Assets").is_dir():
        raise SystemExit(f"Unity project Assets missing: {project}")

    existing = read_existing_guids(project)
    entries = {}

    with tarfile.open(package, "r:gz") as tf:
        members = {m.name: m for m in tf.getmembers() if m.isfile()}
        roots = sorted({name.split("/", 1)[0] for name in members if "/" in name})

        for root in roots:
            pn = members.get(f"{root}/pathname")
            asset = members.get(f"{root}/asset")
            meta = members.get(f"{root}/asset.meta")
            if pn is None or meta is None:
                continue

            path = safe_path(tf.extractfile(pn).read().decode("utf-8", "ignore"))
            meta_bytes = tf.extractfile(meta).read()
            meta_text = meta_bytes.decode("utf-8", "ignore")
            mg = GUID_RE.search(meta_text)
            guid = mg.group(1).lower() if mg else root.lower()

            entries[root] = {
                "path": path,
                "asset_member": asset,
                "meta_member": meta,
                "guid": guid,
            }

        collisions = {}
        for item in entries.values():
            path = project / item["path"]
            if item["guid"] in existing and not path.exists():
                collisions[item["guid"]] = deterministic_guid(item["guid"])

        print(f"[HF-WORLD] package entries={len(entries)} existing_guids={len(existing)} collisions={len(collisions)}")
        for old, new in sorted(collisions.items()):
            print(f"[HF-WORLD] remap GUID {old} -> {new}")

        written = 0
        for root, item in entries.items():
            rel = item["path"]
            dest = project / rel

            # Unity package folders have metadata but may not have an asset payload.
            if item["asset_member"] is None:
                dest.mkdir(parents=True, exist_ok=True)
            else:
                dest.parent.mkdir(parents=True, exist_ok=True)
                data = tf.extractfile(item["asset_member"]).read()
                if rel.suffix.lower() in TEXT_EXTS:
                    try:
                        text = data.decode("utf-8")
                        for old, new in collisions.items():
                            text = text.replace(f"guid: {old}", f"guid: {new}")
                        data = text.encode("utf-8")
                    except UnicodeDecodeError:
                        pass
                dest.write_bytes(data)

            meta_dest = Path(str(dest) + ".meta")
            meta_dest.parent.mkdir(parents=True, exist_ok=True)
            meta_text = tf.extractfile(item["meta_member"]).read().decode("utf-8", "ignore")
            for old, new in collisions.items():
                meta_text = meta_text.replace(f"guid: {old}", f"guid: {new}")
            meta_dest.write_text(meta_text, encoding="utf-8")
            written += 1

    required = project / "Assets/Stylized Medieval Kingdom URP/Scenes/Demo Stylized Medieval.unity"
    if not required.is_file():
        raise SystemExit(f"Expected demo scene missing after extraction: {required}")

    print(f"[HF-WORLD] extracted {written} assets")
    print(f"[HF-WORLD] CITY01_SOURCE={required}")


if __name__ == "__main__":
    main()
