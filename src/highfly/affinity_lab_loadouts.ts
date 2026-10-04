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
 * - Only Heroic Leap is implemented in the first mechanical slice.
 * - The remaining receptors are the audited expansion queue, not fake/proxy skills.
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
      { id: 'whirlwind', label: 'Torbellino', target: 'enemy', implemented: false, variants: { base: 'whirlwind' } },
      { id: 'thunder_clap', label: 'Golpe de Trueno', target: 'none', implemented: false, variants: { base: 'thunder_clap' } },
      { id: 'faultline', label: 'Falla', target: 'none', implemented: false, variants: { base: 'faultline' } },
      { id: 'cleave', label: 'Cleave', target: 'enemy', implemented: false, variants: { base: 'cleave' } },
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
      { id: 'eviscerate', label: 'Remate', target: 'enemy', implemented: false, variants: { base: 'eviscerate' } },
      { id: 'ambush', label: 'Emboscada', target: 'enemy', implemented: false, variants: { base: 'ambush' } },
      { id: 'sinister_strike', label: 'Wicked Slash', target: 'enemy', implemented: false, variants: { base: 'sinister_strike' } },
      { id: 'rupture', label: 'Ruptura', target: 'enemy', implemented: false, variants: { base: 'rupture' } },
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
      { id: 'fireball', label: 'Cinderbolt', target: 'enemy', implemented: false, variants: { base: 'fireball' } },
      { id: 'meteor', label: 'Meteorito', target: 'position', implemented: false, variants: { base: 'meteor' } },
      { id: 'arcane_missiles', label: 'Dardos Etéreos', target: 'enemy', implemented: false, variants: { base: 'arcane_missiles' } },
      { id: 'frostbolt', label: 'Frostbolt', target: 'enemy', implemented: false, variants: { base: 'frostbolt' } },
      { id: 'frozen_orb', label: 'Orbe Helado', target: 'enemy', implemented: false, variants: { base: 'frozen_orb' } },
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
