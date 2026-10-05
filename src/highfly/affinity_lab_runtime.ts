import '../styles/hf_affinity_lab.css';
import { configureWeaponInfusion } from '../sim/combat/highfly_weapon_affinity';
import {
  type AffinityReceptor,
  HIGHFLY_AFFINITY_AUDIT_V1,
  HIGHFLY_AFFINITY_CLASSES,
  type HighflyAffinity,
  type HighflyClass,
} from './affinity_lab_loadouts';
import {
  type HighflyCharacterQ0Body,
  highflyCharacterQ0Body,
  setHighflyCharacterQ0Body,
} from './character_q0_visual';
import {
  AFFINITY_MODES,
  buildAffinityHotbar,
  parseWeaponAffinities,
  weaponAffinity,
} from './weapon_affinity_core';

const w = window as any;
const CLASS_SPECS: Partial<Record<HighflyClass, readonly string[]>> = {
  warrior: ['arms', 'fury', 'prot'],
  rogue: ['assassination', 'combat', 'subtlety'],
  mage: ['arcane', 'fire', 'frost'],
};
const savedQ0Body = (localStorage.getItem('hf_character_q0_body') ||
  'qmale') as HighflyCharacterQ0Body;
setHighflyCharacterQ0Body(
  new URLSearchParams(location.search).get('affinitylab') === '1' ? savedQ0Body : 'claude',
);
let q0Collapsed = localStorage.getItem('hf_character_q0_collapsed') !== '0';

const weaponInlays = parseWeaponAffinities(localStorage.getItem('hf_q1_weapon_inlays'));
let affinity: HighflyAffinity = 'base';
let lastWeaponSignature = '';
let lastHotbarSignature = '';

function equippedWeaponId(): string | null {
  const g = game();
  if (!g) return null;
  // PlayerMeta equipment is ClaudeCraft's canonical gameplay owner. mainhandItemId
  // is only the render mirror and may lag an equip/unequip by a frame.
  return g.sim.equipment?.mainhand ?? g.sim.player?.mainhandItemId ?? null;
}

function selectWeaponAffinity(next: HighflyAffinity): void {
  const g = game();
  const weaponId = equippedWeaponId();
  if (!g || !weaponId || !AFFINITY_MODES.includes(next)) return;
  weaponInlays[weaponId] = next;
  localStorage.setItem('hf_q1_weapon_inlays', JSON.stringify(weaponInlays));
  affinity = next;
  configureWeaponInfusion(g.sim.player, weaponId, next);
  const cls = metaClass();
  if (cls) renderHud(cls);
}

function syncNativeHotbar(cls: HighflyClass): void {
  const g = game();
  if (!g?.hud?.actionBarController) return;
  const bar = g.hud.actionBarController;
  const ids = buildAffinityHotbar(cls, g.sim.known, affinity);
  const signature =
    cls +
    ':' +
    (g.sim.player.specId ?? '') +
    ':' +
    bar.activeForm +
    ':' +
    bar.profile +
    ':' +
    ids.map((a) => a.id).join(',');
  if (signature === lastHotbarSignature) return;
  const firstTen = [...ids, ...Array.from({ length: Math.max(0, 10 - ids.length) }, () => null)];
  bar.replaceActions([...firstTen, ...bar.actions.slice(10)]);
  lastHotbarSignature = signature;
}

function syncWeaponAffinity(): boolean {
  const g = game();
  if (!g) return false;
  const id = equippedWeaponId();
  const mode = weaponAffinity(weaponInlays, id);
  const signature = (id ?? '') + ':' + mode;
  if (signature === lastWeaponSignature) return false;
  lastWeaponSignature = signature;
  affinity = mode;
  configureWeaponInfusion(g.sim.player, id, mode);
  return true;
}

function game(): any {
  return w.__game?.sim ? w.__game : null;
}

function activeQ0VisualKey(): string | null {
  const g = game();
  if (!g) return null;
  return g.renderer?.views?.get?.(g.sim.player.id)?.visualKey ?? null;
}

function expectedQ0VisualKey(cls: HighflyClass, body = highflyCharacterQ0Body()): string {
  return body === 'claude' ? 'player_' + cls : 'player_' + cls + '_' + body;
}

function updateQ0VisualStatus(): void {
  const cls = metaClass();
  const el = document.getElementById('hf-q0-active');
  if (!cls || !el) return;
  const selected = highflyCharacterQ0Body();
  const actual = activeQ0VisualKey();
  const expected = expectedQ0VisualKey(cls, selected);
  const ok = actual === expected;
  const label = selected === 'claude' ? 'CLAUDE' : selected === 'qmale' ? 'Q-MALE' : 'Q-FEMALE';
  el.textContent = 'ACTIVO: ' + label + (ok ? ' ✓' : ' …');
  el.classList.toggle('hf-q0-ready', ok);
  el.classList.toggle('hf-q0-wait', !ok);
  el.title = 'renderer visualKey: ' + (actual ?? 'N/D');
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
  const target = [...sim.entities.values()].find(
    (e: any) => e.id !== p.id && e.kind === 'mob' && e.ownerId == null,
  );
  if (!target) return null;
  const x = p.pos.x + Math.sin(p.facing) * distance;
  const z = p.pos.z + Math.cos(p.facing) * distance;
  target.templateId = 'training_dummy';
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
  target.facing = p.facing;
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

function prepareCast(
  id: string,
  receptor: AffinityReceptor,
): { sim: any; p: any; target: any; cls: string; spec: string | null } | null {
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
  const targetDistance =
    receptor.target === 'position'
      ? 8
      : receptor.target === 'none'
        ? 5
        : meta?.cls === 'mage' ||
            meta?.cls === 'warlock' ||
            meta?.cls === 'priest' ||
            meta?.cls === 'shaman'
          ? 12
          : 2.4;
  const target = stageDummy(targetDistance);
  if (meta?.cls === 'rogue') {
    if (receptor.id === 'eviscerate' || receptor.id === 'rupture') {
      p.comboPoints = 5;
      p.comboUntil = sim.time + 30;
    }
    if (receptor.id === 'ambush') {
      p.auras = p.auras.filter((a: any) => a.id !== 'hf_affinity_lab_stealth');
      p.auras.push({
        id: 'hf_affinity_lab_stealth',
        name: 'Afinidad LAB · Sigilo',
        kind: 'stealth',
        remaining: 8,
        duration: 8,
        value: 0,
        sourceId: p.id,
      });
      p.stealthed = true;
      if (target) target.facing = p.facing;
    }
  }
  return { sim, p, target, cls, spec };
}

function cast(receptor: AffinityReceptor, mode: HighflyAffinity): void {
  const id = receptor.variants.base;
  if (!id) {
    toast('VARIANTE AÚN NO IMPLEMENTADA', true);
    return;
  }
  const prep = prepareCast(id, receptor);
  if (!prep) {
    toast('JUEGO NO LISTO', true);
    return;
  }
  const { sim, p, target, cls, spec } = prep;
  configureWeaponInfusion(p, equippedWeaponId(), mode);
  const startHp = target?.hp ?? null;
  try {
    if (receptor.target === 'position') {
      const x = p.pos.x + Math.sin(p.facing) * 8;
      const z = p.pos.z + Math.cos(p.facing) * 8;
      sim.castAbility(id, p.id, { x, z });
    } else if (receptor.target === 'enemy' && target) {
      // Canonical Claude targeting: p.targetId already owns the entity target.
      // Argument 3 is reserved for ground aim, not an entity id.
      sim.castAbility(id, p.id);
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
  const startedAt = performance.now();
  const measureImpact = () => {
    const liveTarget = target?.id != null ? sim.entities.get(target.id) : null;
    const impact = startHp != null && liveTarget ? startHp - liveTarget.hp : null;
    const timedOut = performance.now() - startedAt >= 3500;
    if ((impact ?? 0) <= 0 && !timedOut) {
      window.setTimeout(measureImpact, 50);
      return;
    }
    const liveMeta = sim.meta(p.id);
    const stable = liveMeta?.cls === cls && (p.specId ?? liveMeta?.spec ?? null) === spec;
    result(
      (mode === 'base' ? 'BASE' : mode.toUpperCase()) +
        ' · impacto observado ' +
        (impact == null ? 'N/D' : impact) +
        ' · clase/spec ' +
        (stable ? 'OK' : 'FAIL'),
    );
  };
  window.setTimeout(measureImpact, 50);
}

function selectSpec(cls: HighflyClass, spec: string): void {
  const g = game();
  if (!g || metaClass() !== cls) return;
  const sim = g.sim;
  const p = sim.player;

  // LAB-only tester transition: leave combat before an EXPLICIT spec change.
  // Production keeps Claude's normal "no spec swaps in combat" rule untouched.
  p.inCombat = false;
  p.combatTimer = 99;
  p.autoAttack = false;
  p.gcdRemaining = 0;
  p.castingAbility = null;
  p.channeling = false;
  p.leap = null;
  p.queuedCastTargetId = null;
  const target = p.targetId != null ? sim.entities.get(p.targetId) : null;
  if (target) {
    target.inCombat = false;
    target.aggroTargetId = null;
  }

  const ok = sim.setSpec(spec);
  if (!ok) {
    toast('NO SE PUDO CAMBIAR SPEC', true);
    return;
  }
  p.resource = p.maxResource;
  toast('SPEC · ' + spec.toUpperCase());
  renderHud(cls);
}

function switchClass(cls: HighflyClass): void {
  const params = new URLSearchParams(location.search);
  params.set('affinitylab', '1');
  params.set('labclass', cls);
  location.href = location.pathname + '?' + params.toString();
}

function selectQ0Body(next: HighflyCharacterQ0Body): void {
  const before = highflyCharacterQ0Body();
  const selected = setHighflyCharacterQ0Body(next);
  localStorage.setItem('hf_character_q0_body', selected);
  const cls = metaClass();
  if (cls) renderHud(cls);
  if (selected !== before) {
    toast(
      'PERSONAJE · ' +
        (selected === 'claude' ? 'CLAUDE' : selected === 'qmale' ? 'Q-MALE' : 'Q-FEMALE'),
    );
    result('HOT SWAP VISUAL · esperando renderer…');
    const started = performance.now();
    const verify = () => {
      updateQ0VisualStatus();
      const liveCls = metaClass();
      if (!liveCls) return;
      const ok = activeQ0VisualKey() === expectedQ0VisualKey(liveCls, selected);
      if (ok) {
        result('HOT SWAP OK · cuerpo visual cambiado; SIM / skill / target / cámara intactos.');
        return;
      }
      if (performance.now() - started < 2500) window.setTimeout(verify, 50);
      else result('Q0 WARNING · selección cambió pero el renderer no confirmó el cuerpo.');
    };
    window.setTimeout(verify, 50);
  }
}

function toggleQ0Collapsed(): void {
  q0Collapsed = !q0Collapsed;
  localStorage.setItem('hf_character_q0_collapsed', q0Collapsed ? '1' : '0');
  const cls = metaClass();
  if (cls) renderHud(cls);
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
  document.body.classList.add('hf-affinity-active', 'hf-q1-active');
  document.getElementById('hf-affinity-hud')?.remove();
  const root = document.createElement('section');
  root.id = 'hf-affinity-hud';
  root.className = 'ui-panel';
  root.classList.toggle('hf-collapsed', q0Collapsed);
  document.body.classList.toggle('hf-q0-collapsed', q0Collapsed);
  const head = document.createElement('div');
  head.className = 'hf-aff-head';
  const title = document.createElement('span');
  title.className = 'hf-aff-title';
  title.textContent = 'HIGHFLY Q1';
  head.append(title);
  const activeBody = document.createElement('span');
  activeBody.id = 'hf-q0-active';
  activeBody.className = 'hf-q0-active';
  head.append(activeBody);
  const current = document.createElement('span');
  current.className = 'hf-aff-summary';
  current.textContent = audit.label + ' / ' + affinity.toUpperCase();
  head.append(current);
  const minimize = document.createElement('button');
  minimize.className = 'hf-q0-collapse ui-btn';
  minimize.textContent = q0Collapsed ? 'AJUSTES' : 'CERRAR';
  minimize.setAttribute('aria-expanded', String(!q0Collapsed));
  minimize.addEventListener('click', toggleQ0Collapsed);
  head.append(minimize);
  root.append(head);
  const controls = document.createElement('div');
  controls.className = 'hf-aff-controls';
  const row = (label: string) => {
    const group = document.createElement('div');
    group.className = 'hf-aff-row';
    const caption = document.createElement('span');
    caption.className = 'hf-q0-label';
    caption.textContent = label;
    group.append(caption);
    controls.append(group);
    return group;
  };
  const bodies = row('PERSONAJE');
  for (const candidate of ['claude', 'qmale', 'qfemale'] as const) {
    const button = document.createElement('button');
    button.className =
      'hf-q0-body ui-btn' + (highflyCharacterQ0Body() === candidate ? ' hf-active' : '');
    button.dataset.body = candidate;
    button.textContent =
      candidate === 'claude' ? 'CLAUDE' : candidate === 'qmale' ? 'Q-MALE' : 'Q-FEMALE';
    button.addEventListener('click', () => selectQ0Body(candidate));
    bodies.append(button);
  }
  const classes = row('CLASE');
  for (const candidate of HIGHFLY_AFFINITY_CLASSES) {
    const button = document.createElement('button');
    button.className = 'hf-aff-classpick ui-btn' + (candidate === cls ? ' hf-active' : '');
    button.textContent = candidate.toUpperCase();
    button.addEventListener('click', () => switchClass(candidate));
    classes.append(button);
  }
  const specs = CLASS_SPECS[cls] ?? [];
  const currentSpec =
    game()?.sim?.player?.specId ?? game()?.sim?.meta(game()?.sim?.player?.id)?.spec ?? null;
  if (specs.length) {
    const specRow = row('SPEC');
    for (const spec of specs) {
      const button = document.createElement('button');
      button.className = 'hf-aff-spec ui-btn' + (spec === currentSpec ? ' hf-active' : '');
      button.textContent = spec.toUpperCase();
      button.addEventListener('click', () => selectSpec(cls, spec));
      specRow.append(button);
    }
  }
  const elements = row('ELEMENTO DEL ARMA');
  for (const element of AFFINITY_MODES) {
    const button = document.createElement('button');
    button.className = 'hf-aff-element ui-btn' + (affinity === element ? ' hf-active' : '');
    button.dataset.affinity = element;
    button.textContent =
      element === 'base'
        ? 'BASE'
        : element === 'fire'
          ? 'FUEGO'
          : element === 'frost'
            ? 'HIELO'
            : 'RAYO';
    button.disabled = equippedWeaponId() === null;
    button.addEventListener('click', () => selectWeaponAffinity(element));
    elements.append(button);
  }
  const detail = document.createElement('div');
  detail.className = 'hf-aff-weapon';
  detail.textContent = equippedWeaponId()
    ? 'ARMA: ' + equippedWeaponId()
    : 'EQUIPÁ UN ARMA PARA ACTIVAR ELEMENTOS';
  controls.append(detail);
  const reset = document.createElement('button');
  reset.className = 'hf-aff-reset ui-btn';
  reset.textContent = 'REINICIAR PRUEBA';
  reset.addEventListener('click', resetLab);
  controls.append(reset);
  const resultEl = document.createElement('div');
  resultEl.id = 'hf-affinity-result';
  resultEl.textContent = '10 habilidades nativas. Afinidad en receptores compatibles.';
  controls.append(resultEl);
  root.append(controls);
  document.body.append(root);
  updateQ0VisualStatus();
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
      const weaponChanged = syncWeaponAffinity();
      syncNativeHotbar(cls);
      if (cls !== lastClass || weaponChanged || !document.getElementById('hf-affinity-hud')) {
        lastClass = cls;
        renderHud(cls);
      }
    }
    updateQ0VisualStatus();
    window.setTimeout(tick, 250);
  };
  tick();
}

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', boot, { once: true });
} else {
  boot();
}

w.__highflyCharacterQ0 = {
  body: () => highflyCharacterQ0Body(),
  activeVisualKey: () => activeQ0VisualKey(),
  selectBody: (next: HighflyCharacterQ0Body) => selectQ0Body(next),
  toggleCollapsed: () => toggleQ0Collapsed(),
};

w.__highflyAffinityLab = {
  audit: HIGHFLY_AFFINITY_AUDIT_V1,
  affinity: () => {
    syncWeaponAffinity();
    return affinity;
  },
  selectAffinity: (next: HighflyAffinity) => selectWeaponAffinity(next),
  weaponId: () => equippedWeaponId(),
  nativeHotbar: () => game()?.hud?.actionBarController?.actions?.slice(0, 10) ?? [],
  weaponAffinities: () => ({ ...weaponInlays }),
  resetLab: () => resetLab(),
  selectSpec: (spec: string) => {
    const cls = metaClass();
    if (cls) selectSpec(cls, spec);
  },
  castReceptor: (id: string, mode: HighflyAffinity = 'base') => {
    const cls = metaClass();
    const receptor = cls
      ? HIGHFLY_AFFINITY_AUDIT_V1[cls].receptors.find((r) => r.id === id && r.implemented)
      : null;
    if (receptor) cast(receptor, mode);
  },
  castBase: () => {
    const cls = metaClass();
    const receptor = cls
      ? HIGHFLY_AFFINITY_AUDIT_V1[cls].receptors.find((r) => r.implemented)
      : null;
    if (receptor) cast(receptor, 'base');
  },
  castElement: () => {
    const cls = metaClass();
    const receptor = cls
      ? HIGHFLY_AFFINITY_AUDIT_V1[cls].receptors.find((r) => r.implemented)
      : null;
    if (receptor) cast(receptor, affinity);
  },
};
