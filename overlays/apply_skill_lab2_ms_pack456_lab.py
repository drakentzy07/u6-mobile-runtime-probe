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

# Applied AFTER apply_current_skill_lab_pack01.py.
# It exposes the staged 4/7–6/7 Mage lineages to the playable lab without
# modifying the already-GREEN permanent harness source.
classes=read("src/sim/content/classes.ts")
tail="""
// HIGHFLY MAGE+SHAMAN PACK 4-6 LAB ONLY: direct QA reachability.
for (const id of [
  'hf_ms_dragon_breath_01',
  'hf_ms_dragon_king_breath_01',
  'hf_ms_faultwake_01',
  'hf_ms_primordial_cataclysm_01',
  'hf_ms_storms_end_01',
  'hf_ms_arc_bolt_01',
  'hf_ms_overcharged_bolt_01',
  'hf_ms_judgment_sky_01',
]) {
  if (ABILITIES[id]) ABILITIES[id].hiddenFromPlayer = false;
}
"""
if tail.strip() not in classes:
    write("src/sim/content/classes.ts",classes+tail)

rep(
    "index.html",
    """    aether_darts: { label:'Mage', base:['arcane_missiles','Dardos Etéreos'], evo:['hf_ms_aether_storm_01','Tormenta de Éter'], mutation:['hf_ms_thousand_celestial_darts_01','Mil Dardos Celestes'], spec:'arcane', target:'enemy', aim:'🎯 TARGET · canal 3s · 3 ticks reales' }
  };""",
    """    aether_darts: { label:'Mage', base:['arcane_missiles','Dardos Etéreos'], evo:['hf_ms_aether_storm_01','Tormenta de Éter'], mutation:['hf_ms_thousand_celestial_darts_01','Mil Dardos Celestes'], spec:'arcane', target:'enemy', aim:'🎯 TARGET · canal 3s · 3 ticks reales' },
    dragons_breath: { label:'Mage', base:['dragons_breath','Aliento Dracónico'], evo:['hf_ms_dragon_breath_01','Aliento del Dragón'], mutation:['hf_ms_dragon_king_breath_01','Aliento del Rey Dragón'], spec:'fire', target:'none', dummyDistance:8, aim:'FRONTAL · carga 4 etapas · máximo 12m' },
    faultwake: { label:'Mage', base:['hf_ms_faultwake_01','Falla'], evo:['hf_ms_primordial_cataclysm_01','Cataclismo Primordial'], mutation:['hf_ms_storms_end_01','Fin de la Tormenta'], spec:'fire', target:'position', dummyDistance:8, aim:'🎯 SUELO · 6s · tick 1.5s · Thunder interno' },
    arc_bolt: { label:'Mage', base:['hf_ms_arc_bolt_01','Rayo Arcano'], evo:['hf_ms_overcharged_bolt_01','Rayo Sobrecargado'], mutation:['hf_ms_judgment_sky_01','Juicio del Cielo'], spec:'fire', target:'enemy', aim:'🎯 TARGET · builder Thunder interno' }
  };""",
)

rep(
    "index.html",
    """      var labels = { pyrelance:'PYRELANCE', meteor:'METEORITO', aether_darts:'DARDOS ETÉREOS' };""",
    """      var labels = { pyrelance:'PYRELANCE', meteor:'METEORITO', aether_darts:'DARDOS ETÉREOS', dragons_breath:'ALIENTO', faultwake:'FALLA', arc_bolt:'RAYO ARCANO' };""",
)

print("HIGHFLY_SKILL_LAB2_MS_PACK456_LAB=1")
