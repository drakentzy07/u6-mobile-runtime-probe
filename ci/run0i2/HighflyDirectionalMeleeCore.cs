using System;
using UnityEngine;

namespace Highfly.Run0I2
{
    // RUN0I.3 OFFICIAL CANDIDATE: unified 1H / dual / thrust grammar.
    public enum HighflyLoadoutProfile
    {
        Unarmed,
        Sword1H,
        DualSword,
        SwordShield,
        Axe1H,
        DualAxe,
        AxeShield,
        Dagger1H,
        DualDaggers,
        Spear2H
    }

    public enum HighflyWeaponKind { None, Sword, Axe, Dagger, Spear, Shield }
    public enum HighflyGuardStyle { WeaponGuard, CrossGuard, ShieldGuard, PoleGuard }
    public enum HighflyStrikeDirection
    {
        VerticalDown,
        VerticalUp,
        HorizontalLeftToRight,
        HorizontalRightToLeft,
        DiagonalUpRight,
        DiagonalUpLeft,
        DiagonalDownRight,
        DiagonalDownLeft,
        Thrust,
        Cross,
        Spin
    }
    public enum HighflyHandUsage { Right, Left, Both, Alternating }

    [Serializable]
    public sealed class HighflyEquipmentState
    {
        public HighflyWeaponKind MainHand = HighflyWeaponKind.None;
        public HighflyWeaponKind OffHand = HighflyWeaponKind.None;
        public HighflyWeaponKind TwoHand = HighflyWeaponKind.None;

        public void Clear()
        {
            MainHand=HighflyWeaponKind.None;
            OffHand=HighflyWeaponKind.None;
            TwoHand=HighflyWeaponKind.None;
        }

        public void SetPreset(HighflyLoadoutProfile profile)
        {
            Clear();
            switch (profile)
            {
                case HighflyLoadoutProfile.Sword1H: MainHand=HighflyWeaponKind.Sword; break;
                case HighflyLoadoutProfile.DualSword: MainHand=HighflyWeaponKind.Sword; OffHand=HighflyWeaponKind.Sword; break;
                case HighflyLoadoutProfile.SwordShield: MainHand=HighflyWeaponKind.Sword; OffHand=HighflyWeaponKind.Shield; break;
                case HighflyLoadoutProfile.Axe1H: MainHand=HighflyWeaponKind.Axe; break;
                case HighflyLoadoutProfile.DualAxe: MainHand=HighflyWeaponKind.Axe; OffHand=HighflyWeaponKind.Axe; break;
                case HighflyLoadoutProfile.AxeShield: MainHand=HighflyWeaponKind.Axe; OffHand=HighflyWeaponKind.Shield; break;
                case HighflyLoadoutProfile.Dagger1H: MainHand=HighflyWeaponKind.Dagger; break;
                case HighflyLoadoutProfile.DualDaggers: MainHand=HighflyWeaponKind.Dagger; OffHand=HighflyWeaponKind.Dagger; break;
                case HighflyLoadoutProfile.Spear2H: TwoHand=HighflyWeaponKind.Spear; break;
            }
        }
    }

    public static class HighflyLoadoutResolver
    {
        public static HighflyLoadoutProfile Resolve(HighflyEquipmentState state)
        {
            if (state==null) return HighflyLoadoutProfile.Unarmed;
            if (state.TwoHand==HighflyWeaponKind.Spear) return HighflyLoadoutProfile.Spear2H;
            if (state.MainHand==HighflyWeaponKind.Sword && state.OffHand==HighflyWeaponKind.Sword) return HighflyLoadoutProfile.DualSword;
            if (state.MainHand==HighflyWeaponKind.Sword && state.OffHand==HighflyWeaponKind.Shield) return HighflyLoadoutProfile.SwordShield;
            if (state.MainHand==HighflyWeaponKind.Axe && state.OffHand==HighflyWeaponKind.Axe) return HighflyLoadoutProfile.DualAxe;
            if (state.MainHand==HighflyWeaponKind.Axe && state.OffHand==HighflyWeaponKind.Shield) return HighflyLoadoutProfile.AxeShield;
            if (state.MainHand==HighflyWeaponKind.Dagger && state.OffHand==HighflyWeaponKind.Dagger) return HighflyLoadoutProfile.DualDaggers;
            if (state.MainHand==HighflyWeaponKind.Dagger) return HighflyLoadoutProfile.Dagger1H;
            if (state.MainHand==HighflyWeaponKind.Sword) return HighflyLoadoutProfile.Sword1H;
            if (state.MainHand==HighflyWeaponKind.Axe) return HighflyLoadoutProfile.Axe1H;
            return HighflyLoadoutProfile.Unarmed;
        }
    }

    [Serializable]
    public sealed class HighflyStrikePattern
    {
        public readonly string Id;
        public readonly HighflyStrikeDirection Direction;
        public readonly HighflyHandUsage Hands;
        public readonly string MotionSlot;
        public readonly float AnimationSpeed;
        public readonly float ActiveStartN;
        public readonly float ActiveEndN;
        public readonly float MovementMeters;
        public readonly float SteerDegreesPerSecond;
        public readonly float Damage;
        public readonly float Hitstop;
        public readonly float LinkAtN;
        public readonly int HitsPerConfirmedContact;

        public HighflyStrikePattern(
            string id,
            HighflyStrikeDirection direction,
            HighflyHandUsage hands,
            string motionSlot,
            float animationSpeed,
            float activeStartN,
            float activeEndN,
            float movementMeters,
            float steerDegreesPerSecond,
            float damage,
            float hitstop,
            float linkAtN=0.82f,
            int hitsPerConfirmedContact=1)
        {
            Id=id; Direction=direction; Hands=hands; MotionSlot=motionSlot;
            AnimationSpeed=animationSpeed; ActiveStartN=activeStartN; ActiveEndN=activeEndN;
            MovementMeters=movementMeters; SteerDegreesPerSecond=steerDegreesPerSecond;
            Damage=damage; Hitstop=hitstop; LinkAtN=linkAtN;
            HitsPerConfirmedContact=Mathf.Max(1,hitsPerConfirmedContact);
        }

        public string Arrow
        {
            get
            {
                switch (Direction)
                {
                    case HighflyStrikeDirection.VerticalDown: return "↓";
                    case HighflyStrikeDirection.VerticalUp: return "↑";
                    case HighflyStrikeDirection.HorizontalLeftToRight: return "→";
                    case HighflyStrikeDirection.HorizontalRightToLeft: return "←";
                    case HighflyStrikeDirection.DiagonalUpRight: return "/";
                    case HighflyStrikeDirection.DiagonalUpLeft: return "↖";
                    case HighflyStrikeDirection.DiagonalDownRight: return "↘";
                    case HighflyStrikeDirection.DiagonalDownLeft: return "\\";
                    case HighflyStrikeDirection.Thrust: return "T";
                    case HighflyStrikeDirection.Cross: return "X";
                    case HighflyStrikeDirection.Spin: return "S";
                    default: return "?";
                }
            }
        }
    }

    [Serializable]
    public sealed class HighflyLoadoutDefinition
    {
        public readonly HighflyLoadoutProfile Profile;
        public readonly string Label;
        public readonly HighflyWeaponKind Primary;
        public readonly HighflyWeaponKind? Secondary;
        public readonly HighflyGuardStyle Guard;
        public readonly bool UsesSecondaryTrace;
        public readonly HighflyStrikePattern[] Basic;

        public HighflyLoadoutDefinition(
            HighflyLoadoutProfile profile,
            string label,
            HighflyWeaponKind primary,
            HighflyWeaponKind? secondary,
            HighflyGuardStyle guard,
            bool usesSecondaryTrace,
            params HighflyStrikePattern[] basic)
        {
            Profile=profile; Label=label; Primary=primary; Secondary=secondary;
            Guard=guard; UsesSecondaryTrace=usesSecondaryTrace; Basic=basic ?? Array.Empty<HighflyStrikePattern>();
        }

        public HighflyStrikePattern GetBasic(int step)
        {
            if (Basic.Length==0) return null;
            int index=Mathf.Clamp(step-1,0,Basic.Length-1);
            return Basic[index];
        }

        public string Grammar
        {
            get
            {
                if (Basic.Length==0) return "-";
                string result="";
                for (int i=0;i<Basic.Length;i++)
                {
                    if (i>0) result+=" → ";
                    result+=Basic[i].Arrow;
                }
                return result;
            }
        }
    }

    public static class HighflyMeleeLibrary
    {
        private static HighflyStrikePattern P(
            string id, HighflyStrikeDirection dir, HighflyHandUsage hands, string clip,
            float speed, float activeA, float activeB, float move, float steer,
            float damage, float hitstop, float link=0.82f)
            => new HighflyStrikePattern(id,dir,hands,clip,speed,activeA,activeB,move,steer,damage,hitstop,link);

        private static readonly HighflyLoadoutDefinition Unarmed = new HighflyLoadoutDefinition(
            HighflyLoadoutProfile.Unarmed,"SIN ARMAS",HighflyWeaponKind.None,null,HighflyGuardStyle.WeaponGuard,false,
            P("UA_PUNCH_R",HighflyStrikeDirection.Thrust,HighflyHandUsage.Right,"Unarmed_Punch_A",1.10f,.18f,.46f,.16f,250f,9f,.024f),
            P("UA_PUNCH_L",HighflyStrikeDirection.Thrust,HighflyHandUsage.Left,"Unarmed_Punch_B",1.06f,.18f,.48f,.18f,245f,10f,.028f),
            P("UA_KICK_FINISH",HighflyStrikeDirection.HorizontalLeftToRight,HighflyHandUsage.Right,"Unarmed_Kick",1.00f,.22f,.58f,.24f,220f,14f,.038f,.86f));

        private static readonly HighflyLoadoutDefinition Sword1H = new HighflyLoadoutDefinition(
            HighflyLoadoutProfile.Sword1H,"ESPADA 1H",HighflyWeaponKind.Sword,null,HighflyGuardStyle.WeaponGuard,false,
            P("S1_VDOWN",HighflyStrikeDirection.VerticalDown,HighflyHandUsage.Right,"Warrior_A",2.00f,.22f,.46f,.22f,210f,22f,.045f),
            P("S1_HLR",HighflyStrikeDirection.HorizontalLeftToRight,HighflyHandUsage.Right,"Warrior_B",1.50f,.20f,.50f,.25f,190f,24f,.050f),
            P("S1_SPIN",HighflyStrikeDirection.Spin,HighflyHandUsage.Right,"Warrior_C",1.50f,.28f,.66f,.34f,160f,30f,.070f,.86f));

        private static readonly HighflyLoadoutDefinition DualSword = new HighflyLoadoutDefinition(
            HighflyLoadoutProfile.DualSword,"DOBLE ESPADA",HighflyWeaponKind.Sword,HighflyWeaponKind.Sword,HighflyGuardStyle.CrossGuard,true,
            // Right -> left -> crossed X finish using the audited dual-wield bank.
            P("DS_RIGHT",HighflyStrikeDirection.DiagonalDownLeft,HighflyHandUsage.Right,"DualSword_A",1.10f,.18f,.45f,.25f,230f,19f,.038f),
            P("DS_LEFT",HighflyStrikeDirection.DiagonalDownRight,HighflyHandUsage.Left,"DualSword_B",1.08f,.18f,.50f,.28f,220f,21f,.043f),
            P("DS_X",HighflyStrikeDirection.Cross,HighflyHandUsage.Both,"DualSword_C",1.02f,.24f,.66f,.36f,185f,29f,.062f,.84f));

        private static readonly HighflyLoadoutDefinition SwordShield = new HighflyLoadoutDefinition(
            HighflyLoadoutProfile.SwordShield,"ESPADA + ESCUDO",HighflyWeaponKind.Sword,HighflyWeaponKind.Shield,HighflyGuardStyle.ShieldGuard,false,
            P("SS_ATK_R",HighflyStrikeDirection.VerticalDown,HighflyHandUsage.Right,"Warrior_A",1.85f,.22f,.46f,.20f,175f,23f,.050f),
            P("SS_ATK_L",HighflyStrikeDirection.HorizontalLeftToRight,HighflyHandUsage.Right,"Warrior_B",1.42f,.20f,.50f,.22f,160f,25f,.055f),
            P("SS_SHIELD_BASH",HighflyStrikeDirection.Thrust,HighflyHandUsage.Left,"Shield_Bash",1.14f,.18f,.60f,.52f,145f,34f,.080f,.84f));

        private static readonly HighflyLoadoutDefinition Axe1H = new HighflyLoadoutDefinition(
            HighflyLoadoutProfile.Axe1H,"HACHA 1H",HighflyWeaponKind.Axe,null,HighflyGuardStyle.WeaponGuard,false,
            // Real KayKit 1H axe opening beats, then the proven spinning finisher.
            P("AXE_1H_A",HighflyStrikeDirection.VerticalDown,HighflyHandUsage.Right,"Axe_A",1.34f,.20f,.48f,.22f,190f,25f,.050f),
            P("AXE_1H_B",HighflyStrikeDirection.HorizontalLeftToRight,HighflyHandUsage.Right,"Axe_B",1.28f,.18f,.50f,.24f,175f,27f,.055f),
            P("AXE_1H_SPIN",HighflyStrikeDirection.Spin,HighflyHandUsage.Right,"Warrior_C",1.42f,.28f,.66f,.30f,150f,34f,.078f,.86f));

        private static readonly HighflyLoadoutDefinition DualAxe = new HighflyLoadoutDefinition(
            HighflyLoadoutProfile.DualAxe,"DOBLE HACHA",HighflyWeaponKind.Axe,HighflyWeaponKind.Axe,HighflyGuardStyle.CrossGuard,true,
            // SKILL4 crown fix: dual axe keeps the proven classic axe 1->2->3 body language.
            // Both axes stay equipped; the finisher is the visible spinning third beat, not the dual-X grammar.
            P("DAXE_1",HighflyStrikeDirection.VerticalDown,HighflyHandUsage.Right,"Axe_A",1.34f,.20f,.48f,.22f,190f,25f,.050f),
            P("DAXE_2",HighflyStrikeDirection.HorizontalLeftToRight,HighflyHandUsage.Right,"Axe_B",1.28f,.18f,.50f,.24f,175f,27f,.055f),
            P("DAXE_3_SPIN",HighflyStrikeDirection.Spin,HighflyHandUsage.Both,"Warrior_C",1.42f,.28f,.66f,.34f,150f,36f,.085f,.86f));

        private static readonly HighflyLoadoutDefinition AxeShield = new HighflyLoadoutDefinition(
            HighflyLoadoutProfile.AxeShield,"HACHA + ESCUDO",HighflyWeaponKind.Axe,HighflyWeaponKind.Shield,HighflyGuardStyle.ShieldGuard,false,
            P("AS_ATK_R",HighflyStrikeDirection.VerticalDown,HighflyHandUsage.Right,"Axe_A",1.32f,.20f,.48f,.18f,155f,29f,.065f),
            P("AS_ATK_L",HighflyStrikeDirection.HorizontalLeftToRight,HighflyHandUsage.Right,"Axe_B",1.26f,.18f,.50f,.20f,145f,31f,.070f),
            P("AS_SHIELD_BASH",HighflyStrikeDirection.Thrust,HighflyHandUsage.Left,"Shield_Bash",1.12f,.18f,.60f,.50f,125f,42f,.095f,.86f));

        private static readonly HighflyLoadoutDefinition Dagger1H = new HighflyLoadoutDefinition(
            HighflyLoadoutProfile.Dagger1H,"DAGA 1H",HighflyWeaponKind.Dagger,null,HighflyGuardStyle.WeaponGuard,false,
            P("DG1_A",HighflyStrikeDirection.VerticalDown,HighflyHandUsage.Right,"Warrior_A",1.92f,.18f,.43f,.20f,280f,16f,.030f),
            P("DG1_B",HighflyStrikeDirection.HorizontalLeftToRight,HighflyHandUsage.Right,"Warrior_B",1.62f,.17f,.47f,.22f,270f,18f,.034f),
            P("DG1_C",HighflyStrikeDirection.Spin,HighflyHandUsage.Right,"Warrior_C",1.52f,.23f,.62f,.28f,230f,24f,.045f,.80f));

        private static readonly HighflyLoadoutDefinition DualDaggers = new HighflyLoadoutDefinition(
            HighflyLoadoutProfile.DualDaggers,"DOBLE DAGA",HighflyWeaponKind.Dagger,HighflyWeaponKind.Dagger,HighflyGuardStyle.CrossGuard,true,
            P("DG_RIGHT",HighflyStrikeDirection.DiagonalDownLeft,HighflyHandUsage.Right,"Dagger_A",1.18f,.13f,.37f,.22f,335f,17f,.029f,.74f),
            P("DG_LEFT",HighflyStrikeDirection.DiagonalDownRight,HighflyHandUsage.Left,"Dagger_B",1.16f,.14f,.43f,.24f,325f,19f,.033f,.76f),
            P("DG_X",HighflyStrikeDirection.Cross,HighflyHandUsage.Both,"Dagger_C",1.08f,.18f,.56f,.30f,295f,25f,.048f,.80f));

        private static readonly HighflyLoadoutDefinition Spear2H = new HighflyLoadoutDefinition(
            HighflyLoadoutProfile.Spear2H,"LANZA 2H",HighflyWeaponKind.Spear,null,HighflyGuardStyle.PoleGuard,false,
            // SKILL4: use the complete audited KayKit 2H bank: thrust -> sweep -> chop.
            P("SP_THRUST_A",HighflyStrikeDirection.Thrust,HighflyHandUsage.Both,"Spear_A",1.12f,.20f,.48f,.44f,145f,27f,.055f),
            P("SP_SWEEP_B",HighflyStrikeDirection.HorizontalLeftToRight,HighflyHandUsage.Both,"Spear_B",1.18f,.18f,.52f,.48f,150f,30f,.060f),
            P("SP_CHOP_C",HighflyStrikeDirection.VerticalDown,HighflyHandUsage.Both,"Spear_C",1.22f,.18f,.56f,.56f,155f,38f,.080f,.84f));

                public static HighflyLoadoutDefinition Get(HighflyLoadoutProfile profile)
        {
            switch (profile)
            {
                case HighflyLoadoutProfile.Unarmed: return Unarmed;
                case HighflyLoadoutProfile.Sword1H: return Sword1H;
                case HighflyLoadoutProfile.DualSword: return DualSword;
                case HighflyLoadoutProfile.SwordShield: return SwordShield;
                case HighflyLoadoutProfile.Axe1H: return Axe1H;
                case HighflyLoadoutProfile.DualAxe: return DualAxe;
                case HighflyLoadoutProfile.AxeShield: return AxeShield;
                case HighflyLoadoutProfile.Dagger1H: return Dagger1H;
                case HighflyLoadoutProfile.DualDaggers: return DualDaggers;
                case HighflyLoadoutProfile.Spear2H: return Spear2H;
                default: return Unarmed;
            }
        }
    }
}
