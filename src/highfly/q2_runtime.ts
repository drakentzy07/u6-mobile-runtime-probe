import '../styles/hf_q2_clean.css';
import {
  configureHighflyWeaponElement,
  HIGHFLY_WEAPON_ELEMENTS,
  type HighflyWeaponElement,
} from '../sim/combat/highfly_q2_elemental_basic';

const w = window as any;
const params = new URLSearchParams(location.search);
const active = params.get('q2test') === '1';
if (active) document.body.classList.add('hf-q2-active');

let mode = (params.get('gem') || localStorage.getItem('hf_q2_test_gem') || 'base') as HighflyWeaponElement;
if (!HIGHFLY_WEAPON_ELEMENTS.includes(mode)) mode = 'base';
let enabled = mode !== 'base';
let lastSignature = '';
let seededFor = '';

function game(): any {
  return w.__game?.sim ? w.__game : null;
}

function equippedWeaponId(): string | null {
  const g = game();
  return g?.sim?.equipment?.mainhand ?? g?.sim?.player?.mainhandItemId ?? null;
}

function toast(message: string, detail = ''): void {
  let el = document.getElementById('hf-q2-toast');
  if (!el) {
    el = document.createElement('div');
    el.id = 'hf-q2-toast';
    document.body.append(el);
  }
  el.innerHTML = '';
  const title = document.createElement('strong');
  title.textContent = message;
  el.append(title);
  if (detail) {
    const body = document.createElement('span');
    body.textContent = detail;
    el.append(body);
  }
  el.classList.add('show');
  window.setTimeout(() => el?.classList.remove('show'), 1800);
}

function configureElement(): void {
  const g = game();
  if (!g) return;
  const weapon = equippedWeaponId();
  const effective = enabled ? mode : 'base';
  const signature = [weapon ?? '', effective].join(':');
  if (signature === lastSignature) return;
  lastSignature = signature;
  configureHighflyWeaponElement(g.sim.player, weapon, effective, enabled);
  paintGemSeat();
}

function eligibleAbilities(g: any): string[] {
  return g.sim.known
    .filter((entry: any) => {
      const def = entry.def;
      return !def.passive && !def.hiddenFromPlayer && !def.requiresStealth;
    })
    .map((entry: any) => entry.def.id);
}

function seedTenNativeSkills(): void {
  const g = game();
  const bar = g?.hud?.actionBarController;
  if (!bar) return;
  const cls = g.sim.meta(g.sim.player.id)?.cls ?? '';
  const spec = g.sim.player.specId ?? '';
  const signature = cls + ':' + spec + ':' + g.sim.player.level;
  if (seededFor === signature) return;
  const ids = [...new Set(eligibleAbilities(g))].slice(0, 10);
  const firstTen = [...ids.map((id) => ({ type: 'ability' as const, id })), ...Array.from({ length: Math.max(0, 10 - ids.length) }, () => null)];
  bar.replaceActions([...firstTen, ...bar.actions.slice(10)]);
  seededFor = signature;
}

function paintGemSeat(): void {
  const el = document.getElementById('hf-q2-gem-seat');
  if (!el) return;
  const effective = enabled ? mode : 'base';
  el.dataset.element = effective;
  el.textContent =
    effective === 'fire' ? '🔥' :
    effective === 'frost' ? '❄️' :
    effective === 'lightning' ? '⚡' :
    effective === 'air' ? '🌪️' : '◇';
  el.title = effective === 'base' ? 'Gema elemental inactiva' : 'Gema ' + effective.toUpperCase() + ' activa';
}

function nextGem(): void {
  const choices: HighflyWeaponElement[] = ['fire', 'frost', 'lightning', 'air'];
  const current = choices.indexOf(mode);
  mode = choices[(current + 1 + choices.length) % choices.length];
  enabled = true;
  localStorage.setItem('hf_q2_test_gem', mode);
  lastSignature = '';
  configureElement();
  toast('GEMA · ' + mode.toUpperCase(), 'ATK4 elemental habilitado');
}

function ensureGemSeat(): void {
  if (document.getElementById('hf-q2-gem-seat')) return;
  const el = document.createElement('button');
  el.id = 'hf-q2-gem-seat';
  el.type = 'button';
  el.className = 'hf-q2-utility-seat';
  let timer = 0;
  let held = false;
  el.addEventListener('pointerdown', () => {
    held = false;
    timer = window.setTimeout(() => {
      held = true;
      nextGem();
    }, 550);
  });
  const end = () => {
    if (timer) window.clearTimeout(timer);
    timer = 0;
  };
  el.addEventListener('pointerup', () => {
    end();
    if (held) return;
    enabled = !enabled && mode !== 'base';
    if (mode === 'base') nextGem();
    else {
      lastSignature = '';
      configureElement();
      toast(enabled ? 'GEMA ACTIVA' : 'GEMA DESACTIVADA', mode.toUpperCase());
    }
  });
  el.addEventListener('pointercancel', end);
  document.body.append(el);
  paintGemSeat();
}

function installSkillHolds(): void {
  document.querySelectorAll<HTMLButtonElement>('#actionbar .action-btn[data-hotbar-slot]').forEach((btn) => {
    if (btn.dataset.hfQ2Hold === '1') return;
    btn.dataset.hfQ2Hold = '1';
    let timer = 0;
    let inspected = false;
    btn.addEventListener('pointerdown', () => {
      inspected = false;
      timer = window.setTimeout(() => {
        inspected = true;
        const name = btn.getAttribute('aria-label') || 'Skill';
        const description = btn.getAttribute('aria-description') || 'Habilidad original de la clase.';
        toast(name.replace(/^Slot \d+:\s*/i, ''), description);
      }, 520);
    }, { capture: true });
    const finish = () => {
      if (timer) window.clearTimeout(timer);
      timer = 0;
    };
    btn.addEventListener('pointerup', finish, { capture: true });
    btn.addEventListener('pointercancel', finish, { capture: true });
    btn.addEventListener('click', (ev) => {
      if (!inspected) return;
      ev.preventDefault();
      ev.stopImmediatePropagation();
      inspected = false;
    }, { capture: true });
  });
}

function installAttackUseHold(): void {
  const attack = document.getElementById('mobile-action-attack') as HTMLButtonElement | null;
  const use = document.getElementById('mobile-interact') as HTMLButtonElement | null;
  if (!attack || !use || attack.dataset.hfQ2Use === '1') return;
  attack.dataset.hfQ2Use = '1';
  attack.setAttribute('aria-label', 'Atacar / Usar');
  let timer = 0;
  let useHeld = false;
  attack.addEventListener('pointerdown', () => {
    useHeld = false;
    timer = window.setTimeout(() => {
      useHeld = true;
      use.click();
      toast('USAR', 'Interacción contextual');
    }, 550);
  }, { capture: true });
  const finish = () => {
    if (timer) window.clearTimeout(timer);
    timer = 0;
  };
  attack.addEventListener('pointerup', (ev) => {
    finish();
    if (!useHeld) return;
    ev.preventDefault();
    ev.stopImmediatePropagation();
  }, { capture: true });
  attack.addEventListener('pointercancel', finish, { capture: true });
  attack.addEventListener('click', (ev) => {
    if (!useHeld) return;
    ev.preventDefault();
    ev.stopImmediatePropagation();
    useHeld = false;
  }, { capture: true });
}

function boot(): void {
  if (!active) return;
  const tick = () => {
    const g = game();
    if (g) {
      if (g.sim.player.level < 60) g.sim.setPlayerLevel(60);
      seedTenNativeSkills();
      configureElement();
      ensureGemSeat();
      installSkillHolds();
      installAttackUseHold();
    }
    window.setTimeout(tick, 300);
  };
  tick();
}

if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', boot, { once: true });
else boot();

w.__highflyQ2 = {
  element: () => (enabled ? mode : 'base'),
  setElement: (next: HighflyWeaponElement) => {
    if (!HIGHFLY_WEAPON_ELEMENTS.includes(next)) return;
    mode = next;
    enabled = next !== 'base';
    lastSignature = '';
    configureElement();
  },
};
