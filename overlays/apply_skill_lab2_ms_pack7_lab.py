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
tail="""
// HIGHFLY MAGE+SHAMAN 7/7 LAB ONLY: direct QA reachability.
for (const id of [
  'hf_ms_cinder_jolt_01',
  'hf_ms_magma_burst_01',
  'hf_ms_volcanic_core_01',
  'hf_ms_primordial_eruption_01',
]) {
  if (ABILITIES[id]) ABILITIES[id].hiddenFromPlayer = false;
}
"""
if tail.strip() not in classes:
    write("src/sim/content/classes.ts",classes+tail)

rep(
    "index.html",
    """    arc_bolt: { label:'Mage', base:['hf_ms_arc_bolt_01','Rayo Arcano'], evo:['hf_ms_overcharged_bolt_01','Rayo Sobrecargado'], mutation:['hf_ms_judgment_sky_01','Juicio del Cielo'], spec:'fire', target:'enemy', aim:'🎯 TARGET · builder Thunder interno' }
  };""",
    """    arc_bolt: { label:'Mage', base:['hf_ms_arc_bolt_01','Rayo Arcano'], evo:['hf_ms_overcharged_bolt_01','Rayo Sobrecargado'], mutation:['hf_ms_judgment_sky_01','Juicio del Cielo'], spec:'fire', target:'enemy', aim:'🎯 TARGET · builder Thunder interno' },
    magma_burst: { label:'Mage', base:['hf_ms_magma_burst_01','Explosión Magmática'], evo:['hf_ms_volcanic_core_01','Núcleo Volcánico'], mutation:['hf_ms_primordial_eruption_01','Erupción Primordial'], spec:'fire', target:'enemy', aim:'🎯 TARGET · Cinder Jolt + Magma Surge interno' }
  };""",
)

rep(
    "index.html",
    """      var labels = { pyrelance:'PYRELANCE', meteor:'METEORITO', aether_darts:'DARDOS ETÉREOS', dragons_breath:'ALIENTO', faultwake:'FALLA', arc_bolt:'RAYO ARCANO' };""",
    """      var labels = { pyrelance:'PYRELANCE', meteor:'METEORITO', aether_darts:'DARDOS ETÉREOS', dragons_breath:'ALIENTO', faultwake:'FALLA', arc_bolt:'RAYO ARCANO', magma_burst:'EXPLOSIÓN MAGMÁTICA' };""",
)

print("HIGHFLY_SKILL_LAB2_MS_PACK7_LAB=1")
