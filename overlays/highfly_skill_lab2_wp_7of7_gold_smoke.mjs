import fs from 'node:fs';
import { chromium } from 'playwright';

const BASE='http://127.0.0.1:4179/u6-mobile-runtime-probe/';
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

async function commonSnapshot() {
  return page.evaluate(()=>{
    const sim=window.__game.sim;
    const p=sim.player;
    const target=sim.entities.get(p.targetId);
    return {
      cls:sim.meta(p.id)?.cls,
      resourceType:p.resourceType,
      resource:p.resource,
      hp:p.hp,
      maxHp:p.maxHp,
      equipment:JSON.stringify(sim.meta(p.id)?.equipment),
      devotion:Boolean(p.paladinDevotion),
      target:target?{
        id:target.id,
        hp:target.hp,
        maxHp:target.maxHp,
        dead:target.dead,
      }:null,
      valkyr:Boolean(p.valkyrsCalling),
      jumping:p.jumping,
      onGround:p.onGround,
      channeling:p.channeling,
      castingAbility:p.castingAbility,
      shieldWall:p.auras.some((a)=>a.kind==='shield_wall' && a.sourceId===p.id),
      speed:p.auras.some((a)=>a.kind==='buff_speed' && a.sourceId===p.id),
      groundZones:sim.groundAoEs?.filter((z)=>z.sourceId===p.id).length ?? 0,
    };
  });
}

async function hardReset({hurt=false}={}) {
  await page.evaluate((shouldHurt)=>{
    const sim=window.__game.sim;
    const p=sim.player;
    window.__highflySkillLab.reset();

    p.cooldowns.clear();
    p.gcdRemaining=0;
    p.resource=p.maxResource;
    p.valkyrsCalling=null;
    p.jumping=false;
    p.onGround=true;
    p.channeling=false;
    p.castingAbility=null;
    p.castRemaining=0;
    p.channelTicksLeft=0;
    p.auras=p.auras.filter((a)=>
      a.kind!=='shield_wall' &&
      a.kind!=='buff_speed' &&
      a.kind!=='sudden_death'
    );
    if(Array.isArray(sim.groundAoEs)) sim.groundAoEs.length=0;

    const target=sim.entities.get(p.targetId);
    if(target){
      target.dead=false;
      target.hp=target.maxHp;
      target.auras=target.auras.filter((a)=>a.kind!=='stun');
    }
    p.hp=shouldHurt?Math.max(1,p.maxHp-500):p.maxHp;
    sim.events=[];
  },hurt);
}

await openLineage('execute');
const executeIds=await ids();
const execute=[];
for(const id of [executeIds.base,executeIds.evo,executeIds.mutation]) {
  await hardReset();
  await page.evaluate(()=>{
    const sim=window.__game.sim;
    const target=sim.entities.get(sim.player.targetId);
    if(target) target.hp=Math.max(1,Math.floor(target.maxHp*0.19));
  });
  const before=await commonSnapshot();
  await page.evaluate((abilityId)=>window.__highflySkillLab.cast(abilityId),id);
  await page.waitForTimeout(450);
  const after=await commonSnapshot();
  execute.push({id,before,after});
}
await page.screenshot({path:'../skill-lab2-wp-7of7-execute.png',fullPage:true});

await openLineage('consecration');
const groundIds=await ids();
const ground=[];
for(const id of [groundIds.base,groundIds.evo,groundIds.mutation]) {
  await hardReset();
  const before=await commonSnapshot();
  await page.evaluate((abilityId)=>window.__highflySkillLab.cast(abilityId),id);
  await page.waitForTimeout(1350);
  const after=await commonSnapshot();
  ground.push({id,before,after});
}
await page.screenshot({path:'../skill-lab2-wp-7of7-ground.png',fullPage:true});

await openLineage('valkyr');
const valkyrIds=await ids();
const valkyr=[];
for(const id of [valkyrIds.base,valkyrIds.evo,valkyrIds.mutation]) {
  await hardReset();
  const before=await commonSnapshot();
  await page.evaluate((abilityId)=>window.__highflySkillLab.cast(abilityId),id);
  await page.waitForFunction(
    ()=>Boolean(window.__game?.sim?.player?.valkyrsCalling),
    null,
    {timeout:3000},
  );
  await page.waitForFunction(
    ()=>{
      const p=window.__game?.sim?.player;
      return Boolean(p && !p.valkyrsCalling && p.onGround && !p.jumping);
    },
    null,
    {timeout:10000},
  );
  await page.waitForTimeout(150);
  const after=await commonSnapshot();
  valkyr.push({id,before,after});
}
await page.screenshot({path:'../skill-lab2-wp-7of7-valkyr.png',fullPage:true});

await openLineage('aegis');
const aegisIds=await ids();
const aegis=[];
for(const id of [aegisIds.base,aegisIds.evo,aegisIds.mutation]) {
  await hardReset({hurt:true});
  const before=await commonSnapshot();
  await page.evaluate((abilityId)=>window.__highflySkillLab.cast(abilityId),id);
  await page.waitForFunction(
    ()=>Boolean(window.__game?.sim?.player?.channeling),
    null,
    {timeout:3000},
  );
  const during=await commonSnapshot();
  await page.waitForFunction(
    ()=>{
      const p=window.__game?.sim?.player;
      return Boolean(p && !p.channeling && p.castingAbility==null);
    },
    null,
    {timeout:10000},
  );
  await page.waitForTimeout(150);
  const after=await commonSnapshot();
  aegis.push({id,before,during,after});
}
await page.screenshot({path:'../skill-lab2-wp-7of7-aegis.png',fullPage:true});

const criticalErrors=[...new Set([...pageErrors,...consoleErrors])].filter((message)=>{
  if(/character visual unavailable, skipping view/i.test(message)) return false;
  if(/THREE\.GLTFLoader: Couldn't load texture blob:/i.test(message)) return false;
  if(/Failed to load resource:.*(?:404|502)/i.test(message)) return false;
  return /TypeError|ReferenceError|SyntaxError|RangeError|WebGL.*Context Lost/i.test(message);
});

function identityStable(r){
  return r.after.cls==='warrior' &&
    r.after.resourceType==='rage' &&
    !r.after.devotion &&
    r.after.equipment===r.before.equipment;
}

const executePassed=
  executeIds.activeClass==='warrior' &&
  executeIds.base==='execute' &&
  executeIds.evo==='hf_bloody_verdict_01' &&
  executeIds.mutation==='hf_kings_end_01' &&
  execute.every((r)=>
    r.before.target && r.after.target &&
    r.after.target.hp<r.before.target.hp &&
    r.after.resource===r.before.resource-15 &&
    identityStable(r)
  );

const groundPassed=
  groundIds.activeClass==='warrior' &&
  groundIds.base==='hf_wp_consecration_01' &&
  groundIds.evo==='hf_wp_radiant_sanctuary_01' &&
  groundIds.mutation==='hf_wp_dawn_domain_01' &&
  ground.every((r)=>
    r.before.target && r.after.target &&
    r.after.target.hp<r.before.target.hp &&
    r.after.groundZones===1 &&
    r.after.resource===r.before.resource-20 &&
    identityStable(r)
  );

const valkyrPassed=
  valkyrIds.activeClass==='warrior' &&
  valkyrIds.base==='hf_wp_valkyrs_calling_01' &&
  valkyrIds.evo==='hf_wp_valkyr_descent_01' &&
  valkyrIds.mutation==='hf_wp_divine_descent_01' &&
  valkyr.every((r)=>
    r.before.target && r.after.target &&
    r.after.target.hp<r.before.target.hp &&
    !r.after.valkyr &&
    !r.after.jumping &&
    r.after.onGround &&
    r.after.resource===r.before.resource-35 &&
    identityStable(r)
  );

const aegisPassed=
  aegisIds.activeClass==='warrior' &&
  aegisIds.base==='hf_wp_aegis_first_dawn_01' &&
  aegisIds.evo==='hf_wp_dawn_aegis_01' &&
  aegisIds.mutation==='hf_wp_unbreakable_dawn_01' &&
  aegis.every((r)=>
    r.during.channeling &&
    r.during.shieldWall &&
    !r.after.channeling &&
    !r.after.shieldWall &&
    r.after.speed &&
    r.after.hp>r.before.hp &&
    r.after.resource===r.before.resource-60 &&
    identityStable(r)
  );

const passed=executePassed && groundPassed && valkyrPassed && aegisPassed && criticalErrors.length===0;
const report={
  executeIds,execute,
  groundIds,ground,
  valkyrIds,valkyr,
  aegisIds,aegis,
  gates:{executePassed,groundPassed,valkyrPassed,aegisPassed},
  criticalErrors,
  passed,
};
fs.writeFileSync('../skill-lab2-wp-7of7-gold-report.json',JSON.stringify(report,null,2));
console.log('HIGHFLY_SKILL_LAB2_WP_7OF7_GOLD_REPORT');
console.log(JSON.stringify(report,null,2));

await browser.close();
if(!passed) process.exitCode=2;
