# HIGHFLY — EVOLUTION LAB v0.1

Branch: `highfly/evolution-lab-v0.1`

## Purpose
Build HIGHFLY skill lineages in isolation from the main app and from the currently approved public Skill Lab.

Progression model:

```
CLAUDE BASE -> RANK MAX -> MASTERY -> EVO I -> EVO II -> HIGHFLY I -> HIGHFLY II
```

- **Claude Base / Ranks**: reuse the frozen ClaudeCraft combat authority.
- **Mastery**: progression gate; never grants STR/AGI/VIT/PER/INT.
- **EVO I / EVO II**: HIGHFLY-authored evolutions.
- **HIGHFLY I / II**: rare optional mutation / supermutation. Not every lineage must have them.
- **Unique Scenario Skills**: separate system; never part of the normal evolution ladder.

## Quality gate — no proxy final
A skill is not approved until all of these are green:
1. Gameplay identity
2. Sim-authoritative damage/heal/CC
3. Targeting / aim shape
4. Animation route and facing
5. VFX
6. SFX
7. Camera feedback / hit timing
8. Mobile controls and readability
9. Performance budget
10. Persistence / stable ability identity
11. Class/spec/weapon validation
12. Browser/mobile playtest

## Reuse-first
Prefer existing Claude primitives (projectile, leap, dash, charge, ground AoE, channel, summon, pull, teleport, forms, etc.) before adding new runtime mechanics.

Donor presentation can provide animation/VFX/SFX only. It never owns gameplay damage authority.

## Licensing rule
Final HIGHFLY content may only depend on assets we can redistribute for the intended release:
- MIT / permissive source
- CC0 / compatible free assets
- assets created specifically for HIGHFLY

Any Claude media asset with project-only, purchased, permission-only, NC, unclear, or unrecorded redistribution rights must be replaced before release. Development may use the frozen upstream only as permitted by its recorded per-asset terms.

## First production queue
1. **Warrior — Vaulting Charge lineage**
   - Claude Base: Vaulting Charge / Heroic Leap
   - EVO I reference: Salto Demoledor (already user-approved visually/gameplay)
   - Next: facing fix + explicit AoE proof
   - EVO II design follows only after EVO I freeze
2. **Mage — Cinderbolt / Fireball lineage**
   - First ranged/projectile evolution family
   - Goal: prove projectile evolution pipeline and premium fire presentation
3. **Rogue — Lurker's Strike / Ambush lineage**
   - First stealth/opener evolution family
   - Goal: prove positional/stealth evolution pipeline

These three deliberately cover movement-AoE, ranged projectile, and stealth melee before scaling the factory across all nine classes.
