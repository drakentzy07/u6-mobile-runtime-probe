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
        raise SystemExit(f"{path}: Q2 anchor expected {count}, found {found}: {old[:150]!r}")
    write(path, text.replace(old, new, count))

# Q2 sources: Claude-native only. Q-Male/Q-Female and Q1 skill-infusion sources
# remain archived in the host branch but are never copied into the runtime.
for src, dst in [
    ("src/highfly/q2_runtime.ts", "src/highfly/q2_runtime.ts"),
    ("src/highfly/q2_elemental_basic.ts", "src/sim/combat/highfly_q2_elemental_basic.ts"),
    ("src/highfly/q2_elemental_weapon_vfx.ts", "src/highfly/q2_elemental_weapon_vfx.ts"),
    ("src/styles/hf_q2_clean.css", "src/styles/hf_q2_clean.css"),
]:
    target = ROOT / dst
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(HOST / src, target)

# Dedicated fast playtest entry for all nine ORIGINAL Claude classes.
rep(
    "src/main.ts",
    "  void startGame(sim, sim, null, `offline:${playerClass}:${name}`, true);",
    "  void startGame(sim, sim, null, `offline:${playerClass}:${name}`, !highflyQ2Test);",
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
const highflyQ2Test = startupParams.get('q2test') === '1';
const HIGHFLY_Q2_CLASSES: readonly PlayerClass[] = [
  'warrior', 'paladin', 'hunter', 'rogue', 'priest', 'shaman', 'mage', 'warlock', 'druid',
];
const requestedQ2Class = startupParams.get('labclass') as PlayerClass | null;
const highflyQ2Class: PlayerClass =
  requestedQ2Class && HIGHFLY_Q2_CLASSES.includes(requestedQ2Class)
    ? requestedQ2Class
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
    """} else if (highflyQ2Test) {
  startSitePresence('home');
  void startOffline(highflyQ2Class, 'HIGHFLY Q2', 0);
} else if (diagnosticsAutoOffline) {
  startSitePresence('home');
  void startOffline('warrior', 'Diagnostics', 0);
} else {
  startSitePresence('home');
  wireStartScreens();
  initHomepageMusic();
}""",
)

# Runtime activates only behind ?q2test=1 and never rewrites class skills.
main = ROOT / "src/main.ts"
main.write_text("import './highfly/q2_runtime';\n" + main.read_text(encoding="utf-8"), encoding="utf-8")

# Keep the native desktop action bar painted on Q2 mobile so S1-S10 are the real
# native slots, not proxy buttons or duplicated elemental variants.
rep(
    "src/ui/hud.ts",
    "    if (!this.isMobileLayout())\n      this.actionBarPainter.paint(this.actionBarView.tick(actionBarWorld));",
    "    if (!this.isMobileLayout() || document.body.classList.contains('hf-q2-active'))\n      this.actionBarPainter.paint(this.actionBarView.tick(actionBarWorld));",
)

# Extend the proven HIGHFLY 1-2-3 action combo by ONE conditional fourth intent.
rep(
    "src/ui/hud.ts",
    "  onHighflyBasicAttack: ((step: 1 | 2 | 3) => boolean) | null = null;\n"
    "  /** Mobile skill intent hook: acquire before the existing cast path. */",
    "  onHighflyBasicAttack: ((step: 1 | 2 | 3 | 4) => boolean) | null = null;\n"
    "  onHighflyElementalFinisherReady: (() => boolean) | null = null;\n"
    "  /** Mobile skill intent hook: acquire before the existing cast path. */",
)
rep(
    "src/ui/hud.ts",
    "  private highflyComboStep: 0 | 1 | 2 | 3 = 0;",
    "  private highflyComboStep: 0 | 1 | 2 | 3 | 4 = 0;",
)
rep(
    "src/ui/hud.ts",
    """  private activateFixedAttackSlot(): void {
    const now = performance.now() / 1000;
    if (now < this.highflyComboReadyAt) return;
    if (now > this.highflyComboExpiresAt) this.highflyComboStep = 0;
    const next = ((this.highflyComboStep % 3) + 1) as 1 | 2 | 3;

    // HIGHFLY fixed Attack is an action-RPG intent, not a continuous
    // ClaudeCraft auto-attack toggle. Advance only when the strike is accepted.
    // After the finisher, force a short recovery before a fresh 1-2-3 can start.
    if (this.onHighflyBasicAttack?.(next)) {
      this.highflyComboStep = next === 3 ? 0 : next;
      this.highflyComboExpiresAt = now + 0.95;
      // One real-time cadence source for action combat:
      // quick 1 -> 2 -> 3, then a clearly perceptible short finisher recovery.
      this.highflyComboReadyAt =
        now + (next === 1 ? 0.30 : next === 2 ? 0.34 : 0.62);
      this.highflyAcceptedBasicAttacks += 1;
      document.body.dataset.highflyBasicAttackCount = String(this.highflyAcceptedBasicAttacks);
      this.flashActionSlot(0);
    }
  }""",
    """  private activateFixedAttackSlot(): void {
    const now = performance.now() / 1000;
    if (now < this.highflyComboReadyAt) return;
    if (now > this.highflyComboExpiresAt) this.highflyComboStep = 0;
    const elemental = this.onHighflyElementalFinisherReady?.() ?? false;
    const next: 1 | 2 | 3 | 4 =
      this.highflyComboStep === 3 && elemental
        ? 4
        : (((this.highflyComboStep % 3) + 1) as 1 | 2 | 3);

    if (this.onHighflyBasicAttack?.(next)) {
      if (next === 4) this.highflyComboStep = 0;
      else if (next === 3) this.highflyComboStep = elemental ? 3 : 0;
      else this.highflyComboStep = next;
      this.highflyComboExpiresAt = now + 0.95;
      this.highflyComboReadyAt =
        now + (next === 1 ? 0.30 : next === 2 ? 0.34 : next === 3 ? (elemental ? 0.38 : 0.62) : 0.72);
      this.highflyAcceptedBasicAttacks += 1;
      document.body.dataset.highflyBasicAttackCount = String(this.highflyAcceptedBasicAttacks);
      document.body.dataset.highflyBasicAttackStep = String(next);
      this.flashActionSlot(0);
    }
  }""",
)

# SIM-owned element state + elemental fourth strike.
aa = ROOT / "src/sim/combat/auto_attack.ts"
aa.write_text(
    "import { applyHighflyElementalFinisher, highflyElementalFinisherReady, highflyWeaponElement } from './highfly_q2_elemental_basic';\n"
    + aa.read_text(encoding="utf-8"),
    encoding="utf-8",
)
rep("src/sim/combat/auto_attack.ts", "  step: 1 | 2 | 3,", "  step: 1 | 2 | 3 | 4,")
rep(
    "src/sim/combat/auto_attack.ts",
    """  const ranged = rangedAutoProfile(p, r.meta.cls);
  if (ranged && d <= ranged.maxRange && d >= (ranged.wand ? 0 : ranged.minRange)) {""",
    """  const element = highflyWeaponElement(p);
  if (step === 4 && !highflyElementalFinisherReady(p)) return false;
  const ranged = rangedAutoProfile(p, r.meta.cls);
  if (ranged && d <= ranged.maxRange && d >= (ranged.wand ? 0 : ranged.minRange)) {""",
)
rep(
    "src/sim/combat/auto_attack.ts",
    """    rangedSwing(ctx, p, t, { ...ranged, min: shot.min, max: shot.max, speed: shot.speed });
    p.swingTimer = step === 1 ? 0.36 : step === 2 ? 0.4 : 0.58;
    return true;""",
    """    rangedSwing(
      ctx,
      p,
      t,
      { ...ranged, min: shot.min, max: shot.max, speed: shot.speed },
      step === 4 && element !== 'base'
        ? { abilityId: 'highfly_basic_4_' + element, element }
        : undefined,
    );
    p.swingTimer = step === 1 ? 0.36 : step === 2 ? 0.4 : step === 3 ? 0.58 : 0.68;
    return true;""",
)
rep(
    "src/sim/combat/auto_attack.ts",
    """  meleeSwing(ctx, p, t, 0, 'HIGHFLY Basic', {
    autoAttackHand: 'mainhand',
    abilityId: 'highfly_basic_' + step,
    weaponMult: 1,
    autoAttack: false,
  });
  p.swingTimer = step === 1 ? 0.36 : step === 2 ? 0.4 : 0.58;
  return true;""",
    """  const landed = meleeSwing(ctx, p, t, 0, 'HIGHFLY Basic', {
    autoAttackHand: 'mainhand',
    abilityId: step === 4 && element !== 'base' ? 'highfly_basic_4_' + element : 'highfly_basic_' + step,
    weaponMult: 1,
    autoAttack: false,
  });
  if (landed && step === 4 && element !== 'base') applyHighflyElementalFinisher(ctx, p, t, element);
  p.swingTimer = step === 1 ? 0.36 : step === 2 ? 0.4 : step === 3 ? 0.58 : 0.68;
  return true;""",
)

# Ranged fourth hit keeps the canonical projectile path and only adds the rider
# after that projectile really lands.
rep(
    "src/sim/combat/auto_attack.ts",
    """export function rangedSwing(
  ctx: SimContext,
  attacker: Entity,
  target: Entity,
  ranged: { min: number; max: number; speed: number; wand?: boolean; school?: string },
): void {""",
    """export function rangedSwing(
  ctx: SimContext,
  attacker: Entity,
  target: Entity,
  ranged: { min: number; max: number; speed: number; wand?: boolean; school?: string },
  highfly?: { abilityId: string; element: Exclude<ReturnType<typeof highflyWeaponElement>, 'base'> },
): void {""",
)
rep(
    "src/sim/combat/auto_attack.ts",
    """        kind: 'miss',
        ...(ranged.wand ? {} : { attackAnimationStarted: true as const }),""",
    """        kind: 'miss',
        ...(highfly ? { abilityId: highfly.abilityId } : {}),
        ...(ranged.wand ? {} : { attackAnimationStarted: true as const }),""",
)
rep(
    "src/sim/combat/auto_attack.ts",
    """    ctx.dealDamage(
      atk,
      tgt,
      Math.max(1, Math.round(dmg)),
      crit,
      school,
      label,
      'hit',
      false,
      undefined,
      true,
      !ranged.wand,
    );""",
    """    const dealt = ctx.dealDamage(
      atk,
      tgt,
      Math.max(1, Math.round(dmg)),
      crit,
      school,
      label,
      'hit',
      false,
      undefined,
      true,
      !ranged.wand,
      false,
      highfly?.abilityId ?? null,
    );
    if (dealt > 0 && highfly) applyHighflyElementalFinisher(ctx, atk, tgt, highfly.element);""",
)

# Delegate + HUD readiness through the existing Sim.
rep(
    "src/sim/sim.ts",
    """  highflyBasicAttack(step: 1 | 2 | 3, pid?: number): boolean {
    return highflyBasicAttackImpl(this.ctx, step, pid);
  }""",
    """  highflyBasicAttack(step: 1 | 2 | 3 | 4, pid?: number): boolean {
    return highflyBasicAttackImpl(this.ctx, step, pid);
  }

  highflyElementalFinisherReady(pid?: number): boolean {
    const r = this.ctx.resolve(pid);
    return !!r && highflyElementalFinisherReadyImpl(r.e);
  }""",
)
# Import helper into Sim next to the existing basic attack import block.
rep(
    "src/sim/sim.ts",
    "  highflyBasicAttack as highflyBasicAttackImpl,",
    "  highflyBasicAttack as highflyBasicAttackImpl,",
)
sim = ROOT / "src/sim/sim.ts"
sim_text = sim.read_text(encoding="utf-8")
auto_import = "from './combat/auto_attack';"
idx = sim_text.find(auto_import)
if idx < 0:
    raise SystemExit("sim.ts auto_attack import not found")
line_start = sim_text.rfind("import ", 0, idx)
line_end = sim_text.find("\n", idx)
helper = "import { highflyElementalFinisherReady as highflyElementalFinisherReadyImpl } from './combat/highfly_q2_elemental_basic';\n"
sim.write_text(sim_text[:line_end+1] + helper + sim_text[line_end+1:], encoding="utf-8")

rep(
    "src/main.ts",
    """  hud.onHighflyBasicAttack = (step) => {
    const actionWorld = world as typeof world & {
      highflyBasicAttack?: (comboStep: 1 | 2 | 3) => boolean;
    };
    return actionWorld.highflyBasicAttack?.(step) ?? false;
  };""",
    """  hud.onHighflyBasicAttack = (step) => {
    const actionWorld = world as typeof world & {
      highflyBasicAttack?: (comboStep: 1 | 2 | 3 | 4) => boolean;
    };
    return actionWorld.highflyBasicAttack?.(step) ?? false;
  };
  hud.onHighflyElementalFinisherReady = () => {
    const actionWorld = world as typeof world & {
      highflyElementalFinisherReady?: () => boolean;
    };
    return actionWorld.highflyElementalFinisherReady?.() ?? false;
  };""",
)

# Render-only elemental weapon aura + impact particles. Native class skill events
# are not rewritten or recolored.
renderer = ROOT / "src/render/renderer.ts"
renderer.write_text(
    "import { paintQ2ElementalBasic, q2WeaponElementColor } from '../highfly/q2_elemental_weapon_vfx';\n"
    + renderer.read_text(encoding="utf-8"),
    encoding="utf-8",
)
rep(
    "src/render/renderer.ts",
    "    this.riftDeathZoneVisuals?.handleEvent(ev);",
    "    this.riftDeathZoneVisuals?.handleEvent(ev);\n"
    "    if (ev.type === 'damage') paintQ2ElementalBasic(ev, this.sim.entities.get(ev.sourceId), this.vfx);",
)
rep(
    "src/render/renderer.ts",
    "      v.visual.setWeaponAura(weaponAura ? weaponAura.color : null, weaponAura?.tip ?? false);",
    "      const q2Color = q2WeaponElementColor(e);\n"
    "      v.visual.setWeaponAura(q2Color ?? (weaponAura ? weaponAura.color : null), q2Color !== null ? false : (weaponAura?.tip ?? false));",
)

# Give Warrior/Rogue a definite fourth visual beat; every other class keeps its
# original attack animation fallback.
rep(
    "src/render/characters/manifest.ts",
    "        highfly_basic_3: 'Warrior_Reaping_Arc',",
    "        highfly_basic_3: 'Warrior_Reaping_Arc',\n"
    "        highfly_basic_4_fire: 'Warrior_Reaping_Arc',\n"
    "        highfly_basic_4_frost: 'Warrior_Reaping_Arc',\n"
    "        highfly_basic_4_lightning: 'Warrior_Reaping_Arc',\n"
    "        highfly_basic_4_air: 'Warrior_Reaping_Arc',",
)
rep(
    "src/render/characters/manifest.ts",
    "        highfly_basic_3: 'Rogue_Finisher_Slash',",
    "        highfly_basic_3: 'Rogue_Finisher_Slash',\n"
    "        highfly_basic_4_fire: 'Rogue_Finisher_Slash',\n"
    "        highfly_basic_4_frost: 'Rogue_Finisher_Slash',\n"
    "        highfly_basic_4_lightning: 'Rogue_Finisher_Slash',\n"
    "        highfly_basic_4_air: 'Rogue_Finisher_Slash',",
)

print("HIGHFLY_Q2_CLAUDE_CLEAN_APPLIED=1")
