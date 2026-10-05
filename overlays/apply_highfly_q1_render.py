from pathlib import Path
import shutil

ROOT = Path('.')
HOST = Path('../')

def rep(path, old, new, count=1):
    p = ROOT / path
    text = p.read_text()
    assert text.count(old) == count, (path, text.count(old), old[:120])
    p.write_text(text.replace(old, new, count))

for name in ['character_q1_weapon_fit.ts', 'weapon_infusion_vfx.ts', 'character_q1_clip_retarget.ts']:
    shutil.copyfile(HOST / 'src/highfly' / name, ROOT / 'src/highfly' / name)

rep('src/render/characters/manifest.ts', '      ...base,\n      url,', '      ...base,\n      url,\n      authoredAtlas: true,\n      tintStrength: 0,')
rep('src/render/characters/assets.ts', "  payload.userData[HELD_PROP_TAG] = true;", "  payload.userData[HELD_PROP_TAG] = true;\n  payload.userData.q1PropUrl = att.url;\n  payload.userData.q1PropBone = att.bone;")
p = ROOT / 'src/render/characters/assets.ts'
p.write_text("import { retargetQ1Clip } from '../../highfly/character_q1_clip_retarget';\n" + p.read_text())
rep('src/render/characters/assets.ts', '''  for (const url of def.animUrls ?? []) {
    for (const clip of resolvedGltf(url).animations) clips.set(clip.name, clip);
  }''', '''  for (const url of def.animUrls ?? []) {
    const donor = resolvedGltf(url);
    for (const clip of donor.animations) {
      clips.set(clip.name, key.endsWith('_qmale') || key.endsWith('_qfemale')
        ? retargetQ1Clip(clip, donor.scene, gltf.scene) : clip);
    }
  }''')

visual = 'src/render/characters/visual.ts'
p = ROOT / visual
text = p.read_text()
p.write_text("import { fitQ1Weapons } from '../../highfly/character_q1_weapon_fit';\n" + text)
rep(visual, """    const highflyQ0HeldScale =
      key.endsWith('_qmale') ? 0.74 : key.endsWith('_qfemale') ? 0.70 : 1;
    if (highflyQ0HeldScale !== 1) {
      this.model.traverse((o) => {
        if (o.userData?.heldPropHolder === true) o.scale.multiplyScalar(highflyQ0HeldScale);
      });
    }""", "    fitQ1Weapons(this.model, key, weaponItemId, offhandItemId);")
rep(visual, '    this.rebuildCasters();\n    this.applyVisualMaterials();\n    if (this.weaponAuraSanguine)', '    fitQ1Weapons(this.model, this.key, this.weaponItemId, this.offhandItemId);\n    this.rebuildCasters();\n    this.applyVisualMaterials();\n    if (this.weaponAuraSanguine)')
rep(visual, '  private finishWeaponAttach(payloads: THREE.Object3D[]): void {', '  private finishWeaponAttach(payloads: THREE.Object3D[]): void {\n    fitQ1Weapons(this.model, this.key, this.weaponItemId, this.offhandItemId);')

renderer = 'src/render/renderer.ts'
p = ROOT / renderer
p.write_text("import { paintWeaponInfusion, weaponInfusionColor } from '../highfly/weapon_infusion_vfx';\n" + p.read_text())
rep(renderer, '    this.riftDeathZoneVisuals?.handleEvent(ev);', '    this.riftDeathZoneVisuals?.handleEvent(ev);\n    if (ev.type === \'damage\') paintWeaponInfusion(ev, this.sim.entities.get(ev.sourceId), this.vfx);')
rep(renderer, '      v.visual.setWeaponAura(weaponAura ? weaponAura.color : null, weaponAura?.tip ?? false);', '      const q1InfusionColor = weaponInfusionColor(e);\n      v.visual.setWeaponAura(q1InfusionColor ?? (weaponAura ? weaponAura.color : null), q1InfusionColor !== null ? false : (weaponAura?.tip ?? false));')
print('HIGHFLY_CHARACTER_Q1_RENDER=1')
