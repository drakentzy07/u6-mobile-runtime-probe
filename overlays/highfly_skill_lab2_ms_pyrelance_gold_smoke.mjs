import fs from 'node:fs';
import { chromium } from 'playwright';

const BASE='http://127.0.0.1:4180/u6-mobile-runtime-probe/';
const browser=await chromium.launch({channel:'chrome',headless:true});
const page=await browser.newPage({viewport:{width:1440,height:900}});

const pageErrors=[];
const consoleErrors=[];
page.on('pageerror',(err)=>pageErrors.push(String(err)));
page.on('console',(msg)=>{ if(msg.type()==='error') consoleErrors.push(msg.text()); });

await page.goto(BASE+'?skilllab=1&labclass=mage&lablineage=pyrelance',{
  waitUntil:'domcontentloaded',
  timeout:60000,
});
await page.waitForFunction(
  ()=>Boolean(window.__game?.sim?.player && window.__highflySkillLab),
  null,
  {timeout:90000},
);
await page.waitForFunction(
  ()=>document.querySelector('#hf-lab-status')?.textContent?.startsWith('LISTO'),
  null,
  {timeout:25000},
);

const ids=await page.evaluate(()=>({
  base:document.querySelector('#hf-lab-base')?.getAttribute('data-ability'),
  evo:document.querySelector('#hf-lab-evo')?.getAttribute('data-ability'),
  mutation:document.querySelector('#hf-lab-mutation')?.getAttribute('data-ability'),
  activeClass:window.__highflySkillLab?.activeClass,
  activeLineage:window.__highflySkillLab?.activeLineage,
}));

async function snapshot(){
  return page.evaluate(()=>{
    const sim=window.__game.sim;
    const p=sim.player;
    const target=sim.entities.get(p.targetId);
    return {
      cls:sim.meta(p.id)?.cls,
      spec:sim.meta(p.id)?.spec,
      resourceType:p.resourceType,
      resource:p.resource,
      maxResource:p.maxResource,
      castingAbility:p.castingAbility,
      castRemaining:p.castRemaining,
      channeling:p.channeling,
      equipment:JSON.stringify(sim.meta(p.id)?.equipment),
      target:target?{id:target.id,hp:target.hp,maxHp:target.maxHp,dead:target.dead}:null,
    };
  });
}

async function hardReset(){
  await page.evaluate(()=>{
    const sim=window.__game.sim;
    const p=sim.player;
    window.__highflySkillLab.reset();
    p.cooldowns.clear();
    p.gcdRemaining=0;
    p.resource=p.maxResource;
    p.castingAbility=null;
    p.castRemaining=0;
    p.channeling=false;
    p.auras=p.auras.filter((a)=>
      a.id!=='heating_up' &&
      a.id!=='hot_streak' &&
      a.id!=='hot_streak_instant' &&
      a.kind!=='next_cast_free' &&
      a.kind!=='next_cast_instant'
    );
    const target=sim.entities.get(p.targetId);
    if(target){
      target.dead=false;
      target.hp=target.maxHp;
      target.auras=target.auras.filter((a)=>a.sourceId!==p.id);
    }
  });
}

async function castAndMeasure(id){
  await hardReset();
  const before=await snapshot();
  await page.evaluate((abilityId)=>window.__highflySkillLab.cast(abilityId),id);
  await page.waitForTimeout(180);
  const during=await snapshot();

  await page.waitForFunction(
    ({abilityId,beforeHp})=>{
      const sim=window.__game?.sim;
      const p=sim?.player;
      if(!sim||!p) return false;
      const target=sim.entities.get(p.targetId);
      return p.castingAbility!==abilityId && target && target.hp<beforeHp;
    },
    {abilityId:id,beforeHp:before.target?.hp ?? 0},
    {timeout:14000},
  );
  await page.waitForTimeout(250);
  const after=await snapshot();
  return {id,before,during,after};
}

const base=await castAndMeasure(ids.base);
const evo=await castAndMeasure(ids.evo);
const mutation=await castAndMeasure(ids.mutation);

await page.screenshot({path:'../skill-lab2-ms-pyrelance-mutation.png',fullPage:true});

const criticalErrors=[
  ...pageErrors,
  ...consoleErrors.filter((x)=>!/favicon|404|Failed to load resource/i.test(x)),
];

function identityStable(r){
  return r.before.cls==='mage' &&
    r.during.cls==='mage' &&
    r.after.cls==='mage' &&
    r.before.resourceType==='mana' &&
    r.during.resourceType==='mana' &&
    r.after.resourceType==='mana' &&
    r.before.equipment===r.during.equipment &&
    r.before.equipment===r.after.equipment;
}
function castPassed(r){
  return Boolean(
    r.before.target &&
    r.during.castingAbility===r.id &&
    r.during.castRemaining>0 &&
    r.after.target &&
    r.after.target.hp<r.before.target.hp &&
    !r.after.target.dead &&
    identityStable(r)
  );
}

const idsPassed=
  ids.base==='pyroblast' &&
  ids.evo==='hf_ms_crimson_pyrelance_01' &&
  ids.mutation==='hf_ms_crimson_rain_01' &&
  ids.activeClass==='mage' &&
  ids.activeLineage==='pyrelance';

const basePassed=castPassed(base);
const evoPassed=castPassed(evo);
const mutationPassed=castPassed(mutation);
const passed=idsPassed && basePassed && evoPassed && mutationPassed && criticalErrors.length===0;

const report={
  ids,
  base,
  evo,
  mutation,
  gates:{idsPassed,basePassed,evoPassed,mutationPassed},
  criticalErrors,
  passed,
};
fs.writeFileSync('../skill-lab2-ms-pyrelance-gold-report.json',JSON.stringify(report,null,2));
console.log('HIGHFLY_SKILL_LAB2_MS_PYRELANCE_GOLD_REPORT');
console.log(JSON.stringify(report,null,2));

await browser.close();
if(!passed) process.exitCode=2;
