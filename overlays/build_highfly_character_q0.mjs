import fs from 'node:fs';
import path from 'node:path';
import { manualRigOntoReference } from './scripts/asset_pipeline/lib/manual_rig.mjs';
import { openGlb } from './scripts/asset_pipeline/lib/glb.mjs';

const root=process.cwd();
const reference=path.resolve(root,'public/models/chars/players/knight.glb');
const sourceRoot=path.resolve(root,'../character_q0/work');
const outRoot=path.resolve(root,'public/models/chars/players/q0');
fs.mkdirSync(outRoot,{recursive:true});

const expectedJoints=[
  'root','hips','spine','chest',
  'upperarm.l','lowerarm.l','wrist.l','hand.l','handslot.l',
  'upperarm.r','lowerarm.r','wrist.r','hand.r','handslot.r',
  'head',
  'upperleg.r','lowerleg.r','foot.r','toes.r',
  'upperleg.l','lowerleg.l','foot.l','toes.l',
];

async function inspect(file){
  const doc=await openGlb(file);
  const r=doc.getRoot();
  const skin=r.listSkins()[0];
  if(!skin)throw new Error(file+' has no skin');
  const joints=skin.listJoints().map(j=>j.getName());
  const animations=r.listAnimations().map(a=>a.getName());
  let verts=0,tris=0,materials=new Set();
  for(const mesh of r.listMeshes()){
    for(const prim of mesh.listPrimitives()){
      const pos=prim.getAttribute('POSITION');
      if(pos)verts+=pos.getCount();
      const idx=prim.getIndices();
      tris+=idx?Math.floor(idx.getCount()/3):Math.floor((pos?.getCount()??0)/3);
      if(prim.getMaterial())materials.add(prim.getMaterial().getName()||'material');
    }
  }
  return {bytes:fs.statSync(file).size,joints,animations,verts,tris,materials:[...materials]};
}

const jobs=[
  {id:'qmale',src:'q0_male_raw.glb',out:'hf_q0_male_rigmedium.glb'},
  {id:'qfemale',src:'q0_female_raw.glb',out:'hf_q0_female_rigmedium.glb'},
];

const referenceInfo=await inspect(reference);
const report={reference:{file:'knight.glb',...referenceInfo},candidates:{}};

for(const job of jobs){
  const src=path.join(sourceRoot,job.src);
  const out=path.join(outRoot,job.out);
  if(!fs.existsSync(src))throw new Error('missing Q0 donor '+src);
  const fit=await manualRigOntoReference(src,reference,out,{
    preRotated:true,
    centerTorso:true,
    influences:4,
    falloff:4,
  });
  const info=await inspect(out);
  const sameJoints=JSON.stringify(info.joints)===JSON.stringify(referenceInfo.joints);
  const requiredSockets=['handslot.r','handslot.l'].every(n=>info.joints.includes(n));
  const originalRigLeak=info.joints.some(n=>['pelvis','spine_01','spine_02','spine_03','clavicle_l','clavicle_r'].includes(n));
  if(!sameJoints)throw new Error(job.id+' joint vocabulary differs from canonical Rig_Medium');
  if(!requiredSockets)throw new Error(job.id+' missing handslot sockets');
  if(originalRigLeak)throw new Error(job.id+' leaked Quaternius runtime joints');
  if(info.animations.length!==referenceInfo.animations.length)throw new Error(job.id+' clip count differs from reference');
  report.candidates[job.id]={file:job.out,fit,...info,sameJoints,requiredSockets,originalRigLeak};
}

fs.writeFileSync(path.resolve(root,'../character-q0-rig-report.json'),JSON.stringify(report,null,2));
console.log('HIGHFLY_CHARACTER_Q0_RIG_REPORT');
console.log(JSON.stringify(report,null,2));
