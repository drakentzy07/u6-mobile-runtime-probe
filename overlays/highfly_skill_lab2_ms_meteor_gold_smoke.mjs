import fs from 'node:fs';
import { chromium } from 'playwright';

const BASE='http://127.0.0.1:4181/u6-mobile-runtime-probe/';
const browser=await chromium.launch({channel:'chrome',headless:true});
const page=await browser.newPage({viewport:{width:1440,height:900}});

const pageErrors=[];
const consoleErrors=[];
page.on('pageerror',(err)=>pageErrors.push(String(err)));
page.on('console',(msg)=>{ if(msg.type()==='error') consoleErrors.push(msg.text()); });

await page.goto(BASE+'?skilllab=1&labclass=mage&lablineage=meteor',{
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
      cooldowns:Object.fromEntries(p.cooldowns),
      target:target?{id:target.id,hp:target.hp,maxHp:target.maxHp,dead:target.dead}:null,
      groundZones:(sim.groundAoEs ?? []).filter((z)=>z.sourceId===p.id).map((z)=>({
        abilityId:z.abilityId,
        radius:z.radius,
        interval:z.interval,
        remaining:z.remaining,
      })),
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
    if(Array.isArray(sim.groundAoEs)) sim.groundAoEs.length=0;
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
    const sim=window.__game.sim;
    const p=sim.player;
    window.__highflySkillLab.cast(abilityId);
    return {
      resource:p.resource,
      maxResource:p.maxResource,
      cooldown:p.cooldowns.get(abilityId) ?? 0,
      groundZones:(sim.groundAoEs ?? []).filter((z)=>z.sourceId===p.id && z.abilityId===abilityId).length,
      status:document.querySelector('#hf-lab-status')?.textContent ?? '',
    };
  },id);

  await page.waitForFunction(
    (abilityId)=>{
      const sim=window.__game?.sim;
      const p=sim?.player;
      return Boolean(sim && p && (sim.groundAoEs ?? []).some((z)=>z.sourceId===p.id && z.abilityId===abilityId));
    },
    id,
    {timeout:10000},
  );
  const armed=await snapshot();

  await page.waitForFunction(
    (beforeHp)=>{
      const sim=window.__game?.sim;
      const p=sim?.player;
      if(!sim||!p) return false;
      const target=sim.entities.get(p.targetId);
      return Boolean(target && target.hp<beforeHp);
    },
    before.target?.hp ?? 0,
    {timeout:60000},
  );
  await page.waitForTimeout(200);
  const after=await snapshot();
  return {id,before,castStart,armed,after};
}

const base=await castAndMeasure(ids.base);
const evo=await castAndMeasure(ids.evo);
const mutation=await castAndMeasure(ids.mutation);

await page.screenshot({path:'../skill-lab2-ms-meteor-mutation.png',fullPage:true});

const criticalErrors=[...new Set([...pageErrors,...consoleErrors])].filter((message)=>{
  if(/character visual unavailable, skipping view/i.test(message)) return false;
  if(/THREE\.GLTFLoader: Couldn't load texture blob:/i.test(message)) return false;
  if(/Failed to load resource:.*(?:404|502)/i.test(message)) return false;
  return /TypeError|ReferenceError|SyntaxError|RangeError|WebGL.*Context Lost/i.test(message);
});

function identityStable(r){
  return r.before.cls==='mage' &&
    r.armed.cls==='mage' &&
    r.after.cls==='mage' &&
    r.before.resourceType==='mana' &&
    r.armed.resourceType==='mana' &&
    r.after.resourceType==='mana' &&
    r.before.equipment===r.armed.equipment &&
    r.before.equipment===r.after.equipment;
}

function castPassed(r){
  const armedZones=r.armed.groundZones.filter((z)=>z.abilityId===r.id);
  return Boolean(
    r.before.target &&
    r.castStart.status.startsWith('CAST') &&
    r.castStart.resource===r.castStart.maxResource-120 &&
    r.castStart.cooldown>0 &&
    armedZones.length===1 &&
    armedZones[0].radius===8 &&
    armedZones[0].interval===2 &&
    r.after.target &&
    r.after.target.hp<r.before.target.hp &&
    !r.after.target.dead &&
    identityStable(r)
  );
}

const idsPassed=
  ids.base==='meteor' &&
  ids.evo==='hf_ms_fallen_star_01' &&
  ids.mutation==='hf_ms_celestial_extinction_01' &&
  ids.activeClass==='mage' &&
  ids.activeLineage==='meteor';

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
fs.writeFileSync('../skill-lab2-ms-meteor-gold-report.json',JSON.stringify(report,null,2));
console.log('HIGHFLY_SKILL_LAB2_MS_METEOR_GOLD_REPORT');
console.log(JSON.stringify(report,null,2));

await browser.close();
if(!passed) process.exitCode=2;
