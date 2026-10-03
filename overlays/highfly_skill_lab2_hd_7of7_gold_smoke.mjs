import fs from 'node:fs';
import { chromium } from 'playwright';

const BASE='http://127.0.0.1:4188/u6-mobile-runtime-probe/';
const browser=await chromium.launch({channel:'chrome',headless:true});
const page=await browser.newPage({viewport:{width:1440,height:900}});
const pageErrors=[],consoleErrors=[];
page.on('pageerror',(e)=>pageErrors.push(String(e)));
page.on('console',(m)=>{if(m.type()==='error')consoleErrors.push(m.text());});

const EXPECTED={
  frostjaw:['frostjaw_trap','hf_hunter_prison_01','hf_hd_boreal_domain_01'],
  fevered:['rapid_fire','hf_hd_frenzied_salvo_01','hf_hd_predator_rain_01'],
  volley:['volley','hf_hd_arrow_storm_01','hf_hd_hunters_judgment_01'],
  stampede:['stampede','hf_hd_wild_pack_01','hf_hd_kings_hunt_01'],
  moonseed:['hf_hd_moonseed_01','hf_hd_crescent_seed_01','hf_hd_lunar_wave_01'],
  lunar_tempest:['hf_hd_lunar_tempest_01','hf_hd_lunar_eclipse_01','hf_hd_eternal_night_01'],
  galeheart:['hf_hd_galeheart_01','hf_hd_wild_hurricane_01','hf_hd_natures_wrath_01'],
};

async function openLineage(lineage){
  await page.goto(BASE+'?skilllab=1&labclass=hunter&lablineage='+lineage,{waitUntil:'domcontentloaded',timeout:60000});
  await page.waitForFunction(()=>Boolean(window.__game?.sim?.player&&window.__highflySkillLab),null,{timeout:90000});
  await page.waitForFunction(()=>document.querySelector('#hf-lab-status')?.textContent?.startsWith('LISTO'),null,{timeout:25000});
  return page.evaluate(()=>({
    ids:[
      document.querySelector('#hf-lab-base')?.getAttribute('data-ability'),
      document.querySelector('#hf-lab-evo')?.getAttribute('data-ability'),
      document.querySelector('#hf-lab-mutation')?.getAttribute('data-ability'),
    ],
    activeClass:window.__highflySkillLab.activeClass,
    activeLineage:window.__highflySkillLab.activeLineage,
  }));
}

async function resetFixture(){
  await page.evaluate(()=>{
    const sim=window.__game.sim,p=sim.player;
    window.__highflySkillLab.reset();
    p.cooldowns.clear();
    p.gcdRemaining=0;
    p.resource=p.maxResource;
    p.castingAbility=null;
    p.castRemaining=0;
    p.castTotal=0;
    p.channeling=false;
    p.channelTicksLeft=0;
    p.channelTickTimer=0;
    p.auras=p.auras.filter((a)=>
      a.kind!=='moontide' &&
      a.id!=='hunter_coldsight_read' &&
      a.id!=='hunter_coldsight_fevered_draw_progress'
    );
    if(sim.ctx?.groundAoEs) sim.ctx.groundAoEs.length=0;
    for(const [id,e] of [...sim.entities.entries()]){
      if(e.ownerId===p.id && e.guardianState?.key?.startsWith('hunter_stampede_')) sim.entities.delete(id);
    }
    const t=sim.entities.get(p.targetId);
    if(t){
      t.dead=false;t.hp=t.maxHp;
      t.auras=t.auras.filter((a)=>a.sourceId!==p.id);
      t.aiState='idle';t.aggroTargetId=null;t.inCombat=false;t.moveSpeed=0;
    }
    sim.events=[];
    window.__highflySkillLab.stageDummy();
  });
}

async function snap(id){
  return page.evaluate((abilityId)=>{
    const sim=window.__game.sim,p=sim.player,t=sim.entities.get(p.targetId);
    const meta=sim.meta(p.id);
    const guardians=[...sim.entities.values()].filter((e)=>
      e.ownerId===p.id && e.guardianState?.key?.startsWith('hunter_stampede_') && !e.dead
    );
    const mt=p.auras.find((a)=>a.kind==='moontide'&&a.sourceId===p.id);
    return {
      cls:meta?.cls,
      spec:sim.playerMods(meta)?.spec,
      resourceType:p.resourceType,
      resource:p.resource,
      maxResource:p.maxResource,
      equipment:JSON.stringify(meta?.equipment),
      castingAbility:p.castingAbility,
      channeling:p.channeling,
      cooldown:p.cooldowns.get(abilityId)??0,
      coldsight:p.auras.some((a)=>a.id==='hunter_coldsight_read'),
      moontide:mt?.stacks??0,
      guardians:guardians.length,
      target:t?{
        hp:t.hp,maxHp:t.maxHp,
        rootIds:t.auras.filter((a)=>a.kind==='root').map((a)=>a.id),
        slowed:t.auras.some((a)=>a.kind==='slow'),
        dots:t.auras.filter((a)=>a.kind==='dot'&&a.sourceId===p.id).map((a)=>a.id),
      }:null,
    };
  },id);
}

function identity(before,after){
  return before.cls==='hunter'&&after.cls==='hunter'&&
    before.resourceType==='focus'&&after.resourceType==='focus'&&
    before.equipment===after.equipment;
}

async function castAndGate(lineage,id){
  console.log('HD7_GATE_BEGIN', lineage, id);
  await resetFixture();
  const before=await snap(id);
  await page.evaluate((abilityId)=>window.__highflySkillLab.cast(abilityId),id);

  if(lineage==='frostjaw'){
    await page.waitForFunction((abilityId)=>{
      const sim=window.__game.sim,p=sim.player,t=sim.entities.get(p.targetId);
      return Boolean(t?.auras.some((a)=>a.kind==='root'&&a.id===abilityId+'_freeze'));
    },id,{timeout:15000});
  } else if(lineage==='fevered'){
    await page.waitForFunction((abilityId)=>window.__game.sim.player.castingAbility===abilityId,id,{timeout:5000});
    await page.waitForFunction(()=>window.__game.sim.player.castingAbility===null,null,{timeout:45000});
    await page.waitForFunction(()=>window.__game.sim.player.auras.some((a)=>a.id==='hunter_coldsight_read'),null,{timeout:5000});
  } else if(lineage==='volley'||lineage==='galeheart'){
    await page.waitForFunction((abilityId)=>window.__game.sim.player.castingAbility===abilityId,id,{timeout:5000});
    await page.waitForFunction(()=>window.__game.sim.player.castingAbility===null,null,{timeout:90000});
  } else if(lineage==='stampede'){
    await page.waitForFunction(()=>{
      const sim=window.__game.sim,p=sim.player;
      return [...sim.entities.values()].filter((e)=>e.ownerId===p.id&&e.guardianState?.key?.startsWith('hunter_stampede_')&&!e.dead).length===3;
    },null,{timeout:8000});
  } else {
    await page.waitForFunction((startHp)=>{
      const sim=window.__game.sim,p=sim.player,t=sim.entities.get(p.targetId);
      return Boolean(t&&t.hp<startHp);
    },before.target.hp,{timeout:10000});
  }

  const after=await snap(id);
  let mechanic=false;
  if(lineage==='frostjaw'){
    mechanic=after.target?.rootIds.includes(id+'_freeze')&&after.target?.slowed&&after.target.hp===before.target.hp;
  } else if(lineage==='fevered'){
    mechanic=after.coldsight&&after.target.hp<before.target.hp;
  } else if(lineage==='volley'||lineage==='galeheart'){
    mechanic=after.target.hp<before.target.hp;
  } else if(lineage==='stampede'){
    mechanic=after.guardians===3;
  } else if(lineage==='moonseed'){
    mechanic=id==='hf_hd_lunar_wave_01'
      ? after.target.hp<before.target.hp&&after.moontide<3
      : after.target.hp<before.target.hp&&after.moontide>=1;
  } else if(lineage==='lunar_tempest'){
    mechanic=after.target.hp<before.target.hp&&after.target.dots.includes('moonfire');
  }
  const cooldownOk=(lineage==='lunar_tempest'||id==='hf_hd_lunar_wave_01')?true:after.cooldown>0;
  const result={id,before,after,passed:Boolean(mechanic&&cooldownOk&&identity(before,after))};
  console.log('HD7_GATE_END', lineage, id, JSON.stringify({passed:result.passed,cooldown:after.cooldown,casting:after.castingAbility,hp:after.target?.hp,guardians:after.guardians,moontide:after.moontide,coldsight:after.coldsight}));
  return result;
}

const report={lineages:{},passed:false};
for(const [lineage,expectedIds] of Object.entries(EXPECTED)){
  const header=await openLineage(lineage);
  const idsOk=JSON.stringify(header.ids)===JSON.stringify(expectedIds)&&header.activeClass==='hunter'&&header.activeLineage===lineage;
  const casts=[];
  for(const id of expectedIds) casts.push(await castAndGate(lineage,id));
  report.lineages[lineage]={header,idsOk,casts,passed:idsOk&&casts.every((x)=>x.passed)};
}
await page.screenshot({path:'../skill-lab2-hd-7of7-final.png',fullPage:true});

const criticalErrors=[...new Set([...pageErrors,...consoleErrors])].filter((m)=>{
  if(/character visual unavailable, skipping view/i.test(m))return false;
  if(/THREE\.GLTFLoader: Couldn't load texture blob:/i.test(m))return false;
  if(/Failed to load resource:.*(?:404|502)/i.test(m))return false;
  return /TypeError|ReferenceError|SyntaxError|RangeError|WebGL.*Context Lost/i.test(m);
});
report.criticalErrors=criticalErrors;
report.passed=Object.values(report.lineages).every((x)=>x.passed)&&criticalErrors.length===0;

fs.writeFileSync('../skill-lab2-hd-7of7-gold-report.json',JSON.stringify(report,null,2));
console.log('HIGHFLY_SKILL_LAB2_HD_7OF7_GOLD_REPORT');
console.log(JSON.stringify(report,null,2));

await browser.close();
if(!report.passed)process.exitCode=2;
