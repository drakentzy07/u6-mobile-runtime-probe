from pathlib import Path
import shutil

SOURCE = Path(__file__).resolve().parent.parent

def rep(path: str, old: str, new: str) -> None:
    target = Path(path)
    text = target.read_text(encoding='utf-8')
    if text.count(old) != 1:
        raise SystemExit(f'{path}: Q1 HUD anchor expected once, found {text.count(old)}: {old[:160]!r}')
    target.write_text(text.replace(old, new, 1), encoding='utf-8')

for name in ['src/highfly/affinity_lab_runtime.ts', 'src/highfly/weapon_affinity_core.ts', 'src/styles/hf_affinity_lab.css', 'tests/highfly_q1_hotbar.test.ts']:
    target = Path(name)
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(SOURCE / name, target)

rep('src/ui/hud.ts',
    '    if (!this.isMobileLayout())\n      this.actionBarPainter.paint(this.actionBarView.tick(actionBarWorld));',
    "    if (!this.isMobileLayout() || document.body.classList.contains('hf-q1-active'))\n      this.actionBarPainter.paint(this.actionBarView.tick(actionBarWorld));")

rep('src/ui/hud.ts',
    "    const action = this.actionForSlot(barSlot);\n    if (action?.type === 'ability') {\n      // cast by ability id:",
    "    const action = this.actionForSlot(barSlot);\n    if (action && this.isMobileLayout() && document.body.classList.contains('hf-q1-active')) this.onMobileActionIntent?.(action);\n    if (action?.type === 'ability') {\n      // cast by ability id:")

rep('src/ui/hud.ts',
    '    if (this.empowerHold.press(slot, this.empoweredAbilityIdForSlot(slot), this.sim)) return;',
    "    if (this.empoweredAbilityIdForSlot(slot) && this.isMobileLayout() && document.body.classList.contains('hf-q1-active')) { const action = this.actionForSlot(slot); if (action) this.onMobileActionIntent?.(action); }\n    if (this.empowerHold.press(slot, this.empoweredAbilityIdForSlot(slot), this.sim)) return;")
print('HIGHFLY_Q1_NATIVE_HUD=1')
