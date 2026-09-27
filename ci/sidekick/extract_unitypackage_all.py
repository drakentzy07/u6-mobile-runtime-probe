#!/usr/bin/env python3
"""Reconstruct every Assets/* entry from a Unity .unitypackage."""
from __future__ import annotations
import argparse, shutil, tarfile, tempfile
from pathlib import Path

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--package",required=True,type=Path)
    ap.add_argument("--project",required=True,type=Path)
    args=ap.parse_args()
    if not args.package.is_file(): raise SystemExit("missing package")
    if not (args.project/"Assets").is_dir(): raise SystemExit("not a Unity project")

    copied=0
    with tempfile.TemporaryDirectory(prefix="unitypackage_") as td:
        root=Path(td)
        with tarfile.open(args.package,"r:*") as tf:
            tf.extractall(root)
        for pathname in root.rglob("pathname"):
            logical=pathname.read_text(encoding="utf-8",errors="ignore").strip()
            if not logical.startswith("Assets/"): continue
            asset_dir=pathname.parent
            asset=asset_dir/"asset"
            meta=asset_dir/"asset.meta"
            if not asset.is_file(): continue
            dst=args.project/logical
            dst.parent.mkdir(parents=True,exist_ok=True)
            shutil.copy2(asset,dst)
            if meta.is_file(): shutil.copy2(meta,Path(str(dst)+".meta"))
            copied+=1
    if copied<2: raise SystemExit(f"unitypackage unexpectedly small: {copied}")
    print(f"UNITYPACKAGE_EXTRACT_OK=1")
    print(f"UNITYPACKAGE_ASSETS={copied}")

if __name__=="__main__":
    main()
