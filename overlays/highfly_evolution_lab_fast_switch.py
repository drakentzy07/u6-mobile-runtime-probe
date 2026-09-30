from pathlib import Path

p = Path("src/main.ts")
s = p.read_text(encoding="utf-8")

def replace_once(old: str, new: str) -> None:
    global s
    count = s.count(old)
    if count != 1:
        raise SystemExit(f"expected exactly one anchor, got {count}: {old[:100]!r}")
    s = s.replace(old, new, 1)

# Evolution Lab-only boot + class switcher. This never ships unless
# VITE_HIGHFLY_EVOLUTION_LAB=1 is explicitly set by the isolated lab build.
replace_once(
    """async function startOffline(
  playerClass: PlayerClass,
""",
    """const HIGHFLY_EVOLUTION_LAB = import.meta.env.VITE_HIGHFLY_EVOLUTION_LAB === '1';
const HIGHFLY_EVOLUTION_LAB_CLASS_KEY = 'highfly.evolutionLab.class';
const HIGHFLY_EVOLUTION_LAB_CLASSES: readonly PlayerClass[] = [
  'warrior',
  'paladin',
  'hunter',
  'rogue',
  'priest',
  'shaman',
  'mage',
  'warlock',
  'druid',
];

function highflyEvolutionLabClass(): PlayerClass {
  try {
    const stored = localStorage.getItem(HIGHFLY_EVOLUTION_LAB_CLASS_KEY) as PlayerClass | null;
    return stored && HIGHFLY_EVOLUTION_LAB_CLASSES.includes(stored) ? stored : 'warrior';
  } catch {
    return 'warrior';
  }
}

function highflyEvolutionLabPrime(): void {
  if (!HIGHFLY_EVOLUTION_LAB) return;
  try {
    localStorage.setItem('woc.tutorial.v1', 'done');
  } catch {
    // Storage can be unavailable in hardened/private contexts; lab still boots.
  }
}

function mountHighflyEvolutionLabSwitcher(current: PlayerClass): void {
  if (!HIGHFLY_EVOLUTION_LAB || document.getElementById('hf-evolution-lab')) return;

  const root = document.createElement('div');
  root.id = 'hf-evolution-lab';
  root.style.cssText =
    'position:fixed;right:max(10px,env(safe-area-inset-right));top:max(10px,env(safe-area-inset-top));z-index:2147483000;font:600 13px/1.2 system-ui,sans-serif;color:#fff;';

  const toggle = document.createElement('button');
  toggle.type = 'button';
  toggle.textContent = 'LAB · ' + current.toUpperCase();
  toggle.setAttribute('aria-expanded', 'false');
  toggle.style.cssText =
    'min-height:42px;padding:9px 13px;border:1px solid rgba(255,255,255,.35);border-radius:12px;background:rgba(8,12,20,.92);color:#fff;box-shadow:0 4px 18px rgba(0,0,0,.35);font:inherit;touch-action:manipulation;';

  const panel = document.createElement('div');
  panel.hidden = true;
  panel.style.cssText =
    'margin-top:6px;width:min(326px,80vw);padding:8px;border:1px solid rgba(255,255,255,.25);border-radius:12px;background:rgba(8,12,20,.96);box-shadow:0 6px 24px rgba(0,0,0,.45);';

  const title = document.createElement('div');
  title.textContent = 'Cambio rápido de clase';
  title.style.cssText = 'padding:2px 4px 8px;opacity:.8;';
  panel.appendChild(title);

  const grid = document.createElement('div');
  grid.style.cssText = 'display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:6px;';

  for (const cls of HIGHFLY_EVOLUTION_LAB_CLASSES) {
    const button = document.createElement('button');
    button.type = 'button';
    button.dataset.class = cls;
    button.textContent = cls.toUpperCase();
    button.disabled = cls === current;
    button.style.cssText =
      'min-height:38px;padding:7px 5px;border:1px solid rgba(255,255,255,.2);border-radius:9px;background:rgba(255,255,255,.08);color:#fff;font:inherit;touch-action:manipulation;';
    if (cls === current) {
      button.style.opacity = '.45';
    } else {
      button.addEventListener('click', () => {
        try {
          localStorage.setItem(HIGHFLY_EVOLUTION_LAB_CLASS_KEY, cls);
          localStorage.setItem('woc.tutorial.v1', 'done');
        } catch {
          // Reload still works; it will fall back to Warrior if storage failed.
        }
        location.reload();
      });
    }
    grid.appendChild(button);
  }

  panel.appendChild(grid);
  toggle.addEventListener('click', () => {
    panel.hidden = !panel.hidden;
    toggle.setAttribute('aria-expanded', panel.hidden ? 'false' : 'true');
  });

  root.append(toggle, panel);
  document.body.appendChild(root);
}

async function startOffline(
  playerClass: PlayerClass,
""",
)

replace_once(
    """  await nextPaint();
  mountGameUi();

  const canvas = $('#game-canvas') as unknown as HTMLCanvasElement;
""",
    """  await nextPaint();
  mountGameUi();
  mountHighflyEvolutionLabSwitcher(world.cfg.playerClass);

  const canvas = $('#game-canvas') as unknown as HTMLCanvasElement;
""",
)

# In the lab, bypass the first-arrival tutorial/cinematic. Normal builds keep it.
replace_once(
    """  void startGame(sim, sim, null, `offline:${playerClass}:${name}`, true);
""",
    """  void startGame(sim, sim, null, `offline:${playerClass}:${name}`, !HIGHFLY_EVOLUTION_LAB);
""",
)

# Evolution Lab boots directly to the world. First visit = Warrior; afterwards
# the LAB switcher persists the requested class and a reload returns here.
replace_once(
    """if (editorPlaytest) {
  startSitePresence('home');
""",
    """if (HIGHFLY_EVOLUTION_LAB) {
  highflyEvolutionLabPrime();
  startSitePresence('home');
  void startOffline(highflyEvolutionLabClass(), 'EvolutionLab', 0);
} else if (editorPlaytest) {
  startSitePresence('home');
""",
)

p.write_text(s, encoding="utf-8")
print("HIGHFLY_EVOLUTION_LAB_FAST_SWITCH=1")
