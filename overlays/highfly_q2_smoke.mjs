import { chromium } from 'playwright';

const base = process.env.HIGHFLY_Q2_URL || 'http://127.0.0.1:4190/u6-mobile-runtime-probe/';
const classes = ['warrior','paladin','hunter','rogue','priest','shaman','mage','warlock','druid'];
const browser = await chromium.launch({ headless: true });
const context = await browser.newContext({
  viewport: { width: 900, height: 420 },
  hasTouch: true,
  isMobile: true,
});
const page = await context.newPage();
const report = { classes: [], layout: null, element: null };

function require(value, message) {
  if (!value) throw new Error(message);
}

for (const cls of classes) {
  await page.goto(base + '?q2test=1&labclass=' + cls + '&gem=fire', {
    waitUntil: 'domcontentloaded',
    timeout: 180000,
  });
  await page.waitForFunction(() => window.__game?.sim?.player && window.__highflyQ2, null, { timeout: 180000 });
  await page.evaluate(() => {
    document.body.classList.add('mobile-touch', 'game-active', 'hf-q2-active');
    window.dispatchEvent(new Event('resize'));
  });
  await page.waitForFunction((want) => {
    const g = window.__game;
    const p = g?.sim?.player;
    const v = p ? g.renderer?.views?.get(p.id) : null;
    return g?.sim?.meta?.(p?.id)?.cls === want && v?.visualKey === 'player_' + want;
  }, cls, { timeout: 60000 });

  const state = await page.evaluate(() => {
    const g = window.__game, p = g.sim.player, v = g.renderer.views.get(p.id);
    return {
      cls: g.sim.meta(p.id)?.cls,
      key: v?.visualKey,
      level: p.level,
      qBodies: !!document.querySelector('[data-body="qmale"], [data-body="qfemale"], #hf-affinity-hud'),
      hotbar: g.hud.actionBarController.actions.slice(0, 10),
    };
  });
  require(state.key === 'player_' + cls, cls + ': not using original Claude visual');
  require(state.qBodies === false, cls + ': Q/Affinity lab UI leaked into Q2');
  require(state.hotbar.length === 10, cls + ': hotbar length is not ten');
  report.classes.push(state);
}

await page.goto(base + '?q2test=1&labclass=warrior&gem=fire', { waitUntil: 'domcontentloaded', timeout: 180000 });
await page.waitForFunction(() => window.__game?.sim?.player && window.__highflyQ2, null, { timeout: 180000 });
await page.evaluate(() => {
  document.body.classList.add('mobile-touch', 'game-active', 'hf-q2-active');
  window.dispatchEvent(new Event('resize'));
});
await page.waitForTimeout(1200);

const layout = await page.evaluate(() => {
  const rows = [...document.querySelectorAll('#actionbar .action-btn[data-hotbar-slot]')]
    .filter((el) => {
      const slot = Number(el.dataset.hotbarSlot);
      const r = el.getBoundingClientRect();
      const s = getComputedStyle(el);
      return slot >= 1 && slot <= 10 && r.width > 0 && r.height > 0 && s.display !== 'none';
    })
    .map((el) => {
      const r = el.getBoundingClientRect();
      return { slot: Number(el.dataset.hotbarSlot), x:r.x, y:r.y, w:r.width, h:r.height };
    })
    .sort((a,b) => a.slot-b.slot);
  const attack = document.getElementById('mobile-action-attack')?.getBoundingClientRect();
  const target = document.getElementById('mobile-target-cycle')?.getBoundingClientRect();
  const gem = document.getElementById('hf-q2-gem-seat')?.getBoundingClientRect();
  const buff = document.getElementById('buff-bar')?.getBoundingClientRect();
  return {
    viewport:[innerWidth,innerHeight],
    rows,
    attack: attack ? {x:attack.x,y:attack.y,w:attack.width,h:attack.height}:null,
    target: target ? {x:target.x,y:target.y,w:target.width,h:target.height}:null,
    gem: gem ? {x:gem.x,y:gem.y,w:gem.width,h:gem.height}:null,
    buff: buff ? {x:buff.x,y:buff.y,w:buff.width,h:buff.height}:null,
    interactDisplay: getComputedStyle(document.getElementById('mobile-interact')).display,
  };
});
require(layout.rows.length === 10, 'mobile: expected ten visible native skill buttons');
for (let i=0;i<layout.rows.length;i++) for (let j=i+1;j<layout.rows.length;j++) {
  const a=layout.rows[i], b=layout.rows[j];
  require(a.x+a.w<=b.x+1 || b.x+b.w<=a.x+1 || a.y+a.h<=b.y+1 || b.y+b.h<=a.y+1,
    'mobile: skill buttons overlap S'+a.slot+'/S'+b.slot);
}
require(layout.attack && layout.target && layout.gem, 'mobile: Attack/Target/Gem control missing');
require(layout.interactDisplay === 'none', 'mobile: separate Use button must be merged into Attack/Use');
report.layout = layout;

await page.evaluate(() => window.__highflyQ2.setElement('base'));
await page.waitForTimeout(450);
const noGem = await page.evaluate(() => ({
  mode: window.__highflyQ2.element(),
  ready: window.__game.sim.highflyElementalFinisherReady(),
}));
require(noGem.mode === 'base' && noGem.ready === false, 'ATK4 must be locked without active gem');

await page.evaluate(() => window.__highflyQ2.setElement('fire'));
await page.waitForTimeout(450);
const fire = await page.evaluate(() => ({
  mode: window.__highflyQ2.element(),
  ready: window.__game.sim.highflyElementalFinisherReady(),
}));
require(fire.mode === 'fire' && fire.ready === true, 'ATK4 must unlock with active fire gem');
report.element = { noGem, fire };

await page.screenshot({ path: '../highfly-q2-claude-clean-mobile.png', fullPage: true });
await import('node:fs').then(({ writeFileSync }) =>
  writeFileSync('../highfly-q2-smoke-report.json', JSON.stringify(report, null, 2))
);
console.log('HIGHFLY_Q2_SMOKE_PASS');
await browser.close();
