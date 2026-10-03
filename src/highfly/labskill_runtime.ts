import '../styles/hf_labskill.css';
import {
  HIGHFLY_LABSKILL_LOADOUTS,
  HIGHFLY_LABSKILL_STAGE_INDEX,
  type LabSkillSeat,
  type LabSkillStage,
} from './labskill_loadouts';

const w=window as any;
const PRINCIPALS=new Set(['warrior','rogue','mage','hunter']);
let stage:LabSkillStage=(localStorage.getItem('hf_labskill_stage') as LabSkillStage)||'mutation';
if(!Object.prototype.hasOwnProperty.call(HIGHFLY_LABSKILL_STAGE_INDEX,stage))stage='mutation';

function creatorWiring():void{
  const desired=sessionStorage.getItem('hf_labskill_pick');
  const chips=[...document.querySelectorAll<HTMLButtonElement>('#offline-select .mini-class')];
  for(const chip of chips){
    chip.addEventListener('click',()=>sessionStorage.setItem('hf_labskill_pick',chip.dataset.class||'warrior'));
  }
  if(desired&&PRINCIPALS.has(desired)){
    setTimeout(()=>document.querySelector<HTMLButtonElement>('#offline-select .mini-class[data-class="'+desired+'"]')?.click(),350);
  }
}

function game():any{return w.__game?.sim?w.__game:null}

function metaClass():string|null{
  const g=game();if(!g)return null;
  const p=g.sim.player;
  return g.sim.meta(p.id)?.cls??null;
}

function stageUndead():any{
  const g=game();if(!g)return null;
  const sim=g.sim,p=sim.player;
  let undead=[...sim.entities.values()].find((e:any)=>e.kind==='mob'&&e.ownerId===p.id&&!e.dead&&e.templateId==='necromancy_skeletal_warrior');
  if(undead)return undead;
  undead=[...sim.entities.values()].find((e:any)=>e.kind==='mob'&&e.ownerId==null&&!e.dead&&e.id!==p.targetId);
  if(!undead)return null;
  undead.ownerId=p.id;
  undead.templateId='necromancy_skeletal_warrior';
  undead.hostile=false;
  undead.aiState='idle';
  undead.aggroTargetId=null;
  undead.inCombat=false;
  undead.tappedById=null;
  undead.loot=null;
  undead.lootable=false;
  undead.moveSpeed=0;
  return undead;
}

function stageDummy(distance=7,executeWindow=false):any{
  const g=game();if(!g)return null;
  const sim=g.sim,p=sim.player;
  let t=[...sim.entities.values()].find((e:any)=>e.id!==p.id&&e.kind==='mob'&&!e.dead&&e.ownerId==null);
  if(!t)return null;
  const x=p.pos.x+Math.sin(p.facing)*distance;
  const z=p.pos.z+Math.cos(p.facing)*distance;
  t.dead=false;
  t.hostile=true;
  t.maxHp=Math.max(t.maxHp||1,50000);
  t.hp=executeWindow?Math.max(1,Math.floor(t.maxHp*.19)):t.maxHp;
  t.pos.x=x;t.pos.y=p.pos.y;t.pos.z=z;t.facing=p.facing;
  t.aiState='idle';t.aggroTargetId=null;t.inCombat=false;t.moveSpeed=0;
  if(t.prevPos){t.prevPos.x=x;t.prevPos.y=p.pos.y;t.prevPos.z=z}
  p.targetId=t.id;
  return t;
}

function addAuraOnce(aura:any):void{
  const g=game();if(!g)return;
  const p=g.sim.player;
  if(!p.auras.some((a:any)=>a.id===aura.id))p.auras.push(aura);
}

function prepareSeat(seat:LabSkillSeat,id:string):{sim:any;p:any;target:any}|null{
  const g=game();if(!g)return null;
  const sim=g.sim,p=sim.player;
  sim.setPlayerLevel(20);
  if(seat.spec)sim.setSpec(seat.spec);
  p.hp=p.maxHp;
  p.resource=p.maxResource;
  p.gcdRemaining=0;
  p.gm=true;
  p.cooldowns.delete(id);
  if(seat.combo)p.comboPoints=seat.combo;
  if(seat.hurtSelf)p.hp=Math.max(1,p.maxHp-500);
  if(seat.stealth){
    p.auras=p.auras.filter((a:any)=>a.kind!=='stealth');
    p.auras.push({id:'hf_labskill_stealth',name:'Duskveil',kind:'stealth',value:.5,remaining:3600,duration:3600,sourceId:p.id,school:'physical'});
  }
  if(seat.heritageUndead){
    addAuraOnce({id:'soul_fragments',name:'Soul Fragments',kind:'soul_fragments',value:5,stacks:5,remaining:3600,duration:3600,sourceId:p.id,school:'shadow'});
    const frag=p.auras.find((a:any)=>a.kind==='soul_fragments');
    if(frag){frag.value=5;frag.stacks=5;frag.remaining=3600}
    stageUndead();
  }
  if(seat.moontideMutation&&stage==='mutation'){
    const mt=p.auras.find((a:any)=>a.kind==='moontide'&&a.sourceId===p.id);
    if(mt){mt.stacks=3;mt.remaining=3600;mt.duration=3600}
    else p.auras.push({id:'moontide',name:'Moontide',kind:'moontide',value:0,stacks:3,remaining:3600,duration:3600,sourceId:p.id,school:'nature'});
  }
  const target=(seat.target==='enemy'||seat.target==='position')
    ?stageDummy(seat.dummyDistance??(p.resourceType==='energy'?2.5:8),seat.executeWindow===true)
    :null;
  return {sim,p,target};
}

let toastTimer:number|undefined;
function toast(text:string,bad=false):void{
  let el=document.getElementById('hf-labskill-toast');
  if(!el){el=document.createElement('div');el.id='hf-labskill-toast';document.body.append(el)}
  el.textContent=text;
  el.style.color=bad?'#ffaaaa':'#c9edff';
  el.classList.add('hf-show');
  if(toastTimer)clearTimeout(toastTimer);
  toastTimer=window.setTimeout(()=>el?.classList.remove('hf-show'),1600);
}

function castSeat(seat:LabSkillSeat):void{
  const idx=HIGHFLY_LABSKILL_STAGE_INDEX[stage];
  const id=seat.ids[idx];
  const prep=prepareSeat(seat,id);
  if(!prep){toast('JUEGO NO LISTO',true);return}
  const {sim,p,target}=prep;
  try{
    if(seat.target==='position'){
      const x=p.pos.x+Math.sin(p.facing)*8;
      const z=p.pos.z+Math.cos(p.facing)*8;
      sim.castAbility(id,p.id,{x,z});
    }else if(seat.target==='self'){
      sim.castAbility(id,p.id,p.id);
    }else if(seat.target==='none'){
      sim.castAbility(id,p.id);
    }else if(target){
      sim.castAbility(id,p.id,target.id);
    }else{
      throw new Error('Sin dummy disponible');
    }
    toast(seat.label+' · '+seat.names[idx]);
  }catch(err){
    toast('ERROR · '+String(err),true);
  }
}

function resetLab():void{
  const g=game();if(!g)return;
  const p=g.sim.player;
  p.cooldowns.clear();
  p.gcdRemaining=0;
  p.resource=p.maxResource;
  p.hp=p.maxHp;
  p.castingAbility=null;
  p.channeling=false;
  p.leap=null;
  stageDummy(8,false);
  toast('RESET OK');
}

function renderHud(cls:string):void{
  const loadout=HIGHFLY_LABSKILL_LOADOUTS[cls];if(!loadout)return;
  document.body.classList.add('hf-labskill-active');
  document.getElementById('hf-labskill-hud')?.remove();
  const root=document.createElement('div');
  root.id='hf-labskill-hud';

  const header=document.createElement('div');
  header.className='hf-ls-head';
  header.innerHTML=
    '<span class="hf-ls-title">HIGHFLY LABSKILL</span>'+
    '<span class="hf-ls-pair">'+loadout.pair+'</span>'+
    '<span class="hf-ls-level">LVL 20</span>'+
    '<span class="hf-ls-spacer"></span>';

  for(const s of ['base','evo','mutation'] as LabSkillStage[]){
    const b=document.createElement('button');
    b.className='hf-ls-stage'+(stage===s?' hf-active':'');
    b.textContent=s==='mutation'?'MUT':s.toUpperCase();
    b.addEventListener('click',()=>{
      stage=s;
      localStorage.setItem('hf_labskill_stage',stage);
      renderHud(cls);
    });
    header.append(b);
  }

  const reset=document.createElement('button');
  reset.className='hf-ls-reset';
  reset.textContent='RESET';
  reset.addEventListener('click',resetLab);
  header.append(reset);

  const change=document.createElement('button');
  change.className='hf-ls-change';
  change.textContent='CAMBIAR';
  change.addEventListener('click',()=>{
    sessionStorage.setItem('hf_labskill_pick',cls);
    location.href=location.pathname;
  });
  header.append(change);
  root.append(header);

  for(const side of ['main','heritage'] as const){
    const row=document.createElement('div');
    row.className='hf-ls-row hf-ls-row-'+side;
    const tag=document.createElement('div');
    tag.className='hf-ls-side';
    tag.textContent=side==='main'?'MAIN':'HERITAGE';
    row.append(tag);

    loadout.seats.filter((x)=>x.side===side).forEach((seat)=>{
      const idx=HIGHFLY_LABSKILL_STAGE_INDEX[stage];
      const b=document.createElement('button');
      b.className='hf-ls-skill';
      b.dataset.slot=seat.label;
      b.dataset.ability=seat.ids[idx];
      b.innerHTML=
        '<span class="hf-ls-key">'+seat.label+'</span>'+
        '<span class="hf-ls-name">'+seat.names[idx]+'</span>';
      b.title=seat.label+' · '+seat.names[idx];
      b.addEventListener('click',()=>castSeat(seat));
      row.append(b);
    });
    root.append(row);
  }
  document.body.append(root);
}

function boot():void{
  const params=new URLSearchParams(location.search);
  const legacySkillLab=params.get('skilllab')==='1';
  const dedicatedLabSkill=params.get('labskill')==='1';
  if(legacySkillLab && !dedicatedLabSkill) return;

  creatorWiring();
  let lastClass:string|null=null;
  const tick=()=>{
    const cls=metaClass();
    if(cls&&PRINCIPALS.has(cls)){
      const g=game();
      const p=g.sim.player;
      const meta=g.sim.meta(p.id);
      if(meta?.level!==20) g.sim.setPlayerLevel(20);
      if(cls!==lastClass||!document.getElementById('hf-labskill-hud')){
        lastClass=cls;
        renderHud(cls);
      }
    }
    window.setTimeout(tick,250);
  };
  tick();
}

if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',boot,{once:true});
else boot();

w.__highflyLabSkill={
  loadouts:HIGHFLY_LABSKILL_LOADOUTS,
  stage:()=>stage,
  castSlot:(slot:string)=>{
    const cls=metaClass();
    const l=cls?HIGHFLY_LABSKILL_LOADOUTS[cls]:null;
    const seat=l?.seats.find((x)=>x.label===slot);
    if(seat)castSeat(seat);
  },
};
