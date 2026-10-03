from pathlib import Path

ROOT=Path(".")

def read(path:str)->str:
    return (ROOT/path).read_text(encoding="utf-8")

def write(path:str,text:str)->None:
    p=ROOT/path
    p.parent.mkdir(parents=True,exist_ok=True)
    p.write_text(text,encoding="utf-8")

def rep(path:str,old:str,new:str)->None:
    text=read(path)
    n=text.count(old)
    if n!=1:
        raise SystemExit(f"{path}: expected 1 anchor, found {n}: {old[:180]!r}")
    write(path,text.replace(old,new,1))

def append_set_items(path:str,const_name:str,items:list[str])->None:
    text=read(path)
    start=text.find(f"const {const_name}")
    if start<0:
        raise SystemExit(f"{path}: missing const {const_name}")
    set_start=text.find("new Set([",start)
    if set_start<0:
        raise SystemExit(f"{path}: missing new Set([ for {const_name}")
    body_start=set_start+len("new Set([")
    end=text.find("]);",body_start)
    if end<0:
        raise SystemExit(f"{path}: missing ]); for {const_name}")
    body=text[body_start:end]
    missing=[item for item in items if f"'{item}'" not in body]
    if not missing:
        return
    stripped=body.rstrip()
    prefix=body[:len(body)-len(stripped)]
    if stripped and not stripped.rstrip().endswith(","):
        stripped += ","
    addition="".join(f"\n  '{item}'," for item in missing)
    new_body=stripped+addition+"\n"
    write(path,text[:body_start]+new_body+text[end:])

# Internal-only Thundercall bridge for Mage+Shaman Heritage.
# No UI/resource bar is added. Mage stays Mage and pays Mana.
faultwake_path="src/sim/combat/shaman_thundercall.ts"
faultwake_text=read(faultwake_path)
if "const HIGHFLY_MS_FAULTWAKE_VENTS" not in faultwake_text:
    marker="const THUNDER_VENTS"
    pos=faultwake_text.find(marker)
    if pos<0:
        raise SystemExit(f"{faultwake_path}: missing THUNDER_VENTS declaration")
    line_start=faultwake_text.rfind("\n",0,pos)+1
    highfly_decl="""const HIGHFLY_MS_FAULTWAKE_VENTS: ReadonlySet<string> = new Set([
  'hf_ms_faultwake_01',
  'hf_ms_primordial_cataclysm_01',
  'hf_ms_storms_end_01',
]);

"""
    # Insert before the whole declaration line so an existing
    # `export const THUNDER_VENTS` keeps its export token intact.
    write(
        faultwake_path,
        faultwake_text[:line_start]+highfly_decl+faultwake_text[line_start:],
    )

append_set_items(
    faultwake_path,
    "THUNDER_VENTS",
    ["hf_ms_faultwake_01","hf_ms_primordial_cataclysm_01","hf_ms_storms_end_01"],
)

thunder_text=read(faultwake_path)
old_expr="ctx.playerMods(meta).spec === 'elemental'"
if "meta.cls === 'mage'" not in thunder_text:
    if thunder_text.count(old_expr)!=1:
        raise SystemExit(f"{faultwake_path}: expected one elemental spec predicate")
    thunder_text=thunder_text.replace(
        old_expr,
        "(ctx.playerMods(meta).spec === 'elemental' || meta.cls === 'mage')",
        1,
    )
    write(faultwake_path,thunder_text)

rep(
    "src/sim/combat/shaman_thundercall.ts",
    """  if (abilityId === 'earthquake') {
    return (1 + charges * FAULTWAKE_BONUS_PER_CHARGE) * (1 + primalBonus);
  }""",
    """  if (abilityId === 'earthquake' || HIGHFLY_MS_FAULTWAKE_VENTS.has(abilityId)) {
    return (1 + charges * FAULTWAKE_BONUS_PER_CHARGE) * (1 + primalBonus);
  }""",
)

rep(
    "src/sim/combat/shaman_thundercall.ts",
    """    abilityId === 'earthquake' &&
    isThundercall(ctx, player) &&""",
    """    (abilityId === 'earthquake' || HIGHFLY_MS_FAULTWAKE_VENTS.has(abilityId)) &&
    isThundercall(ctx, player) &&""",
)

rep(
    "src/sim/combat/effect_dispatch.ts",
    """const CHARGE_MAX_DURATION = 3; // seconds before a blocked charge gives up""",
    """const HIGHFLY_MS_ARC_BOLT_IDS: ReadonlySet<string> = new Set([
  'hf_ms_arc_bolt_01',
  'hf_ms_overcharged_bolt_01',
  'hf_ms_judgment_sky_01',
]);

const CHARGE_MAX_DURATION = 3; // seconds before a blocked charge gives up""",
)

rep(
    "src/sim/combat/effect_dispatch.ts",
    """        if (ability.id === 'lightning_bolt') {
          thundercallOnArcBoltImpact(ctx, p);
          triggerWardCycle(ctx, p);
          rollArcOverload(ctx, p, target, ability.id, finalDamage, resolvedDamage, threatOpts.mult);
        }""",
    """        if (ability.id === 'lightning_bolt' || HIGHFLY_MS_ARC_BOLT_IDS.has(ability.id)) {
          thundercallOnArcBoltImpact(ctx, p);
          // HIGHFLY Mage adapters inherit only the internal Thunder builder.
          // Native Shaman ward cycling / Arc Overload remain Shaman-owned.
          if (ability.id === 'lightning_bolt') {
            triggerWardCycle(ctx, p);
            rollArcOverload(
              ctx,
              p,
              target,
              ability.id,
              finalDamage,
              resolvedDamage,
              threatOpts.mult,
            );
          }
        }""",
)

write(
    "tests/highfly_skill_lab2_ms_shaman_heritage_bridge.test.ts",
    """import { describe, expect, it } from 'vitest';
import { addThunderCharges, thunderCharges } from '../src/sim/combat/shaman_thundercall';
import { ABILITIES, MOBS } from '../src/sim/data';
import { createMob } from '../src/sim/entity';
import { Sim } from '../src/sim/sim';
import type { Entity } from '../src/sim/types';
import { EMPTY_TEST_WORLD } from './sim_shared';

const ARC='hf_ms_arc_bolt_01';
const FAULT='hf_ms_faultwake_01';

function grantHidden(sim: Sim, id: string): void {
  const meta=sim.meta(sim.playerId)!;
  if(meta.known.some((known)=>known.def.id===id)) return;
  const def=ABILITIES[id]!;
  meta.known.push({
    def,rank:1,cost:def.cost,castTime:def.castTime,cooldown:def.cooldown,effects:def.effects,
    threatFlat:def.threat?.flat ?? 0,threatMult:def.threat?.mult ?? 1,bonusCharges:0,
  });
}

function mage(seed:number):Sim {
  const sim=new Sim({seed,playerClass:'mage',autoEquip:true,world:EMPTY_TEST_WORLD});
  sim.setPlayerLevel(20);
  sim.setSpec('fire');
  sim.tick();
  sim.player.resource=sim.player.maxResource;
  sim.player.hitBonus=1;
  grantHidden(sim,ARC);
  grantHidden(sim,FAULT);
  return sim;
}

function target(sim:Sim,z=8):Entity {
  const mob=createMob((sim as unknown as {nextId:number}).nextId++,MOBS.training_dummy,20,{
    x:sim.player.pos.x,y:sim.player.pos.y,z:sim.player.pos.z+z,
  });
  mob.hostile=true;
  mob.maxHp=mob.hp=100_000;
  mob.weapon.min=0; mob.weapon.max=0; mob.weapon.speed=1000; mob.swingTimer=1000; mob.moveSpeed=0;
  sim.addEntity(mob);
  return mob;
}

function ticks(sim:Sim,seconds:number):void {
  for(let i=0;i<Math.ceil(seconds*20)+3;i++) sim.tick();
}

describe('HIGHFLY Mage Shaman internal Thundercall bridge',()=>{
  it('Arc Bolt heritage builds hidden Thunder while Mage remains Mana-only',()=>{
    const sim=mage(301);
    const t=target(sim);
    const equipment=JSON.stringify(sim.meta(sim.playerId)?.equipment);
    sim.targetEntity(t.id);
    const hp=t.hp;
    sim.castAbility(ARC);
    ticks(sim,1.8);
    expect(t.hp).toBeLessThan(hp);
    expect(thunderCharges(sim.player)).toBe(1);
    expect(sim.meta(sim.playerId)?.cls).toBe('mage');
    expect(sim.player.resourceType).toBe('mana');
    expect(JSON.stringify(sim.meta(sim.playerId)?.equipment)).toBe(equipment);
  });

  it('Faultwake heritage vents hidden Thunder and keeps Mage identity',()=>{
    const sim=mage(302);
    const t=target(sim);
    const ctx=(sim as unknown as {ctx:Parameters<typeof addThunderCharges>[0]}).ctx;
    addThunderCharges(ctx,sim.player,5);
    expect(thunderCharges(sim.player)).toBe(5);
    const mana0=sim.player.resource;
    sim.castAbility(FAULT,sim.player.id,{x:t.pos.x,z:t.pos.z});
    expect(sim.player.resource).toBe(mana0-80);
    expect(thunderCharges(sim.player)).toBe(0);
    expect(sim.meta(sim.playerId)?.cls).toBe('mage');
    expect(sim.player.resourceType).toBe('mana');
  });
});
""",
)

print("HIGHFLY_SKILL_LAB2_MS_SHAMAN_HERITAGE_BRIDGE=1")
