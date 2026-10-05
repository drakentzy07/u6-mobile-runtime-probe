"""Q1 native additive infusion hooks, applied after the Q0 lab overlays."""
from pathlib import Path

HOST = Path(__file__).resolve().parents[1]


def replace(path: str, before: str, after: str) -> None:
    target = Path(path)
    text = target.read_text()
    if after in text:
        return
    if text.count(before) != 1:
        raise SystemExit(f'{path}: expected one Q1 anchor: {before[:100]!r}')
    target.write_text(text.replace(before, after, 1))


Path('src/sim/combat/highfly_weapon_affinity.ts').write_text(
    (HOST / 'src/highfly/weapon_infusion_sim.ts').read_text()
)
Path('tests/highfly_q1_infusion.test.ts').write_text(
    (HOST / 'tests/highfly_q1_infusion.test.ts').read_text()
)

casting = 'src/sim/combat/casting_lifecycle.ts'
infusion_import = (
    "import { beginWeaponInfusionCast, captureWeaponInfusion, clearPendingWeaponInfusion } "
    "from './highfly_weapon_affinity';"
)
prior_import = "import { captureWeaponInfusion } from './highfly_weapon_affinity';"
if prior_import in Path(casting).read_text():
    replace(casting, prior_import, infusion_import)
replace(casting, "import { sharedCooldownIds } from './ability_cooldown_groups';",
        "import { sharedCooldownIds } from './ability_cooldown_groups';\n"
        + infusion_import)
replace(casting, "    reserveRadiantResonance(p, ability.id);\n    p.castingAbility = ability.id;",
        "    reserveRadiantResonance(p, ability.id);\n    p.castingAbility = ability.id;\n"
        "    beginWeaponInfusionCast(p, ability.id);")
replace(casting, "export function cancelCast(ctx: SimContext, p: Entity): void {",
        "export function cancelCast(ctx: SimContext, p: Entity): void {\n"
        "  clearPendingWeaponInfusion(p);")
replace(casting, "    clearRadiantResonanceReservation(p);\n    // the aim point is consumed",
        "    clearRadiantResonanceReservation(p);\n    clearPendingWeaponInfusion(p);\n"
        "    // the aim point is consumed")
replace(casting, "  if (cooldown <= 0 || togglingOff) return;",
        "  if (!togglingOff) captureWeaponInfusion(p, abilityId);\n"
        "  if (cooldown <= 0 || togglingOff) return;")
damage = 'src/sim/combat/damage.ts'
replace(damage, "import { isUnbreakableControlAura } from './cc';",
        "import { isUnbreakableControlAura } from './cc';\n"
        "import { applyWeaponInfusionHit } from './highfly_weapon_affinity';")
replace(damage, '  // A proc echo fires once its carrier falls below the stored health fraction.',
        '  // Q1: after native damage breaks old controls, add the weapon rider once.\n'
        '  // Periodic ticks and copied hits never generate additional infusions.\n'
        '  if (direct && !copiedHit) {\n'
        '    applyWeaponInfusionHit(ctx, source, target, abilityId ?? ability, kind, craftedHpLoss);\n'
        '  }\n\n'
        '  // A proc echo fires once its carrier falls below the stored health fraction.')

effects = 'src/sim/combat/effect_dispatch.ts'
replace(effects, "import { buildBenisonPrayer, consumeBenisonPrayers } from './priest/benison_dawnweave';",
        "import { buildBenisonPrayer, consumeBenisonPrayers } from './priest/benison_dawnweave';\n"
        "import { applyWeaponInfusionHit } from './highfly_weapon_affinity';")
replace(effects, "        if (dotId === 'rupture') {\n          ctx.emit({",
        "        if (dotId === 'rupture') {\n"
        "          // Pure native bleed has no direct hit; infuse once on accepted application.\n"
        "          applyWeaponInfusionHit(ctx, p, target, ability.id, 'hit', 1);\n"
        "          ctx.emit({")

print('HIGHFLY Q1 native infusion hooks applied')
