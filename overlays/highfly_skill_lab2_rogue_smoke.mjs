import fs from 'node:fs';
import { chromium } from 'playwright';

const BASE = 'http://127.0.0.1:4173/u6-mobile-runtime-probe/';
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

const ids = await page.evaluate(() => ({
  base: document.querySelector('#hf-lab-base')?.getAttribute('data-ability'),
  evo: document.querySelector('#hf-lab-evo')?.getAttribute('data-ability'),
  mutation: document.querySelector('#hf-lab-mutation')?.getAttribute('data-ability'),
}));

await page.click('#hf-lab-reset');
await page.waitForTimeout(120);
const before = await page.evaluate(() => {
  const sim = window.__game.sim;
  const p = sim.player;
  const t = p.targetId != null ? sim.entities.get(p.targetId) : null;
  return {
    energy: p.resource,
    targetId: p.targetId ?? null,
    targetHp: t?.hp ?? null,
  };
});

await page.click('#hf-lab-mutation');
await page.waitForTimeout(700);

const after = await page.evaluate(() => {
  const sim = window.__game.sim;
  const p = sim.player;
  const t = p.targetId != null ? sim.entities.get(p.targetId) : null;
  const mark = t?.auras?.find?.((a) => a.id === 'hf_eclipse_mark');
  return {
    energy: p.resource,
    targetId: p.targetId ?? null,
    targetHp: t?.hp ?? null,
    combo: p.comboPoints ?? p.combo ?? null,
    mark: mark ? { id: mark.id, name: mark.name, remaining: mark.remaining } : null,
    status: document.querySelector('#hf-lab-status')?.textContent ?? '',
  };
});

await page.screenshot({ path: '../skill-lab2-run1-rogue-eclipse.png', fullPage: true });

const criticalErrors = [...new Set([...pageErrors, ...consoleErrors])].filter((message) => {
  if (/character visual unavailable, skipping view/i.test(message)) return false;
  if (/THREE\.GLTFLoader: Couldn't load texture blob:/i.test(message)) return false;
  if (/Failed to load resource:.*(?:404|502)/i.test(message)) return false;
  return /TypeError|ReferenceError|SyntaxError|RangeError|WebGL.*Context Lost/i.test(message);
});

const passed =
  ids.base === 'ambush' &&
  ids.evo === 'hf_shadow_hunt_01' &&
  ids.mutation === 'hf_eclipse_mortal_01' &&
  after.status.startsWith('CAST') &&
  before.targetId === after.targetId &&
  before.targetHp != null &&
  after.targetHp != null &&
  after.targetHp < before.targetHp &&
  after.mark?.id === 'hf_eclipse_mark' &&
  criticalErrors.length === 0;

const report = { ids, before, after, criticalErrors, passed };
fs.writeFileSync('../skill-lab2-run1-rogue-report.json', JSON.stringify(report, null, 2));
console.log('HIGHFLY_SKILL_LAB2_RUN1_ROGUE_REPORT');
console.log(JSON.stringify(report, null, 2));

await browser.close();
if (!passed) process.exitCode = 2;
