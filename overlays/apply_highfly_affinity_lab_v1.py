from pathlib import Path
import shutil

ROOT = Path(".")
HOST = ROOT.resolve().parent

def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")

def write(path: str, text: str) -> None:
    p = ROOT / path
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(text, encoding="utf-8")

def rep(path: str, old: str, new: str, count: int = 1) -> None:
    text = read(path)
    found = text.count(old)
    if found != count:
        raise SystemExit(f"{path}: expected {count} anchor(s), found {found}: {old[:180]!r}")
    write(path, text.replace(old, new, count))

# Dedicated AFFINITY LAB sources. The old 4-pair lab remains untouched/frozen.
for src, dst in [
    ("src/highfly/affinity_lab_loadouts.ts", "src/highfly/affinity_lab_loadouts.ts"),
    ("src/highfly/affinity_lab_runtime.ts", "src/highfly/affinity_lab_runtime.ts"),
    ("src/styles/hf_affinity_lab.css", "src/styles/hf_affinity_lab.css"),
    ("tests/highfly_affinity_lab_v1.test.ts", "tests/highfly_affinity_lab_v1.test.ts"),
]:
    target = ROOT / dst
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(HOST / src, target)

# Lab-only fast boot: keep all nine native Claude classes and their real specs/resources.
rep(
    "src/main.ts",
    "  void startGame(sim, sim, null, `offline:${playerClass}:${name}`, true);",
    "  void startGame(sim, sim, null, `offline:${playerClass}:${name}`, !highflyAffinityLab);",
)

rep(
    "src/main.ts",
    """const diagnosticsAutoOffline =
  import.meta.env.DEV &&
  startupParams.get('diagnostics') === '1' &&
  startupParams.get('diagnosticsAuto') === '1';
if (editorPlaytest) {""",
    """const diagnosticsAutoOffline =
  import.meta.env.DEV &&
  startupParams.get('diagnostics') === '1' &&
  startupParams.get('diagnosticsAuto') === '1';
const highflyAffinityLab = startupParams.get('affinitylab') === '1';
const HIGHFLY_AFFINITY_LAB_CLASSES: readonly PlayerClass[] = [
  'warrior',
  'paladin',
  'hunter',
  'rogue',
  'priest',
  'shaman',
  'mage',
  'warlock',
  'druid',
];
const requestedAffinityLabClass = startupParams.get('labclass') as PlayerClass | null;
const highflyAffinityLabClass: PlayerClass =
  requestedAffinityLabClass && HIGHFLY_AFFINITY_LAB_CLASSES.includes(requestedAffinityLabClass)
    ? requestedAffinityLabClass
    : 'warrior';
if (editorPlaytest) {""",
)

rep(
    "src/main.ts",
    """} else if (diagnosticsAutoOffline) {
  startSitePresence('home');
  void startOffline('warrior', 'Diagnostics', 0);
} else {
  startSitePresence('home');
  wireStartScreens();
  initHomepageMusic();
}""",
    """} else if (highflyAffinityLab) {
  startSitePresence('home');
  void startOffline(highflyAffinityLabClass, 'HIGHFLY Affinity Lab', 0);
} else if (diagnosticsAutoOffline) {
  startSitePresence('home');
  void startOffline('warrior', 'Diagnostics', 0);
} else {
  startSitePresence('home');
  wireStartScreens();
  initHomepageMusic();
}""",
)

# Reachability: variants remain Warrior skills; no cross-class clone and no spec mutation.
classes = read("src/sim/content/classes.ts")
anchor = "      'heroic_leap',"
if classes.count(anchor) != 1:
    raise SystemExit(f"classes.ts heroic_leap roster anchor expected 1, found {classes.count(anchor)}")
classes = classes.replace(
    anchor,
    anchor
    + "\n      'hf_aff_heroic_leap_fire_01',"
    + "\n      'hf_aff_heroic_leap_frost_01',"
    + "\n      'hf_aff_heroic_leap_lightning_01',",
    1,
)

classes += r"""
// HIGHFLY AFFINITY LAB v1 — first real BASE vs element slice.
// These remain Warrior abilities and reuse the canonical repositionToAim pipeline.
function hfAffinityLeap(
  id: string,
  name: string,
  school: 'fire' | 'frost' | 'nature',
  landingAoe: { min: number; max: number; radius: number },
): void {
  const source = (ABILITIES as Record<string, any>).heroic_leap;
  if (!source) throw new Error('HIGHFLY AFFINITY LAB missing heroic_leap donor');
  (ABILITIES as Record<string, any>)[id] = {
    ...source,
    id,
    name,
    class: 'warrior',
    school,
    hiddenFromPlayer: false,
    effects: [{ type: 'repositionToAim', landingAoe }],
  };
}

// Budget rule: element redistributes value; it is not free bonus damage.
hfAffinityLeap('hf_aff_heroic_leap_fire_01', 'Salto Heroico Ígneo', 'fire', {
  min: 20,
  max: 26,
  radius: 6,
});
hfAffinityLeap('hf_aff_heroic_leap_frost_01', 'Salto Heroico Boreal', 'frost', {
  min: 18,
  max: 24,
  radius: 6,
});
hfAffinityLeap('hf_aff_heroic_leap_lightning_01', 'Salto Heroico Fulminante', 'nature', {
  min: 21,
  max: 27,
  radius: 6,
});
"""
write("src/sim/content/classes.ts", classes)

# The canonical placement preview must sweep the same path for every affinity variant.
rep(
    "src/sim/combat/heroic_leap.ts",
    "  if (abilityId !== 'heroic_leap') return point;",
    "  if (abilityId !== 'heroic_leap' && !abilityId.startsWith('hf_aff_heroic_leap_')) return point;",
)

# Elemental rider remains SIM-authoritative and only runs after the canonical impact.
rep(
    "src/sim/combat/heroic_leap.ts",
    "    ctx.dealDamage(entity, target, damage, false, flight.school, flight.abilityName, 'hit');",
    """    ctx.dealDamage(entity, target, damage, false, flight.school, flight.abilityName, 'hit');
    highflyAffinityLeapRider(ctx, entity, target, flight.abilityId);""",
)

rep(
    "src/sim/combat/heroic_leap.ts",
    """/** Bloodmarch Ragegear 4pc (Warfare Season 2): landing Vaulting Charge""",
    r"""function highflyAffinityLeapRider(
  ctx: SimContext,
  source: Entity,
  target: Entity,
  abilityId: string,
): void {
  if (abilityId === 'hf_aff_heroic_leap_fire_01') {
    ctx.applyAura(target, {
      id: `hf_affinity_leap_fire_${source.id}`,
      name: 'Quemadura Ígnea',
      kind: 'dot',
      remaining: 4,
      duration: 4,
      value: 2,
      tickInterval: 2,
      tickTimer: 2,
      sourceId: source.id,
      school: 'fire',
    });
    return;
  }
  if (abilityId === 'hf_aff_heroic_leap_frost_01') {
    ctx.applyAura(target, {
      id: `hf_affinity_leap_frost_${source.id}`,
      name: 'Frío Boreal',
      kind: 'slow',
      remaining: 4,
      duration: 4,
      value: 0.65,
      sourceId: source.id,
      school: 'frost',
    });
    return;
  }
  if (abilityId === 'hf_aff_heroic_leap_lightning_01') {
    ctx.applyAura(target, {
      id: `hf_affinity_leap_lightning_${source.id}`,
      name: 'Shock Fulminante',
      kind: 'stun',
      remaining: 0.35,
      duration: 0.35,
      value: 0,
      sourceId: source.id,
      school: 'nature',
    });
  }
}

/** Bloodmarch Ragegear 4pc (Warfare Season 2): landing Vaulting Charge""",
)

# Runtime is lab-only and loaded after the normal app markup.
rep(
    "index.html",
    "</body>",
    """  <script type="module" src="/src/highfly/affinity_lab_runtime.ts"></script>
</body>""",
)

print("HIGHFLY_AFFINITY_LAB_V1=1")
