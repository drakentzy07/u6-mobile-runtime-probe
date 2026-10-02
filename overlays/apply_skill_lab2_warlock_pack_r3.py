from pathlib import Path
ROOT=Path('.')
CLASSES=ROOT/'src/sim/content/classes.ts'
TEST=ROOT/'tests/highfly_skill_lab2_warlock_pack_r3.test.ts'

LINEAGES=[
  ('reaping_command','hf_unholy_dominion_01','Dominio Profano','hf_march_of_dead_01','Marcha de los Muertos','reuse_evo'),
  ('evil_eye','hf_abyss_gaze_01','Mirada del Abismo','hf_eye_of_end_01','Ojo del Fin','clone_base'),
  ('umbral_anchor','hf_umbral_return_01','Retorno Umbrio','hf_point_no_return_01','Punto de No Retorno','clone_base'),
]

def matching_end(text,start):
 depth=0; quote=None; esc=False
 for i in range(start,len(text)):
  c=text[i]
  if quote:
   if esc: esc=False
   elif c=='\\': esc=True
   elif c==quote: quote=None
   continue
  if c in "'\"\`": quote=c
  elif c=='{': depth+=1
  elif c=='}':
   depth-=1
   if depth==0:return i+1
 raise SystemExit('unbalanced ability definition')

def clone_def(text,source,new_id,new_name):
 marker=f"  {source}: {{\n"; start=text.find(marker)
 if start<0: raise SystemExit(f'missing source definition: {source}')
 brace=text.find('{',start); end=matching_end(text,brace); block=text[start:end]
 block=block.replace(f"  {source}: {{",f"  {new_id}: {{",1).replace(f"id: '{source}'",f"id: '{new_id}'",1)
 np=block.find('name: '); le=block.find('\n',np)
 if np<0: raise SystemExit(f'missing name in {source}')
 block=block[:np]+f"name: '{new_name}',"+block[le:]
 cp=block.find("class: 'warlock',")
 if cp>=0 and 'hiddenFromPlayer: true' not in block:
  ins=cp+len("class: 'warlock',"); block=block[:ins]+"\n    hiddenFromPlayer: true,"+block[ins:]
 return text[:start]+block+',\n'+text[start:]

s=CLASSES.read_text(encoding='utf-8')
for base,evo,en,mut,mn,mode in LINEAGES:
 if mode=='reuse_evo':
  # Dominio Profano is the frozen Production Pack02 EVO. REUSE FIRST:
  # preserve its authoritative reapingCommand + commandUndead rider and only
  # create the mutation endpoint from that proven EVO.
  if f"  {evo}: {{" not in s: raise SystemExit(f'missing frozen Production Pack02 EVO: {evo}')
  evo_roster=f"      '{evo}',"
  if s.count(evo_roster)!=1: raise SystemExit(f'expected one Warlock EVO roster anchor for {evo}, found {s.count(evo_roster)}')
  s=s.replace(evo_roster,evo_roster+f"\n      '{mut}',",1)
  s=clone_def(s,evo,mut,mn)
 else:
  roster=f"      '{base}',"
  if s.count(roster)!=1: raise SystemExit(f'expected one Warlock roster anchor for {base}, found {s.count(roster)}')
  s=s.replace(roster,roster+f"\n      '{evo}',\n      '{mut}',",1)
  s=clone_def(s,base,evo,en)
  s=clone_def(s,base,mut,mn)

CLASSES.write_text(s,encoding='utf-8')

TEST.write_text("""import { describe, expect, it } from 'vitest';
import { ABILITIES, CLASSES } from '../src/sim/data';

function authorityShape(id: string) {
  const d=ABILITIES[id]!;
  return {
    class:d.class,learnLevel:d.learnLevel,cost:d.cost,castTime:d.castTime,
    cooldown:d.cooldown,range:d.range,school:d.school,requiresTarget:d.requiresTarget,
    targetType:d.targetType,effects:d.effects,ranks:d.ranks
  };
}

describe('HIGHFLY Skill Lab 2.0 Warlock Heritage R3',()=>{
  it('reaping_command reuses frozen Production Pack02 EVO and MUT preserves that EVO authority',()=>{
    const base=ABILITIES.reaping_command!;
    const evo=ABILITIES.hf_unholy_dominion_01!;
    const mut=ABILITIES.hf_march_of_dead_01!;
    expect(base.effects).toEqual([{type:'reapingCommand'}]);
    expect(evo.effects).toEqual([
      {type:'reapingCommand'},
      {type:'commandUndead',duration:6,dmgPct:0.15,hastePct:0.1},
    ]);
    expect(authorityShape(mut)).toEqual(authorityShape(evo));
    expect(evo.hiddenFromPlayer).toBe(true);
    expect(mut.hiddenFromPlayer).toBe(true);
  });

  for(const [base,evo,mut] of [
    ['evil_eye','hf_abyss_gaze_01','hf_eye_of_end_01'],
    ['umbral_anchor','hf_umbral_return_01','hf_point_no_return_01'],
  ] as const) {
    it(`${base}: BASE -> EVO -> MUT preserves Claude SIM authority`,()=>{
      expect(authorityShape(evo)).toEqual(authorityShape(base));
      expect(authorityShape(mut)).toEqual(authorityShape(base));
      expect(ABILITIES[evo]!.hiddenFromPlayer).toBe(true);
      expect(ABILITIES[mut]!.hiddenFromPlayer).toBe(true);
    });
  }

  it('R3 integration gate contains all six isolated endpoints in Warlock only',()=>{
    const endpoints=[
      'hf_unholy_dominion_01','hf_march_of_dead_01',
      'hf_abyss_gaze_01','hf_eye_of_end_01',
      'hf_umbral_return_01','hf_point_no_return_01',
    ];
    const kit=new Set(CLASSES.warlock.abilities);
    for(const id of endpoints){
      expect(kit.has(id)).toBe(true);
      for(const [cls,def] of Object.entries(CLASSES)) if(cls!=='warlock'){
        expect(def.abilities.includes(id)).toBe(false);
      }
    }
  });
});
""",encoding='utf-8')
print('HIGHFLY Skill Lab 2.0 Warlock Heritage R3 overlay applied')
