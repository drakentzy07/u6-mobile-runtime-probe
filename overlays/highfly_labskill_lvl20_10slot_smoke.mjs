import fs from 'node:fs';
import { chromium } from 'playwright';

const BASE='http://127.0.0.1:4189/u6-mobile-runtime-probe/';
const browser=await chromium.launch({channel:'chrome',headless:true});
const page=await browser.newPage({viewport:{width:1440,height:900}});
const errors=[];
page.on('pageerror',(e)=>errors.push(String(e)));

await page.goto(BASE,{waitUntil:'domcontentloaded',timeout:60000});
await page.waitForSelector('#offline-select',{state:'attached',timeout:30000});
const creator=await page.evaluate(()=>({
  title:document.querySelector('#offline-select .hf-labskill-creator-title')?.textContent?.trim(),
  ids:[...document.querySelectorAll('#offline-select .mini-class')].map((e)=>e.getAttribute('data-class')),
  labels:[...document.querySelectorAll('#offline-select .mini-class')].map((e)=>e.textContent?.trim()),
}));

await page.goto(BASE+'?skilllab=1&labskill=1&labclass=mage',{waitUntil:'domcontentloaded',timeout:60000});
await page.waitForFunction(()=>Boolean(window.__game?.sim?.player),null,{timeout:90000});
await page.waitForSelector('#hf-labskill-hud',{timeout:30000});
await page.waitForFunction(()=>document.querySelectorAll('#hf-labskill-hud .hf-ls-skill').length===10,null,{timeout:15000});

const before=await page.evaluate(()=>{
  const sim=window.__game.sim,p=sim.player,m=sim.meta(p.id);
  return {
    cls:m?.cls,
    level:m?.level,
    resourceType:p.resourceType,
    equipment:JSON.stringify(m?.equipment),
    pair:document.querySelector('#hf-labskill-hud .hf-ls-pair')?.textContent?.trim(),
    slots:[...document.querySelectorAll('#hf-labskill-hud .hf-ls-skill')].map((e)=>({
      slot:e.getAttribute('data-slot'),
      ability:e.getAttribute('data-ability'),
    })),
    main:document.querySelectorAll('#hf-labskill-hud .hf-ls-row-main .hf-ls-skill').length,
    heritage:document.querySelectorAll('#hf-labskill-hud .hf-ls-row-heritage .hf-ls-skill').length,
    loadoutCounts:Object.fromEntries(Object.entries(window.__highflyLabSkill.loadouts).map(([k,v])=>[k,v.seats.length])),
  };
});

await page.evaluate(()=>window.__highflyLabSkill.castSlot('S9'));
await page.waitForTimeout(1200);

const after=await page.evaluate(()=>{
  const sim=window.__game.sim,p=sim.player,m=sim.meta(p.id);
  const t=sim.entities.get(p.targetId);
  return {
    cls:m?.cls,
    level:m?.level,
    resourceType:p.resourceType,
    equipment:JSON.stringify(m?.equipment),
    targetHp:t?.hp??null,
    cooldown:p.cooldowns.get('hf_ms_cinder_jolt_01')??0,
    toast:document.querySelector('#hf-labskill-toast')?.textContent??'',
  };
});

await page.screenshot({path:'../labskill-lvl20-mage-10slot.png',fullPage:true});

const passed=
  creator.title==='HIGHFLY LABSKILL' &&
  JSON.stringify(creator.ids)===JSON.stringify(['warrior','rogue','mage','hunter']) &&
  creator.labels.join('|').includes('WARRIOR + PALADIN') &&
  before.cls==='mage' &&
  before.level===20 &&
  before.resourceType==='mana' &&
  before.pair==='MAGE + SHAMAN' &&
  before.main===5 &&
  before.heritage===5 &&
  before.slots.length===10 &&
  before.slots[8]?.slot==='S9' &&
  before.slots[8]?.ability==='hf_ms_cinder_jolt_01' &&
  Object.values(before.loadoutCounts).every((n)=>n===10) &&
  after.cls==='mage' &&
  after.level===20 &&
  after.resourceType==='mana' &&
  after.equipment===before.equipment &&
  after.targetHp!==null &&
  after.targetHp<50000 &&
  errors.filter((x)=>/TypeError|ReferenceError|SyntaxError|RangeError/.test(x)).length===0;

const report={creator,before,after,errors,passed};
fs.writeFileSync('../labskill-lvl20-10slot-report.json',JSON.stringify(report,null,2));
console.log('HIGHFLY_LABSKILL_LVL20_10SLOT_REPORT');
console.log(JSON.stringify(report,null,2));
await browser.close();
if(!passed)process.exitCode=2;
