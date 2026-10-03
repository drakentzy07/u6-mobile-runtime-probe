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

classes=read("src/sim/content/classes.ts")
lab_ids=[
  'hf_hd_boreal_domain_01',
  'hf_hd_frenzied_salvo_01','hf_hd_predator_rain_01',
  'hf_hd_arrow_storm_01','hf_hd_hunters_judgment_01',
  'hf_hd_wild_pack_01','hf_hd_kings_hunt_01',
  'hf_hd_moonseed_01','hf_hd_crescent_seed_01','hf_hd_lunar_wave_01',
  'hf_hd_lunar_tempest_01','hf_hd_lunar_eclipse_01','hf_hd_eternal_night_01',
  'hf_hd_galeheart_01','hf_hd_wild_hurricane_01','hf_hd_natures_wrath_01',
]
tail="\n// HIGHFLY HUNTER+DRUID 7/7 LAB ONLY.\nfor (const id of "+repr(lab_ids)+") {\n  if (ABILITIES[id]) ABILITIES[id].hiddenFromPlayer = false;\n}\n"
if "HIGHFLY HUNTER+DRUID 7/7 LAB ONLY." not in classes:
    write("src/sim/content/classes.ts",classes+tail)

rep(
    "index.html",
    """  var HUNTER_LINES = {
    frostjaw: { label:'Hunter', base:['frostjaw_trap','Trampa Colmillo Helado'], evo:['hf_hunter_prison_01','Prisión del Cazador'], mutation:['hf_hd_boreal_domain_01','Dominio Boreal'], spec:null, target:'enemy', dummyDistance:6, aim:'🎯 TARGET OPCIONAL · una trampa · arm 0.75s' }
  };""",
    """  var HUNTER_LINES = {
    frostjaw: { label:'Hunter', base:['frostjaw_trap','Trampa Colmillo Helado'], evo:['hf_hunter_prison_01','Prisión del Cazador'], mutation:['hf_hd_boreal_domain_01','Dominio Boreal'], spec:null, target:'enemy', dummyDistance:6, aim:'🎯 TARGET · trampa 0.75s · root + slow' },
    fevered: { label:'Hunter', base:['rapid_fire','Disparo Frenético'], evo:['hf_hd_frenzied_salvo_01','Salva Frenética'], mutation:['hf_hd_predator_rain_01','Lluvia del Depredador'], spec:'marksmanship', target:'enemy', dummyDistance:12, aim:'🎯 TARGET 8–35m · canal 2.4s · 6 pulsos' },
    volley: { label:'Hunter', base:['volley','Lluvia de Flechas'], evo:['hf_hd_arrow_storm_01','Tormenta de Flechas'], mutation:['hf_hd_hunters_judgment_01','Juicio del Cazador'], spec:null, target:'position', dummyDistance:8, aim:'🎯 SUELO 35m · radio 8 · 6 ticks/3s' },
    stampede: { label:'Hunter', base:['stampede','Estampida'], evo:['hf_hd_wild_pack_01','Manada Salvaje'], mutation:['hf_hd_kings_hunt_01','Cacería del Rey'], spec:'beast_mastery', target:'enemy', dummyDistance:12, aim:'🎯 TARGET 35m · 3 guardians · 12s' },
    moonseed: { label:'Hunter', base:['hf_hd_moonseed_01','Semilla Lunar'], evo:['hf_hd_crescent_seed_01','Semilla Creciente'], mutation:['hf_hd_lunar_wave_01','Oleada Lunar'], spec:null, target:'enemy', dummyDistance:12, aim:'🎯 TARGET · Focus · Moontide 3 → Oleada' },
    lunar_tempest: { label:'Hunter', base:['hf_hd_lunar_tempest_01','Tormenta Lunar'], evo:['hf_hd_lunar_eclipse_01','Eclipse Lunar'], mutation:['hf_hd_eternal_night_01','Noche Eterna'], spec:null, target:'enemy', dummyDistance:12, aim:'🎯 TARGET · direct + DoT lunar extendible' },
    galeheart: { label:'Hunter', base:['hf_hd_galeheart_01','Corazón del Vendaval'], evo:['hf_hd_wild_hurricane_01','Huracán Salvaje'], mutation:['hf_hd_natures_wrath_01','Ira de la Naturaleza'], spec:null, target:'position', dummyDistance:8, aim:'🎯 SUELO 30m · radio 8 · 6 pulsos/6s' }
  };""",
)

rep(
    "index.html",
    """      var labels = { frostjaw:'FROSTJAW' };""",
    """      var labels = {
        frostjaw:'FROSTJAW',
        fevered:'FEVERED',
        volley:'VOLLEY',
        stampede:'STAMPEDE',
        moonseed:'MOONSEED',
        lunar_tempest:'LUNAR',
        galeheart:'GALEHEART'
      };""",
)

rep(
    "index.html",
    """    if (config.necromancy) {
      var fragments = p.auras.find(function (a) { return a.kind === 'soul_fragments'; });
      if (fragments) { fragments.stacks = 5; fragments.value = 5; }
    }
    var target = config.target === 'enemy' ? stageDummy() : null;""",
    """    if (config.necromancy) {
      var fragments = p.auras.find(function (a) { return a.kind === 'soul_fragments'; });
      if (fragments) { fragments.stacks = 5; fragments.value = 5; }
    }
    if (activeClass === 'hunter' && activeLineage === 'moonseed' && abilityId === 'hf_hd_lunar_wave_01') {
      var moontide = p.auras.find(function (a) { return a.kind === 'moontide' && a.sourceId === p.id; });
      if (moontide) {
        moontide.stacks = 3;
        moontide.remaining = 3600;
        moontide.duration = 3600;
      } else {
        p.auras.push({ id:'moontide', name:'Moontide', kind:'moontide', value:0, stacks:3, remaining:3600, duration:3600, sourceId:p.id, school:'nature' });
      }
    }
    var target = config.target === 'enemy' ? stageDummy() : null;""",
)

print("HIGHFLY_SKILL_LAB2_HD_7OF7_LAB=1")
