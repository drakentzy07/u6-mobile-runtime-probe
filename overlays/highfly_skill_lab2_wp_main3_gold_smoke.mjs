import fs from 'node:fs';
import { chromium } from 'playwright';

const BASE='http://127.0.0.1:4178/u6-mobile-runtime-probe/';
const browser=await chromium.launch({channel:'chrome',headless:true});
const page=await browser.newPage({viewport:{width:1440,height:900}});

const pageErrors=[];
const consoleErrors=[];
page.on('pageerror',(err)=>pageErrors.push(String(err)));
page.on('console',(msg)=>{ if(msg.type()==='error') consoleErrors.push(msg.text()); });

async function openLineage(lineage) {
  await page.goto(BASE+'?skilllab=1&labclass=warrior&lablineage='+lineage,{
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
}

async function ids() {
  return page.evaluate(()=>({
    base:document.querySelector('#hf-lab-base')?.getAttribute('data-ability'),
    evo:document.querySelector('#hf-lab-evo')?.getAttribute('data-ability'),
    mutation:document.querySelector('#hf-lab-mutation')?.getAttribute('data-ability'),
    activeClass:window.__highflySkillLab?.activeClass,
    activeLineage:window.__highflySkillLab?.activeLineage,
  }));
}

async function targetSnapshot() {
  return page.evaluate(()=>{
    const sim=window.__game.sim;
    const p=sim.player;
    const target=sim.entities.get(p.targetId);
    return {
      player:{x:p.pos.x,y:p.pos.y,z:p.pos.z,onGround:p.onGround,leap:Boolean(p.leap)},
      target:target?{id:target.id,hp:target.hp,auras:target.auras.map((a)=>({kind:a.kind,remaining:a.remaining}))}:null,
      echo:p.auras.find((a)=>a.kind==='aoe_echo') ?? null,
      resourceType:p.resourceType,
      cls:sim.meta(p.id)?.cls,
      status:document.querySelector('#hf-lab-status')?.textContent ?? '',
    };
  });
}

async function castAndWait(id, waitMs=350) {
  const before=await targetSnapshot();
  await page.evaluate((abilityId)=>{
    const sim=window.__game.sim;
    const p=sim.player;
    p.auras=p.auras.filter((a)=>a.kind!=='aoe_echo');
    const target=sim.entities.get(p.targetId);
    if(target) target.auras=target.auras.filter((a)=>a.kind!=='stun');
    window.__highflySkillLab.reset();
    window.__highflySkillLab.cast(abilityId);
  },id);
  await page.waitForTimeout(waitMs);
  const after=await targetSnapshot();
  return {id,before,after};
}

await openLineage('heroic_leap');
const leapIds=await ids();
const leap=[];
for(const id of [leapIds.base,leapIds.evo,leapIds.mutation]) {
  leap.push(await castAndWait(id,1200));
}
await page.screenshot({path:'../skill-lab2-wp-main3-leap.png',fullPage:true});

await openLineage('whirlwind');
const whirlIds=await ids();
const whirlwind=[];
for(const id of [whirlIds.base,whirlIds.evo,whirlIds.mutation]) {
  whirlwind.push(await castAndWait(id,450));
}
await page.screenshot({path:'../skill-lab2-wp-main3-whirlwind.png',fullPage:true});

await openLineage('faultline');
const faultIds=await ids();
const faultline=[];
for(const id of [faultIds.base,faultIds.evo,faultIds.mutation]) {
  faultline.push(await castAndWait(id,450));
}
await page.screenshot({path:'../skill-lab2-wp-main3-faultline.png',fullPage:true});

const criticalErrors=[...new Set([...pageErrors,...consoleErrors])].filter((message)=>{
  if(/character visual unavailable, skipping view/i.test(message)) return false;
  if(/THREE\.GLTFLoader: Couldn't load texture blob:/i.test(message)) return false;
  if(/Failed to load resource:.*(?:404|502)/i.test(message)) return false;
  return /TypeError|ReferenceError|SyntaxError|RangeError|WebGL.*Context Lost/i.test(message);
});

const leapPassed=
  leapIds.activeClass==='warrior' &&
  leapIds.base==='heroic_leap' &&
  leapIds.evo==='hf_demolishing_leap_01' &&
  leapIds.mutation==='hf_ascending_cataclysm_01' &&
  leap.every((r)=>{
    const moved=Math.hypot(
      r.after.player.x-r.before.player.x,
      r.after.player.z-r.before.player.z,
    );
    return moved>4 &&
      r.after.player.onGround &&
      !r.after.player.leap &&
      r.before.target && r.after.target &&
      r.after.target.hp<r.before.target.hp &&
      r.after.cls==='warrior' &&
      r.after.resourceType==='rage';
  });

const whirlwindPassed=
  whirlIds.activeClass==='warrior' &&
  whirlIds.base==='whirlwind' &&
  whirlIds.evo==='hf_cutting_whirlwind_01' &&
  whirlIds.mutation==='hf_colossus_tempest_01' &&
  whirlwind.every((r)=>
    r.before.target && r.after.target &&
    r.after.target.hp<r.before.target.hp &&
    r.after.echo &&
    r.after.echo.charges===2 &&
    r.after.cls==='warrior' &&
    r.after.resourceType==='rage'
  );

const faultlinePassed=
  faultIds.activeClass==='warrior' &&
  faultIds.base==='faultline' &&
  faultIds.evo==='hf_seismic_fault_01' &&
  faultIds.mutation==='hf_world_fracture_01' &&
  faultline.every((r)=>
    r.before.target && r.after.target &&
    r.after.target.hp<r.before.target.hp &&
    r.after.target.auras.some((a)=>a.kind==='stun' && a.remaining>2) &&
    r.after.cls==='warrior' &&
    r.after.resourceType==='rage'
  );

const passed=leapPassed && whirlwindPassed && faultlinePassed && criticalErrors.length===0;
const report={
  leapIds,leap,
  whirlIds,whirlwind,
  faultIds,faultline,
  gates:{leapPassed,whirlwindPassed,faultlinePassed},
  criticalErrors,
  passed,
};
fs.writeFileSync('../skill-lab2-wp-main3-gold-report.json',JSON.stringify(report,null,2));
console.log('HIGHFLY_SKILL_LAB2_WP_MAIN3_GOLD_REPORT');
console.log(JSON.stringify(report,null,2));

await browser.close();
if(!passed) process.exitCode=2;
