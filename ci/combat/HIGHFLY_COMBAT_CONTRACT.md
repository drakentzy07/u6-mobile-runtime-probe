# HIGHFLY — Dragon Combat Core v0.1 Contract

## Host / donor rule

**Lucid Knight / Project Somnia is the host.** The pinned Lucid source is
`DohunWi/3D-Action@da73b5be7ffa7216c48e196c4a69fe3ff66c6c65` on Unity 6000.4.0f1.

**Dragon Souls is a surgical donor only.** The audited donor is
`btuhany/DragonSouls-Unity3D@f54824255517801d5d3443848e1e4275d8d5066d`
(MIT, original Unity 2021.3.20f1).

There is exactly one logical HIGHFLY player core. We do not transplant or
instantiate a second PlayerController, state machine, camera stack, HUD,
damage model, stamina/stat model, Animator ownership layer, or lock-on system.

## Keep from Lucid

- PlayerController and PlayerState lifecycle.
- CharacterController locomotion/root-motion ownership.
- PlayerStats / Ego / Lucidity / Volition.
- PlayerWeapon / WeaponData / hitboxes and damage.
- PlayerLockOn and the current Cinemachine lock-on integration.
- Roll i-frames, parry/counter and existing attack cancel-to-roll behavior.
- SkillData as the compatibility bridge until HighflySkillSystem supersedes it.
- Existing rig / Animator / animation-event ownership.

## Learn from Dragon Souls

The donor mechanics are converted into HIGHFLY-owned modules rather than copied
as a competing architecture.

1. **Typed input buffer**
   - Dragon Souls stores the next Light/Heavy input during an attack.
   - HIGHFLY generalizes this to Light, Heavy, Dodge, Parry, Counter,
     Skill1-4 and Ultimate with explicit TTLs.

2. **Explicit combo windows**
   - Dragon Souls uses `attackDuration + comboPermissionDelay`.
   - HIGHFLY stores normalized consume windows per combo node so the same data
     remains usable when animations are retimed or replaced.

3. **Branching**
   - Dragon Souls demonstrates Light/Heavy arrays and a Light-Light-Heavy branch.
   - HIGHFLY represents branches as data edges from a combo node to the next node.
     This is compatible with future weapon/class skill trees.

4. **Defensive buffering / cancel policy**
   - Dragon Souls can queue another roll and retain attack intent across a roll.
   - HIGHFLY separates buffer lifetime from cancel permission so dodge/parry/skill
     cancels can be tuned per animation instead of being globally hard-coded.

5. **Free / Target combat concepts**
   - Keep the behavioral idea of free movement versus target-relative movement.
   - Lucid remains the owner of camera and lock-on. Dragon Souls camera prefabs,
     old Cinemachine setup and literal controller stack are not transplanted.

## v0.1 code boundary

`HighflyCombatCore.cs` is intentionally host-agnostic. It provides:

- `HighflyActionBuffer`: deterministic fixed-capacity combat input buffer.
- `HighflyCombatWindow`: normalized cancel/consume windows.
- `HighflyComboNode` / `HighflyComboProfile`: data-driven branches.
- `HighflyCombatCore`: buffering, consumption and telemetry.

It does **not** currently execute Lucid attacks. Integration will be a thin bridge
into the existing Lucid PlayerController after Mobile/Core v0.1 is validated.

## Promotion rule

LAB first:

design → isolated implementation → Web/PC test → Samsung S23 Ultra →
correct/tune → approve → promote the exact same HIGHFLY CORE into APP.

No LAB-only PlayerController, no APP-only combat implementation.
