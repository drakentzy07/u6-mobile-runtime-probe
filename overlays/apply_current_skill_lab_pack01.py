from pathlib import Path

ROOT = Path(".")

def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")

def write(path: str, text: str) -> None:
    p = ROOT / path
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(text, encoding="utf-8")

def rep(path: str, old: str, new: str) -> None:
    text = read(path)
    n = text.count(old)
    if n != 1:
        raise SystemExit(f"{path}: expected 1 anchor, found {n}: {old[:140]!r}")
    write(path, text.replace(old, new, 1))

rep(
    "src/main.ts",
    """const diagnosticsAutoOffline =
  import.meta.env.DEV &&
  startupParams.get('diagnostics') === '1' &&
  startupParams.get('diagnosticsAuto') === '1';
if (editorPlaytest) {""",
    """const diagnosticsAutoOffline =
  import.meta.env.DEV &&
  startupParams.get('diagnostics') === '1' &&
  startupParams.get('diagnosticsAuto') === '1';
const highflySkillLab = startupParams.get('skilllab') === '1';
const HIGHFLY_SKILL_LAB_CLASSES: readonly PlayerClass[] = [
  'warrior',
  'hunter',
  'mage',
  'priest',
  'druid',
];
const requestedSkillLabClass = startupParams.get('labclass') as PlayerClass | null;
const highflySkillLabClass: PlayerClass =
  requestedSkillLabClass && HIGHFLY_SKILL_LAB_CLASSES.includes(requestedSkillLabClass)
    ? requestedSkillLabClass
    : 'warrior';
if (editorPlaytest) {""",
)

rep(
    "src/main.ts",
    """} else if (diagnosticsAutoOffline) {
  startSitePresence('home');
  void startOffline('warrior', 'Diagnostics', 0);
} else {
  startSitePresence('home');
  wireStartScreens();
  initHomepageMusic();
}""",
    """} else if (highflySkillLab) {
  startSitePresence('home');
  void startOffline(highflySkillLabClass, 'HIGHFLY Lab', 0);
} else if (diagnosticsAutoOffline) {
  startSitePresence('home');
  void startOffline('warrior', 'Diagnostics', 0);
} else {
  startSitePresence('home');
  wireStartScreens();
  initHomepageMusic();
}""",
)

LAB_SCRIPT = r"""
<script>
(function () {
  var params = new URLSearchParams(location.search);
  if (params.get('skilllab') !== '1') return;

  var CLASSES = {
    warrior: { label: 'Warrior', base: ['heroic_leap','Salto Heroico'], evo: ['hf_jump_smash_01','Salto Demoledor'], spec: null, target: 'position' },
    hunter: { label: 'Hunter', base: ['frostjaw_trap','Trampa Colmillo Helado'], evo: ['hf_hunter_prison_01','Prisión del Cazador'], spec: null, target: 'enemy' },
    mage: { label: 'Mage', base: ['pyroblast','Lanza Pírica'], evo: ['hf_phoenix_lance_01','Lanza del Fénix'], spec: 'fire', target: 'enemy' },
    priest: { label: 'Priest', base: ['power_word_shield','Salmo Protector'], evo: ['hf_living_covenant_01','Pacto Viviente'], spec: null, target: 'self' },
    druid: { label: 'Druid', base: ['moonseed','Semilla Lunar'], evo: ['moonlash','Oleada Lunar'], spec: 'balance', target: 'enemy', moonkin: true }
  };

  var requested = params.get('labclass') || 'warrior';
  var activeClass = Object.prototype.hasOwnProperty.call(CLASSES, requested) ? requested : 'warrior';
  var config = CLASSES[activeClass];

  var style = document.createElement('style');
  style.textContent =
    '#hf-skill-lab-panel{position:fixed;z-index:2147483000;top:8px;left:50%;transform:translateX(-50%);width:min(920px,94vw);background:rgba(7,9,16,.94);border:1px solid rgba(159,109,255,.75);border-radius:14px;box-shadow:0 10px 40px rgba(0,0,0,.55);color:#fff;font:600 13px/1.2 Arial,sans-serif;padding:10px;backdrop-filter:blur(10px);user-select:none}' +
    '#hf-skill-lab-panel .hf-row{display:flex;gap:7px;align-items:center;flex-wrap:wrap}' +
    '#hf-skill-lab-panel .hf-title{font-weight:900;letter-spacing:.08em;margin-right:7px;color:#c9a7ff}' +
    '#hf-skill-lab-panel button{border:1px solid #4b5064;background:#171a26;color:#e9ebf7;border-radius:9px;padding:8px 11px;font-weight:800;cursor:pointer;touch-action:manipulation}' +
    '#hf-skill-lab-panel button.hf-active{border-color:#a76cff;background:#392258;color:white}' +
    '#hf-skill-lab-panel button.hf-base{border-color:#4f9cff}' +
    '#hf-skill-lab-panel button.hf-evo{border-color:#b16cff;background:#271638}' +
    '#hf-skill-lab-panel .hf-skills{margin-top:8px}' +
    '#hf-skill-lab-panel .hf-status{margin-left:auto;color:#9af5b5;font-weight:700}' +
    '#hf-skill-lab-panel .hf-sub{opacity:.68;font-size:11px;font-weight:600}' +
    '@media(max-width:800px){#hf-skill-lab-panel{top:4px;padding:7px;width:96vw;font-size:11px}#hf-skill-lab-panel button{padding:7px 8px;font-size:11px}#hf-skill-lab-panel .hf-sub{display:none}}';
  document.head.appendChild(style);

  var classButtons = Object.keys(CLASSES).map(function (id) {
    var c = CLASSES[id];
    return '<button class="hf-class ' + (id === activeClass ? 'hf-active' : '') + '" data-class="' + id + '">' + c.label + '</button>';
  }).join('');

  var panel = document.createElement('div');
  panel.id = 'hf-skill-lab-panel';
  panel.setAttribute('data-active-class', activeClass);
  panel.innerHTML =
    '<div class="hf-row"><span class="hf-title">HIGHFLY SKILL LAB</span>' +
    classButtons +
    '<span id="hf-lab-status" class="hf-status">CARGANDO...</span></div>' +
    '<div class="hf-row hf-skills">' +
    '<button class="hf-base" id="hf-lab-base" data-ability="' + config.base[0] + '">BASE · ' + config.base[1] + '</button>' +
    '<button class="hf-evo" id="hf-lab-evo" data-ability="' + config.evo[0] + '">EVO · ' + config.evo[1] + '</button>' +
    '<button id="hf-lab-reset">RESET</button>' +
    '<button id="hf-lab-dummy">DUMMY DELANTE</button>' +
    '<span class="hf-sub">Clase: ' + config.label + ' · Lv20 · recursos/cooldowns restaurables</span></div>';
  document.body.appendChild(panel);

  function setStatus(text, bad) {
    var el = document.getElementById('hf-lab-status');
    if (!el) return;
    el.textContent = text;
    el.style.color = bad ? '#ff9a9a' : '#9af5b5';
  }

  document.querySelectorAll('#hf-skill-lab-panel .hf-class').forEach(function (button) {
    button.addEventListener('click', function () {
      var next = new URL(location.href);
      next.searchParams.set('skilllab', '1');
      next.searchParams.set('labclass', button.getAttribute('data-class'));
      location.replace(next.toString());
    });
  });

  function game() {
    return window.__game && window.__game.sim ? window.__game : null;
  }

  function clearFirstRunUi() {
    var wanted = ['Dismiss','Understood','Got it','Skip tutorial','Entendido','Omitir tutorial'];
    document.querySelectorAll('button').forEach(function (b) {
      if (wanted.indexOf((b.textContent || '').trim()) >= 0) b.click();
    });
    var c = document.querySelector('.camera-prompt-confirm');
    if (c) c.click();
    var t = document.querySelector('button.tut-skip');
    if (t) t.click();
    var p = document.querySelector('#mobile-preflight-continue');
    if (p) p.click();
  }

  function prepare() {
    var g = game();
    if (!g) return false;
    var sim = g.sim;
    var p = sim.player;
    sim.setPlayerLevel(20);
    if (config.spec) sim.setSpec(config.spec);
    p.hp = p.maxHp;
    p.resource = p.maxResource;
    p.gcdRemaining = 0;
    p.gm = true;
    if (config.moonkin && !p.auras.some(function (a) { return a.kind === 'form_moonkin'; })) {
      p.auras.push({ id:'hf_skill_lab_moonkin', name:'Moonkin Form', kind:'form_moonkin', value:0, remaining:3600, duration:3600, sourceId:p.id, school:'arcane' });
    }
    return true;
  }

  function stageDummy() {
    var g = game();
    if (!g || !prepare()) return null;
    var sim = g.sim;
    var p = sim.player;
    var x = p.pos.x + Math.sin(p.facing) * 7;
    var z = p.pos.z + Math.cos(p.facing) * 7;
    var target = Array.from(sim.entities.values()).find(function (e) {
      return e.id !== p.id && e.kind === 'mob' && !e.dead && e.ownerId == null;
    });
    if (!target) {
      setStatus('SIN MOB DISPONIBLE', true);
      return null;
    }
    target.dead = false;
    target.hostile = true;
    target.hp = Math.max(target.hp || 1, 50000);
    target.maxHp = Math.max(target.maxHp || 1, 50000);
    target.pos.x = x;
    target.pos.y = p.pos.y;
    target.pos.z = z;
    if (target.prevPos) {
      target.prevPos.x = x;
      target.prevPos.y = p.pos.y;
      target.prevPos.z = z;
    }
    p.targetId = target.id;
    setStatus('DUMMY LISTO', false);
    return target;
  }

  function resetLab() {
    var g = game();
    if (!g || !prepare()) return;
    var p = g.sim.player;
    p.cooldowns.clear();
    p.gcdRemaining = 0;
    p.resource = p.maxResource;
    p.hp = p.maxHp;
    p.castingAbility = null;
    p.leap = null;
    stageDummy();
    setStatus('RESET OK', false);
  }

  function cast(abilityId) {
    var g = game();
    if (!g || !prepare()) {
      setStatus('JUEGO NO LISTO', true);
      return;
    }
    var sim = g.sim;
    var p = sim.player;
    p.cooldowns.delete(abilityId);
    p.gcdRemaining = 0;
    p.resource = p.maxResource;
    var target = config.target === 'enemy' ? stageDummy() : null;
    var x = p.pos.x + Math.sin(p.facing) * 8;
    var z = p.pos.z + Math.cos(p.facing) * 8;
    try {
      if (config.target === 'position') sim.castAbility(abilityId, p.id, { x:x, z:z });
      else if (config.target === 'self') sim.castAbility(abilityId, p.id, p.id);
      else if (target) sim.castAbility(abilityId, p.id, target.id);
      else throw new Error('No hay target');
      setStatus('CAST · ' + abilityId, false);
    } catch (err) {
      setStatus('ERROR · ' + String(err), true);
    }
  }

  document.getElementById('hf-lab-base').addEventListener('click', function (e) { cast(e.currentTarget.getAttribute('data-ability')); });
  document.getElementById('hf-lab-evo').addEventListener('click', function (e) { cast(e.currentTarget.getAttribute('data-ability')); });
  document.getElementById('hf-lab-reset').addEventListener('click', resetLab);
  document.getElementById('hf-lab-dummy').addEventListener('click', stageDummy);

  var tries = 0;
  var boot = setInterval(function () {
    tries++;
    clearFirstRunUi();
    if (prepare()) {
      clearInterval(boot);
      stageDummy();
      setStatus('LISTO · ' + config.label.toUpperCase(), false);
      window.__highflySkillLab = { cast:cast, reset:resetLab, stageDummy:stageDummy, activeClass:activeClass };
    } else if (tries > 240) {
      clearInterval(boot);
      setStatus('BOOT TIMEOUT', true);
    }
  }, 250);
})();
</script>
"""

rep(
    "index.html",
    """  <script type="module" src="/src/main.ts"></script>
</body>""",
    """  <script type="module" src="/src/main.ts"></script>
""" + LAB_SCRIPT + """
</body>""",
)

print("HIGHFLY_CURRENT_SKILL_LAB_PACK01=1")
