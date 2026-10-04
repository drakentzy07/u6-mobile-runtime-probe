from pathlib import Path

ROOT = Path('.')

def read(path: str) -> str:
    return (ROOT / path).read_text(encoding='utf-8')

def write(path: str, text: str) -> None:
    p = ROOT / path
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(text, encoding='utf-8')

def rep(path: str, old: str, new: str, count: int = 1) -> None:
    text = read(path)
    found = text.count(old)
    if found != count:
        raise SystemExit(f'{path}: expected {count} anchor(s), found {found}: {old[:160]!r}')
    write(path, text.replace(old, new, count))

# Q0 is renderer-only. Never touch SIM, skills, combat, movement or targeting.
manifest = 'src/render/characters/manifest.ts'
rep(
    manifest,
    "import { NPC_PROP_SET_IDS, type NpcPropSet } from './npc_looks';",
    "import { NPC_PROP_SET_IDS, type NpcPropSet } from './npc_looks';\n"
    "import { highflyCharacterQ0VisualKey } from '../../highfly/character_q0_visual';",
)

dispatch_anchor = """// ---------------------------------------------------------------------------
// Dispatch: entity -> visual key (mirrors the old buildRigFor selection:
// e.kind + e.templateId + MOBS[id].family)
// ---------------------------------------------------------------------------
"""
q0_defs = """// ---------------------------------------------------------------------------
// HIGHFLY CHARACTER Q0 — renderer-only A/B/C body experiment.
// Every Q0 body is locally skinned onto the canonical KayKit Rig_Medium.
// Preserve each class's clips/anim donors/weapon configuration; only base mesh URL changes.
// ---------------------------------------------------------------------------
for (const cls of ALL_CLASSES) {
  const base = VISUALS[`player_${cls}`];
  if (!base) continue;
  for (const [suffix, url] of [
    ['qmale', `${PLAYERS}/q0/hf_q0_male_rigmedium.glb`],
    ['qfemale', `${PLAYERS}/q0/hf_q0_female_rigmedium.glb`],
  ] as const) {
    VISUALS[`player_${cls}_${suffix}`] = {
      ...base,
      url,
      // The Q0 body carries the canonical knight Rig_Medium clips; the original
      // class model remains an animation donor so Mage/Rogue/etc keep their exact
      // vocabulary and bespoke class clips on the SAME joint names.
      animUrls: [base.url, ...(base.animUrls ?? [])],
    };
  }
}

"""
if dispatch_anchor not in read(manifest):
    raise SystemExit('manifest dispatch anchor missing')
rep(manifest, dispatch_anchor, q0_defs + dispatch_anchor)

rep(
    manifest,
    """  if (e.kind === 'player') {
    if (isMechWearer(e)) return 'player_mech';
    return VISUALS[`player_${e.templateId}`] ? `player_${e.templateId}` : 'player_warrior';
  }""",
    """  if (e.kind === 'player') {
    if (isMechWearer(e)) return 'player_mech';
    const baseKey = VISUALS[`player_${e.templateId}`] ? `player_${e.templateId}` : 'player_warrior';
    return highflyCharacterQ0VisualKey(baseKey);
  }""",
)


# Q0 must override the app-level modular body ONLY while Q-MALE/Q-FEMALE is selected.
# Otherwise createCharacterVisual() would resolve modularKeyFor(e) before visualKeyFor(e),
# making the internal visualKey change while the visible body remained Claude.
index = 'src/render/characters/index.ts'
rep(
    index,
    "import { type Entity, isMechWearer, type PlayerClass } from '../../sim/types';",
    "import { type Entity, isMechWearer, type PlayerClass } from '../../sim/types';\n"
    "import { highflyCharacterQ0Body } from '../../highfly/character_q0_visual';",
)

rep(
    index,
    """  const look = formKey || isMechWearer(e) ? null : (modularLookProvider?.(e) ?? null);""",
    """  const q0VisualReplacement =
    !formKey && e.kind === 'player' && highflyCharacterQ0Body() !== 'claude';
  const look =
    formKey || isMechWearer(e) || q0VisualReplacement
      ? null
      : (modularLookProvider?.(e) ?? null);""",
)

print('HIGHFLY_CHARACTER_Q0=1')
