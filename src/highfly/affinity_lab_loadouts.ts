export type HighflyClass =
  | 'warrior'
  | 'paladin'
  | 'rogue'
  | 'warlock'
  | 'mage'
  | 'shaman'
  | 'hunter'
  | 'druid'
  | 'priest';

export type HighflyAffinity = 'base' | 'fire' | 'frost' | 'lightning';
export type AffinityTarget = 'enemy' | 'position' | 'none' | 'self';

export interface AffinityVariantSet {
  base: string;
  fire?: string;
  frost?: string;
  lightning?: string;
}

export interface AffinityReceptor {
  id: string;
  label: string;
  target: AffinityTarget;
  variants: AffinityVariantSet;
  implemented: boolean;
  spec?: string;
  note?: string;
}

export interface AffinityClassAudit {
  label: string;
  receptors: AffinityReceptor[];
}

/**
 * AFFINITY LAB v1
 * - Native Claude class remains authoritative.
 * - Base ability remains available beside the selected elemental comparison.
 * - TANDA 1 implements Warrior + Rogue + Mage.
 * - Spec-gated receptors remain spec-gated; the lab may change spec only by explicit tester action.
 * - The remaining six classes stay audited expansion queue, never fake/proxy skills.
 */
export const HIGHFLY_AFFINITY_AUDIT_V1: Record<HighflyClass, AffinityClassAudit> = {
  warrior: {
    label: 'WARRIOR',
    receptors: [
      {
        id: 'heroic_leap',
        label: 'Salto Heroico',
        target: 'position',
        implemented: true,
        variants: {
          base: 'heroic_leap',
          fire: 'hf_aff_heroic_leap_fire_01',
          frost: 'hf_aff_heroic_leap_frost_01',
          lightning: 'hf_aff_heroic_leap_lightning_01',
        },
        note: 'Primer A/B real del pipeline.',
      },
      { id: 'whirlwind', label: 'Torbellino', target: 'enemy', implemented: true, spec: 'fury', variants: { base: 'whirlwind', fire: 'hf_aff_whirlwind_fire_01', frost: 'hf_aff_whirlwind_frost_01', lightning: 'hf_aff_whirlwind_lightning_01' } },
      { id: 'thunder_clap', label: 'Golpe de Trueno', target: 'none', implemented: true, spec: 'prot', variants: { base: 'thunder_clap', fire: 'hf_aff_thunder_clap_fire_01', frost: 'hf_aff_thunder_clap_frost_01', lightning: 'hf_aff_thunder_clap_lightning_01' } },
      { id: 'faultline', label: 'Falla', target: 'none', implemented: false, variants: { base: 'faultline' } },
      { id: 'cleave', label: 'Barrido', target: 'none', implemented: true, spec: 'arms', variants: { base: 'cleave', fire: 'hf_aff_cleave_fire_01', frost: 'hf_aff_cleave_frost_01', lightning: 'hf_aff_cleave_lightning_01' } },
    ],
  },
  paladin: {
    label: 'PALADIN',
    receptors: [
      { id: 'crusader_strike', label: 'Oathstrike', target: 'enemy', implemented: false, variants: { base: 'crusader_strike' } },
      { id: 'consecration', label: 'Tierra Consagrada', target: 'none', implemented: false, variants: { base: 'consecration' } },
      { id: 'hammer_of_wrath', label: 'Martillo de Ira', target: 'enemy', implemented: false, variants: { base: 'hammer_of_wrath' } },
      { id: 'dawnfall', label: 'Dawnfall', target: 'enemy', implemented: false, variants: { base: 'dawnfall' } },
      { id: 'sunward_disc', label: 'Disco Solar', target: 'enemy', implemented: false, variants: { base: 'sunward_disc' } },
    ],
  },
  rogue: {
    label: 'ROGUE',
    receptors: [
      { id: 'eviscerate', label: 'Remate', target: 'enemy', implemented: true, variants: { base: 'eviscerate', fire: 'hf_aff_eviscerate_fire_01', frost: 'hf_aff_eviscerate_frost_01', lightning: 'hf_aff_eviscerate_lightning_01' } },
      { id: 'ambush', label: 'Emboscada', target: 'enemy', implemented: true, variants: { base: 'ambush', fire: 'hf_aff_ambush_fire_01', frost: 'hf_aff_ambush_frost_01', lightning: 'hf_aff_ambush_lightning_01' } },
      { id: 'sinister_strike', label: 'Wicked Slash', target: 'enemy', implemented: true, variants: { base: 'sinister_strike', fire: 'hf_aff_sinister_strike_fire_01', frost: 'hf_aff_sinister_strike_frost_01', lightning: 'hf_aff_sinister_strike_lightning_01' } },
      { id: 'rupture', label: 'Ruptura', target: 'enemy', implemented: true, variants: { base: 'rupture', fire: 'hf_aff_rupture_fire_01', frost: 'hf_aff_rupture_frost_01', lightning: 'hf_aff_rupture_lightning_01' } },
    ],
  },
  warlock: {
    label: 'WARLOCK',
    receptors: [
      { id: 'shadow_bolt', label: 'Gloom Bolt', target: 'enemy', implemented: false, variants: { base: 'shadow_bolt' } },
      { id: 'immolate', label: 'Burning Pact', target: 'enemy', implemented: false, variants: { base: 'immolate' } },
      { id: 'chaos_bolt', label: 'Ruinbolt', target: 'enemy', implemented: false, variants: { base: 'chaos_bolt' } },
      { id: 'rain_of_fire', label: 'Lluvia de Fuego', target: 'position', implemented: false, variants: { base: 'rain_of_fire' } },
      { id: 'soul_lance', label: 'Soul Lance', target: 'enemy', implemented: false, variants: { base: 'soul_lance' } },
    ],
  },
  mage: {
    label: 'MAGE',
    receptors: [
      { id: 'fireball', label: 'Cinderbolt', target: 'enemy', implemented: true, variants: { base: 'fireball', fire: 'hf_aff_fireball_fire_01', frost: 'hf_aff_fireball_frost_01', lightning: 'hf_aff_fireball_lightning_01' } },
      { id: 'meteor', label: 'Meteorito', target: 'position', implemented: false, variants: { base: 'meteor' } },
      { id: 'arcane_missiles', label: 'Dardos Etéreos', target: 'enemy', implemented: true, spec: 'arcane', variants: { base: 'arcane_missiles', fire: 'hf_aff_arcane_missiles_fire_01', frost: 'hf_aff_arcane_missiles_frost_01', lightning: 'hf_aff_arcane_missiles_lightning_01' } },
      { id: 'frostbolt', label: 'Rimelance', target: 'enemy', implemented: true, variants: { base: 'frostbolt', fire: 'hf_aff_frostbolt_fire_01', frost: 'hf_aff_frostbolt_frost_01', lightning: 'hf_aff_frostbolt_lightning_01' } },
      { id: 'frost_nova', label: 'Nova de Hielo', target: 'none', implemented: true, variants: { base: 'frost_nova', fire: 'hf_aff_frost_nova_fire_01', frost: 'hf_aff_frost_nova_frost_01', lightning: 'hf_aff_frost_nova_lightning_01' } },
    ],
  },
  shaman: {
    label: 'SHAMAN',
    receptors: [
      { id: 'lightning_bolt', label: 'Arc Bolt', target: 'enemy', implemented: false, variants: { base: 'lightning_bolt' } },
      { id: 'lava_burst', label: 'Magma Burst', target: 'enemy', implemented: false, variants: { base: 'lava_burst' } },
      { id: 'stormstrike', label: 'Stormstrike', target: 'enemy', implemented: false, variants: { base: 'stormstrike' } },
      { id: 'earth_shock', label: 'Earth Shock', target: 'enemy', implemented: false, variants: { base: 'earth_shock' } },
      { id: 'earthquake', label: 'Earthquake', target: 'position', implemented: false, variants: { base: 'earthquake' } },
    ],
  },
  hunter: {
    label: 'HUNTER',
    receptors: [
      { id: 'volley', label: 'Volley', target: 'position', implemented: false, variants: { base: 'volley' } },
      { id: 'frostjaw_trap', label: 'Frostjaw Trap', target: 'position', implemented: false, variants: { base: 'frostjaw_trap' } },
      { id: 'rapid_fire', label: 'Rapid Fire', target: 'enemy', implemented: false, variants: { base: 'rapid_fire' } },
      { id: 'arcane_shot', label: 'Arcane Shot', target: 'enemy', implemented: false, variants: { base: 'arcane_shot' } },
      { id: 'shrapnel_charge', label: 'Shrapnel Charge', target: 'enemy', implemented: false, variants: { base: 'shrapnel_charge' } },
    ],
  },
  druid: {
    label: 'DRUID',
    receptors: [
      { id: 'wrath', label: 'Wildbolt', target: 'enemy', implemented: false, variants: { base: 'wrath' } },
      { id: 'starfire', label: 'Skyfall', target: 'enemy', implemented: false, variants: { base: 'starfire' } },
      { id: 'moonfire', label: 'Moonfire', target: 'enemy', implemented: false, variants: { base: 'moonfire' } },
      { id: 'hurricane', label: 'Hurricane', target: 'position', implemented: false, variants: { base: 'hurricane' } },
      { id: 'wildwake', label: 'Wildwake', target: 'position', implemented: false, variants: { base: 'wildwake' } },
    ],
  },
  priest: {
    label: 'PRIEST',
    receptors: [
      { id: 'smite', label: 'Scouring Hymn', target: 'enemy', implemented: false, variants: { base: 'smite' } },
      { id: 'holy_nova', label: 'Sunburst Canticle', target: 'none', implemented: false, variants: { base: 'holy_nova' } },
      { id: 'mind_blast', label: 'Mind Blast', target: 'enemy', implemented: false, variants: { base: 'mind_blast' } },
      { id: 'mind_flay', label: 'Mind Flay', target: 'enemy', implemented: false, variants: { base: 'mind_flay' } },
    ],
  },
};

export const HIGHFLY_AFFINITY_CLASSES = Object.keys(HIGHFLY_AFFINITY_AUDIT_V1) as HighflyClass[];
