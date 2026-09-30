import fs from 'node:fs';
import { chromium } from 'playwright';

const BASE = 'http://127.0.0.1:4173/u6-mobile-runtime-probe/';
const CASES = [
  { cls: 'warrior', base: 'heroic_leap', evo: 'hf_jump_smash_01' },
  { cls: 'hunter', base: 'frostjaw_trap', evo: 'hf_hunter_prison_01' },
  { cls: 'mage', base: 'pyroblast', evo: 'hf_phoenix_lance_01' },
  { cls: 'priest', base: 'power_word_shield', evo: 'hf_living_covenant_01' },
  { cls: 'druid', base: 'moonseed', evo: 'moonlash' },
];

const browser = await chromium.launch({ channel: 'chrome', headless: true });
const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
const pageErrors = [];
const consoleErrors = [];
page.on('pageerror', (err) => pageErrors.push(String(err)));
page.on('console', (msg) => {
  if (msg.type() === 'error') consoleErrors.push(msg.text());
});

const results = [];
for (const c of CASES) {
  await page.goto(BASE + '?skilllab=1&labclass=' + c.cls, {
    waitUntil: 'domcontentloaded',
    timeout: 60000,
  });
  await page.waitForFunction(
    () => Boolean(window.__game?.sim?.player && window.__highflySkillLab),
    null,
    { timeout: 90000 },
  );
  await page.waitForSelector('#hf-skill-lab-panel[data-active-class="' + c.cls + '"]', {
    timeout: 15000,
  });
  await page.waitForFunction(
    () => document.querySelector('#hf-lab-status')?.textContent?.startsWith('LISTO'),
    null,
    { timeout: 15000 },
  );

  const ui = await page.evaluate(() => ({
    classes: document.querySelectorAll('#hf-skill-lab-panel .hf-class').length,
    active: document.querySelector('#hf-skill-lab-panel')?.getAttribute('data-active-class'),
    base: document.querySelector('#hf-lab-base')?.getAttribute('data-ability'),
    evo: document.querySelector('#hf-lab-evo')?.getAttribute('data-ability'),
    status: document.querySelector('#hf-lab-status')?.textContent ?? '',
  }));

  const before = await page.evaluate(() => {
    const sim = window.__game.sim;
    const p = sim.player;
    const target = p.targetId != null ? sim.entities.get(p.targetId) : null;
    return {
      resource: p.resource,
      maxResource: p.maxResource,
      targetId: p.targetId ?? null,
      targetHp: target?.hp ?? null,
    };
  });

  await page.click('#hf-lab-evo');
  await page.waitForTimeout(c.cls === 'mage' ? 250 : 500);

  const after = await page.evaluate((abilityId) => {
    const sim = window.__game.sim;
    const p = sim.player;
    const target = p.targetId != null ? sim.entities.get(p.targetId) : null;
    return {
      resource: p.resource,
      maxResource: p.maxResource,
      cooldown: p.cooldowns.get(abilityId) ?? 0,
      castingAbility: p.castingAbility ?? null,
      leap: Boolean(p.leap),
      targetId: p.targetId ?? null,
      targetHp: target?.hp ?? null,
      status: document.querySelector('#hf-lab-status')?.textContent ?? '',
    };
  }, c.evo);

  const castSignal =
    c.cls === 'warrior' ? after.leap || after.cooldown > 0 :
    c.cls === 'hunter' ? after.cooldown > 0 :
    c.cls === 'mage' ? after.castingAbility === c.evo :
    c.cls === 'priest' ? after.cooldown > 0 :
    after.resource < after.maxResource;

  const passed =
    ui.classes === 5 &&
    ui.active === c.cls &&
    ui.base === c.base &&
    ui.evo === c.evo &&
    ui.status.startsWith('LISTO') &&
    after.status.startsWith('CAST') &&
    castSignal;

  results.push({ cls: c.cls, ui, before, after, passed });
  await page.screenshot({ path: '../skill-lab-' + c.cls + '.png', fullPage: true });
}

const criticalErrors = [...new Set([...pageErrors, ...consoleErrors])].filter((message) => {
  if (/character visual unavailable, skipping view/i.test(message)) return false;
  if (/THREE\.GLTFLoader: Couldn't load texture blob:/i.test(message)) return false;
  if (/Failed to load resource:.*(?:404|502)/i.test(message)) return false;
  return /TypeError|ReferenceError|SyntaxError|RangeError|WebGL.*Context Lost/i.test(message);
});

const report = {
  cases: results,
  criticalErrors,
  passed: results.every((r) => r.passed) && criticalErrors.length === 0,
};
fs.writeFileSync('../current-skill-lab-report.json', JSON.stringify(report, null, 2));
console.log('HIGHFLY_CURRENT_SKILL_LAB_REPORT');
console.log(JSON.stringify(report, null, 2));

await browser.close();
if (!report.passed) process.exitCode = 2;
