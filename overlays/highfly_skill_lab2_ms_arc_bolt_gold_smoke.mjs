import fs from 'node:fs';
import { chromium } from 'playwright';

const BASE='http://127.0.0.1:4185/u6-mobile-runtime-probe/';
const browser=await chromium.launch({channel:'chrome',headless:true});
const page=await browser.newPage({viewport:{width:1440,height:900}});
const pageErrors=[],consoleErrors=[];
page.on('pageerror',(e)=>pageErrors.push(String(e)));
page.on('console',(m)=>{if(m.type()==='error')consoleErrors.push(m.text());});
await page.goto(BASE+'?skilllab=1&labclass=mage&lablineage=arc_bolt',{waitUntil:'domcontentloaded',timeout:60000});
await page.waitForFunction(()=>Boolean(window.__game?.sim?.player&&window.__highflySkillLab),null,{timeout:90000});
await page.waitForFunction(()=>document.querySelector('#hf-lab-status')?.textContent?.startsWith('LISTO'),null,{timeout:25000});
const ids=await page.evaluate(()=>({
  base:document.querySelector('#hf-lab-base')?.getAttribute('data-ability'),
  evo:document.querySelector('#hf-lab-evo')?.getAttribute('data-ability'),
  mutation:document.querySelector('#hf-lab-mutation')?.getAttribute('data-ability'),
  activeClass:window.__highflySkillLab?.activeClass,activeLineage:window.__highflySkillLab?.activeLineage,
}));
async function snap(){return page.evaluate(()=>{
  const sim=window.__game.sim,p=sim.player,t=sim.entities.get(p.targetId);
  return {cls:sim.meta(p.id)?.cls,resourceType:p.resourceType,resource:p.resource,maxResource:p.maxResource,
    equipment:JSON.stringify(sim.meta(p.id)?.equipment),castingAbility:p.castingAbility,castRemaining:p.castRemaining,
    thunder:p.auras.find((a)=>a.id==='shaman_thunder_charges')?.stacks??0,target:t?{hp:t.hp,maxHp:t.maxHp,dead:t.dead}:null};
});}
async function reset(){await page.evaluate(()=>{
  const sim=window.__game.sim,p=sim.player;window.__highflySkillLab.reset();
  p.cooldowns.clear();p.gcdRemaining=0;p.resource=p.maxResource;p.castingAbility=null;p.castRemaining=0;
  p.auras=p.auras.filter((a)=>a.id!=='shaman_thunder_charges');
  const t=sim.entities.get(p.targetId);if(t){t.dead=false;t.hp=t.maxHp;t.auras=t.auras.filter((a)=>a.sourceId!==p.id);}
});}
async function cast(id){
  await reset();const before=await snap();await page.evaluate((x)=>window.__highflySkillLab.cast(x),id);
  await page.waitForFunction((x)=>window.__game?.sim?.player?.castingAbility===x,id,{timeout:10000});
  const during=await snap();
  await page.waitForFunction((hp)=>{const sim=window.__game?.sim,p=sim?.player,t=p?sim.entities.get(p.targetId):null;return Boolean(p&&t&&p.castingAbility==null&&t.hp<hp);},before.target?.hp??0,{timeout:60000});
  const after=await snap();return{id,before,during,after};
}
const base=await cast(ids.base),evo=await cast(ids.evo),mutation=await cast(ids.mutation);
await page.screenshot({path:'../skill-lab2-ms-arc-bolt-mutation.png',fullPage:true});
const criticalErrors=[...new Set([...pageErrors,...consoleErrors])].filter((m)=>{
  if(/character visual unavailable, skipping view/i.test(m))return false;
  if(/THREE\.GLTFLoader: Couldn't load texture blob:/i.test(m))return false;
  if(/Failed to load resource:.*(?:404|502)/i.test(m))return false;
  return /TypeError|ReferenceError|SyntaxError|RangeError|WebGL.*Context Lost/i.test(m);
});
function ok(r){return Boolean(r.before.target&&r.before.thunder===0&&r.during.castingAbility===r.id&&r.during.castRemaining>0&&
  r.after.target&&r.after.target.hp<r.before.target.hp&&r.after.thunder>=1&&r.before.cls==='mage'&&r.after.cls==='mage'&&
  r.before.resourceType==='mana'&&r.after.resourceType==='mana'&&r.before.equipment===r.after.equipment);}
const idsPassed=ids.base==='hf_ms_arc_bolt_01'&&ids.evo==='hf_ms_overcharged_bolt_01'&&ids.mutation==='hf_ms_judgment_sky_01'&&ids.activeClass==='mage'&&ids.activeLineage==='arc_bolt';
const basePassed=ok(base),evoPassed=ok(evo),mutationPassed=ok(mutation);
const costParity=(base.before.resource-base.after.resource)===(evo.before.resource-evo.after.resource)&&
  (base.before.resource-base.after.resource)===(mutation.before.resource-mutation.after.resource);
const passed=idsPassed&&basePassed&&evoPassed&&mutationPassed&&costParity&&criticalErrors.length===0;
const report={ids,base,evo,mutation,gates:{idsPassed,basePassed,evoPassed,mutationPassed,costParity},criticalErrors,passed};
fs.writeFileSync('../skill-lab2-ms-arc-bolt-gold-report.json',JSON.stringify(report,null,2));
console.log('HIGHFLY_SKILL_LAB2_MS_ARC_BOLT_GOLD_REPORT');console.log(JSON.stringify(report,null,2));
await browser.close();if(!passed)process.exitCode=2;
