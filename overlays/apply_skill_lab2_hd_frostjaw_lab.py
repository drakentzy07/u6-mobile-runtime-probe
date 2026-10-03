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
// HIGHFLY HUNTER+DRUID 1/7 LAB ONLY.
for (const id of ['hf_hd_boreal_domain_01']) {
  if (ABILITIES[id]) ABILITIES[id].hiddenFromPlayer = false;
}
"""
if tail.strip() not in classes:
    write("src/sim/content/classes.ts",classes+tail)

rep(
    "index.html",
    """  var MAGE_LINES = {""",
    """  var HUNTER_LINES = {
    frostjaw: { label:'Hunter', base:['frostjaw_trap','Trampa Colmillo Helado'], evo:['hf_hunter_prison_01','Prisión del Cazador'], mutation:['hf_hd_boreal_domain_01','Dominio Boreal'], spec:null, target:'enemy', dummyDistance:6, aim:'🎯 TARGET OPCIONAL · una trampa · arm 0.75s' }
  };

  var MAGE_LINES = {""",
)

rep(
    "index.html",
    """  var activeLineage = params.get('lablineage') || (activeClass === 'warrior' ? 'heroic_leap' : activeClass === 'mage' ? 'pyrelance' : 'ambush');""",
    """  var activeLineage = params.get('lablineage') || (activeClass === 'warrior' ? 'heroic_leap' : activeClass === 'mage' ? 'pyrelance' : activeClass === 'hunter' ? 'frostjaw' : 'ambush');""",
)

rep(
    "index.html",
    """  } else if (activeClass === 'mage') {
    if (!Object.prototype.hasOwnProperty.call(MAGE_LINES, activeLineage)) activeLineage = 'pyrelance';
    config = MAGE_LINES[activeLineage];
  }""",
    """  } else if (activeClass === 'mage') {
    if (!Object.prototype.hasOwnProperty.call(MAGE_LINES, activeLineage)) activeLineage = 'pyrelance';
    config = MAGE_LINES[activeLineage];
  } else if (activeClass === 'hunter') {
    if (!Object.prototype.hasOwnProperty.call(HUNTER_LINES, activeLineage)) activeLineage = 'frostjaw';
    config = HUNTER_LINES[activeLineage];
  }""",
)

rep(
    "index.html",
    """  var panel = document.createElement('div');""",
    """  if (activeClass === 'hunter') {
    lineageLabel = 'HUNTER PRIME:';
    lineageButtons = Object.keys(HUNTER_LINES).map(function (id) {
      var labels = { frostjaw:'FROSTJAW' };
      return '<button class="hf-lineage ' + (id === activeLineage ? 'hf-active' : '') + '" data-lineage="' + id + '">' + labels[id] + '</button>';
    }).join('');
  }

  var panel = document.createElement('div');""",
)

print("HIGHFLY_SKILL_LAB2_HD_FROSTJAW_LAB=1")
