import fs from 'node:fs';
const spec=JSON.parse(fs.readFileSync(new URL('../specs/HF_SMITHY_01.json', import.meta.url)));
const catalog=JSON.parse(fs.readFileSync(new URL('../catalog/asset_catalog.seed.json', import.meta.url)));
const assets=catalog.assets.filter(a=>a.verified);
const byType=(type)=>assets.find(a=>a.type===type);
const layout={schemaVersion:'HF-GeneratedLayout/0.1',id:spec.id,generatedFrom:spec.schemaVersion,placements:[],qa:{errors:[],warnings:[]}};
const put=(assetId,floor,zone,x,z,yaw=0,role='prop')=>layout.placements.push({assetId,floor,zone,position:[x,0,z],yaw,role});
const forge=byType('smithing.forge'), anvil=byType('smithing.anvil'), bellows=byType('smithing.bellows'), grind=byType('smithing.grindstone');
const rack1=assets.find(a=>a.id==='HF_DISPLAY_RACK_001'), rack2=assets.find(a=>a.id==='HF_DISPLAY_RACK_002');
for(const req of [forge,anvil,bellows,grind,rack1]) if(!req) layout.qa.errors.push('Missing required verified asset');
if(!layout.qa.errors.length){
  put(rack1.id,'SHOP','DISPLAY_L',1.0,3.0,90,'display');
  put(rack2.id,'SHOP','DISPLAY_R',11.0,3.0,-90,'display');
  put(rack1.id,'SHOP','DISPLAY_L',1.0,6.0,90,'display');
  put(rack2.id,'SHOP','DISPLAY_R',11.0,6.0,-90,'display');
  put(forge.id,'WORKSHOP_BASEMENT','HOT_ZONE',1.6,2.0,90,'station');
  put(bellows.id,'WORKSHOP_BASEMENT','HOT_ZONE',3.6,2.1,-90,'support');
  put(anvil.id,'WORKSHOP_BASEMENT','ANVIL_ZONE',4.0,2.5,0,'station');
  put(grind.id,'WORKSHOP_BASEMENT','GRIND_ZONE',8.2,2.0,0,'station');
  put(rack1.id,'WORKSHOP_BASEMENT','STORAGE',10.2,6.8,-90,'storage');
}
const dist=(a,b)=>Math.hypot(a.position[0]-b.position[0],a.position[2]-b.position[2]);
const fp=layout.placements.find(p=>p.assetId===forge?.id), ap=layout.placements.find(p=>p.assetId===anvil?.id);
if(fp&&ap&&dist(fp,ap)>3.5) layout.qa.errors.push(`Forge/anvil too far: ${dist(fp,ap).toFixed(2)}m`);
layout.qa.status=layout.qa.errors.length?'REJECT':layout.qa.warnings.length?'WARNING':'PASS';
process.stdout.write(JSON.stringify(layout,null,2));
