#!/usr/bin/env python3
import argparse, json, re
from pathlib import Path

PIN = "cecebab4da06287351b37e118c630c185717b81c"
CANDIDATES = ["heroic_leap", "ambush", "backstab", "eviscerate", "pummel"]

def read(root, rel):
    p = root / rel
    if not p.is_file():
        raise SystemExit(f"missing ClaudeCraft source: {rel}")
    return p.read_text(encoding="utf-8")

def parse_vfx(src):
    out = {}
    for line in src.splitlines():
        m = re.match(r"^\s{2}([A-Za-z0-9_]+):\s*(\{.*\}),?\s*(?://.*)?$", line)
        if not m:
            continue
        try:
            out[m.group(1)] = json.loads(m.group(2))
        except json.JSONDecodeError:
            pass
    return out

def balanced_blocks_after(src, marker):
    pos = 0
    while True:
        pos = src.find(marker, pos)
        if pos < 0:
            return
        start = src.find("{", pos)
        if start < 0:
            return
        depth = 0
        end = None
        for i in range(start, len(src)):
            c = src[i]
            if c == "{":
                depth += 1
            elif c == "}":
                depth -= 1
                if depth == 0:
                    end = i
                    break
        if end is None:
            return
        yield src[start + 1:end]
        pos = end + 1

def parse_animation_bindings(manifest):
    bindings = {}
    for body in balanced_blocks_after(manifest, "attackByAbility:"):
        for m in re.finditer(r"^\s*([A-Za-z0-9_]+):\s*['\"]([^'\"]+)['\"],?\s*(?://.*)?$", body, re.M):
            bindings[m.group(1)] = m.group(2)
    return bindings

def parse_fixed_sfx_keys(src):
    m = re.search(r"SFX_FIXED_CATALOG_KEYS\s*=\s*(\[[^\n]+\])", src)
    if not m:
        return []
    return json.loads(m.group(1))

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--source", required=True)
    ap.add_argument("--out", required=True)
    args = ap.parse_args()
    root = Path(args.source)

    vfx_src = read(root, "src/render/ability_vfx_full_specs.ts")
    registry = read(root, "src/render/ability_vfx_registry.ts")
    manifest = read(root, "src/render/characters/manifest.ts")
    sfx_manifest = read(root, "src/game/sfx_manifest.generated.ts")
    credits = read(root, "CREDITS.md")

    vfx = parse_vfx(vfx_src)
    bindings = parse_animation_bindings(manifest)
    bespoke_ids = sorted(set(re.findall(r"abilityId === '([^']+)'", registry)))
    effective_vfx_ids = sorted(set(vfx) | set(bespoke_ids))
    fixed_sfx = set(parse_fixed_sfx_keys(sfx_manifest))

    selected = []
    failures = []
    for skill_id in CANDIDATES:
        row = {
            "id": skill_id,
            "animationClip": bindings.get(skill_id),
            "vfx": vfx.get(skill_id),
            "hasVfx": skill_id in effective_vfx_ids,
            "hasAuthoredAnimationBinding": skill_id in bindings,
            "claudeSfxCueNamedExactly": skill_id in fixed_sfx,
            "audioImport": "REBUILD_OR_LICENSE_CLEAR",
        }
        if not row["hasVfx"]:
            failures.append(f"{skill_id}: missing VFX")
        if not row["hasAuthoredAnimationBinding"]:
            failures.append(f"{skill_id}: missing attackByAbility binding")
        selected.append(row)

    # Important legal gate: ClaudeCraft itself documents mixed audio licensing,
    # including CC BY-NC, project-only and rights-reserved material. RUN0A copies
    # cue semantics/timing only; it intentionally copies zero audio bytes.
    audio_guard = {
        "directAudioBytesImported": False,
        "policy": "COPY_TIMING_AND_CUE_SEMANTICS_ONLY",
        "creditsHasNonCommercialAudio": "CC BY-NC 4.0" in credits,
        "creditsHasRightsReservedAudio": "rights reserved" in credits.lower(),
    }

    report = {
        "schema": "highfly.claudecraft.bridge.run0a.v1",
        "source": {
            "repo": "levy-street/world-of-claudecraft",
            "commit": PIN,
            "version": "v0.43.3",
        },
        "inventory": {
            "baseVfxSpecCount": len(vfx),
            "bespokeRegistryIdCount": len(bespoke_ids),
            "effectiveVfxIdCount": len(effective_vfx_ids),
            "attackByAbilityBindingCount": len(bindings),
            "fixedSfxCueCount": len(fixed_sfx),
        },
        "selectedSkills": selected,
        "audioGuard": audio_guard,
        "failures": failures,
    }

    if len(vfx) < 287:
        failures.append(f"VFX regression: expected >=287, got {len(vfx)}")
    if len(bindings) < 140:
        failures.append(f"animation binding regression: expected >=140, got {len(bindings)}")
    if not audio_guard["creditsHasNonCommercialAudio"]:
        failures.append("audio legal guard could not detect documented BY-NC material")

    out = Path(args.out)
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")

    print(json.dumps(report["inventory"], indent=2))
    for row in selected:
        print(f"{row['id']}: anim={row['animationClip']} vfx={row['hasVfx']} sfxExact={row['claudeSfxCueNamedExactly']}")
    if failures:
        raise SystemExit("RUN0A FAIL: " + " | ".join(failures))
    print("RUN0A GREEN: metadata bridge contract validated")

if __name__ == "__main__":
    main()
