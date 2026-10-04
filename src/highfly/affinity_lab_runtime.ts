import '../styles/hf_affinity_lab.css';
import {
  HIGHFLY_AFFINITY_AUDIT_V1,
  HIGHFLY_AFFINITY_CLASSES,
  type AffinityReceptor,
  type HighflyAffinity,
  type HighflyClass,
} from './affinity_lab_loadouts';

const w = window as any;
const ELEMENTS: readonly Exclude<HighflyAffinity, 'base'>[] = ['fire', 'frost', 'lightning'];
let affinity = (localStorage.getItem('hf_affinity_lab_element') as HighflyAffinity) || 'fire';
if (!ELEMENTS.includes(affinity as Exclude<HighflyAffinity, 'base'>)) affinity = 'fire';

function game(): any {
  return w.__game?.sim ? w.__game : null;
}

function metaClass(): HighflyClass | null {
  const g = game();
  if (!g) return null;
  const cls = g.sim.meta(g.sim.player.id)?.cls;
  return HIGHFLY_AFFINITY_CLASSES.includes(cls) ? cls : null;
}

function stageDummy(distance = 8): any {
  const g = game();
  if (!g) return null;
  const sim = g.sim;
  const p = sim.player;
  let target = [...sim.entities.values()].find(
    (e: any) => e.id !== p.id && e.kind === 'mob' && e.ownerId == null,
  );
  if (!target) return null;
  const x = p.pos.x + Math.sin(p.facing) * distance;
  const z = p.pos.z + Math.cos(p.facing) * distance;
  target.dead = false;
  target.hostile = true;
  target.maxHp = Math.max(target.maxHp || 1, 50000);
  target.hp = target.maxHp;
  target.pos.x = x;
  target.pos.y = p.pos.y;
  target.pos.z = z;
  if (target.prevPos) {
    target.prevPos.x = x;
    target.prevPos.y = p.pos.y;
    target.prevPos.z = z;
  }
  target.aiState = 'idle';
  target.aggroTargetId = null;
  target.inCombat = false;
  target.moveSpeed = 0;
  p.targetId = target.id;
  return target;
}

let toastTimer: number | undefined;
function toast(message: string, bad = false): void {
  let el = document.getElementById('hf-affinity-toast');
  if (!el) {
    el = document.createElement('div');
    el.id = 'hf-affinity-toast';
    document.body.append(el);
  }
  el.textContent = message;
  el.classList.toggle('hf-bad', bad);
  el.classList.add('hf-show');
  if (toastTimer) clearTimeout(toastTimer);
  toastTimer = window.setTimeout(() => el?.classList.remove('hf-show'), 1800);
}

function result(message: string): void {
  const el = document.getElementById('hf-affinity-result');
  if (el) el.textContent = message;
}

function prepareCast(id: string): { sim: any; p: any; target: any; cls: string; spec: string | null } | null {
  const g = game();
  if (!g) return null;
  const sim = g.sim;
  const p = sim.player;
  if (p.level !== 20) sim.setPlayerLevel(20);
  const meta = sim.meta(p.id);
  const cls = meta?.cls ?? '';
  const spec = p.specId ?? meta?.spec ?? null;
  p.hp = p.maxHp;
  p.resource = p.maxResource;
  p.gcdRemaining = 0;
  p.gm = true;
  p.cooldowns.delete(id);
  p.castingAbility = null;
  p.channeling = false;
  p.leap = null;
  const target = stageDummy(8);
  return { sim, p, target, cls, spec };
}

function cast(receptor: AffinityReceptor, mode: HighflyAffinity): void {
  const id = receptor.variants[mode];
  if (!id) {
    toast('VARIANTE AÚN NO IMPLEMENTADA', true);
    return;
  }
  const prep = prepareCast(id);
  if (!prep) {
    toast('JUEGO NO LISTO', true);
    return;
  }
  const { sim, p, target, cls, spec } = prep;
  const startHp = target?.hp ?? null;
  try {
    if (receptor.target === 'position') {
      const x = p.pos.x + Math.sin(p.facing) * 8;
      const z = p.pos.z + Math.cos(p.facing) * 8;
      sim.castAbility(id, p.id, { x, z });
    } else if (receptor.target === 'enemy' && target) {
      sim.castAbility(id, p.id, target.id);
    } else if (receptor.target === 'self') {
      sim.castAbility(id, p.id, p.id);
    } else {
      sim.castAbility(id, p.id);
    }
  } catch (error) {
    toast('ERROR · ' + String(error), true);
    return;
  }

  const afterMeta = sim.meta(p.id);
  const afterSpec = p.specId ?? afterMeta?.spec ?? null;
  if (afterMeta?.cls !== cls || afterSpec !== spec) {
    toast('GATE FAIL · CLASE/SPEC CAMBIÓ', true);
    result('FAIL: el cast alteró clase o spec.');
    return;
  }

  toast((mode === 'base' ? 'BASE' : mode.toUpperCase()) + ' · ' + receptor.label);
  result('CAST EN CURSO · midiendo impacto…');
  window.setTimeout(() => {
    const liveTarget = target?.id != null ? sim.entities.get(target.id) : null;
    const impact = startHp != null && liveTarget ? startHp - liveTarget.hp : null;
    const liveMeta = sim.meta(p.id);
    const stable = liveMeta?.cls === cls && (p.specId ?? liveMeta?.spec ?? null) === spec;
    result(
      (mode === 'base' ? 'BASE' : mode.toUpperCase()) +
        ' · impacto observado ' +
        (impact == null ? 'N/D' : impact) +
        ' · clase/spec ' +
        (stable ? 'OK' : 'FAIL'),
    );
  }, 900);
}

function resetLab(): void {
  const g = game();
  if (!g) return;
  const p = g.sim.player;
  p.cooldowns.clear();
  p.gcdRemaining = 0;
  p.resource = p.maxResource;
  p.hp = p.maxHp;
  p.castingAbility = null;
  p.channeling = false;
  p.leap = null;
  stageDummy(8);
  result('RESET OK');
}

function renderHud(cls: HighflyClass): void {
  const audit = HIGHFLY_AFFINITY_AUDIT_V1[cls];
  document.body.classList.add('hf-affinity-active');
  document.getElementById('hf-affinity-hud')?.remove();

  const root = document.createElement('div');
  root.id = 'hf-affinity-hud';

  const head = document.createElement('div');
  head.className = 'hf-aff-head';
  head.innerHTML =
    '<span class="hf-aff-title">HIGHFLY AFFINITY LAB v1</span>' +
    '<span class="hf-aff-class">' + audit.label + '</span>' +
    '<span class="hf-aff-level">LVL 20</span>' +
    '<span class="hf-aff-spacer"></span>';

  for (const element of ELEMENTS) {
    const button = document.createElement('button');
    button.className = 'hf-aff-element' + (affinity === element ? ' hf-active' : '');
    button.textContent = element === 'fire' ? '🔥 FUEGO' : element === 'frost' ? '❄ HIELO' : '⚡ RAYO';
    button.addEventListener('click', () => {
      affinity = element;
      localStorage.setItem('hf_affinity_lab_element', affinity);
      renderHud(cls);
    });
    head.append(button);
  }

  const reset = document.createElement('button');
  reset.className = 'hf-aff-reset';
  reset.textContent = 'RESET';
  reset.addEventListener('click', resetLab);
  head.append(reset);

  const change = document.createElement('button');
  change.className = 'hf-aff-change';
  change.textContent = 'CAMBIAR CLASE';
  change.addEventListener('click', () => {
    location.href = location.pathname;
  });
  head.append(change);
  root.append(head);

  const implemented = audit.receptors.filter((r) => r.implemented);
  if (implemented.length) {
    for (const receptor of implemented) {
      const row = document.createElement('div');
      row.className = 'hf-aff-compare';
      const label = document.createElement('div');
      label.className = 'hf-aff-receptor';
      label.textContent = receptor.label;
      row.append(label);

      const base = document.createElement('button');
      base.className = 'hf-aff-skill hf-base';
      base.dataset.ability = receptor.variants.base;
      base.textContent = 'BASE · ' + receptor.label;
      base.addEventListener('click', () => cast(receptor, 'base'));
      row.append(base);

      const elementalId = receptor.variants[affinity];
      const elemental = document.createElement('button');
      elemental.className = 'hf-aff-skill hf-elemental';
      elemental.dataset.ability = elementalId ?? '';
      elemental.textContent =
        (affinity === 'fire' ? '🔥 FUEGO' : affinity === 'frost' ? '❄ HIELO' : '⚡ RAYO') +
        ' · ' +
        receptor.label;
      elemental.disabled = !elementalId;
      elemental.addEventListener('click', () => cast(receptor, affinity));
      row.append(elemental);
      root.append(row);
    }
  } else {
    const pending = document.createElement('div');
    pending.className = 'hf-aff-pending';
    pending.textContent = 'RECEPTORES AUDITADOS · implementación bloqueada hasta GREEN de Heroic Leap.';
    root.append(pending);
  }

  const queue = document.createElement('div');
  queue.className = 'hf-aff-queue';
  queue.innerHTML =
    '<strong>RECEPTORES:</strong> ' +
    audit.receptors.map((r) => r.label + (r.implemented ? ' ✓' : '')).join(' · ');
  root.append(queue);

  const resultEl = document.createElement('div');
  resultEl.id = 'hf-affinity-result';
  resultEl.textContent = 'BASE y elemental permanecen disponibles lado a lado.';
  root.append(resultEl);

  document.body.append(root);
}

function boot(): void {
  const params = new URLSearchParams(location.search);
  if (params.get('affinitylab') !== '1') return;

  let lastClass: HighflyClass | null = null;
  const tick = () => {
    const cls = metaClass();
    if (cls) {
      const g = game();
      if (g.sim.player.level !== 20) g.sim.setPlayerLevel(20);
      if (cls !== lastClass || !document.getElementById('hf-affinity-hud')) {
        lastClass = cls;
        renderHud(cls);
      }
    }
    window.setTimeout(tick, 250);
  };
  tick();
}

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', boot, { once: true });
} else {
  boot();
}

w.__highflyAffinityLab = {
  audit: HIGHFLY_AFFINITY_AUDIT_V1,
  affinity: () => affinity,
  selectAffinity: (next: HighflyAffinity) => {
    if (ELEMENTS.includes(next as Exclude<HighflyAffinity, 'base'>)) {
      affinity = next;
      localStorage.setItem('hf_affinity_lab_element', affinity);
      const cls = metaClass();
      if (cls) renderHud(cls);
    }
  },
  castBase: () => {
    const cls = metaClass();
    const receptor = cls ? HIGHFLY_AFFINITY_AUDIT_V1[cls].receptors.find((r) => r.implemented) : null;
    if (receptor) cast(receptor, 'base');
  },
  castElement: () => {
    const cls = metaClass();
    const receptor = cls ? HIGHFLY_AFFINITY_AUDIT_V1[cls].receptors.find((r) => r.implemented) : null;
    if (receptor) cast(receptor, affinity);
  },
};
