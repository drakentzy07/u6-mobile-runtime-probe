import fs from 'node:fs';
import { chromium } from 'playwright';

const BASE = 'http://127.0.0.1:4175/u6-mobile-runtime-probe/';
const ABILITY = 'hf_eclipse_mortal_01';
const browser = await chromium.launch({ channel: 'chrome', headless: true });
const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });

const pageErrors = [];
const consoleErrors = [];
page.on('pageerror', (err) => pageErrors.push(String(err)));
page.on('console', (msg) => {
  if (msg.type() === 'error') consoleErrors.push(msg.text());
});

await page.goto(BASE + '?skilllab=1&labclass=rogue', {
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

const report = await page.evaluate((abilityId) => {
  const sim = window.__game.sim;
  const lab = window.__highflySkillLab;
  const p = sim.player;

  function ready() {
    p.cooldowns.delete(abilityId);
    p.gcdRemaining = 0;
    p.resource = p.maxResource;
    p.castingAbility = null;
    if (!p.auras.some((a) => a.kind === 'stealth')) {
      p.auras.push({
        id: 'hf_skill_lab_stealth',
        name: 'Duskveil',
        kind: 'stealth',
        value: 0.5,
        remaining: 3600,
        duration: 3600,
        sourceId: p.id,
        school: 'physical',
      });
    }
  }

  function faceTarget(t) {
    p.facing = Math.atan2(t.pos.x - p.pos.x, t.pos.z - p.pos.z);
  }

  function syncPrev() {
    if (!p.prevPos) return;
    p.prevPos.x = p.pos.x;
    p.prevPos.y = p.pos.y;
    p.prevPos.z = p.pos.z;
  }

  // A) start deliberately IN FRONT of the dummy. EVO/MUTATION must use the
  // swept micro-step and finish genuinely behind before Claude accepts Ambush.
  lab.reset();
  const frontTarget = lab.stageDummy();
  ready();
  const fsin = Math.sin(frontTarget.facing);
  const fcos = Math.cos(frontTarget.facing);
  p.pos.x = frontTarget.pos.x + fsin * 2.5;
  p.pos.z = frontTarget.pos.z + fcos * 2.5;
  syncPrev();
  faceTarget(frontTarget);
  const frontBefore = {
    hp: frontTarget.hp,
    px: p.pos.x,
    pz: p.pos.z,
    tx: frontTarget.pos.x,
    tz: frontTarget.pos.z,
    tf: frontTarget.facing,
  };
  sim.events = [];
  sim.castAbility(abilityId, p.id, frontTarget.id);
  const behindDiff = Math.abs(
    Math.atan2(
      Math.sin(Math.atan2(p.pos.x - frontTarget.pos.x, p.pos.z - frontTarget.pos.z) - frontTarget.facing),
      Math.cos(Math.atan2(p.pos.x - frontTarget.pos.x, p.pos.z - frontTarget.pos.z) - frontTarget.facing),
    ),
  );
  const frontAfter = {
    hp: frontTarget.hp,
    px: p.pos.x,
    pz: p.pos.z,
    behindDiff,
    dist: Math.hypot(p.pos.x - frontTarget.pos.x, p.pos.z - frontTarget.pos.z),
    blinkCue: sim.events.some((e) => e.type === 'spellfx' && e.fx === 'blinkStep' && e.ability === abilityId),
    errors: sim.events.filter((e) => e.type === 'error').map((e) => ({ text: e.text, reason: e.reason })),
  };

  // B) out-of-range must fail BEFORE reposition and leave the Rogue untouched.
  lab.reset();
  const farTarget = lab.stageDummy();
  ready();
  p.pos.x = farTarget.pos.x + Math.sin(farTarget.facing) * 20;
  p.pos.z = farTarget.pos.z + Math.cos(farTarget.facing) * 20;
  syncPrev();
  faceTarget(farTarget);
  const farBefore = { x: p.pos.x, z: p.pos.z, energy: p.resource, hp: farTarget.hp };
  sim.events = [];
  sim.castAbility(abilityId, p.id, farTarget.id);
  const farAfter = {
    x: p.pos.x,
    z: p.pos.z,
    energy: p.resource,
    hp: farTarget.hp,
    errors: sim.events.filter((e) => e.type === 'error').map((e) => ({ text: e.text, reason: e.reason })),
    blinkCue: sim.events.some((e) => e.type === 'spellfx' && e.fx === 'blinkStep'),
  };

  // C) dead target must fail before any movement or resource spend.
  lab.reset();
  const deadTarget = lab.stageDummy();
  ready();
  const deadBefore = { x: p.pos.x, z: p.pos.z, energy: p.resource, hp: deadTarget.hp };
  deadTarget.dead = true;
  sim.events = [];
  sim.castAbility(abilityId, p.id, deadTarget.id);
  const deadAfter = {
    x: p.pos.x,
    z: p.pos.z,
    energy: p.resource,
    hp: deadTarget.hp,
    errors: sim.events.filter((e) => e.type === 'error').map((e) => ({ text: e.text, reason: e.reason })),
    blinkCue: sim.events.some((e) => e.type === 'spellfx' && e.fx === 'blinkStep'),
  };
  deadTarget.dead = false;

  // D) repeated lab replay: no shadow entities, no stuck scripted movement,
  // Eclipse mark refreshes instead of multiplying.
  lab.reset();
  const entityCountBeforeSpam = sim.entities.size;
  for (let i = 0; i < 5; i++) lab.cast(abilityId);
  const spamTarget = p.targetId != null ? sim.entities.get(p.targetId) : null;
  const spam = {
    entityCountBefore: entityCountBeforeSpam,
    entityCountAfter: sim.entities.size,
    markCount: spamTarget?.auras?.filter?.((a) => a.id === 'hf_eclipse_mark').length ?? 0,
    leap: p.leap ?? null,
    chargeTargetId: p.chargeTargetId ?? null,
    chargePathLength: p.chargePath?.length ?? 0,
  };

  // E) despawned explicit target: no movement, no cue, no damage side effects.
  lab.reset();
  const despawnTarget = lab.stageDummy();
  ready();
  const despawnBefore = { x: p.pos.x, z: p.pos.z, energy: p.resource };
  sim.entities.delete(despawnTarget.id);
  sim.events = [];
  sim.castAbility(abilityId, p.id, despawnTarget.id);
  const despawnAfter = {
    x: p.pos.x,
    z: p.pos.z,
    energy: p.resource,
    errors: sim.events.filter((e) => e.type === 'error').map((e) => ({ text: e.text, reason: e.reason })),
    blinkCue: sim.events.some((e) => e.type === 'spellfx' && e.fx === 'blinkStep'),
  };
  sim.entities.set(despawnTarget.id, despawnTarget);

  return { frontBefore, frontAfter, farBefore, farAfter, deadBefore, deadAfter, spam, despawnBefore, despawnAfter };
}, ABILITY);

await page.waitForTimeout(2200);
const cleanup = await page.evaluate(() => {
  const p = window.__game.sim.player;
  return {
    leap: p.leap ?? null,
    chargeTargetId: p.chargeTargetId ?? null,
    chargePathLength: p.chargePath?.length ?? 0,
  };
});

await page.screenshot({ path: '../skill-lab2-run1c-rogue-gold.png', fullPage: true });

const criticalErrors = [...new Set([...pageErrors, ...consoleErrors])].filter((message) => {
  if (/character visual unavailable, skipping view/i.test(message)) return false;
  if (/THREE\.GLTFLoader: Couldn't load texture blob:/i.test(message)) return false;
  if (/Failed to load resource:.*(?:404|502)/i.test(message)) return false;
  return /TypeError|ReferenceError|SyntaxError|RangeError|WebGL.*Context Lost/i.test(message);
});

const frontPassed =
  report.frontAfter.hp < report.frontBefore.hp &&
  report.frontAfter.behindDiff >= Math.PI / 2 &&
  report.frontAfter.dist >= 1 &&
  report.frontAfter.blinkCue &&
  report.frontAfter.errors.length === 0;

const farPassed =
  Math.abs(report.farAfter.x - report.farBefore.x) < 0.001 &&
  Math.abs(report.farAfter.z - report.farBefore.z) < 0.001 &&
  report.farAfter.energy === report.farBefore.energy &&
  report.farAfter.hp === report.farBefore.hp &&
  report.farAfter.errors.some((e) => e.text === 'Out of range.') &&
  !report.farAfter.blinkCue;

const deadPassed =
  Math.abs(report.deadAfter.x - report.deadBefore.x) < 0.001 &&
  Math.abs(report.deadAfter.z - report.deadBefore.z) < 0.001 &&
  report.deadAfter.energy === report.deadBefore.energy &&
  report.deadAfter.hp === report.deadBefore.hp &&
  report.deadAfter.errors.some((e) => e.reason === 'target_dead') &&
  !report.deadAfter.blinkCue;

const spamPassed =
  report.spam.entityCountAfter === report.spam.entityCountBefore &&
  report.spam.markCount <= 1 &&
  report.spam.leap == null &&
  report.spam.chargeTargetId == null &&
  report.spam.chargePathLength === 0;

const despawnPassed =
  Math.abs(report.despawnAfter.x - report.despawnBefore.x) < 0.001 &&
  Math.abs(report.despawnAfter.z - report.despawnBefore.z) < 0.001 &&
  report.despawnAfter.energy === report.despawnBefore.energy &&
  report.despawnAfter.errors.some((e) => e.text === 'You have no target.') &&
  !report.despawnAfter.blinkCue;

const cleanupPassed =
  cleanup.leap == null && cleanup.chargeTargetId == null && cleanup.chargePathLength === 0;

const passed =
  frontPassed &&
  farPassed &&
  deadPassed &&
  spamPassed &&
  despawnPassed &&
  cleanupPassed &&
  criticalErrors.length === 0;

const finalReport = {
  ...report,
  cleanup,
  gates: { frontPassed, farPassed, deadPassed, spamPassed, despawnPassed, cleanupPassed },
  criticalErrors,
  passed,
};
fs.writeFileSync('../skill-lab2-run1c-rogue-gold-report.json', JSON.stringify(finalReport, null, 2));
console.log('HIGHFLY_SKILL_LAB2_RUN1C_ROGUE_GOLD_REPORT');
console.log(JSON.stringify(finalReport, null, 2));

await browser.close();
if (!passed) process.exitCode = 2;
