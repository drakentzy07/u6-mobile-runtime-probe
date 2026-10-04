import fs from 'node:fs';
import { chromium } from 'playwright';

const BASE='http://127.0.0.1:4190/u6-mobile-runtime-probe/';
const browser=await chromium.launch({channel:'chrome',headless:true});
const page=await browser.newPage({viewport:{width:1600,height:900}});
const errors=[];
page.on('pageerror',(e)=>errors.push(String(e)));

await page.goto(BASE+'?affinitylab=1&labclass=warrior',{waitUntil:'domcontentloaded',timeout:60000});
await page.waitForFunction(()=>Boolean(window.__game?.sim?.player),null,{timeout:90000});
await page.waitForSelector('#hf-affinity-hud',{timeout:30000});
await page.waitForFunction(()=>Boolean(window.__highflyAffinityLab),null,{timeout:15000});

const before=await page.evaluate(()=>{
  const sim=window.__game.sim,p=sim.player,m=sim.meta(p.id);
  const buttons=[...document.querySelectorAll('#hf-affinity-hud .hf-aff-skill')].map((e)=>({
    ability:e.getAttribute('data-ability'),
    text:e.textContent?.trim()??'',
  }));
  return {
    cls:m?.cls,
    level:p.level,
    spec:p.specId??m?.spec??null,
    resourceType:p.resourceType,
    equipment:JSON.stringify(m?.equipment),
    affinity:window.__highflyAffinityLab.affinity(),
    buttons,
    auditClasses:Object.keys(window.__highflyAffinityLab.audit),
  };
});

await page.evaluate(()=>window.__highflyAffinityLab.castBase());
await page.waitForFunction(()=>Boolean(window.__game?.sim?.player?.leap),null,{timeout:5000}).catch(()=>{});
await page.waitForTimeout(1200);
const baseResult=await page.evaluate(()=>{
  const sim=window.__game.sim,p=sim.player,m=sim.meta(p.id);
  const t=sim.entities.get(p.targetId);
  return {
    cls:m?.cls,
    spec:p.specId??m?.spec??null,
    resourceType:p.resourceType,
    equipment:JSON.stringify(m?.equipment),
    targetHp:t?.hp??null,
    targetMaxHp:t?.maxHp??null,
    toast:document.querySelector('#hf-affinity-toast')?.textContent??'',
    result:document.querySelector('#hf-affinity-result')?.textContent??'',
  };
});

await page.click('#hf-affinity-hud .hf-aff-reset');
await page.evaluate(()=>window.__highflyAffinityLab.selectAffinity('fire'));
await page.waitForFunction(()=>window.__highflyAffinityLab.affinity()==='fire',null,{timeout:3000});
await page.evaluate(()=>window.__highflyAffinityLab.castElement());
await page.waitForTimeout(1300);
const fireImpact=await page.evaluate(()=>{
  const sim=window.__game.sim,p=sim.player,m=sim.meta(p.id);
  const t=sim.entities.get(p.targetId);
  return {
    cls:m?.cls,
    spec:p.specId??m?.spec??null,
    resourceType:p.resourceType,
    equipment:JSON.stringify(m?.equipment),
    targetHp:t?.hp??null,
    targetMaxHp:t?.maxHp??null,
    hasBurn:Boolean(t?.auras?.some((a)=>String(a.id).startsWith('hf_affinity_leap_fire_'))),
    result:document.querySelector('#hf-affinity-result')?.textContent??'',
  };
});

await page.click('#hf-affinity-hud .hf-aff-reset');
await page.evaluate(()=>window.__highflyAffinityLab.selectAffinity('frost'));
await page.evaluate(()=>window.__highflyAffinityLab.castElement());
await page.waitForTimeout(850);
const frostImpact=await page.evaluate(()=>{
  const sim=window.__game.sim,p=sim.player,m=sim.meta(p.id);
  const t=sim.entities.get(p.targetId);
  return {
    cls:m?.cls,
    spec:p.specId??m?.spec??null,
    hasSlow:Boolean(t?.auras?.some((a)=>String(a.id).startsWith('hf_affinity_leap_frost_'))),
  };
});

await page.click('#hf-affinity-hud .hf-aff-reset');
await page.evaluate(()=>window.__highflyAffinityLab.selectAffinity('lightning'));
await page.evaluate(()=>window.__highflyAffinityLab.castElement());
await page.waitForTimeout(700);
const lightningImpact=await page.evaluate(()=>{
  const sim=window.__game.sim,p=sim.player,m=sim.meta(p.id);
  const t=sim.entities.get(p.targetId);
  return {
    cls:m?.cls,
    spec:p.specId??m?.spec??null,
    targetHp:t?.hp??null,
    targetMaxHp:t?.maxHp??null,
  };
});

await page.screenshot({path:'../affinity-lab-v1-warrior.png',fullPage:true});

const expectedClasses=['warrior','paladin','rogue','warlock','mage','shaman','hunter','druid','priest'];
const stable=(x)=>x.cls===before.cls&&x.spec===before.spec;
const passed=
  before.cls==='warrior' &&
  before.level===20 &&
  before.resourceType==='rage' &&
  JSON.stringify(before.auditClasses)===JSON.stringify(expectedClasses) &&
  before.buttons.some((b)=>b.ability==='heroic_leap') &&
  before.buttons.some((b)=>b.ability==='hf_aff_heroic_leap_fire_01') &&
  stable(baseResult) &&
  baseResult.resourceType===before.resourceType &&
  baseResult.equipment===before.equipment &&
  baseResult.targetHp!==null &&
  baseResult.targetMaxHp!==null &&
  baseResult.targetHp<baseResult.targetMaxHp &&
  stable(fireImpact) &&
  fireImpact.resourceType===before.resourceType &&
  fireImpact.equipment===before.equipment &&
  fireImpact.targetHp!==null &&
  fireImpact.targetMaxHp!==null &&
  fireImpact.targetHp<fireImpact.targetMaxHp &&
  fireImpact.hasBurn &&
  stable(frostImpact) &&
  frostImpact.hasSlow &&
  stable(lightningImpact) &&
  lightningImpact.targetHp!==null &&
  lightningImpact.targetMaxHp!==null &&
  lightningImpact.targetHp<lightningImpact.targetMaxHp &&
  errors.filter((x)=>/TypeError|ReferenceError|SyntaxError|RangeError/.test(x)).length===0;

const report={before,baseResult,fireImpact,frostImpact,lightningImpact,errors,passed};
fs.writeFileSync('../affinity-lab-v1-report.json',JSON.stringify(report,null,2));
console.log('HIGHFLY_AFFINITY_LAB_V1_REPORT');
console.log(JSON.stringify(report,null,2));
await browser.close();
if(!passed)process.exitCode=2;
