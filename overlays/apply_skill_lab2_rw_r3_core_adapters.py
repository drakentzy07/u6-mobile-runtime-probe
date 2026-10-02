from pathlib import Path

ROOT = Path(".")
CLASSES = ROOT / "src/sim/content/classes.ts"
CASTING = ROOT / "src/sim/combat/casting_lifecycle.ts"
TEST = ROOT / "tests/highfly_skill_lab2_rw_r3_core_adapters.test.ts"

def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")

def write(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")

def rep(path: Path, old: str, new: str) -> None:
    text = read(path)
    n = text.count(old)
    if n != 1:
        raise SystemExit(f"{path}: expected 1 anchor, found {n}: {old[:180]!r}")
    write(path, text.replace(old, new, 1))

# ---------------------------------------------------------------------------
# HIGHFLY Skill Lab 2.0 — Rogue+Warlock R3 core adapters
#
# Main-class authority remains Rogue:
# - body / weapon / equipment remain Rogue
# - Energy remains the only visible combat resource
# - Warlock Mana / Soul Fragments are translated away at the adapter boundary
#
# Reaping Command:
# - reuses Claude reapingCommand + ownedNecromancyUndead
# - requires an owned undead exactly like Claude
# - NO soulFragmentCost on the Rogue adapter
#
# Umbral Anchor:
# - reuses Claude warlockUmbralAnchor effect and stored anchor aura
# - adds the Rogue alias to Claude's two ID-specific lifecycle gates so placement
#   is cooldown-free and an out-of-range recall is rejected before Energy billing.
# ---------------------------------------------------------------------------

rep(
    CLASSES,
    """      'hf_rw_evil_eye_01',
      'instant_poison',""",
    """      'hf_rw_evil_eye_01',
      'hf_rw_reaping_command_01',
      'hf_rw_umbral_anchor_01',
      'instant_poison',""",
)

rep(
    CLASSES,
    """  backstab: {
    id: 'backstab',""",
    """  hf_rw_reaping_command_01: {
    id: 'hf_rw_reaping_command_01',
    name: 'Mandato de Siega',
    class: 'rogue',
    learnLevel: 1,
    cost: 45,
    castTime: 0,
    cooldown: 8,
    range: 30,
    school: 'shadow',
    requiresTarget: true,
    projectile: false,
    effects: [{ type: 'reapingCommand' }],
    description:
      'HIGHFLY Heritage adapter: ordena undead propiedad del Rogue usando Energy. No importa Mana ni Soul Fragments.',
  },
  hf_rw_umbral_anchor_01: {
    id: 'hf_rw_umbral_anchor_01',
    name: 'Ancla Umbral',
    class: 'rogue',
    learnLevel: 1,
    cost: 25,
    castTime: 0,
    cooldown: 45,
    range: 0,
    school: 'shadow',
    requiresTarget: false,
    effects: [{ type: 'warlockUmbralAnchor', duration: 300, maxRange: 40 }],
    description:
      'HIGHFLY Heritage adapter: coloca y recupera un ancla usando Energy. La colocacion no inicia cooldown; el recall valido si.',
  },
  backstab: {
    id: 'backstab',""",
)

rep(
    CASTING,
    """  if (ability.id === UMBRAL_ANCHOR_ID) {
    const anchorEffect = res.effects.find((effect) => effect.type === 'warlockUmbralAnchor');""",
    """  if (ability.id === UMBRAL_ANCHOR_ID || ability.id === 'hf_rw_umbral_anchor_01') {
    const anchorEffect = res.effects.find((effect) => effect.type === 'warlockUmbralAnchor');""",
)

rep(
    CASTING,
    """  if (abilityId === UMBRAL_ANCHOR_ID && !hasUmbralAnchor(p)) return;""",
    """  if (
    (abilityId === UMBRAL_ANCHOR_ID || abilityId === 'hf_rw_umbral_anchor_01') &&
    !hasUmbralAnchor(p)
  ) return;""",
)

TEST.write_text(
    """import { describe, expect, it } from 'vitest';
import { summonUndead } from '../src/sim/combat/necromancy';
import { hasUmbralAnchor, umbralAnchorPosition } from '../src/sim/combat/warlock_utility';
import { MOBS } from '../src/sim/data';
import { createMob } from '../src/sim/entity';
import { Sim } from '../src/sim/sim';
import type { SimContext } from '../src/sim/sim_context';
import type { Entity, SimEvent } from '../src/sim/types';
import { EMPTY_TEST_WORLD } from './sim_shared';

const REAP = 'hf_rw_reaping_command_01';
const ANCHOR = 'hf_rw_umbral_anchor_01';

function ctx(sim: Sim): SimContext {
  return (sim as unknown as { ctx: SimContext }).ctx;
}

function makeRogue(seed: number): Sim {
  const sim = new Sim({ seed, playerClass: 'rogue', autoEquip: true, world: EMPTY_TEST_WORLD });
  sim.setPlayerLevel(20);
  sim.player.resource = sim.player.maxResource;
  sim.player.hitBonus = 1;
  return sim;
}

function addTarget(sim: Sim, z = 10): Entity {
  const target = createMob(
    (sim as unknown as { nextId: number }).nextId++,
    MOBS.ridge_stalker,
    20,
    {
      x: sim.player.pos.x,
      y: sim.player.pos.y,
      z: sim.player.pos.z + z,
    },
  );
  target.maxHp = 100_000;
  target.hp = target.maxHp;
  target.weapon.min = 0;
  target.weapon.max = 0;
  target.weapon.speed = 1000;
  target.swingTimer = 1000;
  target.moveSpeed = 0;
  target.hostile = true;
  sim.addEntity(target);
  return target;
}

function ready(sim: Sim, abilityId?: string): void {
  for (let i = 0; i < 40 && sim.player.gcdRemaining > 0; i++) sim.tick();
  if (abilityId) {
    for (let i = 0; i < 120 && (sim.player.cooldowns.get(abilityId) ?? 0) > 0; i++) sim.tick();
  }
}

function errors(events: readonly SimEvent[]): string[] {
  return events
    .filter((event): event is Extract<SimEvent, { type: 'error' }> => event.type === 'error')
    .map((event) => event.text);
}

describe('HIGHFLY Skill Lab 2.0 — Rogue+Warlock R3 core adapters', () => {
  it('Mandato de Siega commands Claude-owned undead with Rogue Energy only', () => {
    const sim = makeRogue(81);
    const target = addTarget(sim);
    const meta = sim.meta(sim.playerId);
    const equipmentBefore = JSON.stringify(meta?.equipment);
    const undead = summonUndead(
      ctx(sim),
      sim.player,
      'necromancy_skeletal_warrior',
      true,
      30,
    );

    expect(undead).toBeTruthy();
    expect(undead?.ownerId).toBe(sim.player.id);
    expect(meta?.cls).toBe('rogue');
    expect(sim.player.resourceType).toBe('energy');
    expect(sim.player.auras.some((aura) => aura.kind === 'soul_fragments')).toBe(false);

    const hpBefore = target.hp;
    const energyBefore = sim.player.resource;
    sim.targetEntity(target.id);
    sim.castAbility(REAP);

    expect(target.hp).toBeLessThan(hpBefore);
    expect(sim.player.resource).toBe(energyBefore - 45);
    expect(sim.player.auras.some((aura) => aura.kind === 'soul_fragments')).toBe(false);
    expect(JSON.stringify(sim.meta(sim.playerId)?.equipment)).toBe(equipmentBefore);
  });

  it('Mandato de Siega refuses cleanly when no owned undead exists', () => {
    const sim = makeRogue(82);
    const target = addTarget(sim);
    const energyBefore = sim.player.resource;

    sim.targetEntity(target.id);
    sim.drainEvents();
    sim.castAbility(REAP);
    const castErrors = errors(sim.drainEvents());

    expect(target.hp).toBe(target.maxHp);
    expect(sim.player.resource).toBe(energyBefore);
    expect(castErrors).toContain('That ability is not ready yet.');
  });

  it('Ancla Umbral placement spends Energy but starts no cooldown; recall returns and starts cooldown', () => {
    const sim = makeRogue(83);
    const p = sim.player;
    const meta = sim.meta(sim.playerId);
    const equipmentBefore = JSON.stringify(meta?.equipment);
    const origin = { ...p.pos };
    const energy0 = p.resource;

    sim.castAbility(ANCHOR);

    expect(hasUmbralAnchor(p)).toBe(true);
    expect(umbralAnchorPosition(p)).toEqual(origin);
    expect(p.resource).toBe(energy0 - 25);
    expect(p.cooldowns.has(ANCHOR)).toBe(false);

    ready(sim);
    p.resource = p.maxResource;
    p.pos = { ...p.pos, x: p.pos.x + 20 };
    p.prevPos = { ...p.pos };
    ctx(sim).rebucket(p);
    const energy1 = p.resource;

    sim.castAbility(ANCHOR);

    expect(hasUmbralAnchor(p)).toBe(false);
    expect(Math.hypot(p.pos.x - origin.x, p.pos.z - origin.z)).toBeLessThan(0.01);
    expect(p.resource).toBe(energy1 - 25);
    expect((p.cooldowns.get(ANCHOR) ?? 0)).toBeGreaterThan(44);
    expect(JSON.stringify(sim.meta(sim.playerId)?.equipment)).toBe(equipmentBefore);
    expect(sim.meta(sim.playerId)?.cls).toBe('rogue');
    expect(p.resourceType).toBe('energy');
  });

  it('Ancla Umbral rejects a >40m recall before Energy billing and preserves the anchor', () => {
    const sim = makeRogue(84);
    const p = sim.player;

    sim.castAbility(ANCHOR);
    expect(hasUmbralAnchor(p)).toBe(true);
    ready(sim);

    p.resource = p.maxResource;
    p.pos = { ...p.pos, x: p.pos.x + 45 };
    p.prevPos = { ...p.pos };
    ctx(sim).rebucket(p);
    const energyBefore = p.resource;

    sim.drainEvents();
    sim.castAbility(ANCHOR);
    const castErrors = errors(sim.drainEvents());

    expect(castErrors).toContain('Your Umbral Anchor is out of range.');
    expect(p.resource).toBe(energyBefore);
    expect(hasUmbralAnchor(p)).toBe(true);
    expect(p.cooldowns.has(ANCHOR)).toBe(false);
  });
});
""",
    encoding="utf-8",
)

print("HIGHFLY_SKILL_LAB2_RW_R3_CORE_ADAPTERS=1")
