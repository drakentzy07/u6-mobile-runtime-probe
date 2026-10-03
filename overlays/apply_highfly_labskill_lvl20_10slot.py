from pathlib import Path
import shutil

ROOT=Path(".")
HOST=ROOT.resolve().parent

def read(path:str)->str:
    return (ROOT/path).read_text(encoding="utf-8")

def write(path:str,text:str)->None:
    p=ROOT/path
    p.parent.mkdir(parents=True,exist_ok=True)
    p.write_text(text,encoding="utf-8")

def rep(path:str,old:str,new:str,count:int=1)->None:
    text=read(path)
    n=text.count(old)
    if n!=count:
        raise SystemExit(f"{path}: expected {count} anchor(s), found {n}: {old[:180]!r}")
    write(path,text.replace(old,new,count))

# Copy dedicated LABSKILL sources from host repo into frozen upstream worktree.
for src,dst in [
    ("src/highfly/labskill_loadouts.ts","src/highfly/labskill_loadouts.ts"),
    ("src/highfly/labskill_runtime.ts","src/highfly/labskill_runtime.ts"),
    ("src/styles/hf_labskill.css","src/styles/hf_labskill.css"),
    ("tests/highfly_labskill_lvl20_10slot.test.ts","tests/highfly_labskill_lvl20_10slot.test.ts"),
]:
    target=ROOT/dst
    target.parent.mkdir(parents=True,exist_ok=True)
    shutil.copyfile(HOST/src,target)

# Same creator, only four combined class choices. data-class remains PRINCIPAL.
creator_old="""                <div class="mini-class-row">
                  <button type="button" class="mini-class" data-class="warrior" data-i18n-aria="classes.warriorAria" data-i18n="classes.warrior" aria-label="Warrior class" aria-pressed="false">Warrior</button>
                  <button type="button" class="mini-class" data-class="paladin" data-i18n-aria="classes.paladinAria" data-i18n="classes.paladin" aria-label="Paladin class" aria-pressed="false">Paladin</button>
                  <button type="button" class="mini-class" data-class="hunter" data-i18n-aria="classes.hunterAria" data-i18n="classes.hunter" aria-label="Hunter class" aria-pressed="false">Hunter</button>
                  <button type="button" class="mini-class" data-class="rogue" data-i18n-aria="classes.rogueAria" data-i18n="classes.rogue" aria-label="Rogue class" aria-pressed="false">Rogue</button>
                  <button type="button" class="mini-class" data-class="priest" data-i18n-aria="classes.priestAria" data-i18n="classes.priest" aria-label="Priest class" aria-pressed="false">Priest</button>
                  <button type="button" class="mini-class" data-class="shaman" data-i18n-aria="classes.shamanAria" data-i18n="classes.shaman" aria-label="Shaman class" aria-pressed="false">Shaman</button>
                  <button type="button" class="mini-class" data-class="mage" data-i18n-aria="classes.mageAria" data-i18n="classes.mage" aria-label="Mage class" aria-pressed="false">Mage</button>
                  <button type="button" class="mini-class" data-class="warlock" data-i18n-aria="classes.warlockAria" data-i18n="classes.warlock" aria-label="Warlock class" aria-pressed="false">Warlock</button>
                  <button type="button" class="mini-class" data-class="druid" data-i18n-aria="classes.druidAria" data-i18n="classes.druid" aria-label="Druid class" aria-pressed="false">Druid</button>
                </div>"""
creator_new="""                <div class="mini-class-row hf-labskill-class-row">
                  <button type="button" class="mini-class" data-class="warrior" aria-label="Warrior + Paladin" aria-pressed="false">WARRIOR + PALADIN</button>
                  <button type="button" class="mini-class" data-class="rogue" aria-label="Rogue + Warlock" aria-pressed="false">ROGUE + WARLOCK</button>
                  <button type="button" class="mini-class" data-class="mage" aria-label="Mage + Shaman" aria-pressed="false">MAGE + SHAMAN</button>
                  <button type="button" class="mini-class" data-class="hunter" aria-label="Hunter + Druid" aria-pressed="false">HUNTER + DRUID</button>
                </div>"""
rep("index.html",creator_old,creator_new,count=2)

rep(
    "index.html",
    """          <h2 class="auth-title" data-i18n="auth.offlineCharacter">Offline Character</h2>""",
    """          <h2 class="auth-title hf-labskill-creator-title">HIGHFLY LABSKILL</h2>
          <div class="hf-labskill-creator-sub">4 PERSONAJES · LVL 20 · 5 MAIN + 5 HERITAGE</div>""",
)

# Add lab-only Heritage filler ids to each PRINCIPAL roster.
classes=read("src/sim/content/classes.ts")
for anchor, ids in [
    ("      'hf_wp_unbreakable_dawn_01',", ["hf_lab_wp_sunward_disc_01","hf_lab_wp_final_edict_01"]),
    ("      'hf_rw_point_no_return_01',", ["hf_lab_rw_gloom_bolt_01","hf_lab_rw_consume_01"]),
    ("      'hf_ms_primordial_eruption_01',", ["hf_lab_ms_earthen_jolt_01"]),
    ("      'hf_hd_natures_wrath_01',", ["hf_lab_hd_wildbolt_01","hf_lab_hd_skyfall_01"]),
]:
    if classes.count(anchor)!=1:
        raise SystemExit(f"classes roster anchor expected 1, found {classes.count(anchor)}: {anchor}")
    classes=classes.replace(anchor,anchor+"".join(f"\n      '{x}'," for x in ids),1)

classes += r"""
// HIGHFLY LABSKILL LVL20 — lab-only Heritage fillers.
// Clone canonical Claude mechanics while keeping MAIN class authority.
function hfLabSkillAlias(
  sourceId: string,
  id: string,
  name: string,
  cls: 'warrior' | 'rogue' | 'mage' | 'hunter',
  cost: number,
): void {
  const source = (ABILITIES as Record<string, any>)[sourceId];
  if (!source) throw new Error('HIGHFLY LABSKILL missing donor ' + sourceId);
  const clone: any = {
    ...source,
    id,
    name,
    class: cls,
    hiddenFromPlayer: false,
    learnLevel: 1,
    cost,
  };
  delete clone.specs;
  delete clone.devotionCost;
  delete clone.requiresAuraKind;
  delete clone.actionReplacement;
  delete clone.talentGrant;
  (ABILITIES as Record<string, any>)[id] = clone;
}

hfLabSkillAlias('sunward_disc','hf_lab_wp_sunward_disc_01','Disco Solar','warrior',25);
hfLabSkillAlias('final_edict','hf_lab_wp_final_edict_01','Edicto Final','warrior',25);
hfLabSkillAlias('shadow_bolt','hf_lab_rw_gloom_bolt_01','Proyectil Sombrío','rogue',25);
hfLabSkillAlias('drain_life','hf_lab_rw_consume_01','Consumir','rogue',25);
hfLabSkillAlias('earth_shock','hf_lab_ms_earthen_jolt_01','Sacudida Telúrica','mage',30);
hfLabSkillAlias('wrath','hf_lab_hd_wildbolt_01','Wildbolt','hunter',20);
hfLabSkillAlias('starfire','hf_lab_hd_skyfall_01','Skyfall','hunter',50);
"""
write("src/sim/content/classes.ts",classes)

# Presentation aliases always route through MAIN-compatible body animations.
rep(
    "src/highfly/presentation_adapter.ts",
    """  hf_hd_natures_wrath_01: {
    animationRoute: 'volley',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_hd_natures_wrath_01',
    sfxRoute: 'hurricane',
  },""",
    """  hf_hd_natures_wrath_01: {
    animationRoute: 'volley',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_hd_natures_wrath_01',
    sfxRoute: 'hurricane',
  },
  hf_lab_wp_sunward_disc_01: {
    animationRoute: 'cleave',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'sunward_disc',
    sfxRoute: 'sunward_disc',
  },
  hf_lab_wp_final_edict_01: {
    animationRoute: 'execute',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'final_edict',
    sfxRoute: 'final_edict',
  },
  hf_lab_rw_gloom_bolt_01: {
    animationRoute: 'sinister_strike',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'shadow_bolt',
    sfxRoute: 'shadow_bolt',
  },
  hf_lab_rw_consume_01: {
    animationRoute: 'sinister_strike',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'drain_life',
    sfxRoute: 'drain_life',
  },
  hf_lab_ms_earthen_jolt_01: {
    animationRoute: 'fireball',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'earth_shock',
    sfxRoute: 'earth_shock',
  },
  hf_lab_hd_wildbolt_01: {
    animationRoute: 'arcane_shot',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'wrath',
    sfxRoute: 'wrath',
  },
  hf_lab_hd_skyfall_01: {
    animationRoute: 'arcane_shot',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'starfire',
    sfxRoute: 'starfire',
  },""",
)

# Dedicated lab runtime is present on both the root creator and ?skilllab=1 smoke route.
rep(
    "index.html",
    """</body>""",
    """  <script type="module" src="/src/highfly/labskill_runtime.ts"></script>
</body>""",
)

print("HIGHFLY_LABSKILL_LVL20_10SLOT=1")
