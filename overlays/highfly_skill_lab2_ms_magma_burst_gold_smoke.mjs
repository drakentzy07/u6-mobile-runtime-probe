import fs from 'node:fs';
import { chromium } from 'playwright';

const BASE='http://127.0.0.1:4186/u6-mobile-runtime-probe/';
const browser=await chromium.launch({channel:'chrome',headless:true});
const page=await browser.newPage({viewport:{width:1440,height:900}});

const pageErrors=[];
const consoleErrors=[];
page.on('pageerror',(err)=>pageErrors.push(String(err)));
page.on('console',(msg)=>{ if(msg.type()==='error') consoleErrors.push(msg.text()); });

await page.goto(BASE+'?skilllab=1&labclass=mage&lablineage=magma_burst',{
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
      resourceType:p.resourceType,
      resource:p.resource,
      maxResource:p.maxResource,
      equipment:JSON.stringify(sim.meta(p.id)?.equipment),
      castingAbility:p.castingAbility,
      castRemaining:p.castRemaining ?? 0,
      cooldowns:Object.fromEntries(p.cooldowns),
      surge:p.auras.find((a)=>a.id==='shaman_magma_surge') ?? null,
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
    p.auras=p.auras.filter((a)=>a.id!=='shaman_magma_surge');
    const target=sim.entities.get(p.targetId);
    if(target){
      target.dead=false;
      target.hp=target.maxHp;
      target.auras=target.auras.filter((a)=>a.sourceId!==p.id);
    }
    sim.events=[];
  });
}

async function castAndMeasure(id){
  await hardReset();
  const before=await snapshot();
  const castStart=await page.evaluate((abilityId)=>{
    const p=window.__game.sim.player;
    window.__highflySkillLab.cast(abilityId);
    return {
      resource:p.resource,
      maxResource:p.maxResource,
      castingAbility:p.castingAbility,
      castRemaining:p.castRemaining ?? 0,
    };
  },id);

  await page.waitForFunction(
    (abilityId)=>window.__game?.sim?.player?.castingAbility===abilityId,
    id,
    {timeout:10000},
  );
  const during=await snapshot();

  await page.waitForFunction(
    (beforeHp)=>{
      const sim=window.__game?.sim;
      const p=sim?.player;
      const target=p?sim.entities.get(p.targetId):null;
      return Boolean(p && target && p.castingAbility==null && target.hp<beforeHp);
    },
    before.target?.hp ?? 0,
    {timeout:60000},
  );
  await page.waitForTimeout(250);
  const after=await snapshot();
  return {id,before,castStart,during,after};
}

const base=await castAndMeasure(ids.base);
const evo=await castAndMeasure(ids.evo);
const mutation=await castAndMeasure(ids.mutation);

await page.screenshot({path:'../skill-lab2-ms-magma-burst-mutation.png',fullPage:true});

const criticalErrors=[...new Set([...pageErrors,...consoleErrors])].filter((message)=>{
  if(/character visual unavailable, skipping view/i.test(message)) return false;
  if(/THREE\.GLTFLoader: Couldn't load texture blob:/i.test(message)) return false;
  if(/Failed to load resource:.*(?:404|502)/i.test(message)) return false;
  return /TypeError|ReferenceError|SyntaxError|RangeError|WebGL.*Context Lost/i.test(message);
});

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
    r.castStart.castingAbility===r.id &&
    r.castStart.castRemaining>0 &&
    r.during.castingAbility===r.id &&
    r.after.target &&
    r.after.target.hp<r.before.target.hp &&
    !r.after.target.dead &&
    (r.after.cooldowns[r.id] ?? 0)>0 &&
    identityStable(r)
  );
}

const baseCost=base.castStart.maxResource-base.castStart.resource;
const evoCost=evo.castStart.maxResource-evo.castStart.resource;
const mutationCost=mutation.castStart.maxResource-mutation.castStart.resource;
const costParity=baseCost===evoCost && baseCost===mutationCost;

const idsPassed=
  ids.base==='hf_ms_magma_burst_01' &&
  ids.evo==='hf_ms_volcanic_core_01' &&
  ids.mutation==='hf_ms_primordial_eruption_01' &&
  ids.activeClass==='mage' &&
  ids.activeLineage==='magma_burst';

const basePassed=castPassed(base);
const evoPassed=castPassed(evo);
const mutationPassed=castPassed(mutation);
const passed=idsPassed && basePassed && evoPassed && mutationPassed && costParity && criticalErrors.length===0;

const report={
  ids,
  base,
  evo,
  mutation,
  gates:{idsPassed,basePassed,evoPassed,mutationPassed,costParity},
  criticalErrors,
  passed,
};
fs.writeFileSync('../skill-lab2-ms-magma-burst-gold-report.json',JSON.stringify(report,null,2));
console.log('HIGHFLY_SKILL_LAB2_MS_MAGMA_BURST_GOLD_REPORT');
console.log(JSON.stringify(report,null,2));

await browser.close();
if(!passed) process.exitCode=2;
