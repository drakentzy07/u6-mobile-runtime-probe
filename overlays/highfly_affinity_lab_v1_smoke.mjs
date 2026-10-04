import fs from 'node:fs';
import { chromium } from 'playwright';

const BASE='http://127.0.0.1:4190/u6-mobile-runtime-probe/';
const browser=await chromium.launch({channel:'chrome',headless:true});
const page=await browser.newPage({viewport:{width:1600,height:900}});
const errors=[];
page.on('pageerror',(e)=>errors.push(String(e)));

const TANDA1=['warrior','rogue','mage'];
const expectedImplemented={
  warrior:['heroic_leap','whirlwind','thunder_clap','cleave'],
  rogue:['eviscerate','ambush','sinister_strike','rupture'],
  mage:['fireball','arcane_missiles','frostbolt','frost_nova'],
};
const modes=['base','fire','frost','lightning'];

async function openClass(cls){
  await page.goto(BASE+'?affinitylab=1&labclass='+cls,{waitUntil:'domcontentloaded',timeout:60000});
  await page.waitForFunction(()=>Boolean(window.__game?.sim?.player),null,{timeout:90000});
  await page.waitForSelector('#hf-affinity-hud',{timeout:30000});
  await page.waitForFunction(()=>Boolean(window.__highflyAffinityLab),null,{timeout:15000});
  await page.waitForFunction((want)=>window.__game?.sim?.meta(window.__game.sim.player.id)?.cls===want,cls,{timeout:10000});
}

async function setSpecIfNeeded(spec){
  if(!spec)return;
  await page.evaluate((s)=>window.__highflyAffinityLab.selectSpec(s),spec);
  await page.waitForFunction((s)=>{
    const sim=window.__game?.sim,p=sim?.player,m=p?sim.meta(p.id):null;
    return (p?.specId??m?.spec??null)===s;
  },spec,{timeout:5000});
}

async function castAndMeasure(receptor,mode){
  console.log('[AFFINITY_CAST]', receptor.id, mode, receptor.spec??'base-spec');
  await setSpecIfNeeded(receptor.spec);
  const before=await page.evaluate(()=>{
    const sim=window.__game.sim,p=sim.player,m=sim.meta(p.id);
    return {
      cls:m?.cls,
      spec:p.specId??m?.spec??null,
      resourceType:p.resourceType,
      equipment:JSON.stringify(m?.equipment),
    };
  });

  await page.evaluate(({id,mode})=>window.__highflyAffinityLab.castReceptor(id,mode),{id:receptor.id,mode});

  await page.waitForFunction(()=>{
    const sim=window.__game?.sim,p=sim?.player;
    const t=p?sim.entities.get(p.targetId):null;
    return Boolean(t && t.hp < t.maxHp);
  },null,{timeout:12000});

  const after=await page.evaluate(()=>{
    const sim=window.__game.sim,p=sim.player,m=sim.meta(p.id);
    const t=sim.entities.get(p.targetId);
    return {
      cls:m?.cls,
      spec:p.specId??m?.spec??null,
      resourceType:p.resourceType,
      equipment:JSON.stringify(m?.equipment),
      targetHp:t?.hp??null,
      targetMaxHp:t?.maxHp??null,
    };
  });

  const stable=
    before.cls===after.cls &&
    before.spec===after.spec &&
    before.resourceType===after.resourceType &&
    before.equipment===after.equipment;

  return {
    receptor:receptor.id,
    mode,
    before,
    after,
    stable,
    damaged:after.targetHp!==null&&after.targetMaxHp!==null&&after.targetHp<after.targetMaxHp,
  };
}

const report={classes:{},errors,passed:false};

for(const cls of TANDA1){
  await openClass(cls);
  const data=await page.evaluate(()=>{
    const sim=window.__game.sim,p=sim.player,m=sim.meta(p.id);
    const cls=m.cls;
    const audit=window.__highflyAffinityLab.audit[cls];
    return {
      cls,
      level:p.level,
      resourceType:p.resourceType,
      audit:audit.receptors.filter((r)=>r.implemented).map((r)=>({
        id:r.id,
        spec:r.spec??null,
        variants:r.variants,
      })),
      classButtons:[...document.querySelectorAll('.hf-aff-classpick')].map((e)=>e.textContent?.trim()??''),
      specButtons:[...document.querySelectorAll('.hf-aff-spec')].map((e)=>e.textContent?.trim()??''),
    };
  });

  const results=[];
  for(const receptor of data.audit){
    for(const mode of modes){
      if(!receptor.variants[mode])continue;
      results.push(await castAndMeasure(receptor,mode));
    }
  }
  report.classes[cls]={data,results};
  await page.screenshot({path:'../affinity-lab-v1-'+cls+'.png',fullPage:true});
}

const allResults=Object.values(report.classes).flatMap((entry)=>entry.results);
report.passed=
  TANDA1.every((cls)=>{
    const entry=report.classes[cls];
    return (
      entry?.data?.cls===cls &&
      entry?.data?.level===20 &&
      JSON.stringify(entry.data.audit.map((r)=>r.id))===JSON.stringify(expectedImplemented[cls]) &&
      entry.data.classButtons.includes('WARRIOR') &&
      entry.data.classButtons.includes('ROGUE') &&
      entry.data.classButtons.includes('MAGE')
    );
  }) &&
  allResults.length===48 &&
  allResults.every((x)=>x.stable&&x.damaged) &&
  errors.filter((x)=>/TypeError|ReferenceError|SyntaxError|RangeError/.test(x)).length===0;

fs.writeFileSync('../affinity-lab-v1-report.json',JSON.stringify(report,null,2));
console.log('HIGHFLY_AFFINITY_LAB_V1_TANDA1_REPORT');
console.log(JSON.stringify(report,null,2));
await browser.close();
if(!report.passed)process.exitCode=2;
