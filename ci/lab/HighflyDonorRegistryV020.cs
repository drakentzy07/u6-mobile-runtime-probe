using System;

namespace Highfly.SkillLab
{
    public enum HighflyDonorTierV020
    {
        Full,
        SemiFull,
        Candidate,
        ReferenceOnly,
        Blocked
    }

    [Serializable]
    public sealed class HighflyDonorEntryV020
    {
        public string Donor;
        public string Skill;
        public string Engine;
        public string License;
        public HighflyDonorTierV020 Tier;
        public bool RawVerified;
        public bool HighflyPorted;
        public string SourceHint;
        public string Notes;

        public HighflyDonorEntryV020(
            string donor,
            string skill,
            string engine,
            string license,
            HighflyDonorTierV020 tier,
            bool rawVerified,
            bool highflyPorted,
            string sourceHint,
            string notes)
        {
            Donor = donor;
            Skill = skill;
            Engine = engine;
            License = license;
            Tier = tier;
            RawVerified = rawVerified;
            HighflyPorted = highflyPorted;
            SourceHint = sourceHint;
            Notes = notes;
        }
    }

    /// <summary>
    /// DONOR LAB 2.0 registry.
    /// IMPORTANT:
    /// - This registry is metadata only. It does not make a candidate playable.
    /// - FULL means the donor exposes the complete gameplay block and its required
    ///   dependencies can be traced for license review.
    /// - SEMI_FULL means the original mechanic/timing can be preserved but one or
    ///   more presentation/dependency pieces must be replaced.
    /// - A skill becomes HighflyPorted only after the RAW donor behavior is verified
    ///   and a minimal HIGHFLY adapter has been tested.
    /// </summary>
    public static class HighflyDonorRegistryV020
    {
        public static readonly HighflyDonorEntryV020[] Entries =
        {
            new(
                "Lucid / 3D-Action",
                "Jump Smash",
                "Unity",
                "source repo license + dependency audit",
                HighflyDonorTierV020.Full,
                true,
                true,
                "Assets/Scripts/SO/Skill/JumpSmash.asset",
                "Native Lucid SkillData/doSmash path. Existing HIGHFLY KEEP checkpoint; presentation cleanup still allowed."),

            new(
                "Sigil Combat",
                "Dash Attack / Dash Slash",
                "Unity 6",
                "MIT",
                HighflyDonorTierV020.Full,
                false,
                false,
                "Samples~/CombatDemo/GA_CombatDemo_DashAttack.asset",
                "Original sample exposes ability asset, attack definition and GA_DashAttack runtime."),

            new(
                "Sigil Combat",
                "Charged Fireball",
                "Unity 6",
                "MIT",
                HighflyDonorTierV020.Full,
                false,
                false,
                "Samples~/CombatDemo/GA_CombatDemo_Fireball.asset",
                "Preserve hold-to-charge behavior, projectile, damage/stun scaling, cost and cooldown."),

            new(
                "Sigil Combat",
                "Flash / Blink",
                "Unity 6",
                "MIT",
                HighflyDonorTierV020.Full,
                false,
                false,
                "Samples~/CombatDemo/GA_CombatDemo_Flash.asset",
                "Movement ability candidate; RAW verification required before adapter."),

            new(
                "Sigil Combat",
                "Melee",
                "Unity 6",
                "MIT",
                HighflyDonorTierV020.Full,
                false,
                false,
                "Samples~/CombatDemo/GA_CombatDemo_Melee.asset",
                "Complete sample combat action; candidate for adapter pattern validation."),

            new(
                "Sigil Combat",
                "Ranged",
                "Unity 6",
                "MIT",
                HighflyDonorTierV020.Full,
                false,
                false,
                "Samples~/CombatDemo/GA_CombatDemo_Ranged.asset",
                "Complete sample ranged action and bullet definition."),

            new(
                "Dragon Souls",
                "Sword Throw / Embed / Recall family",
                "Unity",
                "MIT code; per-asset dependency audit required",
                HighflyDonorTierV020.Candidate,
                false,
                false,
                "ThirdPersonCombat/Assets",
                "Do not rebuild Arte del Sacrificio first. Locate and run the original donor sequence RAW."),

            new(
                "Dragon Souls",
                "Mage Projectile family",
                "Unity",
                "MIT code; per-asset dependency audit required",
                HighflyDonorTierV020.SemiFull,
                false,
                false,
                "Assets/Levels/Prefabs/Combat/EnemyMageProjectile*.prefab",
                "Prefabs, pools, magic circle and projectile controller are present; ownership of every visual dependency still needs review."),

            new(
                "Dragon Souls",
                "Dragon Fire Projectile / Fireball",
                "Unity",
                "MIT code; per-asset dependency audit required",
                HighflyDonorTierV020.SemiFull,
                false,
                false,
                "Assets/Scripts/Weapons/DragonFireProjectile.cs + DragonFireball.cs",
                "Original projectile/fireball code and pools exist; visual dependencies need audit."),

            new(
                "SubspaceHunter",
                "Fire / Electric / Ice / Meteor / Shield / Heal",
                "Unity",
                "MIT/original code; mixed third-party presentation assets",
                HighflyDonorTierV020.SemiFull,
                false,
                false,
                "Assets/SubspaceHunter/Script/WangSiFu/Skill",
                "Keep original skill flow where possible. Missing/unlicensed prefabs are replaced individually, not by rewriting the skill."),

            new(
                "SubspaceHunter",
                "Sword Slash generation",
                "Unity",
                "MIT/original code; dependency audit required",
                HighflyDonorTierV020.SemiFull,
                false,
                false,
                "SwordSlash_poseDection.cs / slash generation scripts",
                "Mechanic is present in public source. Visual slash dependency must be traced."),

            new(
                "ClaudeCraft",
                "Warrior / Rogue / Mage donor set",
                "Unity",
                "pending source/license re-verification",
                HighflyDonorTierV020.Blocked,
                false,
                false,
                "source project not mounted",
                "Previously audited 15 named skills, but they do not count until the source project is recovered and RAW execution is verified."),

            new(
                "AnyRPG",
                "A Lost Soul ability set",
                "Unity",
                "engine open source + redistributable sample content; package audit pending",
                HighflyDonorTierV020.Candidate,
                false,
                false,
                "AnyRPG Engine/A Lost Soul package pending upload",
                "Do not count abilities as FULL until the large package arrives and prefab/animation/VFX dependency graphs are checked."),

            new(
                "GenshinGamePlay",
                "Ability/Timeline composition patterns",
                "Unity",
                "MIT code; Genshin/third-party sample assets excluded",
                HighflyDonorTierV020.ReferenceOnly,
                false,
                false,
                "AbilityComponent / ActorAbility / Mixins / SkillConfig",
                "Architecture donor only. Never transplant Keqing/Genshin sample assets."),

            new(
                "Broken Seals",
                "Naturalist spellbook",
                "Godot/Pandemonium",
                "open-source project; verify per-resource provenance",
                HighflyDonorTierV020.SemiFull,
                false,
                false,
                "game/entity_classes/naturalist/spells",
                "20+ configured spell resources and several original spell-effect scenes; cross-engine adapter/reimplementation required."),

            new(
                "GASam",
                "Fireball / Lightning Bolt",
                "Unreal 5.6",
                "open-source sample; license file present",
                HighflyDonorTierV020.SemiFull,
                false,
                false,
                "Gameplay Ability System sample",
                "Complete gameplay examples with FX, but cross-engine port means logic/timing translation rather than literal Unity file copy."),

            new(
                "Mage Arena / DTF Hackathon",
                "Charged Fireball",
                "Unity",
                "MIT",
                HighflyDonorTierV020.Full,
                false,
                false,
                "DTF/Assets/Prefabs/Fireball.prefab + Scripts/Magic/Fireball.cs + VisualEffects/Fireball.vfx",
                "Compact end-to-end donor: hold-to-charge gameplay, prefab, script and original VFX path are present in the repo."),

            new(
                "Mage Arena / DTF Hackathon",
                "Particle Shield",
                "Unity",
                "MIT",
                HighflyDonorTierV020.Full,
                false,
                false,
                "DTF/Assets/Prefabs/Shield.prefab + Scripts/Magic/Shield.cs + VisualEffects/Shield.vfx",
                "Compact end-to-end defensive donor with prefab, script and VFX path present."),

            new(
                "ArenaGame",
                "Teleport Bolt / Teleport",
                "Unity",
                "GPL-3.0 code; imported presentation assets excluded",
                HighflyDonorTierV020.SemiFull,
                false,
                false,
                "MageThrowTeleportBolt.cs + MageTeleportBolt.cs + MageTeleport.cs",
                "Excellent mechanic donor: throw a marker/bolt and teleport to it. Literal code reuse requires GPL-compatible distribution; otherwise translate behavior."),

            new(
                "ArenaGame",
                "Thunderbolt / Thunderstrike / Storm Shield",
                "Unity",
                "GPL-3.0 code; imported presentation assets excluded",
                HighflyDonorTierV020.SemiFull,
                false,
                false,
                "MageThunderbolt.cs + MageThunderstrike.cs + MageStormShield.cs",
                "Distinct mage mechanics exist as concrete ability classes; replace imported visuals/audio with HIGHFLY-owned/free dependencies."),

            new(
                "ArenaGame",
                "Mechanical Hook",
                "Unity",
                "GPL-3.0 code; imported presentation assets excluded",
                HighflyDonorTierV020.SemiFull,
                false,
                false,
                "WarriorShootMechanicalHook.cs + WarriorMechanicalHook.cs",
                "Hook pulls an enemy on hit or pulls the caster toward world geometry. Strong HIGHFLY mobility/CC donor."),

            new(
                "ArenaGame",
                "Warrior Charge / Slash / Upward Slash / Boomerang",
                "Unity",
                "GPL-3.0 code; imported presentation assets excluded",
                HighflyDonorTierV020.SemiFull,
                false,
                false,
                "Assets/Scripts/entity/actors/actorAbility/Warrior*.cs",
                "Several distinct executable ability classes; presentation dependencies require replacement and GPL rules forbid casual copy-paste into unrelated licensing."),

            new(
                "ArenaGame",
                "Archer Trap / Clawhook / Dancing Arrows / Hound",
                "Unity",
                "GPL-3.0 code; imported presentation assets excluded",
                HighflyDonorTierV020.SemiFull,
                false,
                false,
                "Assets/Scripts/entity/actors/actorAbility/Archer*.cs",
                "Useful trap, grappling and multi-projectile/summon-style mechanics; audit each dependency separately."),

            new(
                "Matchstick Mage",
                "Spell / Bomb combat blocks",
                "Unity",
                "MIT",
                HighflyDonorTierV020.Candidate,
                false,
                false,
                "Assets/Scripts/Player/Spell.cs + Scripts/Bomb + Prefabs/Attacks/Bombs",
                "Project contains playable spell/bomb logic, throw animation, explosion prefab and custom shaders. Verify the complete RAW scenes before promoting to FULL."),

            new(
                "Shades SpellSystem",
                "Large spell library",
                "Unity",
                "CC BY-SA 4.0 project components; Invector dependency and per-asset audit required",
                HighflyDonorTierV020.SemiFull,
                false,
                false,
                "ShadesSpellSystem",
                "Teleport, summons, homing projectiles, status effects, heal, Raise Dead, physics spells and more. ShareAlike/dependency constraints require isolation and careful translation."),

            new(
                "Magic Mayhem",
                "Chain Lightning / Fireball / Heal / Ice Block / Ice Cone / Magic Missile / Meteor / Shield",
                "Unity",
                "NOASSERTION repository license; mixed Asset Store/Mixamo dependencies",
                HighflyDonorTierV020.Blocked,
                false,
                false,
                "Assets/Prefabs/Player/Abilities",
                "Extremely rich structural reference with many complete prefabs and upgrade variants, but do not transplant until project-level and per-asset permissions are clear."),

            new(
                "Warcraft Arena Unity",
                "Mage/Support spell system",
                "Unity",
                "The Unlicense for repository code; Warcraft/franchise presentation and audio/VFX excluded pending provenance",
                HighflyDonorTierV020.SemiFull,
                false,
                false,
                "spell definitions + Spell Effects + Aura Effects",
                "Large data-driven kit: teleport, damage, heal, AoE, absorb, stun, freeze, root, silence, procs and support. Reuse generic system/logic only; replace franchise-named/presentation dependencies."),

            new(
                "Unity_RPG_Course",
                "Sword Throw/Embed/Return + Clone Attack",
                "Unity",
                "NOASSERTION / no clear repository license",
                HighflyDonorTierV020.Blocked,
                false,
                false,
                "Sword_Skill.cs + Sword_Skill_Controller.cs + Clone_Skill.cs + Clone_Skill_Controller.cs",
                "Mechanically excellent and highly relevant: aim/throw/embed/catch plus autonomous clone attack, counter and dash assets are present. Do not copy until permission/license is established.")
        };
    }
}
