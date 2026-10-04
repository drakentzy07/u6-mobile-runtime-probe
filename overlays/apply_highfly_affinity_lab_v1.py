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

def add_affinity_variants(text: str, cls: str, base: str) -> str:
    start = text.index(f"  {cls}: {{", text.index("export const CLASSES"))
    needle = f"      '{base}',"
    pos = text.index(needle, start)
    addition = "".join(
        f"\n      'hf_aff_{base}_{element}_01'," for element in ("fire", "frost", "lightning")
    )
    return text[:pos + len(needle)] + addition + text[pos + len(needle):]

for cls, base in [
    ("warrior", "whirlwind"),
    ("warrior", "thunder_clap"),
    ("warrior", "cleave"),
    ("rogue", "eviscerate"),
    ("rogue", "ambush"),
    ("rogue", "sinister_strike"),
    ("rogue", "rupture"),
    ("mage", "fireball"),
    ("mage", "arcane_missiles"),
    ("mage", "frostbolt"),
    ("mage", "frost_nova"),
]:
    classes = add_affinity_variants(classes, cls, base)

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

type HfAffinityElement = 'fire' | 'frost' | 'lightning';

function hfAffRound(value: number): number {
  return Math.max(0, Math.round(value));
}

function hfAffScaleEffect(effect: any, mult: number): any {
  if (!effect || typeof effect !== 'object') return effect;
  switch (effect.type) {
    case 'directDamage':
    case 'aoeDamage':
    case 'aoeRoot':
    case 'groundAoE':
    case 'chainDamage':
      return {
        ...effect,
        ...(typeof effect.min === 'number' ? { min: hfAffRound(effect.min * mult) } : {}),
        ...(typeof effect.max === 'number' ? { max: hfAffRound(effect.max * mult) } : {}),
      };
    case 'weaponStrike':
      return {
        ...effect,
        ...(typeof effect.bonus === 'number' ? { bonus: hfAffRound(effect.bonus * mult) } : {}),
        ...(typeof effect.weaponMult === 'number' ? { weaponMult: effect.weaponMult * mult } : {}),
      };
    case 'finisherDamage':
      return {
        ...effect,
        base: hfAffRound(effect.base * mult),
        perCombo: hfAffRound(effect.perCombo * mult),
        variance: hfAffRound(effect.variance * mult),
      };
    case 'dot':
      return {
        ...effect,
        ...(typeof effect.total === 'number' ? { total: hfAffRound(effect.total * mult) } : {}),
      };
    default:
      return { ...effect };
  }
}

function hfAffEstimateDamage(effects: readonly any[]): number {
  let total = 0;
  for (const effect of effects) {
    if (!effect || typeof effect !== 'object') continue;
    if (effect.type === 'directDamage' || effect.type === 'aoeDamage' || effect.type === 'aoeRoot') {
      total += ((effect.min ?? 0) + (effect.max ?? 0)) / 2;
    } else if (effect.type === 'finisherDamage') {
      total += (effect.base ?? 0) + (effect.perCombo ?? 0) * 5;
    } else if (effect.type === 'weaponStrike') {
      total += (effect.bonus ?? 0) + 18 * (effect.weaponMult ?? 1);
    } else if (effect.type === 'dot') {
      total += effect.total ?? 0;
    }
  }
  return Math.max(1, total);
}

function hfAffHasAoeDamage(effects: readonly any[]): boolean {
  return effects.some((effect) => effect?.type === 'aoeDamage');
}

function hfAffElementEffects(sourceId: string, effects: readonly any[], element: HfAffinityElement): any[] {
  if (sourceId === 'fireball') {
    const direct = effects.find((e) => e.type === 'directDamage');
    const dot = effects.find((e) => e.type === 'dot');
    const rest = effects.filter((e) => e.type !== 'directDamage' && e.type !== 'dot').map((e) => ({ ...e }));
    if (element === 'fire') {
      return [
        ...(direct ? [hfAffScaleEffect(direct, 0.75)] : []),
        ...(dot ? [{ ...dot, total: hfAffRound((dot.total ?? 0) * 2.5) }] : []),
        ...rest,
      ];
    }
    if (element === 'frost') {
      return [
        ...(direct ? [hfAffScaleEffect(direct, 0.8)] : []),
        ...(dot ? [{ ...dot, total: hfAffRound((dot.total ?? 0) * 0.25) }] : []),
        { type: 'slow', mult: 0.65, duration: 5 },
        ...rest,
      ];
    }
    const out: any[] = [...(direct ? [hfAffScaleEffect(direct, 0.88)] : []), ...rest];
    if (direct) {
      out.push({
        type: 'chainDamage',
        min: Math.max(1, hfAffRound((direct.min ?? 1) * 0.12)),
        max: Math.max(1, hfAffRound((direct.max ?? 1) * 0.12)),
        jumps: 2,
        falloff: 0.65,
        radius: 8,
      });
    }
    return out;
  }

  if (sourceId === 'frostbolt') {
    const direct = effects.find((e) => e.type === 'directDamage');
    const slow = effects.find((e) => e.type === 'slow');
    const rest = effects.filter((e) => e.type !== 'directDamage' && e.type !== 'slow').map((e) => ({ ...e }));
    if (element === 'fire') {
      const estimate = hfAffEstimateDamage(effects);
      return [
        ...(direct ? [hfAffScaleEffect(direct, 0.82)] : []),
        { type: 'dot', total: Math.max(1, hfAffRound(estimate * 0.18)), duration: 4, interval: 2 },
        ...rest,
      ];
    }
    if (element === 'frost') {
      return [
        ...(direct ? [hfAffScaleEffect(direct, 0.75)] : []),
        { type: 'slow', mult: 0.5, duration: Math.max(6, (slow?.duration ?? 5) + 2) },
        ...rest,
      ];
    }
    const out: any[] = [
      ...(direct ? [hfAffScaleEffect(direct, 0.9)] : []),
      { type: 'stun', duration: 0.15 },
      ...rest,
    ];
    if (direct) {
      out.push({
        type: 'chainDamage',
        min: Math.max(1, hfAffRound((direct.min ?? 1) * 0.1)),
        max: Math.max(1, hfAffRound((direct.max ?? 1) * 0.1)),
        jumps: 2,
        falloff: 0.65,
        radius: 8,
      });
    }
    return out;
  }

  if (sourceId === 'arcane_missiles') {
    const direct = effects.find((e) => e.type === 'directDamage');
    const rest = effects.filter((e) => e.type !== 'directDamage').map((e) => ({ ...e }));
    if (element === 'fire') {
      return [
        ...(direct ? [hfAffScaleEffect(direct, 0.82)] : []),
        { type: 'dot', total: Math.max(1, hfAffRound(hfAffEstimateDamage(effects) * 0.15)), duration: 4, interval: 2 },
        ...rest,
      ];
    }
    if (element === 'frost') {
      return [
        ...(direct ? [hfAffScaleEffect(direct, 0.8)] : []),
        { type: 'slow', mult: 0.7, duration: 3 },
        ...rest,
      ];
    }
    const out: any[] = [...(direct ? [hfAffScaleEffect(direct, 0.88)] : []), ...rest];
    if (direct) {
      out.push({
        type: 'chainDamage',
        min: Math.max(1, hfAffRound((direct.min ?? 1) * 0.12)),
        max: Math.max(1, hfAffRound((direct.max ?? 1) * 0.12)),
        jumps: 2,
        falloff: 0.7,
        radius: 8,
      });
    }
    return out;
  }

  if (sourceId === 'frost_nova') {
    const root = effects.find((e) => e.type === 'aoeRoot');
    const rest = effects.filter((e) => e.type !== 'aoeRoot').map((e) => ({ ...e }));
    if (!root) return effects.map((e) => ({ ...e }));
    if (element === 'fire') {
      const min = hfAffRound((root.min ?? 0) * 1.05);
      const max = hfAffRound((root.max ?? 0) * 1.05);
      return [
        { type: 'aoeDamage', min, max, radius: root.radius },
        { type: 'dot', total: Math.max(2, hfAffRound(((min + max) / 2) * 0.6)), duration: 4, interval: 2, perAoeTarget: true },
        ...rest,
      ];
    }
    if (element === 'frost') {
      return [{ ...hfAffScaleEffect(root, 0.72), duration: (root.duration ?? 8) + 2 }, ...rest];
    }
    return [{ ...hfAffScaleEffect(root, 0.95), duration: 0.35 }, ...rest];
  }

  if (sourceId === 'rupture') {
    const dot = effects.find((e) => e.type === 'dot');
    const rest = effects.filter((e) => e.type !== 'dot').map((e) => ({ ...e }));
    if (!dot) return effects.map((e) => ({ ...e }));
    if (element === 'fire') return [{ ...dot, interval: 1 }, ...rest];
    if (element === 'frost') {
      return [hfAffScaleEffect(dot, 0.78), { type: 'slow', mult: 0.7, duration: 6 }, ...rest];
    }
    return [{ ...hfAffScaleEffect(dot, 0.9), interval: 1 }, { type: 'stun', duration: 0.2 }, ...rest];
  }

  const aoe = hfAffHasAoeDamage(effects);
  if (element === 'fire') {
    const scaled = effects.map((effect) => hfAffScaleEffect(effect, 0.82));
    const burn = Math.max(1, hfAffRound(hfAffEstimateDamage(effects) * 0.18));
    return [
      ...scaled,
      { type: 'dot', total: burn, duration: 4, interval: 2, ...(aoe ? { perAoeTarget: true } : {}) },
    ];
  }

  if (element === 'frost') {
    if (aoe) {
      return effects.map((effect) =>
        effect.type === 'aoeDamage'
          ? {
              type: 'aoeRoot',
              min: hfAffRound((effect.min ?? 0) * 0.72),
              max: hfAffRound((effect.max ?? 0) * 0.72),
              radius: effect.radius,
              duration: 1.1,
              ...(effect.softCap ? { softCap: effect.softCap } : {}),
            }
          : { ...effect },
      );
    }
    return [
      ...effects.map((effect) => hfAffScaleEffect(effect, 0.76)),
      { type: 'slow', mult: 0.65, duration: 4 },
    ];
  }

  if (aoe) {
    return effects.map((effect) =>
      effect.type === 'aoeDamage'
        ? {
            type: 'aoeRoot',
            min: hfAffRound((effect.min ?? 0) * 0.9),
            max: hfAffRound((effect.max ?? 0) * 0.9),
            radius: effect.radius,
            duration: 0.25,
            ...(effect.softCap ? { softCap: effect.softCap } : {}),
          }
        : { ...effect },
    );
  }
  return [...effects.map((effect) => hfAffScaleEffect(effect, 0.9)), { type: 'stun', duration: 0.2 }];
}

function hfAffinityClone(
  sourceId: string,
  id: string,
  name: string,
  element: HfAffinityElement,
): void {
  const source = (ABILITIES as Record<string, any>)[sourceId];
  if (!source) throw new Error('HIGHFLY AFFINITY LAB missing donor ' + sourceId);
  const school = element === 'fire' ? 'fire' : element === 'frost' ? 'frost' : 'nature';
  const clone: any = {
    ...source,
    id,
    name,
    school,
    hiddenFromPlayer: false,
    effects: hfAffElementEffects(sourceId, source.effects ?? [], element),
  };
  if (source.ranks) {
    clone.ranks = source.ranks.map((rank: any) => ({
      ...rank,
      effects: hfAffElementEffects(sourceId, rank.effects ?? source.effects ?? [], element),
    }));
  }
  (ABILITIES as Record<string, any>)[id] = clone;
}

for (const [sourceId, label] of [
  ['whirlwind', 'Torbellino'],
  ['thunder_clap', 'Golpe de Trueno'],
  ['cleave', 'Barrido'],
  ['eviscerate', 'Remate'],
  ['ambush', 'Emboscada'],
  ['sinister_strike', 'Wicked Slash'],
  ['rupture', 'Ruptura'],
  ['fireball', 'Cinderbolt'],
  ['arcane_missiles', 'Dardos Etéreos'],
  ['frostbolt', 'Rimelance'],
  ['frost_nova', 'Nova'],
] as const) {
  hfAffinityClone(sourceId, 'hf_aff_' + sourceId + '_fire_01', label + ' Ígneo', 'fire');
  hfAffinityClone(sourceId, 'hf_aff_' + sourceId + '_frost_01', label + ' Boreal', 'frost');
  hfAffinityClone(sourceId, 'hf_aff_' + sourceId + '_lightning_01', label + ' Fulminante', 'lightning');
}
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
