import fs from 'node:fs';
import { chromium } from 'playwright';

const BASE='http://127.0.0.1:4184/u6-mobile-runtime-probe/';
const browser=await chromium.launch({channel:'chrome',headless:true});
const page=await browser.newPage({viewport:{width:1440,height:900}});
const pageErrors=[],consoleErrors=[];
page.on('pageerror',(e)=>pageErrors.push(String(e)));
page.on('console',(m)=>{if(m.type()==='error')consoleErrors.push(m.text());});
await page.goto(BASE+'?skilllab=1&labclass=mage&lablineage=faultwake',{waitUntil:'domcontentloaded',timeout:60000});
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
  const thunder=p.auras.find((a)=>a.id==='shaman_thunder_charges')?.stacks??0;
  return {cls:sim.meta(p.id)?.cls,resourceType:p.resourceType,resource:p.resource,maxResource:p.maxResource,
    equipment:JSON.stringify(sim.meta(p.id)?.equipment),thunder,target:t?{hp:t.hp,maxHp:t.maxHp,dead:t.dead}:null,
    zones:(sim.groundAoEs??[]).filter((z)=>z.sourceId===p.id).map((z)=>({abilityId:z.abilityId,radius:z.radius,interval:z.interval}))};
});}
async function reset(){await page.evaluate(()=>{
  const sim=window.__game.sim,p=sim.player;window.__highflySkillLab.reset();
  p.cooldowns.clear();p.gcdRemaining=0;p.resource=p.maxResource;
  p.auras=p.auras.filter((a)=>a.id!=='shaman_thunder_charges');
  p.auras.push({id:'shaman_thunder_charges',name:'Thunder Charges',kind:'internal_cd',value:0,stacks:5,remaining:86400,duration:86400,sourceId:p.id,school:'nature'});
  if(Array.isArray(sim.groundAoEs))sim.groundAoEs.length=0;
  const t=sim.entities.get(p.targetId);if(t){t.dead=false;t.hp=t.maxHp;t.auras=t.auras.filter((a)=>a.sourceId!==p.id);}
});}
async function cast(id){
  await reset();const before=await snap();await page.evaluate((x)=>window.__highflySkillLab.cast(x),id);
  await page.waitForFunction((x)=>{const sim=window.__game?.sim,p=sim?.player;return Boolean(sim&&p&&(sim.groundAoEs??[]).some((z)=>z.sourceId===p.id&&z.abilityId===x));},id,{timeout:10000});
  const armed=await snap();
  await page.waitForFunction((hp)=>{const sim=window.__game?.sim,p=sim?.player,t=p?sim.entities.get(p.targetId):null;return Boolean(t&&t.hp<hp);},before.target?.hp??0,{timeout:60000});
  const after=await snap();return{id,before,armed,after};
}
const base=await cast(ids.base),evo=await cast(ids.evo),mutation=await cast(ids.mutation);
await page.screenshot({path:'../skill-lab2-ms-faultwake-mutation.png',fullPage:true});
const criticalErrors=[...new Set([...pageErrors,...consoleErrors])].filter((m)=>{
  if(/character visual unavailable, skipping view/i.test(m))return false;
  if(/THREE\.GLTFLoader: Couldn't load texture blob:/i.test(m))return false;
  if(/Failed to load resource:.*(?:404|502)/i.test(m))return false;
  return /TypeError|ReferenceError|SyntaxError|RangeError|WebGL.*Context Lost/i.test(m);
});
function ok(r){const z=r.armed.zones.filter((x)=>x.abilityId===r.id);return Boolean(r.before.target&&r.before.thunder===5&&r.armed.thunder===0&&z.length===1&&z[0].radius===8&&z[0].interval===1.5&&
  r.after.target&&r.after.target.hp<r.before.target.hp&&r.before.cls==='mage'&&r.after.cls==='mage'&&
  r.before.resourceType==='mana'&&r.after.resourceType==='mana'&&r.before.equipment===r.after.equipment);}
const idsPassed=ids.base==='hf_ms_faultwake_01'&&ids.evo==='hf_ms_primordial_cataclysm_01'&&ids.mutation==='hf_ms_storms_end_01'&&ids.activeClass==='mage'&&ids.activeLineage==='faultwake';
const basePassed=ok(base),evoPassed=ok(evo),mutationPassed=ok(mutation);
const passed=idsPassed&&basePassed&&evoPassed&&mutationPassed&&criticalErrors.length===0;
const report={ids,base,evo,mutation,gates:{idsPassed,basePassed,evoPassed,mutationPassed},criticalErrors,passed};
fs.writeFileSync('../skill-lab2-ms-faultwake-gold-report.json',JSON.stringify(report,null,2));
console.log('HIGHFLY_SKILL_LAB2_MS_FAULTWAKE_GOLD_REPORT');console.log(JSON.stringify(report,null,2));
await browser.close();if(!passed)process.exitCode=2;
