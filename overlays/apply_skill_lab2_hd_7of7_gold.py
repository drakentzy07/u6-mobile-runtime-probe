from pathlib import Path

ROOT=Path(".")

def read(path:str)->str:
    return (ROOT/path).read_text(encoding="utf-8")

def write(path:str,text:str)->None:
    p=ROOT/path
    p.parent.mkdir(parents=True,exist_ok=True)
    p.write_text(text,encoding="utf-8")

def rep(path:str,old:str,new:str,count:int=1)->None:
    text=read(path)
    n=text.count(old)
    if n!=count:
        raise SystemExit(f"{path}: expected {count} anchor(s), found {n}: {old[:180]!r}")
    write(path,text.replace(old,new))

# ---------------------------------------------------------------------------
# HIGHFLY HUNTER + DRUID 7/7 GOLD
# Reuse Claude authority. Main remains Hunter/Focus/body/weapon.
# Heritage Druid mechanics are translated into Hunter-owned hidden endpoints.
# ---------------------------------------------------------------------------

MAIN_IDS = [
    "hf_hd_frenzied_salvo_01","hf_hd_predator_rain_01",
    "hf_hd_arrow_storm_01","hf_hd_hunters_judgment_01",
    "hf_hd_wild_pack_01","hf_hd_kings_hunt_01",
]
HERITAGE_IDS = [
    "hf_hd_moonseed_01","hf_hd_crescent_seed_01","hf_hd_lunar_wave_01",
    "hf_hd_lunar_tempest_01","hf_hd_lunar_eclipse_01","hf_hd_eternal_night_01",
    "hf_hd_galeheart_01","hf_hd_wild_hurricane_01","hf_hd_natures_wrath_01",
]
ALL_NEW_IDS = MAIN_IDS + HERITAGE_IDS

rep(
    "src/sim/content/classes.ts",
    """      'hf_hd_boreal_domain_01',
      'tame_beast',""",
    """      'hf_hd_boreal_domain_01',
      'hf_hd_frenzied_salvo_01',
      'hf_hd_predator_rain_01',
      'hf_hd_arrow_storm_01',
      'hf_hd_hunters_judgment_01',
      'hf_hd_wild_pack_01',
      'hf_hd_kings_hunt_01',
      'hf_hd_moonseed_01',
      'hf_hd_crescent_seed_01',
      'hf_hd_lunar_wave_01',
      'hf_hd_lunar_tempest_01',
      'hf_hd_lunar_eclipse_01',
      'hf_hd_eternal_night_01',
      'hf_hd_galeheart_01',
      'hf_hd_wild_hurricane_01',
      'hf_hd_natures_wrath_01',
      'tame_beast',""",
)

ABILITY_DEFS = r"""
  hf_hd_frenzied_salvo_01: {
    id: 'hf_hd_frenzied_salvo_01',
    name: 'Salva Frenética',
    class: 'hunter',
    specs: ['marksmanship'],
    hiddenFromPlayer: true,
    learnLevel: 14,
    cost: 0,
    castTime: 0,
    castWhileMoving: true,
    channel: { duration: 2.4, ticks: 6 },
    cooldown: 12,
    range: 35,
    minRange: 8,
    school: 'physical',
    projectile: true,
    scalesWith: 'ranged',
    requiresTarget: true,
    effects: [{ type: 'directDamage', min: 19, max: 26 }],
    description:
      'EVO de Disparo Frenético. Conserva seis pulsos reales, movimiento durante el canal y Coldsight Read; mejora la lectura de la salva sin añadir impactos de simulación.',
  },
  hf_hd_predator_rain_01: {
    id: 'hf_hd_predator_rain_01',
    name: 'Lluvia del Depredador',
    class: 'hunter',
    specs: ['marksmanship'],
    hiddenFromPlayer: true,
    learnLevel: 14,
    cost: 0,
    castTime: 0,
    castWhileMoving: true,
    channel: { duration: 2.4, ticks: 6 },
    cooldown: 12,
    range: 35,
    minRange: 8,
    school: 'physical',
    projectile: true,
    scalesWith: 'ranged',
    requiresTarget: true,
    effects: [{ type: 'directDamage', min: 19, max: 26 }],
    description:
      'MUT PRIME Hunter+Druid. Mantiene los seis disparos autoritativos y Coldsight; bestias espirituales y rastros lunares acompañan visualmente la descarga.',
  },
  hf_hd_arrow_storm_01: {
    id: 'hf_hd_arrow_storm_01',
    name: 'Tormenta de Flechas',
    class: 'hunter',
    hiddenFromPlayer: true,
    learnLevel: 11,
    cost: 60,
    castTime: 0,
    cooldown: 8,
    range: 35,
    school: 'physical',
    scalesWith: 'ranged',
    requiresTarget: false,
    targetMode: 'position',
    channel: { duration: 3, ticks: 6 },
    effects: [{ type: 'aoeDamage', min: 12, max: 16, radius: 8 }],
    description:
      'EVO de Lluvia de Flechas. Conserva el círculo, los seis ticks y el escalado ranged; aumenta densidad y lectura del campo.',
  },
  hf_hd_hunters_judgment_01: {
    id: 'hf_hd_hunters_judgment_01',
    name: 'Juicio del Cazador',
    class: 'hunter',
    hiddenFromPlayer: true,
    learnLevel: 11,
    cost: 60,
    castTime: 0,
    cooldown: 8,
    range: 35,
    school: 'physical',
    scalesWith: 'ranged',
    requiresTarget: false,
    targetMode: 'position',
    channel: { duration: 3, ticks: 6 },
    effects: [{ type: 'aoeDamage', min: 12, max: 16, radius: 8 }],
    description:
      'MUT PRIME Hunter+Druid. Viento, raíces y una silueta lunar cierran el mismo Volley Hunter; no existe daño por cada flecha decorativa.',
  },
  hf_hd_wild_pack_01: {
    id: 'hf_hd_wild_pack_01',
    name: 'Manada Salvaje',
    class: 'hunter',
    specs: ['beast_mastery'],
    hiddenFromPlayer: true,
    learnLevel: 17,
    cost: 0,
    castTime: 0,
    cooldown: 90,
    range: 35,
    school: 'physical',
    requiresTarget: true,
    effects: [{
      type: 'hunterStampede',
      beasts: 3,
      duration: 12,
      attackInterval: 2,
      min: 18,
      max: 24,
      rangedPowerCoeff: 0.08,
    }],
    description:
      'EVO de Estampida. Conserva tres guardians, snapshot de Ferocity, doce segundos y ataques cada dos segundos.',
  },
  hf_hd_kings_hunt_01: {
    id: 'hf_hd_kings_hunt_01',
    name: 'Cacería del Rey',
    class: 'hunter',
    specs: ['beast_mastery'],
    hiddenFromPlayer: true,
    learnLevel: 17,
    cost: 0,
    castTime: 0,
    cooldown: 90,
    range: 35,
    school: 'physical',
    requiresTarget: true,
    effects: [{
      type: 'hunterStampede',
      beasts: 3,
      duration: 12,
      attackInterval: 2,
      min: 18,
      max: 24,
      rangedPowerCoeff: 0.08,
    }],
    description:
      'MUT PRIME Hunter+Druid. La misma Stampede Hunter recibe una cacería espiritual Druid visual; guardians, cadence y ownership siguen siendo Hunter.',
  },

  // Heritage Druid translated to Hunter authority: Focus, Hunter body/weapon,
  // no permanent Moonwing/Cat/Bruin form and no secondary Mana bar.
  hf_hd_moonseed_01: {
    id: 'hf_hd_moonseed_01',
    name: 'Semilla Lunar',
    class: 'hunter',
    hiddenFromPlayer: true,
    learnLevel: 10,
    cost: 35,
    castTime: 0,
    cooldown: 8,
    range: 30,
    school: 'arcane',
    requiresTarget: true,
    actionReplacement: {
      abilityId: 'hf_hd_lunar_wave_01',
      auraKind: 'moontide',
      minStacks: 3,
    },
    effects: [
      { type: 'directDamage', min: 20, max: 26 },
      { type: 'extendDot', dot: 'moonfire', seconds: 6, maxBonus: 6 },
    ],
    description:
      'Herencia Druid adaptada al arco Hunter. Golpea, construye Moontide interno y extiende la Tormenta Lunar propia sin cambiar de forma ni de recurso.',
  },
  hf_hd_crescent_seed_01: {
    id: 'hf_hd_crescent_seed_01',
    name: 'Semilla Creciente',
    class: 'hunter',
    hiddenFromPlayer: true,
    learnLevel: 10,
    cost: 35,
    castTime: 0,
    cooldown: 8,
    range: 30,
    school: 'arcane',
    requiresTarget: true,
    actionReplacement: {
      abilityId: 'hf_hd_lunar_wave_01',
      auraKind: 'moontide',
      minStacks: 3,
    },
    effects: [
      { type: 'directDamage', min: 20, max: 26 },
      { type: 'extendDot', dot: 'moonfire', seconds: 6, maxBonus: 6 },
    ],
    description:
      'EVO de Semilla Lunar. Conserva builder, extensión y gate de tres Moontide; el crecimiento es de presentación y lectura.',
  },
  hf_hd_lunar_wave_01: {
    id: 'hf_hd_lunar_wave_01',
    name: 'Oleada Lunar',
    class: 'hunter',
    hiddenFromPlayer: true,
    learnLevel: 10,
    cost: 80,
    castTime: 0,
    cooldown: 0,
    range: 30,
    school: 'arcane',
    requiresTarget: true,
    effects: [{ type: 'directDamage', min: 136, max: 162, spellPowerCoeff: 0.32 }],
    description:
      'MUTACIÓN CONTEXTUAL. A tres Moontide reemplaza temporalmente la Semilla, consume el banco y vuelve al builder; todo bajo Focus Hunter.',
  },
  hf_hd_lunar_tempest_01: {
    id: 'hf_hd_lunar_tempest_01',
    name: 'Tormenta Lunar',
    class: 'hunter',
    hiddenFromPlayer: true,
    learnLevel: 2,
    cost: 25,
    castTime: 0,
    cooldown: 0,
    range: 30,
    school: 'arcane',
    requiresTarget: true,
    effects: [
      { type: 'directDamage', min: 9, max: 12 },
      { type: 'dot', total: 12, duration: 9, interval: 3, auraId: 'moonfire' },
    ],
    ranks: [
      { rank: 2, level: 10, cost: 40, effects: [
        { type: 'directDamage', min: 17, max: 21 },
        { type: 'dot', total: 24, duration: 12, interval: 3, auraId: 'moonfire' },
      ] },
      { rank: 3, level: 16, cost: 60, effects: [
        { type: 'directDamage', min: 50, max: 60 },
        { type: 'dot', total: 70, duration: 12, interval: 3, auraId: 'moonfire' },
      ] },
    ],
    description:
      'Lunar Tempest Druid traducida a Hunter. Directo + DoT conservados; el DoT usa el id canónico moonfire para que Semilla Lunar pueda extenderlo.',
  },
  hf_hd_lunar_eclipse_01: {
    id: 'hf_hd_lunar_eclipse_01',
    name: 'Eclipse Lunar',
    class: 'hunter',
    hiddenFromPlayer: true,
    learnLevel: 2,
    cost: 25,
    castTime: 0,
    cooldown: 0,
    range: 30,
    school: 'arcane',
    requiresTarget: true,
    effects: [
      { type: 'directDamage', min: 9, max: 12 },
      { type: 'dot', total: 12, duration: 9, interval: 3, auraId: 'moonfire' },
    ],
    ranks: [
      { rank: 2, level: 10, cost: 40, effects: [
        { type: 'directDamage', min: 17, max: 21 },
        { type: 'dot', total: 24, duration: 12, interval: 3, auraId: 'moonfire' },
      ] },
      { rank: 3, level: 16, cost: 60, effects: [
        { type: 'directDamage', min: 50, max: 60 },
        { type: 'dot', total: 70, duration: 12, interval: 3, auraId: 'moonfire' },
      ] },
    ],
    description:
      'EVO de Tormenta Lunar. Mantiene direct+DoT y extensión source-owned; la marca creciente evoluciona a eclipse.',
  },
  hf_hd_eternal_night_01: {
    id: 'hf_hd_eternal_night_01',
    name: 'Noche Eterna',
    class: 'hunter',
    hiddenFromPlayer: true,
    learnLevel: 2,
    cost: 25,
    castTime: 0,
    cooldown: 0,
    range: 30,
    school: 'arcane',
    requiresTarget: true,
    effects: [
      { type: 'directDamage', min: 9, max: 12 },
      { type: 'dot', total: 12, duration: 9, interval: 3, auraId: 'moonfire' },
    ],
    ranks: [
      { rank: 2, level: 10, cost: 40, effects: [
        { type: 'directDamage', min: 17, max: 21 },
        { type: 'dot', total: 24, duration: 12, interval: 3, auraId: 'moonfire' },
      ] },
      { rank: 3, level: 16, cost: 60, effects: [
        { type: 'directDamage', min: 50, max: 60 },
        { type: 'dot', total: 70, duration: 12, interval: 3, auraId: 'moonfire' },
      ] },
    ],
    description:
      'MUT PRIME Hunter+Druid. Fases lunares y rayo final son presentación; la autoridad sigue siendo el direct+DoT extendible original.',
  },
  hf_hd_galeheart_01: {
    id: 'hf_hd_galeheart_01',
    name: 'Corazón del Vendaval',
    class: 'hunter',
    hiddenFromPlayer: true,
    learnLevel: 18,
    cost: 90,
    castTime: 0,
    cooldown: 12,
    range: 30,
    school: 'nature',
    requiresTarget: false,
    targetMode: 'position',
    channel: { duration: 6, ticks: 6 },
    effects: [{ type: 'aoeDamage', min: 12, max: 16, radius: 8 }],
    description:
      'Galeheart Druid traducido a Hunter. Flecha ancla la posición; conserva seis pulsos en seis segundos sin auto-shift.',
  },
  hf_hd_wild_hurricane_01: {
    id: 'hf_hd_wild_hurricane_01',
    name: 'Huracán Salvaje',
    class: 'hunter',
    hiddenFromPlayer: true,
    learnLevel: 18,
    cost: 90,
    castTime: 0,
    cooldown: 12,
    range: 30,
    school: 'nature',
    requiresTarget: false,
    targetMode: 'position',
    channel: { duration: 6, ticks: 6 },
    effects: [{ type: 'aoeDamage', min: 12, max: 16, radius: 8 }],
    description:
      'EVO de Corazón del Vendaval. Conserva channel, seis pulsos, radio y Focus; viento y debris ganan lectura Hunter.',
  },
  hf_hd_natures_wrath_01: {
    id: 'hf_hd_natures_wrath_01',
    name: 'Ira de la Naturaleza',
    class: 'hunter',
    hiddenFromPlayer: true,
    learnLevel: 18,
    cost: 90,
    castTime: 0,
    cooldown: 12,
    range: 30,
    school: 'nature',
    requiresTarget: false,
    targetMode: 'position',
    channel: { duration: 6, ticks: 6 },
    effects: [{ type: 'aoeDamage', min: 12, max: 16, radius: 8 }],
    description:
      'MUT PRIME Hunter+Druid. Siluetas de bestias, raíces y colapso visual final no crean ticks extra; la autoridad sigue siendo seis pulsos.',
  },

"""
rep(
    "src/sim/content/classes.ts",
    """  // ====================== PRIEST ======================""",
    ABILITY_DEFS + """  // ====================== PRIEST ======================""",
)

# Fevered Draw aliases share the exact native Coldsight state machine.
rep(
    "src/sim/combat/hunter_coldsight_read.ts",
    """export const FEVERED_DRAW_ABILITY_ID = 'rapid_fire';
export const FEVERED_DRAW_PULSE_COUNT = 6;""",
    """export const FEVERED_DRAW_ABILITY_ID = 'rapid_fire';
export const FEVERED_DRAW_ABILITY_IDS: ReadonlySet<string> = new Set([
  FEVERED_DRAW_ABILITY_ID,
  'hf_hd_frenzied_salvo_01',
  'hf_hd_predator_rain_01',
]);
export const FEVERED_DRAW_PULSE_COUNT = 6;""",
)
rep(
    "src/sim/combat/hunter_coldsight_read.ts",
    """if (abilityId !== FEVERED_DRAW_ABILITY_ID) return;""",
    """if (!FEVERED_DRAW_ABILITY_IDS.has(abilityId)) return;""",
    count=2,
)
rep(
    "src/sim/combat/hunter_coldsight_read.ts",
    """if (completedAbilityId !== FEVERED_DRAW_ABILITY_ID) return;""",
    """if (!completedAbilityId || !FEVERED_DRAW_ABILITY_IDS.has(completedAbilityId)) return;""",
)

# Stampede aliases share Packlord cooldown-reset / glow authority.
rep(
    "src/sim/combat/hunter_packlord.ts",
    """export const STAMPEDE_BAD_LUCK_CAP = 5;""",
    """export const STAMPEDE_BAD_LUCK_CAP = 5;
export const STAMPEDE_ABILITY_IDS: ReadonlySet<string> = new Set([
  'stampede',
  'hf_hd_wild_pack_01',
  'hf_hd_kings_hunt_01',
]);""",
)
rep(
    "src/sim/combat/hunter_packlord.ts",
    """  hunter.cooldowns.delete('stampede');""",
    """  for (const abilityId of STAMPEDE_ABILITY_IDS) hunter.cooldowns.delete(abilityId);""",
)
rep(
    "src/sim/combat/hunter_packlord.ts",
    """  if ((hunter.cooldowns.get('stampede') ?? 0) <= 0) return;""",
    """  if (![...STAMPEDE_ABILITY_IDS].some((abilityId) => (hunter.cooldowns.get(abilityId) ?? 0) > 0))
    return;""",
)
rep(
    "src/sim/combat/hunter_packlord.ts",
    """  return abilityId === 'stampede' && auras.some((aura) => aura.id === STAMPEDE_READY_AURA_ID);""",
    """  return STAMPEDE_ABILITY_IDS.has(abilityId) && auras.some((aura) => aura.id === STAMPEDE_READY_AURA_ID);""",
)

# Reuse the real Moontide aura/action-replacement engine for Hunter Heritage,
# but do not grant Druid forms, specs or a Mana bar.
rep(
    "src/sim/combat/druid_engines.ts",
    """const MOONTIDE_BUILDER_IDS = new Set(['wrath', 'starfire', 'moonseed']);""",
    """const MOONTIDE_BUILDER_IDS = new Set(['wrath', 'starfire', 'moonseed']);
export const HIGHFLY_HUNTER_MOONTIDE_BUILDER_IDS: ReadonlySet<string> = new Set([
  'hf_hd_moonseed_01',
  'hf_hd_crescent_seed_01',
]);
export const HIGHFLY_HUNTER_MOONTIDE_PAYOFF_IDS: ReadonlySet<string> = new Set([
  'hf_hd_lunar_wave_01',
]);""",
)
rep(
    "src/sim/combat/druid_engines.ts",
    """  const meta = ctx.players.get(player.id);
  if (meta?.cls !== 'druid') return;

  if (FORM_ABILITY_IDS.has(abilityId)) {""",
    """  const meta = ctx.players.get(player.id);
  if (meta?.cls === 'hunter') {
    if (HIGHFLY_HUNTER_MOONTIDE_BUILDER_IDS.has(abilityId)) {
      addStage(ctx, player, MOONTIDE_ID, 'Moontide', 'moontide', MOONTIDE_STAGES);
      return;
    }
    if (HIGHFLY_HUNTER_MOONTIDE_PAYOFF_IDS.has(abilityId)) {
      removeOwnedAura(ctx, player, MOONTIDE_ID);
      seedNextEngineBank(ctx, player, MOONTIDE_ID, 'Moontide', 'moontide');
      return;
    }
    return;
  }
  if (meta?.cls !== 'druid') return;

  if (FORM_ABILITY_IDS.has(abilityId)) {""",
)

# Presentation: Hunter body/weapon always wins; Heritage is visual DNA only.
PRESENTATION = r"""
  hf_hd_frenzied_salvo_01: {
    animationRoute: 'rapid_fire',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_hd_frenzied_salvo_01',
    sfxRoute: 'rapid_fire',
  },
  hf_hd_predator_rain_01: {
    animationRoute: 'rapid_fire',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_hd_predator_rain_01',
    sfxRoute: 'rapid_fire',
  },
  hf_hd_arrow_storm_01: {
    animationRoute: 'volley',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_hd_arrow_storm_01',
    sfxRoute: 'volley',
  },
  hf_hd_hunters_judgment_01: {
    animationRoute: 'volley',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_hd_hunters_judgment_01',
    sfxRoute: 'volley',
  },
  hf_hd_wild_pack_01: {
    animationRoute: 'stampede',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_hd_wild_pack_01',
    sfxRoute: 'stampede',
  },
  hf_hd_kings_hunt_01: {
    animationRoute: 'stampede',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_hd_kings_hunt_01',
    sfxRoute: 'stampede',
  },
  hf_hd_moonseed_01: {
    animationRoute: 'arcane_shot',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_hd_moonseed_01',
    sfxRoute: 'moonseed',
  },
  hf_hd_crescent_seed_01: {
    animationRoute: 'arcane_shot',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_hd_crescent_seed_01',
    sfxRoute: 'moonseed',
  },
  hf_hd_lunar_wave_01: {
    animationRoute: 'arcane_shot',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_hd_lunar_wave_01',
    sfxRoute: 'moonlash',
  },
  hf_hd_lunar_tempest_01: {
    animationRoute: 'arcane_shot',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_hd_lunar_tempest_01',
    sfxRoute: 'moonfire',
  },
  hf_hd_lunar_eclipse_01: {
    animationRoute: 'arcane_shot',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_hd_lunar_eclipse_01',
    sfxRoute: 'moonfire',
  },
  hf_hd_eternal_night_01: {
    animationRoute: 'arcane_shot',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_hd_eternal_night_01',
    sfxRoute: 'moonfire',
  },
  hf_hd_galeheart_01: {
    animationRoute: 'volley',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_hd_galeheart_01',
    sfxRoute: 'hurricane',
  },
  hf_hd_wild_hurricane_01: {
    animationRoute: 'volley',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_hd_wild_hurricane_01',
    sfxRoute: 'hurricane',
  },
  hf_hd_natures_wrath_01: {
    animationRoute: 'volley',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_hd_natures_wrath_01',
    sfxRoute: 'hurricane',
  },
"""
rep(
    "src/highfly/presentation_adapter.ts",
    """  hf_hd_boreal_domain_01: {
    animationRoute: 'frostjaw_trap',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_hd_boreal_domain_01',
    sfxRoute: 'frost_nova',
  },""",
    """  hf_hd_boreal_domain_01: {
    animationRoute: 'frostjaw_trap',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_hd_boreal_domain_01',
    sfxRoute: 'frost_nova',
  },
""" + PRESENTATION,
)

write(
    "src/highfly/skill_lab2_hd_pack7_vfx.ts",
    r"""import type { AbilityVfxFullSpec, AbilityVfxSpec } from '../render/ability_vfx_core';

const S = (c:string,p:string,pw:number,a:string,sp=46,rg=1.5):AbilityVfxSpec =>
  ({c,p,pw,sp,rg,vr:1,li:1.3,lg:2.2,wu:0.2,fin:1,a} as AbilityVfxSpec);

export const HF_HD_PACK7_VFX_SPEC: Record<string, AbilityVfxSpec> = {
  hf_hd_frenzied_salvo_01:S('#d9f4d0','nature',1.35,'bolt',46,1.25),
  hf_hd_predator_rain_01:S('#b8e7ff','moon',1.65,'bolt',60,1.6),
  hf_hd_arrow_storm_01:S('#cdecc1','nature',1.4,'nova',52,2.1),
  hf_hd_hunters_judgment_01:S('#d7ccff','moon',1.75,'nova',66,2.5),
  hf_hd_wild_pack_01:S('#a9db8c','nature',1.5,'summon',54,1.9),
  hf_hd_kings_hunt_01:S('#c6b5ff','moon',1.9,'summon',72,2.4),
  hf_hd_moonseed_01:S('#b9a1ff','moon',1.3,'bolt',38,1.15),
  hf_hd_crescent_seed_01:S('#cdbaff','moon',1.5,'bolt',48,1.35),
  hf_hd_lunar_wave_01:S('#d9c8ff','moon',1.9,'burst',72,2.6),
  hf_hd_lunar_tempest_01:S('#ab9cff','moon',1.3,'dot',34,1.1),
  hf_hd_lunar_eclipse_01:S('#9278e8','moon',1.55,'dot',46,1.35),
  hf_hd_eternal_night_01:S('#7358cf','moon',1.85,'dot',62,1.8),
  hf_hd_galeheart_01:S('#a9e0be','nature',1.4,'nova',50,2.2),
  hf_hd_wild_hurricane_01:S('#91d7aa','nature',1.65,'nova',62,2.5),
  hf_hd_natures_wrath_01:S('#c0e9b7','nature',1.95,'nova',78,2.8),
};

const F=(archetype:any,palette:string,power:number,motifs:any[],accent:string,finisher=false):AbilityVfxFullSpec=>({
  archetype,palette,power,windup:0.2,windupStyle:'weapon',motifs,motifAt:'target',
  impact:{ring:1.5,vRing:true,sparks:Math.round(32*power),flipbook:true,light:1.2*power},
  linger:1.5+power,rim:accent,tint:accent,accent,hot:0.18,screenFx:finisher,finisher
});

export const HF_HD_PACK7_VFX_FULL_SPEC: Record<string, AbilityVfxFullSpec> = {
  hf_hd_frenzied_salvo_01:{...F('bolt','nature',1.35,['vines'],'#d9f4d0'),bolt:{style:'arrow',speed:1.25,headScale:1.15,tracer:true,volley:2}},
  hf_hd_predator_rain_01:{...F('bolt','moon',1.7,['swarm','crescents'],'#c8baff',true),bolt:{style:'arrow',speed:1.3,headScale:1.2,tracer:true,volley:3}},
  hf_hd_arrow_storm_01:{...F('nova','nature',1.45,['fountain','vines'],'#b8e8ac'),nova:{radius:8}},
  hf_hd_hunters_judgment_01:{...F('nova','moon',1.8,['fountain','vines','crescents'],'#d8c8ff',true),nova:{radius:8}},
  hf_hd_wild_pack_01:{...F('summon','nature',1.55,['swarm'],'#a5d786'),spirit:null},
  hf_hd_kings_hunt_01:{...F('summon','moon',1.95,['swarm','crescents'],'#d5c4ff',true),spirit:null},
  hf_hd_moonseed_01:{...F('bolt','moon',1.3,['crescents'],'#c8b8ff'),bolt:{style:'arrow',speed:1.1,headScale:1.1,tracer:true}},
  hf_hd_crescent_seed_01:{...F('bolt','moon',1.5,['crescents','vines'],'#d9ccff'),bolt:{style:'arrow',speed:1.15,headScale:1.2,tracer:true}},
  hf_hd_lunar_wave_01:{...F('burst','moon',1.95,['crescents','vines'],'#efe4ff',true),burst:{style:'ground'},decal:'rune'},
  hf_hd_lunar_tempest_01:{...F('dot','moon',1.3,['crescents'],'#bcaaff'),dot:{drip:'fall'}},
  hf_hd_lunar_eclipse_01:{...F('dot','moon',1.6,['crescents','orbitals'],'#9f87ef'),dot:{drip:'fall'},decal:'rune'},
  hf_hd_eternal_night_01:{...F('dot','moon',1.9,['crescents','orbitals'],'#7f67d8',true),dot:{drip:'fall'},shaft:1.5,decal:'rune'},
  hf_hd_galeheart_01:{...F('nova','nature',1.45,['vines','fountain'],'#a8ddbd'),nova:{radius:8}},
  hf_hd_wild_hurricane_01:{...F('nova','nature',1.7,['vines','swarm'],'#92d5ad'),nova:{radius:8}},
  hf_hd_natures_wrath_01:{...F('nova','nature',2.0,['vines','swarm','crescents'],'#c7efb7',true),nova:{radius:8},decal:'rune'},
};
""",
)

rep(
    "src/render/ability_vfx_registry.ts",
    """import {
  HF_HD_FROSTJAW_VFX_FULL_SPEC,
  HF_HD_FROSTJAW_VFX_SPEC,
} from '../highfly/skill_lab2_hd_frostjaw_vfx';""",
    """import {
  HF_HD_FROSTJAW_VFX_FULL_SPEC,
  HF_HD_FROSTJAW_VFX_SPEC,
} from '../highfly/skill_lab2_hd_frostjaw_vfx';
import {
  HF_HD_PACK7_VFX_FULL_SPEC,
  HF_HD_PACK7_VFX_SPEC,
} from '../highfly/skill_lab2_hd_pack7_vfx';""",
)
rep(
    "src/render/ability_vfx_registry.ts",
    """  if (HF_HD_FROSTJAW_VFX_SPEC[abilityId]) return HF_HD_FROSTJAW_VFX_SPEC[abilityId];""",
    """  if (HF_HD_FROSTJAW_VFX_SPEC[abilityId]) return HF_HD_FROSTJAW_VFX_SPEC[abilityId];
  if (HF_HD_PACK7_VFX_SPEC[abilityId]) return HF_HD_PACK7_VFX_SPEC[abilityId];""",
)
rep(
    "src/render/ability_vfx_registry.ts",
    """  if (HF_HD_FROSTJAW_VFX_FULL_SPEC[abilityId]) return HF_HD_FROSTJAW_VFX_FULL_SPEC[abilityId];""",
    """  if (HF_HD_FROSTJAW_VFX_FULL_SPEC[abilityId]) return HF_HD_FROSTJAW_VFX_FULL_SPEC[abilityId];
  if (HF_HD_PACK7_VFX_FULL_SPEC[abilityId]) return HF_HD_PACK7_VFX_FULL_SPEC[abilityId];""",
)

write(
    "tests/highfly_skill_lab2_hd_7of7_gold.test.ts",
    r"""import { describe, expect, it } from 'vitest';
import { ABILITIES, CLASSES } from '../src/sim/data';
import { highflyPresentationRoute } from '../src/highfly/presentation_adapter';
import { abilityVfxFullSpec } from '../src/render/ability_vfx_registry';
import { FEVERED_DRAW_ABILITY_IDS } from '../src/sim/combat/hunter_coldsight_read';
import { STAMPEDE_ABILITY_IDS } from '../src/sim/combat/hunter_packlord';
import {
  HIGHFLY_HUNTER_MOONTIDE_BUILDER_IDS,
  HIGHFLY_HUNTER_MOONTIDE_PAYOFF_IDS,
} from '../src/sim/combat/druid_engines';

const FAMILIES = {
  frostjaw:['frostjaw_trap','hf_hunter_prison_01','hf_hd_boreal_domain_01'],
  fevered:['rapid_fire','hf_hd_frenzied_salvo_01','hf_hd_predator_rain_01'],
  volley:['volley','hf_hd_arrow_storm_01','hf_hd_hunters_judgment_01'],
  stampede:['stampede','hf_hd_wild_pack_01','hf_hd_kings_hunt_01'],
  moonseed:['hf_hd_moonseed_01','hf_hd_crescent_seed_01','hf_hd_lunar_wave_01'],
  lunar:['hf_hd_lunar_tempest_01','hf_hd_lunar_eclipse_01','hf_hd_eternal_night_01'],
  gale:['hf_hd_galeheart_01','hf_hd_wild_hurricane_01','hf_hd_natures_wrath_01'],
} as const;

describe('HIGHFLY Hunter+Druid 7/7 GOLD',()=>{
  it('registers exactly seven PRIME families on Hunter authority',()=>{
    const ids=Object.values(FAMILIES).flat();
    expect(ids).toHaveLength(21);
    expect(CLASSES.hunter.abilities).toEqual(expect.arrayContaining(ids as unknown as string[]));
    for(const id of ids.filter((id)=>id.startsWith('hf_'))){
      expect(ABILITIES[id],id).toBeTruthy();
      expect(ABILITIES[id]!.class,id).toBe('hunter');
    }
  });

  it('preserves Fevered Draw six-pulse Coldsight authority for EVO and MUT',()=>{
    for(const id of FAMILIES.fevered){
      const a=ABILITIES[id]!;
      expect(a.channel,id).toEqual({duration:2.4,ticks:6});
      expect(a.cooldown,id).toBe(12);
      expect(a.range,id).toBe(35);
      expect(a.castWhileMoving,id).toBe(true);
      expect(a.effects,id).toEqual([{type:'directDamage',min:19,max:26}]);
      expect(FEVERED_DRAW_ABILITY_IDS.has(id),id).toBe(true);
    }
  });

  it('preserves Volley six ticks, Focus cost and radius 8 for all stages',()=>{
    for(const id of FAMILIES.volley){
      const a=ABILITIES[id]!;
      expect(a.cost,id).toBe(60);
      expect(a.channel,id).toEqual({duration:3,ticks:6});
      expect(a.cooldown,id).toBe(8);
      expect(a.targetMode,id).toBe('position');
      expect(a.effects,id).toEqual([{type:'aoeDamage',min:12,max:16,radius:8}]);
    }
  });

  it('preserves Stampede 3 guardians / 12s / 2s cadence and reset aliases',()=>{
    for(const id of FAMILIES.stampede){
      const a=ABILITIES[id]!;
      expect(a.cooldown,id).toBe(90);
      expect(a.effects[0],id).toMatchObject({
        type:'hunterStampede',beasts:3,duration:12,attackInterval:2,min:18,max:24,rangedPowerCoeff:0.08,
      });
      expect(STAMPEDE_ABILITY_IDS.has(id),id).toBe(true);
    }
  });

  it('translates Moonseed to Hunter Focus while reusing the native Moontide replacement loop',()=>{
    for(const id of FAMILIES.moonseed.slice(0,2)){
      const a=ABILITIES[id]!;
      expect(a.class).toBe('hunter');
      expect(a.cost).toBe(35);
      expect(a.requiresAuraKind).toBeUndefined();
      expect(a.actionReplacement).toMatchObject({
        abilityId:'hf_hd_lunar_wave_01',auraKind:'moontide',minStacks:3,
      });
      expect(a.effects).toEqual([
        {type:'directDamage',min:20,max:26},
        {type:'extendDot',dot:'moonfire',seconds:6,maxBonus:6},
      ]);
      expect(HIGHFLY_HUNTER_MOONTIDE_BUILDER_IDS.has(id)).toBe(true);
    }
    expect(ABILITIES.hf_hd_lunar_wave_01.effects).toEqual([
      {type:'directDamage',min:136,max:162,spellPowerCoeff:0.32},
    ]);
    expect(HIGHFLY_HUNTER_MOONTIDE_PAYOFF_IDS.has('hf_hd_lunar_wave_01')).toBe(true);
  });

  it('keeps Lunar Tempest direct+DoT ranks and canonical moonfire aura for Moonseed extension',()=>{
    for(const id of FAMILIES.lunar){
      const a=ABILITIES[id]!;
      expect(a.class).toBe('hunter');
      expect(a.effects).toEqual([
        {type:'directDamage',min:9,max:12},
        {type:'dot',total:12,duration:9,interval:3,auraId:'moonfire'},
      ]);
      expect(a.ranks?.[1]?.effects).toEqual([
        {type:'directDamage',min:50,max:60},
        {type:'dot',total:70,duration:12,interval:3,auraId:'moonfire'},
      ]);
    }
  });

  it('keeps Galeheart on a six-pulse ground channel without any Druid form gate',()=>{
    for(const id of FAMILIES.gale){
      const a=ABILITIES[id]!;
      expect(a.class).toBe('hunter');
      expect(a.cost).toBe(90);
      expect(a.channel).toEqual({duration:6,ticks:6});
      expect(a.targetMode).toBe('position');
      expect(a.requiresAuraKind).toBeUndefined();
      expect(a.effects).toEqual([{type:'aoeDamage',min:12,max:16,radius:8}]);
    }
  });

  it('routes every HIGHFLY endpoint through Hunter-compatible presentation with premium VFX',()=>{
    for(const ids of Object.values(FAMILIES)){
      for(const id of ids.filter((x)=>x.startsWith('hf_'))){
        expect(abilityVfxFullSpec(id),id).toBeTruthy();
        const animation=highflyPresentationRoute(id,'animation');
        expect(animation,id).toBeTruthy();
        expect(['rapid_fire','volley','stampede','frostjaw_trap','arcane_shot']).toContain(animation);
      }
    }
  });
});
""",
)

print("HIGHFLY_SKILL_LAB2_HD_7OF7_GOLD=1")
