import fs from 'node:fs';
import { chromium } from 'playwright';

const BASE = process.env.HF_Q1_BASE || 'http://127.0.0.1:4190/u6-mobile-runtime-probe/';
const CLASSES = ['warrior', 'paladin', 'hunter', 'rogue', 'priest', 'shaman', 'mage', 'warlock', 'druid'];
const report = { classes: [], nativeCasts: [], layouts: [], weaponBinding: null, errors: [], failures: [] };
const browser = await chromium.launch({ headless: true, ...(process.env.HF_Q1_CHROME ? { executablePath: process.env.HF_Q1_CHROME } : {}), args: ['--enable-webgl', '--use-gl=angle', '--use-angle=swiftshader'] });
const context = await browser.newContext({ viewport: { width: 1600, height: 900 } });
const page = await context.newPage();
page.on('pageerror', (e) => report.errors.push(String(e)));
const require = (condition, message) => { if (!condition) throw new Error(message); };
async function step(name, fn) {
  try { await fn(); console.log('Q1_PASS ' + name); }
  catch (e) { report.failures.push({ name, error: String(e) }); console.error('Q1_FAIL ' + name + ': ' + e); }
}
async function ready(targetPage, cls) {
  await targetPage.goto(BASE + '?affinitylab=1&labclass=' + cls + '&q1test=1', { waitUntil: 'domcontentloaded', timeout: 60000 });
  await targetPage.waitForFunction((want) => {
    const g = window.__game, p = g?.sim?.player;
    return Boolean(p && window.__highflyAffinityLab?.nativeHotbar && window.__highflyCharacterQ0 && g.sim.meta(p.id)?.cls === want && document.body.classList.contains('hf-q1-active'));
  }, cls, { timeout: 180000 });
  await targetPage.waitForFunction(() => window.__highflyAffinityLab.nativeHotbar().length === 10, null, { timeout: 20000 });
  await targetPage.evaluate(() => {
    window.__highflyAffinityLab.resetLab();
    const sim = window.__game.sim, p = sim.player;
    p.gm = true;
    p.inCombat = true;
    p.combatTimer = 0;
    p.autoAttack = false;
    const t = sim.entities.get(p.targetId);
    if (t) { t.templateId = 'training_dummy'; t.idleStationary = true; t.aggroTargetId = null; }
    for (const e of sim.entities.values()) if (e.kind === 'mob' && e.id !== p.targetId) { e.aggroTargetId = null; e.inCombat = false; }
  });
}
async function selectBody(targetPage, cls, body) {
  await targetPage.evaluate((b) => {
    const g = window.__game, sim = g.sim, p = sim.player;
    const core = () => JSON.stringify({ cls: sim.meta(p.id)?.cls, spec: p.specId, id: p.id, level: p.level, hp: p.hp, maxHp: p.maxHp, resource: p.resource, maxResource: p.maxResource, targetId: p.targetId, equipment: sim.equipment, cooldowns: [...p.cooldowns.entries()].sort(), pos: p.pos, facing: p.facing, camera: g.renderer.camera?.position.toArray() });
    const before = core();
    window.__highflyCharacterQ0.selectBody(b);
    window.__q1AtomicSwap = { before, after: core() };
  }, body);
  const key = 'player_' + cls + (body === 'claude' ? '' : '_' + body);
  // A key is assigned before the asynchronous model finishes loading. Require
  // a real skinned model and its native clips before accepting the replacement.
  await targetPage.waitForFunction((want) => {
    const g = window.__game, p = g?.sim?.player, v = p ? g.renderer?.views?.get(p.id) : null;
    const visual = v?.visual;
    let skinned = 0;
    visual?.model?.traverse?.((o) => { if (o.isSkinnedMesh && o.geometry?.getAttribute('position')?.count > 0) skinned++; });
    return v?.visualKey === want && skinned > 0 && visual?.actions instanceof Map && visual.actions.size > 0;
  }, key, { timeout: 30000 });
}
async function inspect(targetPage) {
  return targetPage.evaluate(() => {
    const g = window.__game, sim = g.sim, p = sim.player, m = sim.meta(p.id), view = g.renderer.views.get(p.id), v = view?.visual;
    let held = 0, skinned = 0, verts = 0, textured = 0;
    const fits = [];
    v?.root?.traverse?.((o) => {
      if (o?.userData?.heldPropHolder === true) held++;
      if (o.isSkinnedMesh) skinned++;
      const pos = o?.geometry?.getAttribute?.('position');
      if (pos) verts += pos.count;
      for (const material of (Array.isArray(o.material) ? o.material : o.material ? [o.material] : [])) if (material.map) textured++;
      if (o?.userData?.q1Fit) fits.push(o.userData.q1Fit);
    });
    const cam = g.renderer.camera;
    return {
      cls: m?.cls, spec: p.specId ?? m?.spec ?? null, id: p.id, level: p.level,
      maxHp: p.maxHp, resourceType: p.resourceType, maxResource: p.maxResource,
      hp: p.hp, resource: p.resource, equipment: JSON.stringify(m?.equipment ?? sim.equipment),
      cooldowns: [...p.cooldowns.entries()].sort(), targetId: p.targetId,
      pos: [p.pos.x, p.pos.y, p.pos.z], facing: p.facing,
      camera: cam ? [cam.position.x, cam.position.y, cam.position.z, cam.rotation.x, cam.rotation.y, cam.rotation.z] : null,
      key: view?.visualKey, modular: Boolean(v?.modularLook), held, skinned, verts, textured, fits,
      handR: Boolean(v?.model?.getObjectByName('handslot.r') ?? v?.model?.getObjectByName('handslotr')),
      handL: Boolean(v?.model?.getObjectByName('handslot.l') ?? v?.model?.getObjectByName('handslotl')),
      actions: v?.actions instanceof Map ? [...v.actions.keys()].sort() : [],
      nativeHotbar: window.__highflyAffinityLab.nativeHotbar(),
      atomicSwap: window.__q1AtomicSwap,
    };
  });
}
function sameCore(a, b) {
  return ['cls', 'spec', 'id', 'level', 'maxHp', 'resourceType', 'maxResource', 'equipment', 'targetId', 'facing'].every((k) => a[k] === b[k]) &&
    JSON.stringify(a.pos) === JSON.stringify(b.pos) && JSON.stringify(a.cooldowns) === JSON.stringify(b.cooldowns);
}
async function layout(targetPage, label) {
  const result = await targetPage.evaluate(() => {
    const slots = [...document.querySelectorAll('#actionbar [data-hotbar-slot]')].filter((b) => {
      const r = b.getBoundingClientRect(), s = getComputedStyle(b);
      return r.width > 0 && r.height > 0 && s.display !== 'none' && s.visibility !== 'hidden';
    });
    return {
      viewport: [innerWidth, innerHeight],
      mobileLandscape: document.body.classList.contains('mobile-touch') && innerWidth > innerHeight,
      nativeHotbar: window.__highflyAffinityLab.nativeHotbar(),
      proxies: document.querySelectorAll('.hf-aff-skill').length,
      slots: slots.map((b) => {
        const r = b.getBoundingClientRect(), s = getComputedStyle(b);
        return { slot: +b.dataset.hotbarSlot, x: r.x, y: r.y, width: r.width, height: r.height, radius: s.borderRadius, empty: b.classList.contains('empty'), icon: getComputedStyle(b.querySelector('.icon-label')).backgroundImage };
      }),
    };
  });
  report.layouts.push({ label, ...result });
  require(result.proxies === 0, label + ': proxy buttons present');
  require(result.slots.length === 10, label + ': expected 10 visible native buttons, got ' + result.slots.length);
  require(result.slots.every((s) => s.slot >= 1 && s.slot <= 10 && !s.empty && Math.abs(s.width - s.height) <= 1 && (s.radius.includes('%') || parseFloat(s.radius) >= s.width / 2 - 1) && s.icon !== 'none'), label + ': skills must be filled circles with native icons');
  require(result.slots.every((s) => s.x >= 0 && s.y >= 0 && s.x + s.width <= result.viewport[0] + 1 && s.y + s.height <= result.viewport[1] + 1), label + ': button outside viewport');
  for (let i = 0; i < result.slots.length; i++) for (let j = i + 1; j < result.slots.length; j++) {
    const a = result.slots[i], b = result.slots[j];
    require(a.x + a.width <= b.x + 1 || b.x + b.width <= a.x + 1 || a.y + a.height <= b.y + 1 || b.y + b.height <= a.y + 1, label + ': native buttons overlap');
  }
  if (result.mobileLandscape) {
    const ordered = [...result.slots].sort((a, b) => a.slot - b.slot);
    const inner = ordered.slice(0, 5), outer = ordered.slice(5, 10);
    const meanX = (rows) => rows.reduce((n, row) => n + row.x, 0) / rows.length;
    require(meanX(outer) < meanX(inner), label + ': S6-S10 must form the outer/left crescent');
    require(inner.every((row, i) => i === 0 || row.y < inner[i - 1].y), label + ': S1-S5 inner crescent order broken');
    require(outer.every((row, i) => i === 0 || row.y < outer[i - 1].y), label + ': S6-S10 outer crescent order broken');
  }
  require(result.nativeHotbar.every((a) => a.type === 'ability' && !a.id.startsWith('hf_aff_')), label + ': hotbar must use native BASE IDs');
}

for (const cls of CLASSES) await step('class-model-parity:' + cls, async () => {
  await ready(page, cls);
  await selectBody(page, cls, 'claude');
  const original = await inspect(page), replacements = [];
  for (const body of ['qmale', 'qfemale']) {
    await selectBody(page, cls, body);
    const candidate = await inspect(page);
    replacements.push({ body, ...candidate });
    require(sameCore(original, candidate) && candidate.atomicSwap.before === candidate.atomicSwap.after, cls + '/' + body + ': visual swap changed core state');
    require(JSON.stringify(original.actions) === JSON.stringify(candidate.actions), cls + '/' + body + ': native animation coverage changed');
    require(candidate.handR && candidate.handL && !candidate.modular && candidate.skinned > 0 && candidate.textured > 0, cls + '/' + body + ': loaded rig/material/socket missing');
    require(candidate.held === original.held, cls + '/' + body + ': held-prop count changed');
    if (candidate.held > 0) {
      const caps = { sword: .37, dagger: .18, axe: .30, shield: .28, spear: .66, bow: .46, staff: .64, other: .30 };
      require(candidate.fits.length === candidate.held, cls + '/' + body + ': not every held prop received BODY x WEAPON fit');
      require(candidate.fits.every((fit) => fit.bodyRatio <= (caps[fit.family] ?? caps.other) + .012), cls + '/' + body + ': weapon exceeds Q1.1 body-ratio cap');
    }
    if (cls === 'warrior') await page.screenshot({ path: '../character-q1-warrior-' + body + '-desktop.png' });
  }
  const row = { cls, original, replacements };
  report.classes.push(row);
  require(new Set([original.verts, ...replacements.map((c) => c.verts)]).size === 3, cls + ': bodies are not distinct meshes');
  await layout(page, cls + '-desktop');
});

await step('weapon-affinity-owned-by-equipped-weapon', async () => {
  await ready(page, 'warrior');
  const state = await page.evaluate(() => {
    const api = window.__highflyAffinityLab, sim = window.__game.sim;
    const weapon = api.weaponId(), hotbarBefore = JSON.stringify(api.nativeHotbar());
    api.selectAffinity('fire');
    const fire = api.affinity(), map = api.weaponAffinities();
    const equipmentWeapon = sim.equipment.mainhand;
    const unequipAccepted = sim.unequipItem('mainhand');
    return { weapon, equipmentWeapon, fire, map, hotbarBefore, unequipAccepted };
  });
  await page.waitForFunction(() => window.__highflyAffinityLab.affinity() === 'base', null, { timeout: 10000 });
  const unequipped = await page.evaluate(() => window.__highflyAffinityLab.affinity());
  await page.evaluate((state) => window.__game.sim.equipItem(state.weapon), state);
  await page.waitForFunction(() => window.__highflyAffinityLab.affinity() === 'fire', null, { timeout: 10000 });
  const restored = await page.evaluate(() => ({ affinity: window.__highflyAffinityLab.affinity(), weapon: window.__highflyAffinityLab.weaponId(), hotbar: JSON.stringify(window.__highflyAffinityLab.nativeHotbar()) }));
  report.weaponBinding = { ...state, unequipped, restored };
  require(Boolean(state.weapon) && state.unequipAccepted === true && state.fire === 'fire' && state.map[state.weapon] === 'fire' && restored.weapon === state.weapon && restored.affinity === 'fire' && unequipped === 'base', 'Element must follow canonical weapon ownership and restore after re-equip');
  require(state.hotbarBefore === restored.hotbar, 'Changing weapon affinity changed BASE slot IDs');
});

async function nativeLeap(targetPage, label) {
  await targetPage.evaluate(() => {
    const api = window.__highflyAffinityLab, g = window.__game;
    api.resetLab(); api.selectAffinity('fire');
    g.hud.optionsHooks?.settings?.set?.('groundReticle', true);
    g.hud.optionsHooks?.settings?.set?.('touchPreciseGroundAim', true);
    const t = g.sim.entities.get(g.sim.player.targetId);
    if (t) { t.templateId = 'training_dummy'; t.idleStationary = true; }
  });
  const before = await targetPage.evaluate(() => {
    const g = window.__game, p = g.sim.player, t = g.sim.entities.get(p.targetId);
    return { slot: window.__highflyAffinityLab.nativeHotbar().findIndex((a) => a.id === 'heroic_leap') + 1, resource: p.resource, hp: t?.hp, pos: { ...p.pos }, target: { x: t.pos.x, z: t.pos.z } };
  });
  require(before.slot > 0, label + ': missing native leap slot');
  const button = targetPage.locator('#actionbar [data-hotbar-slot="' + before.slot + '"]');
  await targetPage.evaluate((slot) => document.querySelector('#actionbar [data-hotbar-slot="' + slot + '"]')?.click(), before.slot);
  await targetPage.waitForFunction(() => window.__game.hud.isGroundAimActive(), null, { timeout: 10000 });
  const aim = await targetPage.evaluate((point) => { const h = window.__game.hud; h.updateGroundAimPoint(point); return h.groundAimReticle(); }, before.target);
  require(aim && !aim.blocked && aim.school === 'physical', label + ': native physical placement preview missing');
  await targetPage.evaluate((slot) => document.querySelector('#actionbar [data-hotbar-slot="' + slot + '"]')?.click(), before.slot);
  await targetPage.waitForFunction(() => (window.__game.sim.player.cooldowns.get('heroic_leap') ?? 0) > 0, null, { timeout: 10000 });
  await targetPage.waitForFunction(() => { const s = window.__game.sim, p = s.player, t = s.entities.get(p.targetId); return !p.leap && t.hp < t.maxHp; }, null, { timeout: 30000 });
  const after = await targetPage.evaluate(() => {
    const g = window.__game, p = g.sim.player, t = g.sim.entities.get(p.targetId);
    return { aimActive: g.hud.isGroundAimActive(), cooldown: p.cooldowns.get('heroic_leap'), resource: p.resource, targetHp: t.hp, auras: t.auras.map((a) => ({ id: a.id, kind: a.kind, value: a.value, school: a.school })), pos: { ...p.pos }, cls: g.sim.meta(p.id).cls };
  });
  report.nativeCasts.push({ label, before, aim, after });
  require(!after.aimActive && after.cooldown > 20 && after.resource === before.resource && after.targetHp < before.hp && after.cls === 'warrior', label + ': native leap cost/cooldown/impact contract failed');
  require(after.auras.some((a) => a.id.startsWith('hf_q1_weapon_fire_') && a.kind === 'dot' && a.school === 'fire'), label + ': weapon additive fire rider absent');
}
await step('native-desktop-leap', () => nativeLeap(page, 'desktop'));
await step('native-paid-rogue-skill', async () => {
  await ready(page, 'rogue');
  const before = await page.evaluate(() => {
    const g = window.__game, p = g.sim.player, t = g.sim.entities.get(p.targetId);
    p.autoAttack = false; p.resource = p.maxResource;
    t.pos.x = p.pos.x + Math.sin(p.facing) * 2; t.pos.z = p.pos.z + Math.cos(p.facing) * 2;
    const k = g.sim.known.find((k) => k.def.id === 'sinister_strike');
    return { slot: window.__highflyAffinityLab.nativeHotbar().findIndex((a) => a.id === 'sinister_strike') + 1, resource: p.resource, cost: k?.cost };
  });
  require(before.slot > 0 && before.cost > 0, 'Missing paid native rogue skill');
  await page.locator('#actionbar [data-hotbar-slot="' + before.slot + '"]').click();
  await page.waitForFunction(() => window.__game.sim.player.gcdRemaining > 0 && window.__game.sim.player.resource < window.__game.sim.player.maxResource, null, { timeout: 3000 });
  const after = await page.evaluate(() => ({ resource: window.__game.sim.player.resource, gcd: window.__game.sim.player.gcdRemaining }));
  report.nativeCasts.push({ label: 'rogue-paid', before, after });
  require(after.resource <= before.resource - before.cost + 5 && after.gcd > 0, 'Paid native button did not consume canonical cost');
});
await step('mobile-landscape-native-hud-and-leap', async () => {
  // ClaudeCraft's own headless mobile checks force the authoritative
  // body.mobile-touch signal after boot: Chromium headless does not reliably
  // expose pointer:coarse / phone media during a cold WebGL startup. Reuse the
  // already-proven game page, then switch the exact HUD/runtime signal and
  // viewport that Hud.isMobileLayout() reads.
  console.log('Q1_MOBILE_STAGE ready-warrior');
  await ready(page, 'warrior');
  console.log('Q1_MOBILE_STAGE ready-warrior-pass');

  await page.setViewportSize({ width: 900, height: 420 });
  await page.evaluate(() => {
    document.body.classList.add('mobile-touch', 'game-active');
    window.dispatchEvent(new Event('resize'));
  });
  await page.waitForFunction(() =>
    document.body.classList.contains('mobile-touch') &&
    window.innerWidth === 900 &&
    window.innerHeight === 420,
    null,
    { timeout: 10000 },
  );
  console.log('Q1_MOBILE_STAGE landscape-surface-pass');

  for (const body of ['qmale', 'qfemale']) {
    console.log('Q1_MOBILE_STAGE select-' + body);
    await selectBody(page, 'warrior', body);
    console.log('Q1_MOBILE_STAGE layout-' + body);
    await layout(page, body + '-mobile-landscape');
    await page.screenshot({ path: '../character-q1-warrior-' + body + '-mobile.png' });
    console.log('Q1_MOBILE_STAGE screenshot-' + body + '-pass');
  }

  console.log('Q1_MOBILE_STAGE leap');
  await nativeLeap(page, 'mobile-landscape');
  console.log('Q1_MOBILE_STAGE leap-pass');

  await page.setViewportSize({ width: 360, height: 800 });
  await page.evaluate(() => {
    document.body.classList.add('mobile-touch', 'game-active');
    window.dispatchEvent(new Event('resize'));
  });
  await page.waitForFunction(() =>
    document.body.classList.contains('mobile-touch') &&
    window.innerWidth === 360 &&
    window.innerHeight === 800,
    null,
    { timeout: 10000 },
  );
  console.log('Q1_MOBILE_STAGE portrait-surface-pass');
  await selectBody(page, 'warrior', 'qmale');
  await layout(page, 'qmale-mobile-portrait');
  const compact = await page.locator('#hf-affinity-hud').boundingBox();
  require(compact && compact.x >= 0 && compact.y >= 0 && compact.x + compact.width <= 361, 'Compact lab panel leaves 360px viewport');
  await page.screenshot({ path: '../character-q1-warrior-qmale-mobile-portrait.png' });
  console.log('Q1_MOBILE_STAGE portrait-pass');
});
report.passed = report.classes.length === 9 && report.failures.length === 0 && report.errors.filter((e) => /TypeError|ReferenceError|SyntaxError|RangeError/.test(e)).length === 0;
fs.writeFileSync('../character-q1-smoke-report.json', JSON.stringify(report, null, 2));
console.log('HIGHFLY_CHARACTER_Q1_SMOKE');
console.log(JSON.stringify({ passed: report.passed, classes: report.classes.length, layouts: report.layouts.length, casts: report.nativeCasts.length, failures: report.failures, errors: report.errors }, null, 2));
await browser.close();
if (!report.passed) process.exitCode = 2;
