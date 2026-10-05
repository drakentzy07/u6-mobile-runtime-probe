import fs from 'node:fs';
import { chromium } from 'playwright';

const BASE='http://127.0.0.1:4190/u6-mobile-runtime-probe/';
const CLASSES=['warrior','paladin','hunter','rogue','priest','shaman','mage','warlock','druid'];
const browser=await chromium.launch({channel:'chrome',headless:true});
const page=await browser.newPage({viewport:{width:1600,height:900}});
const rows=[];
const pageErrors=[];
page.on('pageerror',(e)=>pageErrors.push(String(e)));

async function ready(cls){
  await page.goto(BASE+'?affinitylab=1&labclass='+cls+'&q0test=9x9',{waitUntil:'domcontentloaded',timeout:60000});
  await page.waitForFunction((want)=>{
    const g=window.__game, p=g?.sim?.player;
    return Boolean(p&&window.__highflyCharacterQ0&&window.__highflyAffinityLab&&g.sim.meta(p.id)?.cls===want);
  },cls,{timeout:90000});
  await page.waitForSelector('#hf-affinity-hud',{timeout:30000});
}

async function select(body,cls){
  await page.evaluate((b)=>window.__highflyCharacterQ0.selectBody(b),body);
  const want=body==='claude'?'player_'+cls:'player_'+cls+'_'+body;
  await page.waitForFunction((key)=>{
    const g=window.__game,p=g?.sim?.player;
    return Boolean(p&&g.renderer?.views?.get(p.id)?.visualKey===key);
  },want,{timeout:25000});
  await page.waitForTimeout(250);
}

async function inspect(){
  return page.evaluate(()=>{
    const g=window.__game,sim=g.sim,p=sim.player,m=sim.meta(p.id);
    const view=g.renderer?.views?.get(p.id);
    const v=view?.visual;
    let held=0,weaponMeshes=0,skinned=0,verts=0;
    v?.root?.traverse?.((o)=>{
      if(o?.userData?.heldPropHolder===true)held++;
      if(o?.userData?.weaponMesh===true)weaponMeshes++;
      if(o?.isSkinnedMesh)skinned++;
      const pos=o?.geometry?.getAttribute?.('position');
      if(pos)verts+=pos.count??0;
    });
    const model=v?.model;
    const hasR=Boolean(model?.getObjectByName?.('handslotr')??model?.getObjectByName?.('handslot.r'));
    const hasL=Boolean(model?.getObjectByName?.('handslotl')??model?.getObjectByName?.('handslot.l'));
    const actions=v?.actions instanceof Map ? [...v.actions.keys()].sort() : [];
    return {
      cls:m?.cls??null,
      spec:p.specId??m?.spec??null,
      visualKey:view?.visualKey??null,
      modular:Boolean(v?.modularLook),
      hasR,hasL,held,weaponMeshes,skinned,verts,actions,
      playerId:p.id,level:p.level,hp:p.hp,maxHp:p.maxHp,resource:p.resource,maxResource:p.maxResource,
      pos:[p.pos.x,p.pos.y,p.pos.z],facing:p.facing,targetId:p.targetId,
    };
  });
}

function sameCore(a,b){
  return a.cls===b.cls&&a.spec===b.spec&&a.playerId===b.playerId&&a.level===b.level&&
    a.hp===b.hp&&a.maxHp===b.maxHp&&a.resource===b.resource&&a.maxResource===b.maxResource&&
    JSON.stringify(a.pos)===JSON.stringify(b.pos)&&a.facing===b.facing&&a.targetId===b.targetId;
}
function sameActions(a,b){return JSON.stringify(a.actions)===JSON.stringify(b.actions);}

for(const cls of CLASSES){
  await ready(cls);
  await select('claude',cls);
  const claude=await inspect();
  const classButtons=await page.locator('.hf-aff-classpick').allTextContents();

  await select('qmale',cls);
  const qmale=await inspect();

  await select('qfemale',cls);
  const qfemale=await inspect();

  const expectedButtons=CLASSES.map(x=>x.toUpperCase());
  const row={
    cls,claude,qmale,qfemale,classButtons,
    classButtonsOk:expectedButtons.every(x=>classButtons.includes(x)),
    qmaleParity:sameCore(claude,qmale)&&sameActions(claude,qmale)&&qmale.hasR&&qmale.hasL&&qmale.modular===false&&qmale.held===claude.held&&qmale.weaponMeshes===claude.weaponMeshes,
    qfemaleParity:sameCore(claude,qfemale)&&sameActions(claude,qfemale)&&qfemale.hasR&&qfemale.hasL&&qfemale.modular===false&&qfemale.held===claude.held&&qfemale.weaponMeshes===claude.weaponMeshes,
    distinctBodies:new Set([claude.verts,qmale.verts,qfemale.verts]).size===3,
  };
  rows.push(row);
  await page.screenshot({path:'../character-q0-9x9-'+cls+'.png',fullPage:true});
}

const report={
  rows,pageErrors,
  passed:rows.length===9&&rows.every(r=>r.classButtonsOk&&r.qmaleParity&&r.qfemaleParity&&r.distinctBodies)&&
    pageErrors.filter(x=>/TypeError|ReferenceError|SyntaxError|RangeError/.test(x)).length===0,
};
fs.writeFileSync('../character-q0-9x9-report.json',JSON.stringify(report,null,2));
console.log('HIGHFLY_CHARACTER_Q0_9X9');
console.log(JSON.stringify(report,null,2));
await browser.close();
if(!report.passed)process.exitCode=2;
