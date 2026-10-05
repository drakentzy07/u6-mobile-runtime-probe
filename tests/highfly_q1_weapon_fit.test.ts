import * as THREE from 'three';
import { describe, expect, it } from 'vitest';
import { fitQ1Weapons, Q1_WEAPON_BODY_RATIOS } from '../src/highfly/character_q1_weapon_fit';

function body(height = 2) {
  const root = new THREE.Group();
  const mesh = new THREE.SkinnedMesh(new THREE.BoxGeometry(0.8, height, 0.5));
  mesh.position.y = height / 2;
  root.add(mesh);
  return root;
}

function held(name: string, size: [number, number, number], slot: 0 | 1) {
  const holder = new THREE.Group();
  holder.name = name;
  holder.userData.heldPropHolder = true;
  holder.userData.heldSlot = slot;
  const mesh = new THREE.Mesh(new THREE.BoxGeometry(...size));
  mesh.userData.weaponMesh = true;
  holder.add(mesh);
  return holder;
}

describe('Q1.1 BODY x WEAPON visual fit', () => {
  it('shrinks oversized sword and shield to their body-ratio caps', () => {
    const model = body(2);
    const sword = held('sword_test', [0.2, 3, 0.1], 0);
    const shield = held('shield_test', [1.4, 1.4, 0.15], 1);
    model.add(sword, shield);

    fitQ1Weapons(model, 'player_warrior_qmale', null, null);

    expect(sword.userData.q1Fit.family).toBe('sword');
    expect(shield.userData.q1Fit.family).toBe('shield');
    expect(sword.userData.q1Fit.bodyRatio).toBeCloseTo(Q1_WEAPON_BODY_RATIOS.sword, 5);
    expect(shield.userData.q1Fit.bodyRatio).toBeCloseTo(Q1_WEAPON_BODY_RATIOS.shield, 5);
    expect(sword.userData.q1Fit.scale).toBeLessThan(1);
    expect(shield.userData.q1Fit.scale).toBeLessThan(1);
  });

  it('recomputes from authored grip scale instead of compounding old q1Fit', () => {
    const model = body(2);
    const sword = held('sword_test', [0.2, 3, 0.1], 0);
    sword.scale.setScalar(0.8);
    model.add(sword);

    fitQ1Weapons(model, 'player_warrior_qmale', null, null);
    const first = sword.scale.x;
    fitQ1Weapons(model, 'player_warrior_qmale', null, null);
    expect(sword.scale.x).toBeCloseTo(first, 7);
    expect(sword.userData.q1BaseGripScale).toEqual([0.8, 0.8, 0.8]);
  });

  it('keeps normal-size weapons unchanged and applies the female silhouette factor only to caps', () => {
    const male = body(2);
    const dagger = held('dagger_test', [0.08, 0.2, 0.05], 0);
    male.add(dagger);
    fitQ1Weapons(male, 'player_rogue_qmale', null, null);
    expect(dagger.scale.x).toBe(1);

    const female = body(2);
    const sword = held('sword_test', [0.2, 3, 0.1], 0);
    female.add(sword);
    fitQ1Weapons(female, 'player_warrior_qfemale', null, null);
    expect(sword.userData.q1Fit.bodyRatio).toBeCloseTo(
      Q1_WEAPON_BODY_RATIOS.sword * 0.95,
      5,
    );
  });
});
