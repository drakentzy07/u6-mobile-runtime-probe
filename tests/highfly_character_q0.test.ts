import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
import {
  highflyCharacterQ0Body,
  highflyCharacterQ0VisualKey,
  setHighflyCharacterQ0Body,
} from '../src/highfly/character_q0_visual';

describe('HIGHFLY CHARACTER Q0', () => {
  it('defaults to Claude and changes only the player visual key', () => {
    setHighflyCharacterQ0Body('claude');
    expect(highflyCharacterQ0Body()).toBe('claude');
    expect(highflyCharacterQ0VisualKey('player_warrior')).toBe('player_warrior');
    setHighflyCharacterQ0Body('qmale');
    expect(highflyCharacterQ0VisualKey('player_warrior')).toBe('player_warrior_qmale');
    expect(highflyCharacterQ0VisualKey('mob_wolf')).toBe('mob_wolf');
    setHighflyCharacterQ0Body('qfemale');
    expect(highflyCharacterQ0VisualKey('player_mage')).toBe('player_mage_qfemale');
    setHighflyCharacterQ0Body('claude');
  });

  it('routes Q0 bodies through renderer data only, preserving class donors', () => {
    const manifest = readFileSync('src/render/characters/manifest.ts','utf8');
    expect(manifest).toContain("hf_q0_male_rigmedium.glb");
    expect(manifest).toContain("hf_q0_female_rigmedium.glb");
    expect(manifest).toContain("animUrls: [base.url, ...(base.animUrls ?? [])]");
    expect(manifest).toContain("return highflyCharacterQ0VisualKey(baseKey)");
  });

  it('bypasses Claude modular composition only while a Q0 replacement body is selected', () => {
    const index = readFileSync('src/render/characters/index.ts','utf8');
    expect(index).toContain("highflyCharacterQ0Body() !== 'claude'");
    expect(index).toContain("e.kind === 'player'");
    expect(index).toContain("formKey || isMechWearer(e) || q0VisualReplacement");
    expect(index).toContain("? null");
  });

  it('fits Q0 held props visually without changing combat authority', () => {
    const visual = readFileSync('src/render/characters/visual.ts','utf8');
    expect(visual).toContain("key.endsWith('_qmale') ? 0.74");
    expect(visual).toContain("key.endsWith('_qfemale') ? 0.70");
    expect(visual).toContain("heldPropHolder");
    const q0Slice = visual.slice(
      visual.indexOf('const highflyQ0HeldScale'),
      visual.indexOf('// Release-on-throw', visual.indexOf('const highflyQ0HeldScale')),
    );
    expect(q0Slice).not.toContain('WeaponTrace');
    expect(q0Slice).not.toContain('hitbox');
    expect(q0Slice).not.toContain('castAbility');
  });

  it('exposes all nine native Claude classes in the Q0 lab', () => {
    const runtime = readFileSync('src/highfly/affinity_lab_runtime.ts','utf8');
    expect(runtime).toContain('for (const candidate of HIGHFLY_AFFINITY_CLASSES)');
  });

  it('preserves Warrior and Paladin class-specific presentation hooks on Q0 keys', () => {
    const warriorAliases = readFileSync('src/render/characters/warrior_ability_clips.ts','utf8');
    const warriorFallbacks = readFileSync('src/render/characters/warrior_action_fallbacks.ts','utf8');
    const warriorBody = readFileSync('src/render/characters/warrior_body_effects.ts','utf8');
    const warriorBlend = readFileSync('src/render/characters/warrior_action_blend.ts','utf8');
    const assets = readFileSync('src/render/characters/assets.ts','utf8');
    const visual = readFileSync('src/render/characters/visual.ts','utf8');

    for (const src of [warriorAliases, warriorFallbacks, warriorBody]) {
      expect(src).toContain("'player_warrior_qmale'");
      expect(src).toContain("'player_warrior_qfemale'");
    }
    expect(warriorBlend).toContain("'player_warrior_qmale'");
    expect(warriorBlend).toContain("'player_warrior_qfemale'");

    expect(assets).toContain("'player_paladin_qmale'");
    expect(assets).toContain("'player_paladin_qfemale'");
    expect(visual).toContain("'player_paladin_qmale'");
    expect(visual).toContain("'player_paladin_qfemale'");
    expect(visual).toContain('PaladinBastionSweepFx');
    expect(visual).toContain('PaladinTemplarsVerdictFx');
  });

  it('keeps Q0 selector out of SIM/combat/movement authority', () => {
    const selector = readFileSync('src/highfly/character_q0_visual.ts','utf8');
    expect(selector).not.toContain('castAbility');
    expect(selector).not.toContain('dealDamage');
    expect(selector).not.toContain('cooldown');
    expect(selector).not.toContain('resource');
    expect(selector).not.toContain('pos.');
    expect(selector).not.toContain('camera');
  });

  it('keeps the Affinity Lab hot-swap UI explicit and reload-free', () => {
    const runtime = readFileSync('src/highfly/affinity_lab_runtime.ts','utf8');
    expect(runtime).toContain("['claude', 'qmale', 'qfemale']");
    expect(runtime).toContain("selectQ0Body(candidate)");
    const fnStart=runtime.indexOf('function selectQ0Body(');
    const fnEnd=runtime.indexOf('function resetLab(',fnStart);
    const body=runtime.slice(fnStart,fnEnd);
    expect(body).not.toContain('location.href');
    expect(body).not.toContain('setPlayerLevel');
    expect(body).not.toContain('targetId =');
  });
});
