from pathlib import Path
ROOT=Path('.')
CLASSES=ROOT/'src/sim/content/classes.ts'
TEST=ROOT/'tests/highfly_skill_lab2_warlock_pack_r3.test.ts'
LINEAGES=[('reaping_command','hf_unholy_dominion_01','Dominio Profano','hf_march_of_dead_01','Marcha de los Muertos'),('evil_eye','hf_abyss_gaze_01','Mirada del Abismo','hf_eye_of_end_01','Ojo del Fin'),('umbral_anchor','hf_umbral_return_01','Retorno Umbrio','hf_point_no_return_01','Punto de No Retorno')]
def matching_end(text,start):
 depth=0; quote=None; esc=False
 for i in range(start,len(text)):
  c=text[i]
  if quote:
   if esc: esc=False
   elif c=='\\': esc=True
   elif c==quote: quote=None
   continue
  if c in "'\"`": quote=c
  elif c=='{': depth+=1
  elif c=='}':
   depth-=1
   if depth==0:return i+1
 raise SystemExit('unbalanced ability definition')
def clone_def(text,base,new_id,new_name):
 marker=f"  {base}: {{\n"; start=text.find(marker)
 if start<0: raise SystemExit(f'missing Claude BASE definition: {base}')
 brace=text.find('{',start); end=matching_end(text,brace); block=text[start:end]
 block=block.replace(f"  {base}: {{",f"  {new_id}: {{",1).replace(f"id: '{base}'",f"id: '{new_id}'",1)
 np=block.find('name: '); le=block.find('\n',np)
 if np<0: raise SystemExit(f'missing name in {base}')
 block=block[:np]+f"name: '{new_name}',"+block[le:]
 cp=block.find("class: 'warlock',")
 if cp>=0:
  ins=cp+len("class: 'warlock',"); block=block[:ins]+"\n    hiddenFromPlayer: true,"+block[ins:]
 return text[:start]+block+',\n'+text[start:]
s=CLASSES.read_text(encoding='utf-8')
for base,evo,en,mut,mn in LINEAGES:
 roster=f"      '{base}',"
 if s.count(roster)!=1: raise SystemExit(f'expected one Warlock roster anchor for {base}, found {s.count(roster)}')
 s=s.replace(roster,roster+f"\n      '{evo}',\n      '{mut}',",1)
 s=clone_def(s,base,evo,en); s=clone_def(s,base,mut,mn)
CLASSES.write_text(s,encoding='utf-8')
rows='\n'.join(f"  ['{b}', '{e}', '{m}']," for b,e,_,m,_ in LINEAGES)
TEST.write_text("""import { describe, expect, it } from 'vitest';
import { ABILITIES, CLASSES } from '../src/sim/data';
const LINEAGES = [
%s
] as const;
function authorityShape(id: string) { const d=ABILITIES[id]!; return {class:d.class,learnLevel:d.learnLevel,cost:d.cost,castTime:d.castTime,cooldown:d.cooldown,range:d.range,school:d.school,requiresTarget:d.requiresTarget,targetType:d.targetType,effects:d.effects,ranks:d.ranks}; }
describe('HIGHFLY Skill Lab 2.0 Warlock Heritage R3',()=>{
 for(const [base,evo,mut] of LINEAGES) it(`${base}: BASE -> EVO -> MUT preserves Claude SIM authority`,()=>{ expect(ABILITIES[base]).toBeTruthy(); expect(ABILITIES[evo]).toBeTruthy(); expect(ABILITIES[mut]).toBeTruthy(); expect(authorityShape(evo)).toEqual(authorityShape(base)); expect(authorityShape(mut)).toEqual(authorityShape(base)); expect(ABILITIES[evo]!.hiddenFromPlayer).toBe(true); expect(ABILITIES[mut]!.hiddenFromPlayer).toBe(true); });
 it('R3 integration gate contains all six isolated endpoints in Warlock only',()=>{ const kit=new Set(CLASSES.warlock.abilities); for(const [,evo,mut] of LINEAGES){ expect(kit.has(evo)).toBe(true); expect(kit.has(mut)).toBe(true); for(const [cls,def] of Object.entries(CLASSES)) if(cls!=='warlock'){ expect(def.abilities.includes(evo)).toBe(false); expect(def.abilities.includes(mut)).toBe(false); } } });
});
"""%rows,encoding='utf-8')
print('HIGHFLY Skill Lab 2.0 Warlock Heritage R3 overlay applied')
