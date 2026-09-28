# HIGHFLY — ClaudeCraft RUN0D ORIGINAL FULL

Source pin: `levy-street/world-of-claudecraft@cecebab4da06287351b37e118c630c185717b81c` (v0.43.3)

## Purpose

RUN0C proved batch conversion. RUN0D removes the interpretation layer and ports the original ClaudeCraft visual contract for the four melee skills while keeping the already-approved Heroic Leap unchanged.

Hard rules:

- SKILL4 `7ebd380c...` stays frozen/read-only.
- Heroic Leap uses the approved RUN0B runtime unchanged.
- No invented gap-close, phase or teleport for Ambush/Backstab/Eviscerate/Pummel.
- These are melee abilities. In LAB, stand within 2.65m of the dummy.
- Ambush stealth/behind and Backstab behind are BYPASSED only so their visuals can be inspected repeatedly. The runtime reports whether behind was actually satisfied.
- Eviscerate receives exactly five simulated combo points for the isolated test.
- Pummel preserves the original interrupt-only gameplay contract: 4s lockout, 10 rage on a real interrupt, 10s cooldown; no fake damage/knockback.
- ClaudeCraft audio bytes are not copied. HIGHFLY legal replacement SFX remain.

## Original animation playback

ClaudeCraft's CharacterVisual defaults authored attack one-shots to `attackTimeScale = 1.3`.
RUN0D therefore plays:

- `Claude_Rogue_Ambush` at 1.3x
- `Claude_Rogue_Backstab` at 1.3x
- `Claude_Rogue_Finisher_Slash` at 1.3x
- `Claude_Punch_A` at 1.3x

## Original tier-0 VFX anatomy

RUN0D ports the source sequencer rather than selecting generic prefabs:

- release flash: sparks + point light + school flipbook + vertical shock halo;
- instant impact: release + 0.15s;
- school flipbooks are procedurally generated 8x8 atlases: shadow=void, blood=flame, physical=electric;
- original ribbon styles: vertical, thrust, uppercut and X/cross;
- authored impact sparks, blood, debris, smoke, ring/vRing flags and light;
- strike second swing at +0.22s or single-swing echo at +0.32s;
- tier-0 strike afterglow 1.3s;
- Ambush implosion: 8 converging motes over 0.42s;
- Eviscerate finisher: staggered finisher wave + white vertical halo + 8.5-unit pillar;
- Jawcrack: four yellow stars, radius .45, lift .55, rate 2.4, for 4s.

The generated flipbook code follows the original `electric`, `flame` and `void` frame anatomy. It does not import external flipbook image bytes.

## What to inspect

**Lurker's Strike / Ambush**
Dark shadow release, target-side implosion, large vertical dagger strike, X contact trail, blood + smoke, second vertical contact beat. No impact flipbook/ring/vRing.

**Craven Thrust / Backstab**
Fast precise thrust. Violet/void release. At impact: void flipbook, vertical halo, 8 authored sparks and blood, then the single-swing thrust echo. No ground ring.

**Dirt Nap / Eviscerate**
The loudest rogue finisher. Red/flame release, uppercut/rising slash + X trail, 36 authored sparks before spectacle scaling/cap, blood + extra bleed burst + debris, second swing, default ring/vRing, finisher wave, white halo and tall light pillar.

**Jawcrack / Pummel**
Bare fists. Guard -> chamber -> jaw extension -> recoil. Steel/physical release, uppercut + X trail, 10 authored sparks, vertical halo, no impact flipbook/ground ring/smoke/debris, then four rotating yellow stun stars for 4s.
