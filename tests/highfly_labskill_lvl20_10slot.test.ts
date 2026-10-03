import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
import { ABILITIES, CLASSES } from '../src/sim/data';
import { HIGHFLY_LABSKILL_LOADOUTS } from '../src/highfly/labskill_loadouts';
import { highflyPresentationRoute } from '../src/highfly/presentation_adapter';

describe('HIGHFLY LABSKILL LVL20 5+5',()=>{
  it('exposes exactly four creator choices and every choice is a principal class',()=>{
    const html=readFileSync('index.html','utf8');
    const block=html.match(/<div class="mini-class-row hf-labskill-class-row">([\s\S]*?)<\/div>/)?.[1]??'';
    const ids=[...block.matchAll(/data-class="([^"]+)"/g)].map((m)=>m[1]);
    expect(ids).toEqual(['warrior','rogue','mage','hunter']);
    expect(block).toContain('WARRIOR + PALADIN');
    expect(block).toContain('ROGUE + WARLOCK');
    expect(block).toContain('MAGE + SHAMAN');
    expect(block).toContain('HUNTER + DRUID');
    expect(block).not.toMatch(/data-class="(?:paladin|warlock|shaman|druid|priest)"/);
  });

  it('pins 5 MAIN + 5 HERITAGE seats on all four lab characters',()=>{
    expect(Object.keys(HIGHFLY_LABSKILL_LOADOUTS)).toEqual(['warrior','rogue','mage','hunter']);
    for(const [cls,l] of Object.entries(HIGHFLY_LABSKILL_LOADOUTS)){
      expect(l.principal).toBe(cls);
      expect(l.seats).toHaveLength(10);
      expect(l.seats.filter((x)=>x.side==='main')).toHaveLength(5);
      expect(l.seats.filter((x)=>x.side==='heritage')).toHaveLength(5);
      expect(l.seats.map((x)=>x.label)).toEqual(['S1','S2','S3','S4','S5','S6','S7','S8','S9','S10']);
    }
  });

  it('keeps Heritage filler abilities on principal class authority',()=>{
    const expected:Record<string,string[]>={
      warrior:['hf_lab_wp_sunward_disc_01','hf_lab_wp_final_edict_01'],
      rogue:['hf_lab_rw_gloom_bolt_01','hf_lab_rw_consume_01'],
      mage:['hf_lab_ms_earthen_jolt_01'],
      hunter:['hf_lab_hd_wildbolt_01','hf_lab_hd_skyfall_01'],
    };
    for(const [cls,ids] of Object.entries(expected)){
      expect(CLASSES[cls as keyof typeof CLASSES].abilities).toEqual(expect.arrayContaining(ids));
      for(const id of ids){
        expect(ABILITIES[id]?.class,id).toBe(cls);
        expect(ABILITIES[id]?.learnLevel,id).toBe(1);
        expect(highflyPresentationRoute(id,'animation'),id).toBeTruthy();
      }
    }
  });

  it('never makes a Heritage class authoritative for body or weapon',()=>{
    expect(HIGHFLY_LABSKILL_LOADOUTS.warrior.principal).toBe('warrior');
    expect(HIGHFLY_LABSKILL_LOADOUTS.rogue.principal).toBe('rogue');
    expect(HIGHFLY_LABSKILL_LOADOUTS.mage.principal).toBe('mage');
    expect(HIGHFLY_LABSKILL_LOADOUTS.hunter.principal).toBe('hunter');
  });
});
