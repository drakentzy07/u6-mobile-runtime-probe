import fs from 'node:fs';
import { chromium } from 'playwright';

const BASE='http://127.0.0.1:4187/u6-mobile-runtime-probe/';
const browser=await chromium.launch({channel:'chrome',headless:true});
const page=await browser.newPage({viewport:{width:1440,height:900}});
const pageErrors=[],consoleErrors=[];
page.on('pageerror',(e)=>pageErrors.push(String(e)));
page.on('console',(m)=>{if(m.type()==='error')consoleErrors.push(m.text());});

await page.goto(BASE+'?skilllab=1&labclass=hunter&lablineage=frostjaw',{waitUntil:'domcontentloaded',timeout:60000});
await page.waitForFunction(()=>Boolean(window.__game?.sim?.player&&window.__highflySkillLab),null,{timeout:90000});
await page.waitForFunction(()=>document.querySelector('#hf-lab-status')?.textContent?.startsWith('LISTO'),null,{timeout:25000});

const ids=await page.evaluate(()=>({
  base:document.querySelector('#hf-lab-base')?.getAttribute('data-ability'),
  evo:document.querySelector('#hf-lab-evo')?.getAttribute('data-ability'),
  mutation:document.querySelector('#hf-lab-mutation')?.getAttribute('data-ability'),
  activeClass:window.__highflySkillLab?.activeClass,
  activeLineage:window.__highflySkillLab?.activeLineage,
}));

async function snap(){
  return page.evaluate(()=>{
    const sim=window.__game.sim,p=sim.player,t=sim.entities.get(p.targetId);
    const traps=(sim.ctx?.groundAoEs??[]).filter((z)=>z.sourceId===p.id&&z.hunterTrap).map((z)=>({
      abilityId:z.abilityId,
      radius:z.radius,
      rootAll:z.hunterTrap?.rootAll??false,
      armRemaining:z.hunterTrap?.armRemaining??z.hunterTrap?.armTime??null,
    }));
    return {
      cls:sim.meta(p.id)?.cls,
      resourceType:p.resourceType,
      resource:p.resource,
      equipment:JSON.stringify(sim.meta(p.id)?.equipment),
      cooldowns:Object.fromEntries(p.cooldowns),
      traps,
      target:t?{
        id:t.id,hp:t.hp,maxHp:t.maxHp,dead:t.dead,
        rooted:t.auras.some((a)=>a.id==='frostjaw_trap_freeze'),
        slowed:t.auras.some((a)=>a.kind==='slow'),
      }:null,
    };
  });
}

async function reset(){
  await page.evaluate(()=>{
    const sim=window.__game.sim,p=sim.player;
    window.__highflySkillLab.reset();
    p.cooldowns.clear();p.gcdRemaining=0;p.resource=p.maxResource;
    if(sim.ctx?.groundAoEs) sim.ctx.groundAoEs.length=0;
    const t=sim.entities.get(p.targetId);
    if(t){
      t.dead=false;t.hp=t.maxHp;
      t.auras=t.auras.filter((a)=>a.sourceId!==p.id&&a.id!=='frostjaw_trap_freeze'&&a.kind!=='slow');
    }
    sim.events=[];
  });
}

async function cast(id){
  await reset();
  const before=await snap();
  await page.evaluate((x)=>window.__highflySkillLab.cast(x),id);
  await page.waitForFunction((x)=>{
    const sim=window.__game?.sim,p=sim?.player;
    return Boolean(sim&&p&&(sim.ctx?.groundAoEs??[]).some((z)=>z.sourceId===p.id&&z.hunterTrap&&z.abilityId===x));
  },id,{timeout:10000});
  const armed=await snap();
  await page.waitForFunction(()=>{
    const sim=window.__game?.sim,p=sim?.player,t=p?sim.entities.get(p.targetId):null;
    return Boolean(t&&t.auras.some((a)=>a.id==='frostjaw_trap_freeze'));
  },null,{timeout:15000});
  const triggered=await snap();
  return {id,before,armed,triggered};
}

const base=await cast(ids.base);
const evo=await cast(ids.evo);
const mutation=await cast(ids.mutation);
await page.screenshot({path:'../skill-lab2-hd-frostjaw-mutation.png',fullPage:true});

const criticalErrors=[...new Set([...pageErrors,...consoleErrors])].filter((m)=>{
  if(/character visual unavailable, skipping view/i.test(m))return false;
  if(/THREE\.GLTFLoader: Couldn't load texture blob:/i.test(m))return false;
  if(/Failed to load resource:.*(?:404|502)/i.test(m))return false;
  return /TypeError|ReferenceError|SyntaxError|RangeError|WebGL.*Context Lost/i.test(m);
});

function identity(r){
  return r.before.cls==='hunter'&&r.armed.cls==='hunter'&&r.triggered.cls==='hunter'&&
    r.before.resourceType===r.armed.resourceType&&r.before.resourceType===r.triggered.resourceType&&
    r.before.equipment===r.armed.equipment&&r.before.equipment===r.triggered.equipment;
}
function ok(r,expectedRadius,expectedRootAll){
  const z=r.armed.traps.filter((x)=>x.abilityId===r.id);
  return Boolean(
    r.before.target&&z.length===1&&z[0].radius===expectedRadius&&z[0].rootAll===expectedRootAll&&
    (r.armed.cooldowns[r.id]??0)>0&&r.triggered.target?.rooted&&r.triggered.target?.slowed&&
    r.triggered.target.hp===r.before.target.hp&&identity(r)
  );
}

const idsPassed=ids.base==='frostjaw_trap'&&ids.evo==='hf_hunter_prison_01'&&ids.mutation==='hf_hd_boreal_domain_01'&&ids.activeClass==='hunter'&&ids.activeLineage==='frostjaw';
const basePassed=ok(base,4,false);
const evoPassed=ok(evo,5,true);
const mutationPassed=ok(mutation,5,true);
const passed=idsPassed&&basePassed&&evoPassed&&mutationPassed&&criticalErrors.length===0;

const report={ids,base,evo,mutation,gates:{idsPassed,basePassed,evoPassed,mutationPassed},criticalErrors,passed};
fs.writeFileSync('../skill-lab2-hd-frostjaw-gold-report.json',JSON.stringify(report,null,2));
console.log('HIGHFLY_SKILL_LAB2_HD_FROSTJAW_GOLD_REPORT');
console.log(JSON.stringify(report,null,2));

await browser.close();
if(!passed)process.exitCode=2;
