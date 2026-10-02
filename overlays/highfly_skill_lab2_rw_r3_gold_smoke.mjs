import fs from 'node:fs';
import { chromium } from 'playwright';

const BASE = 'http://127.0.0.1:4177/u6-mobile-runtime-probe/';
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
    { timeout: 25000 },
  );
}

async function ids() {
  return page.evaluate(() => ({
    base: document.querySelector('#hf-lab-base')?.getAttribute('data-ability'),
    evo: document.querySelector('#hf-lab-evo')?.getAttribute('data-ability'),
    mutation: document.querySelector('#hf-lab-mutation')?.getAttribute('data-ability'),
    lineage: window.__highflySkillLab?.activeLineage,
    activeClass: window.__highflySkillLab?.activeClass,
  }));
}

await openLineage('reaping');
const reapingIds = await ids();
const reaping = await page.evaluate((abilityIds) => {
  const sim = window.__game.sim;
  const lab = window.__highflySkillLab;
  const p = sim.player;
  const equipmentBefore = JSON.stringify(sim.meta(p.id)?.equipment);
  const out = [];
  for (const abilityId of abilityIds) {
    lab.reset();
    const undead = lab.stageRogueHeritageUndead();
    const target = lab.stageDummy();
    p.resource = p.maxResource;
    p.gcdRemaining = 0;
    p.cooldowns.delete(abilityId);
    sim.events = [];
    const hp0 = target.hp;
    const energy0 = p.resource;
    sim.castAbility(abilityId, p.id, target.id);
    out.push({
      abilityId,
      undeadOwned: Boolean(undead && undead.ownerId === p.id),
      hp0,
      hp1: target.hp,
      energy0,
      energy1: p.resource,
      fragments: p.auras.some((a) => a.kind === 'soul_fragments'),
      equipmentSame: JSON.stringify(sim.meta(p.id)?.equipment) === equipmentBefore,
      cls: sim.meta(p.id)?.cls,
      resourceType: p.resourceType,
      errors: sim.events.filter((e) => e.type === 'error').map((e) => e.text),
    });
  }
  return out;
}, [reapingIds.base, reapingIds.evo, reapingIds.mutation]);
await page.screenshot({ path: '../skill-lab2-rw-r3-reaping.png', fullPage: true });

await openLineage('evil_eye');
const eyeIds = await ids();
const eye = await page.evaluate((abilityIds) => {
  const sim = window.__game.sim;
  const lab = window.__highflySkillLab;
  const p = sim.player;
  const out = [];
  for (const abilityId of abilityIds) {
    lab.reset();
    for (const entity of sim.entities.values()) {
      entity.auras = entity.auras.filter(
        (a) => !(a.sourceId === p.id && (a.kind === 'affliction_eye' || a.kind === 'affliction_eye_secondary')),
      );
    }
    p.auras = p.auras.filter((a) => a.kind !== 'affliction_doom');
    const target = lab.stageDummy();
    p.resource = p.maxResource;
    p.gcdRemaining = 0;
    p.cooldowns.delete(abilityId);
    sim.events = [];
    const hp0 = target.hp;
    const energy0 = p.resource;
    sim.castAbility(abilityId, p.id, target.id);
    const mark = target.auras.find((a) => a.kind === 'affliction_eye' && a.sourceId === p.id);
    out.push({
      abilityId,
      hp0,
      hp1: target.hp,
      energy0,
      energy1: p.resource,
      mark: Boolean(mark),
      doom: p.auras.some((a) => a.kind === 'affliction_doom'),
      cls: sim.meta(p.id)?.cls,
      resourceType: p.resourceType,
      errors: sim.events.filter((e) => e.type === 'error').map((e) => e.text),
    });
  }
  return out;
}, [eyeIds.base, eyeIds.evo, eyeIds.mutation]);
await page.screenshot({ path: '../skill-lab2-rw-r3-eye.png', fullPage: true });

await openLineage('umbral_anchor');
const anchorIds = await ids();
const anchor = await page.evaluate((abilityIds) => {
  const sim = window.__game.sim;
  const lab = window.__highflySkillLab;
  const p = sim.player;
  const out = [];
  for (const abilityId of abilityIds) {
    lab.reset();
    p.auras = p.auras.filter((a) => a.kind !== 'warlock_anchor');
    p.cooldowns.clear();
    p.resource = p.maxResource;
    p.gcdRemaining = 0;
    sim.events = [];
    const origin = { ...p.pos };
    const energy0 = p.resource;
    sim.castAbility(abilityId, p.id);
    const placed = p.auras.some((a) => a.kind === 'warlock_anchor' && a.sourceId === p.id);
    const placeCooldown = p.cooldowns.get(abilityId) ?? 0;
    p.gcdRemaining = 0;
    p.resource = p.maxResource;
    p.pos = { ...p.pos, x: p.pos.x + 20 };
    p.prevPos = { ...p.pos };
    sim.ctx?.rebucket?.(p);
    if (!sim.ctx?.rebucket) {
      // Sim keeps the authoritative rebucket helper private in source tests;
      // browser placement at this distance remains valid even without rebucket.
    }
    const energy1 = p.resource;
    sim.castAbility(abilityId, p.id);
    const returned = Math.hypot(p.pos.x - origin.x, p.pos.z - origin.z) < 0.1;
    out.push({
      abilityId,
      placed,
      placeCooldown,
      energy0,
      energyAfterPlace: energy0 - 25,
      energyBeforeRecall: energy1,
      energyAfterRecall: p.resource,
      returned,
      anchorRemaining: p.auras.some((a) => a.kind === 'warlock_anchor' && a.sourceId === p.id),
      recallCooldown: p.cooldowns.get(abilityId) ?? 0,
      cls: sim.meta(p.id)?.cls,
      resourceType: p.resourceType,
      errors: sim.events.filter((e) => e.type === 'error').map((e) => e.text),
    });
  }
  return out;
}, [anchorIds.base, anchorIds.evo, anchorIds.mutation]);
await page.screenshot({ path: '../skill-lab2-rw-r3-anchor.png', fullPage: true });

const criticalErrors = [...new Set([...pageErrors, ...consoleErrors])].filter((message) => {
  if (/character visual unavailable, skipping view/i.test(message)) return false;
  if (/THREE\.GLTFLoader: Couldn't load texture blob:/i.test(message)) return false;
  if (/Failed to load resource:.*(?:404|502)/i.test(message)) return false;
  return /TypeError|ReferenceError|SyntaxError|RangeError|WebGL.*Context Lost/i.test(message);
});

const reapingPassed =
  reapingIds.activeClass === 'rogue' &&
  reapingIds.base === 'hf_rw_reaping_command_01' &&
  reapingIds.evo === 'hf_rw_unholy_dominion_01' &&
  reapingIds.mutation === 'hf_rw_march_of_dead_01' &&
  reaping.every((r) =>
    r.undeadOwned &&
    r.hp1 < r.hp0 &&
    r.energy1 === r.energy0 - 45 &&
    !r.fragments &&
    r.equipmentSame &&
    r.cls === 'rogue' &&
    r.resourceType === 'energy' &&
    r.errors.length === 0
  );

const eyePassed =
  eyeIds.activeClass === 'rogue' &&
  eyeIds.base === 'hf_rw_evil_eye_01' &&
  eyeIds.evo === 'hf_rw_abyss_gaze_01' &&
  eyeIds.mutation === 'hf_rw_eye_of_end_01' &&
  eye.every((r) =>
    r.hp1 === r.hp0 &&
    r.energy1 === r.energy0 - 15 &&
    r.mark &&
    !r.doom &&
    r.cls === 'rogue' &&
    r.resourceType === 'energy' &&
    r.errors.length === 0
  );

const anchorPassed =
  anchorIds.activeClass === 'rogue' &&
  anchorIds.base === 'hf_rw_umbral_anchor_01' &&
  anchorIds.evo === 'hf_rw_umbral_return_01' &&
  anchorIds.mutation === 'hf_rw_point_no_return_01' &&
  anchor.every((r) =>
    r.placed &&
    r.placeCooldown === 0 &&
    r.returned &&
    !r.anchorRemaining &&
    r.recallCooldown > 44 &&
    r.energyAfterRecall === r.energyBeforeRecall - 25 &&
    r.cls === 'rogue' &&
    r.resourceType === 'energy' &&
    r.errors.length === 0
  );

const passed = reapingPassed && eyePassed && anchorPassed && criticalErrors.length === 0;
const report = {
  reapingIds, reaping,
  eyeIds, eye,
  anchorIds, anchor,
  gates: { reapingPassed, eyePassed, anchorPassed },
  criticalErrors,
  passed,
};
fs.writeFileSync('../skill-lab2-rw-r3-gold-report.json', JSON.stringify(report, null, 2));
console.log('HIGHFLY_SKILL_LAB2_RW_R3_GOLD_REPORT');
console.log(JSON.stringify(report, null, 2));

await browser.close();
if (!passed) process.exitCode = 2;
