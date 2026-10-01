from pathlib import Path

ROOT = Path(".")

def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")

def write(path: str, text: str) -> None:
    p = ROOT / path
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(text, encoding="utf-8")

def rep(path: str, old: str, new: str) -> None:
    text = read(path)
    n = text.count(old)
    if n != 1:
        raise SystemExit(f"{path}: expected 1 anchor, found {n}: {old[:180]!r}")
    write(path, text.replace(old, new, 1))

# ---------------------------------------------------------------------------
# SKILL LAB 2.0 / RUN1C
# Rogue GOLD hardening for:
#   Emboscada -> Caceria Sombria -> Eclipse Mortal
#
# EVO + MUTATION reuse Claude's swept relocation, but only after the normal
# target/range/LoS gates. If collision prevents a true behind placement, the
# relocation is rolled back and Claude's native behind gate decides the cast.
# BASE Ambush remains untouched.
# ---------------------------------------------------------------------------

rep(
    "src/sim/combat/casting_lifecycle.ts",
    """import { sharedCooldownIds } from './ability_cooldown_groups';""",
    """import { sharedCooldownIds } from './ability_cooldown_groups';
import { relocateSwept } from './heroic_leap';""",
)

rep(
    "src/sim/combat/casting_lifecycle.ts",
    """function applyAbility(
  ctx: SimContext,""",
    """const HIGHFLY_ROGUE_MICRO_REPOSITION_OPENERS: ReadonlySet<string> = new Set([
  'hf_shadow_hunt_01',
  'hf_eclipse_mortal_01',
]);

function applyAbility(
  ctx: SimContext,""",
)

rep(
    "src/sim/combat/casting_lifecycle.ts",
    """    if (ctx.lineOfSightBlocked(p, target, ability)) {
      ctx.error(p.id, 'Line of sight.');
      return;
    }
    const facingDiff = Math.abs(normAngle(angleTo(p.pos, target.pos) - p.facing));""",
    """    if (ctx.lineOfSightBlocked(p, target, ability)) {
      ctx.error(p.id, 'Line of sight.');
      return;
    }

    // HIGHFLY Rogue EVO/MUTATION: the BASE Ambush still demands manual back
    // positioning. Caceria Sombria and Eclipse Mortal may micro-step to the
    // target's back, but ONLY after the native target/range/LoS gates above.
    // The same swept collision resolver used by Claude's scripted movement is
    // authoritative. If it cannot actually seat the Rogue behind the target,
    // restore every relocation-owned field and let the normal behind gate fail.
    if (HIGHFLY_ROGUE_MICRO_REPOSITION_OPENERS.has(ability.id)) {
      const before = {
        pos: { ...p.pos },
        facing: p.facing,
        vy: p.vy,
        onGround: p.onGround,
        fallStartY: p.fallStartY,
        chargeTargetId: p.chargeTargetId,
        chargePath: [...p.chargePath],
      };
      const backDistance = Math.max(FACING_HOLD_DIST + 0.35, 1.5);
      relocateSwept(ctx, p, {
        x: target.pos.x - Math.sin(target.facing) * backDistance,
        y: p.pos.y,
        z: target.pos.z - Math.cos(target.facing) * backDistance,
      });
      const behindAfter = Math.abs(normAngle(angleTo(target.pos, p.pos) - target.facing));
      const trulyBehind =
        behindAfter >= Math.PI / 2 && dist2d(target.pos, p.pos) >= FACING_HOLD_DIST;
      if (!trulyBehind) {
        p.pos = before.pos;
        p.facing = before.facing;
        p.vy = before.vy;
        p.onGround = before.onGround;
        p.fallStartY = before.fallStartY;
        p.chargeTargetId = before.chargeTargetId;
        p.chargePath = before.chargePath;
      } else {
        p.facing = angleTo(p.pos, target.pos);
        ctx.emit({
          type: 'spellfx',
          sourceId: p.id,
          targetId: p.id,
          school: ability.school,
          fx: 'blinkStep',
          ability: ability.id,
        });
      }
    }

    const facingDiff = Math.abs(normAngle(angleTo(p.pos, target.pos) - p.facing));""",
)

write(
    "tests/highfly_skill_lab2_rogue_hardening.test.ts",
    """import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';

describe('HIGHFLY Skill Lab 2.0 RUN1C - Rogue GOLD hardening', () => {
  const source = readFileSync('src/sim/combat/casting_lifecycle.ts', 'utf8');

  it('micro-repositions only EVO and MUTATION, never BASE Ambush', () => {
    expect(source).toContain("const HIGHFLY_ROGUE_MICRO_REPOSITION_OPENERS");
    expect(source).toContain("'hf_shadow_hunt_01'");
    expect(source).toContain("'hf_eclipse_mortal_01'");
    const setBlock = source.slice(
      source.indexOf('const HIGHFLY_ROGUE_MICRO_REPOSITION_OPENERS'),
      source.indexOf('function applyAbility'),
    );
    expect(setBlock).not.toContain("'ambush'");
  });

  it('keeps native target/range/LoS gates before the micro-step', () => {
    const rangeGate = source.indexOf("ctx.error(p.id, 'Out of range.')");
    const losGate = source.indexOf("ctx.error(p.id, 'Line of sight.')");
    const reposition = source.indexOf('HIGHFLY_ROGUE_MICRO_REPOSITION_OPENERS.has(ability.id)');
    expect(rangeGate).toBeGreaterThan(-1);
    expect(losGate).toBeGreaterThan(rangeGate);
    expect(reposition).toBeGreaterThan(losGate);
  });

  it('reuses swept collision, rolls back blocked placement, then native behind gate remains', () => {
    expect(source).toContain("import { relocateSwept } from './heroic_leap'");
    expect(source).toContain('relocateSwept(ctx, p');
    expect(source).toContain('if (!trulyBehind)');
    expect(source).toContain('p.pos = before.pos');
    expect(source).toContain("ctx.error(p.id, 'You must be behind your target.')");
  });

  it('emits Claude blinkStep only after a successful true-behind seat', () => {
    const success = source.indexOf('p.facing = angleTo(p.pos, target.pos);');
    const cue = source.indexOf("fx: 'blinkStep'", success);
    expect(success).toBeGreaterThan(-1);
    expect(cue).toBeGreaterThan(success);
  });
});
""",
)

print("HIGHFLY_SKILL_LAB2_RUN1C_ROGUE_HARDENING=1")
