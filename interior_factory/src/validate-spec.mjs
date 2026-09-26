import fs from 'node:fs';
const spec=JSON.parse(fs.readFileSync(new URL('../specs/HF_SMITHY_01.json', import.meta.url)));
const issues=[];
const ids=new Set();
for(const floor of spec.floors){
  if(ids.has(floor.id)) issues.push(`duplicate floor ${floor.id}`); ids.add(floor.id);
  const [w,h]=floor.size;
  for(const z of floor.zones){
    const [x,y,zw,zh]=z.rect;
    if(x<0||y<0||x+zw>w||y+zh>h) issues.push(`${floor.id}/${z.id} outside floor bounds`);
  }
}
if(spec.floors.length<2) issues.push('smithy requires shop + workshop floors');
if(!spec.requirements.mustContain.includes('smithing.forge')) issues.push('forge requirement missing');
console.log(JSON.stringify({status:issues.length?'REJECT':'PASS',issues},null,2));
process.exitCode=issues.length?1:0;
