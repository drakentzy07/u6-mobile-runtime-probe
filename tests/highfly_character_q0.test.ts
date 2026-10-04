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
    const overlay = readFileSync('overlays/apply_highfly_character_q0.py','utf8');
    expect(overlay).toContain("highflyCharacterQ0Body() !== 'claude'");
    expect(overlay).toContain("e.kind === 'player'");
    expect(overlay).toContain("formKey || isMechWearer(e) || q0VisualReplacement");
    expect(overlay).toContain("? null");
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
