import fs from 'node:fs';
import { chromium } from 'playwright';

const BASE='http://127.0.0.1:4190/u6-mobile-runtime-probe/';
const browser=await chromium.launch({channel:'chrome',headless:true});
const page=await browser.newPage({viewport:{width:1600,height:900}});
const errors=[];
page.on('pageerror',(e)=>errors.push(String(e)));

await page.goto(BASE+'?affinitylab=1&labclass=warrior',{waitUntil:'domcontentloaded',timeout:60000});
await page.waitForFunction(()=>Boolean(window.__game?.sim?.player),null,{timeout:90000});
await page.waitForFunction(()=>Boolean(window.__highflyAffinityLab&&window.__highflyCharacterQ0),null,{timeout:30000});
await page.waitForSelector('#hf-affinity-hud',{timeout:30000});

await page.evaluate(()=>{
  const sim=window.__game.sim,p=sim.player;
  const target=[...sim.entities.values()].find(e=>e.id!==p.id&&e.kind==='mob'&&e.ownerId==null);
  if(target)p.targetId=target.id;
});

async function waitCameraStable(){
  let last=null,stable=0;
  for(let i=0;i<40;i++){
    const cam=await page.evaluate(()=>{
      const g=window.__game;
      const c=g?.renderer?.camera;
      return c?[c.position.x,c.position.y,c.position.z,c.rotation.x,c.rotation.y,c.rotation.z]:null;
    });
    if(cam&&last){
      const maxDelta=Math.max(...cam.map((v,i)=>Math.abs(v-last[i])));
      stable=maxDelta<0.01?stable+1:0;
      if(stable>=3)return cam;
    }
    last=cam;
    await page.waitForTimeout(100);
  }
  return last;
}

const visualSignature=()=>page.evaluate(()=>{
  const g=window.__game,sim=g?.sim,p=sim?.player;
  const view=p?g?.renderer?.views?.get(p.id):null;
  const visual=view?.visual??null;
  let verts=0,tris=0,meshes=0,skinned=0;
  visual?.root?.traverse?.((o)=>{
    if(!o?.isMesh||!o.geometry)return;
    const pos=o.geometry.getAttribute?.('position');
    if(!pos)return;
    meshes++;
    if(o.isSkinnedMesh)skinned++;
    verts+=pos.count??0;
    const idx=o.geometry.getIndex?.();
    tris+=idx?Math.floor(idx.count/3):Math.floor((pos.count??0)/3);
  });
  return {
    visualKey:view?.visualKey??null,
    modular:Boolean(visual?.modularLook),
    verts,tris,meshes,skinned,
  };
});

const snapshot=()=>page.evaluate(()=>{
  const g=window.__game,sim=g.sim,p=sim.player,m=sim.meta(p.id);
  const cam=g.renderer?.camera;
  return {
    playerId:p.id,
    cls:m?.cls,
    spec:p.specId??m?.spec??null,
    level:p.level,
    hp:p.hp,
    maxHp:p.maxHp,
    resource:p.resource,
    maxResource:p.maxResource,
    resourceType:p.resourceType,
    targetId:p.targetId,
    equipment:JSON.stringify(m?.equipment??null),
    cooldowns:JSON.stringify([...p.cooldowns.entries()].sort((a,b)=>String(a[0]).localeCompare(String(b[0])))),
    pos:[p.pos.x,p.pos.y,p.pos.z],
    facing:p.facing,
    camera:cam?[cam.position.x,cam.position.y,cam.position.z,cam.rotation.x,cam.rotation.y,cam.rotation.z]:null,
  };
});

await waitCameraStable();
const initial=await snapshot();
const swaps=[];
for(const body of ['qmale','qfemale','claude']){
  await page.evaluate((b)=>window.__highflyCharacterQ0.selectBody(b),body);
  const want=body==='claude'?'player_warrior':'player_warrior_'+body;
  await page.waitForFunction((key)=>{
    const g=window.__game,p=g?.sim?.player;
    return Boolean(p&&g.renderer?.views?.get(p.id)?.visualKey===key);
  },want,{timeout:20000});
  const after=await snapshot();
  const visual=await visualSignature();
  swaps.push({body,want,after,visual});
}

function cameraNear(a,b,tol=0.05){
  if(a===null||b===null)return a===b;
  if(a.length!==b.length)return false;
  return a.every((v,i)=>Math.abs(v-b[i])<=tol);
}
function eqStable(a,b){
  return a.playerId===b.playerId&&a.cls===b.cls&&a.spec===b.spec&&a.level===b.level&&
    a.hp===b.hp&&a.maxHp===b.maxHp&&a.resource===b.resource&&a.maxResource===b.maxResource&&
    a.resourceType===b.resourceType&&a.targetId===b.targetId&&a.equipment===b.equipment&&
    a.cooldowns===b.cooldowns&&JSON.stringify(a.pos)===JSON.stringify(b.pos)&&
    a.facing===b.facing&&cameraNear(a.camera,b.camera);
}

const casts=[];
for(const body of ['claude','qmale','qfemale']){
  await page.evaluate((b)=>window.__highflyCharacterQ0.selectBody(b),body);
  const want=body==='claude'?'player_warrior':'player_warrior_'+body;
  await page.waitForFunction((key)=>{
    const g=window.__game,p=g?.sim?.player;
    return Boolean(p&&g.renderer?.views?.get(p.id)?.visualKey===key);
  },want,{timeout:20000});

  const before=await page.evaluate(()=>{
    const sim=window.__game.sim,p=sim.player;
    return {cls:sim.meta(p.id)?.cls,spec:p.specId??sim.meta(p.id)?.spec??null};
  });
  await page.evaluate(()=>window.__highflyAffinityLab.castReceptor('heroic_leap','fire'));
  await page.waitForFunction(()=>{
    const sim=window.__game?.sim,p=sim?.player,t=p?sim.entities.get(p.targetId):null;
    return Boolean(t&&t.hp<t.maxHp);
  },null,{timeout:12000});
  const after=await page.evaluate(()=>{
    const sim=window.__game.sim,p=sim.player,t=sim.entities.get(p.targetId),m=sim.meta(p.id);
    return {cls:m?.cls,spec:p.specId??m?.spec??null,targetHp:t?.hp??null,targetMaxHp:t?.maxHp??null,visualKey:window.__game.renderer?.views?.get(p.id)?.visualKey??null};
  });
  casts.push({body,before,after,stable:before.cls===after.cls&&before.spec===after.spec,damaged:after.targetHp<after.targetMaxHp});
}

const buttons=await page.locator('.hf-q0-body').allTextContents();
const collapseText=await page.locator('.hf-q0-collapse').textContent();
await page.evaluate(()=>window.__highflyCharacterQ0.toggleCollapsed());
await page.waitForFunction(()=>document.querySelector('#hf-affinity-hud')?.classList.contains('hf-collapsed'),null,{timeout:3000});
const collapsedButtons=await page.locator('.hf-q0-body').allTextContents();
const activeBadge=await page.locator('#hf-q0-active').textContent();
const report={
  initial,swaps,casts,buttons,collapseText,collapsedButtons,activeBadge,errors,
  passed:
    swaps.length===3&&swaps.every(x=>eqStable(initial,x.after))&&
    swaps.every(x=>x.visual.visualKey===x.want)&&
    swaps.filter(x=>x.body!=='claude').every(x=>x.visual.modular===false&&x.visual.skinned>0)&&
    new Set(swaps.map(x=>x.visual.verts+':'+x.visual.tris)).size===3&&
    casts.length===3&&casts.every(x=>x.stable&&x.damaged)&&
    ['CLAUDE','Q-MALE','Q-FEMALE'].every(x=>buttons.includes(x))&&
    ['CLAUDE','Q-MALE','Q-FEMALE'].every(x=>collapsedButtons.includes(x))&&
    /MINIMIZAR|MAXIMIZAR/.test(collapseText??'')&&
    /ACTIVO:/.test(activeBadge??'')&&
    errors.filter(x=>/TypeError|ReferenceError|SyntaxError|RangeError/.test(x)).length===0,
};
fs.writeFileSync('../character-q0-smoke-report.json',JSON.stringify(report,null,2));
await page.screenshot({path:'../character-q0-warrior.png',fullPage:true});
console.log('HIGHFLY_CHARACTER_Q0_SMOKE');
console.log(JSON.stringify(report,null,2));
await browser.close();
if(!report.passed)process.exitCode=2;
