import fs from 'node:fs';
import { chromium } from 'playwright';

const BASE = 'http://127.0.0.1:4176/u6-mobile-runtime-probe/';
const browser = await chromium.launch({ channel: 'chrome', headless: true });
const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });

const pageErrors = [];
const consoleErrors = [];
page.on('pageerror', (err) => pageErrors.push(String(err)));
page.on('console', (msg) => {
  if (msg.type() === 'error') consoleErrors.push(msg.text());
});

async function openLineage(lineage) {
  await page.goto(BASE + '?skilllab=1&labclass=rogue&lablineage=' + lineage, {
    waitUntil: 'domcontentloaded',
    timeout: 60000,
  });
  await page.waitForFunction(
    () => Boolean(window.__game?.sim?.player && window.__highflySkillLab),
    null,
    { timeout: 90000 },
  );
  await page.waitForFunction(
    () => document.querySelector('#hf-lab-status')?.textContent?.startsWith('LISTO'),
    null,
    { timeout: 20000 },
  );
}

async function ids() {
  return page.evaluate(() => ({
    base: document.querySelector('#hf-lab-base')?.getAttribute('data-ability'),
    evo: document.querySelector('#hf-lab-evo')?.getAttribute('data-ability'),
    mutation: document.querySelector('#hf-lab-mutation')?.getAttribute('data-ability'),
    lineage: window.__highflySkillLab?.activeLineage,
  }));
}

// ---------------------------------------------------------------------------
// R2A — Remate / Remate Cruel / Ultimo Susurro
// ---------------------------------------------------------------------------
await openLineage('eviscerate');
const remateIds = await ids();
const remate = await page.evaluate((abilityIds) => {
  const sim = window.__game.sim;
  const lab = window.__highflySkillLab;
  const p = sim.player;
  const out = [];
  for (const abilityId of abilityIds) {
    lab.reset();
    const target = lab.stageDummy();
    p.comboPoints = 5;
    p.resource = p.maxResource;
    p.gcdRemaining = 0;
    p.cooldowns.delete(abilityId);
    sim.events = [];
    const hp0 = target.hp;
    const energy0 = p.resource;
    sim.castAbility(abilityId, p.id, target.id);
    out.push({
      abilityId,
      hp0,
      hp1: target.hp,
      combo: p.comboPoints,
      energy0,
      energy1: p.resource,
      errors: sim.events.filter((e) => e.type === 'error').map((e) => e.text),
    });
  }
  return out;
}, [remateIds.base, remateIds.evo, remateIds.mutation]);

await page.screenshot({ path: '../skill-lab2-r2-remate.png', fullPage: true });

// ---------------------------------------------------------------------------
// R2B — Desvanecer / Desvanecer Sombrio / Vacio Absoluto
// ---------------------------------------------------------------------------
await openLineage('vanish');
const vanishIds = await ids();
const vanish = await page.evaluate((abilityIds) => {
  const sim = window.__game.sim;
  const lab = window.__highflySkillLab;
  const p = sim.player;
  const out = [];
  for (const abilityId of abilityIds) {
    lab.reset();
    const target = lab.stageDummy();
    p.auras = p.auras.filter((a) => a.kind !== 'stealth');
    p.stealthed = false;
    p.inCombat = true;
    p.resource = p.maxResource;
    p.gcdRemaining = 0;
    p.cooldowns.delete(abilityId);
    target.inCombat = true;
    target.aggroTargetId = p.id;
    sim.events = [];
    sim.castAbility(abilityId, p.id);
    out.push({
      abilityId,
      stealthAura: p.auras.find((a) => a.id === abilityId && a.kind === 'stealth')
        ? true
        : false,
      inCombat: p.inCombat,
      targetAggro: target.aggroTargetId ?? null,
      errors: sim.events.filter((e) => e.type === 'error').map((e) => e.text),
    });
  }
  return out;
}, [vanishIds.base, vanishIds.evo, vanishIds.mutation]);

await page.screenshot({ path: '../skill-lab2-r2-vanish.png', fullPage: true });

// ---------------------------------------------------------------------------
// R2C — Paso Sombrio / Paso Umbrio / Paso del Abismo
// ---------------------------------------------------------------------------
await openLineage('shadowstep');
const stepIds = await ids();
const step = await page.evaluate((abilityIds) => {
  const sim = window.__game.sim;
  const lab = window.__highflySkillLab;
  const p = sim.player;
  const out = [];
  for (const abilityId of abilityIds) {
    lab.reset();
    const target = lab.stageDummy();
    p.auras = p.auras.filter((a) => a.kind !== 'stealth');
    p.auras.push({
      id:'hf_skill_lab_stealth',
      name:'Duskveil',
      kind:'stealth',
      value:0.5,
      remaining:3600,
      duration:3600,
      sourceId:p.id,
      school:'physical',
    });
    p.stealthed = true;
    p.resource = p.maxResource;
    p.gcdRemaining = 0;
    p.cooldowns.delete(abilityId);

    const facing = p.facing;
    target.pos.x = p.pos.x + Math.sin(facing) * 10;
    target.pos.z = p.pos.z + Math.cos(facing) * 10;
    target.pos.y = p.pos.y;
    if (target.prevPos) {
      target.prevPos.x = target.pos.x;
      target.prevPos.y = target.pos.y;
      target.prevPos.z = target.pos.z;
    }
    p.targetId = target.id;
    sim.events = [];
    const d0 = Math.hypot(target.pos.x - p.pos.x, target.pos.z - p.pos.z);
    sim.castAbility(abilityId, p.id, target.id);
    const d1 = Math.hypot(target.pos.x - p.pos.x, target.pos.z - p.pos.z);
    out.push({
      abilityId,
      d0,
      d1,
      stealth: p.auras.some((a) => a.kind === 'stealth'),
      blinkCue: sim.events.some(
        (e) => e.type === 'spellfx' && e.fx === 'blinkStep' && e.ability === abilityId,
      ),
      errors: sim.events.filter((e) => e.type === 'error').map((e) => e.text),
    });
  }
  return out;
}, [stepIds.base, stepIds.evo, stepIds.mutation]);

await page.screenshot({ path: '../skill-lab2-r2-shadowstep.png', fullPage: true });

const entityCount = await page.evaluate(() => window.__game.sim.entities.size);
await page.waitForTimeout(1800);
const entityCountAfter = await page.evaluate(() => window.__game.sim.entities.size);

const criticalErrors = [...new Set([...pageErrors, ...consoleErrors])].filter((message) => {
  if (/character visual unavailable, skipping view/i.test(message)) return false;
  if (/THREE\.GLTFLoader: Couldn't load texture blob:/i.test(message)) return false;
  if (/Failed to load resource:.*(?:404|502)/i.test(message)) return false;
  return /TypeError|ReferenceError|SyntaxError|RangeError|WebGL.*Context Lost/i.test(message);
});

const rematePassed =
  remateIds.base === 'eviscerate' &&
  remateIds.evo === 'hf_cruel_finish_01' &&
  remateIds.mutation === 'hf_last_whisper_01' &&
  remate.every(
    (r) =>
      r.hp1 < r.hp0 &&
      r.combo === 0 &&
      r.energy1 < r.energy0 &&
      r.errors.length === 0,
  );

const vanishPassed =
  vanishIds.base === 'vanish' &&
  vanishIds.evo === 'hf_shadow_vanish_01' &&
  vanishIds.mutation === 'hf_absolute_void_01' &&
  vanish.every(
    (r) =>
      r.stealthAura &&
      r.inCombat === false &&
      r.errors.length === 0,
  );

const stepPassed =
  stepIds.base === 'shadowstep' &&
  stepIds.evo === 'hf_umbral_step_01' &&
  stepIds.mutation === 'hf_abyss_step_01' &&
  step.every(
    (r) =>
      r.d0 > 8 &&
      r.d1 <= 1.7 &&
      r.stealth &&
      r.blinkCue &&
      r.errors.length === 0,
  );

const passed =
  rematePassed &&
  vanishPassed &&
  stepPassed &&
  entityCountAfter === entityCount &&
  criticalErrors.length === 0;

const report = {
  remateIds,
  remate,
  vanishIds,
  vanish,
  stepIds,
  step,
  entityCount,
  entityCountAfter,
  gates: { rematePassed, vanishPassed, stepPassed },
  criticalErrors,
  passed,
};

fs.writeFileSync('../skill-lab2-rogue-pack-r2-report.json', JSON.stringify(report, null, 2));
console.log('HIGHFLY_SKILL_LAB2_ROGUE_PACK_R2_REPORT');
console.log(JSON.stringify(report, null, 2));

await browser.close();
if (!passed) process.exitCode = 2;
