from pathlib import Path

ROOT = Path(".")
CLASSES = ROOT / "src/sim/content/classes.ts"
TEST = ROOT / "tests/highfly_skill_lab2_rw_poc_evil_eye.test.ts"

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
# HIGHFLY Skill Lab 2.0 — Rogue+Warlock POC 01
#
# GOLD contract:
# - Main identity stays Rogue: body/weapon/locomotion/resource authority.
# - Heritage Evil Eye donates a source-owned movable mark only.
# - No visible Mana/Fragments/Condemnation bar is imported.
# - No Warlock auto-gaze damage is imported: Claude's affliction tick already
#   requires Warlock+Affliction metadata, so a Rogue-owned Eye remains a mark.
# - Death cleanup is reused from Claude's global clearAfflictionState teardown.
# ---------------------------------------------------------------------------

rep(
    CLASSES,
    """      'vanish',
      'instant_poison',""",
    """      'vanish',
      'hf_rw_evil_eye_01',
      'instant_poison',""",
)

rep(
    CLASSES,
    """  backstab: {
    id: 'backstab',""",
    """  hf_rw_evil_eye_01: {
    id: 'hf_rw_evil_eye_01',
    name: 'Ojo Maldito',
    class: 'rogue',
    learnLevel: 1,
    cost: 15,
    castTime: 0,
    cooldown: 1,
    range: 30,
    school: 'shadow',
    requiresTarget: true,
    projectile: false,
    effects: [{ type: 'afflictionEvilEye' }],
    description:
      'HIGHFLY Heritage POC: marca source-owned transferible sobre cuerpo y recurso Rogue. No importa Mana, Soul Fragments, Condemnation ni auto-gaze de Warlock.',
  },
  backstab: {
    id: 'backstab',""",
)

TEST.write_text(
    """import { describe, expect, it } from 'vitest';
import { MOBS } from '../src/sim/data';
import { createMob } from '../src/sim/entity';
import { Sim } from '../src/sim/sim';
import type { SimContext } from '../src/sim/sim_context';
import type { Entity } from '../src/sim/types';
import { EMPTY_TEST_WORLD } from './sim_shared';

const ABILITY = 'hf_rw_evil_eye_01';

function ctx(sim: Sim): SimContext {
  return (sim as unknown as { ctx: SimContext }).ctx;
}

function makeRogue(seed = 71): Sim {
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

function primaryEye(target: Entity, sourceId: number) {
  return target.auras.find(
    (aura) => aura.kind === 'affliction_eye' && aura.sourceId === sourceId,
  );
}

function tickUntilReady(sim: Sim): void {
  for (let i = 0; i < 30 && sim.player.gcdRemaining > 0; i++) sim.tick();
  for (let i = 0; i < 30 && (sim.player.cooldowns.get(ABILITY) ?? 0) > 0; i++) sim.tick();
}

describe('HIGHFLY Skill Lab 2.0 — Rogue+Warlock Evil Eye POC', () => {
  it('keeps Rogue as principal identity and spends only Rogue Energy', () => {
    const sim = makeRogue();
    const target = addTarget(sim);

    expect(sim.meta(sim.playerId)?.cls).toBe('rogue');
    expect(sim.player.resourceType).toBe('energy');
    expect(sim.known.some((known) => known.def.id === ABILITY)).toBe(true);

    const equipmentBefore = JSON.stringify(sim.meta(sim.playerId)?.equipment);
    const hpBefore = target.hp;
    const energyBefore = sim.player.resource;

    sim.targetEntity(target.id);
    sim.castAbility(ABILITY);

    expect(target.hp).toBe(hpBefore);
    expect(sim.player.resource).toBe(energyBefore - 15);
    expect(JSON.stringify(sim.meta(sim.playerId)?.equipment)).toBe(equipmentBefore);
    expect(primaryEye(target, sim.player.id)).toBeTruthy();
  });

  it('transfers exactly one source-owned mark without Condemnation or auto-gaze damage', () => {
    const sim = makeRogue(72);
    const first = addTarget(sim, 10);
    const second = addTarget(sim, 12);

    sim.targetEntity(first.id);
    sim.castAbility(ABILITY);
    expect(primaryEye(first, sim.player.id)).toBeTruthy();

    tickUntilReady(sim);
    sim.targetEntity(second.id);
    sim.castAbility(ABILITY);

    expect(primaryEye(first, sim.player.id)).toBeFalsy();
    expect(primaryEye(second, sim.player.id)).toBeTruthy();

    const ownedPrimaryCount = [...sim.entities.values()]
      .flatMap((entity) => entity.auras)
      .filter((aura) => aura.kind === 'affliction_eye' && aura.sourceId === sim.player.id).length;
    expect(ownedPrimaryCount).toBe(1);
    expect(sim.player.auras.some((aura) => aura.kind === 'affliction_doom')).toBe(false);

    const hpBefore = second.hp;
    sim.player.inCombat = true;
    second.inCombat = true;
    for (let i = 0; i < 120; i++) sim.tick();

    expect(second.hp).toBe(hpBefore);
    expect(sim.player.auras.some((aura) => aura.kind === 'affliction_doom')).toBe(false);
  });

  it('rejects out-of-range casts before spending Energy or moving the mark', () => {
    const sim = makeRogue(73);
    const near = addTarget(sim, 10);
    const far = addTarget(sim, 45);

    sim.targetEntity(near.id);
    sim.castAbility(ABILITY);
    expect(primaryEye(near, sim.player.id)).toBeTruthy();

    tickUntilReady(sim);
    const energyBefore = sim.player.resource;
    sim.targetEntity(far.id);
    sim.drainEvents();
    sim.castAbility(ABILITY);
    const errors = sim.drainEvents().filter((event) => event.type === 'error');

    expect(primaryEye(near, sim.player.id)).toBeTruthy();
    expect(primaryEye(far, sim.player.id)).toBeFalsy();
    expect(sim.player.resource).toBe(energyBefore);
    expect(errors.some((event) => event.text === 'Out of range.')).toBe(true);
  });

  it('reuses Claude global death cleanup so a Rogue-owned Eye never orphans', () => {
    const sim = makeRogue(74);
    const target = addTarget(sim, 10);

    sim.targetEntity(target.id);
    sim.castAbility(ABILITY);
    expect(primaryEye(target, sim.player.id)).toBeTruthy();

    ctx(sim).dealDamage(
      target,
      sim.player,
      sim.player.maxHp * 5,
      false,
      'physical',
      'POC lethal hit',
      'hit',
      true,
    );

    expect(sim.player.dead).toBe(true);
    expect(primaryEye(target, sim.player.id)).toBeFalsy();
  });
});
""",
    encoding="utf-8",
)

print("HIGHFLY_SKILL_LAB2_RW_POC_EVIL_EYE=1")
