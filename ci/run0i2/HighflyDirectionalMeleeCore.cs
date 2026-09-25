using System;
using UnityEngine;

namespace Highfly.Run0I2
{
    public enum HighflyLoadoutProfile
    {
        Unarmed,
        Sword1H,
        DualSword,
        SwordShield,
        Axe1H,
        DualAxe,
        AxeShield,
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
            P("UA_HOOK",HighflyStrikeDirection.HorizontalLeftToRight,HighflyHandUsage.Right,"Melee_Hook",1.00f,.20f,.48f,.18f,240f,9f,.025f),
            P("UA_REVERSE",HighflyStrikeDirection.HorizontalRightToLeft,HighflyHandUsage.Left,"Melee_Hook",1.05f,.20f,.48f,.18f,240f,9f,.025f),
            P("UA_FINISH",HighflyStrikeDirection.VerticalDown,HighflyHandUsage.Right,"Melee_Hook",.92f,.24f,.58f,.24f,190f,12f,.035f,.86f));

        private static readonly HighflyLoadoutDefinition Sword1H = new HighflyLoadoutDefinition(
            HighflyLoadoutProfile.Sword1H,"ESPADA 1H",HighflyWeaponKind.Sword,null,HighflyGuardStyle.WeaponGuard,false,
            P("S1_VDOWN",HighflyStrikeDirection.VerticalDown,HighflyHandUsage.Right,"Warrior_A",2.00f,.22f,.46f,.22f,210f,22f,.045f),
            P("S1_HLR",HighflyStrikeDirection.HorizontalLeftToRight,HighflyHandUsage.Right,"Warrior_B",1.50f,.20f,.50f,.25f,190f,24f,.050f),
            P("S1_DUP",HighflyStrikeDirection.DiagonalUpRight,HighflyHandUsage.Right,"Warrior_C",1.50f,.28f,.66f,.34f,160f,30f,.070f,.86f));

        private static readonly HighflyLoadoutDefinition DualSword = new HighflyLoadoutDefinition(
            HighflyLoadoutProfile.DualSword,"DOBLE ESPADA",HighflyWeaponKind.Sword,HighflyWeaponKind.Sword,HighflyGuardStyle.CrossGuard,true,
            P("DS_RDOWN",HighflyStrikeDirection.DiagonalDownRight,HighflyHandUsage.Right,"DualSword_A",1.16f,.18f,.44f,.28f,250f,19f,.038f),
            P("DS_LHOR",HighflyStrikeDirection.HorizontalRightToLeft,HighflyHandUsage.Left,"DualSword_B",1.20f,.18f,.46f,.30f,260f,20f,.040f),
            P("DS_CROSS",HighflyStrikeDirection.Cross,HighflyHandUsage.Both,"DualSword_C",1.12f,.24f,.63f,.38f,220f,28f,.060f,.84f));

        private static readonly HighflyLoadoutDefinition SwordShield = new HighflyLoadoutDefinition(
            HighflyLoadoutProfile.SwordShield,"ESPADA + ESCUDO",HighflyWeaponKind.Sword,HighflyWeaponKind.Shield,HighflyGuardStyle.ShieldGuard,false,
            P("SS_VDOWN",HighflyStrikeDirection.VerticalDown,HighflyHandUsage.Right,"Warrior_A",1.85f,.22f,.46f,.20f,175f,23f,.050f),
            P("SS_HLR",HighflyStrikeDirection.HorizontalLeftToRight,HighflyHandUsage.Right,"Warrior_B",1.42f,.20f,.50f,.22f,160f,25f,.055f),
            P("SS_DUP",HighflyStrikeDirection.DiagonalUpRight,HighflyHandUsage.Right,"Warrior_C",1.42f,.28f,.66f,.28f,145f,31f,.075f,.86f));

        private static readonly HighflyLoadoutDefinition Axe1H = new HighflyLoadoutDefinition(
            HighflyLoadoutProfile.Axe1H,"HACHA 1H",HighflyWeaponKind.Axe,null,HighflyGuardStyle.WeaponGuard,false,
            P("AXE_DDR",HighflyStrikeDirection.DiagonalDownRight,HighflyHandUsage.Right,"Axe_A",.96f,.24f,.52f,.20f,145f,28f,.065f),
            P("AXE_HRL",HighflyStrikeDirection.HorizontalRightToLeft,HighflyHandUsage.Right,"Axe_B",.94f,.24f,.55f,.22f,135f,30f,.070f),
            P("AXE_VDOWN",HighflyStrikeDirection.VerticalDown,HighflyHandUsage.Right,"Axe_C",.90f,.28f,.66f,.28f,115f,38f,.090f,.88f));

        private static readonly HighflyLoadoutDefinition DualAxe = new HighflyLoadoutDefinition(
            HighflyLoadoutProfile.DualAxe,"DOBLE HACHA",HighflyWeaponKind.Axe,HighflyWeaponKind.Axe,HighflyGuardStyle.CrossGuard,true,
            P("DAXE_R",HighflyStrikeDirection.DiagonalDownRight,HighflyHandUsage.Right,"DualAxe_A",1.02f,.20f,.46f,.24f,155f,24f,.055f),
            P("DAXE_L",HighflyStrikeDirection.DiagonalDownLeft,HighflyHandUsage.Left,"DualAxe_B",1.02f,.20f,.48f,.26f,155f,25f,.058f),
            P("DAXE_X",HighflyStrikeDirection.Cross,HighflyHandUsage.Both,"DualAxe_C",.96f,.26f,.64f,.32f,135f,36f,.085f,.86f));

        private static readonly HighflyLoadoutDefinition AxeShield = new HighflyLoadoutDefinition(
            HighflyLoadoutProfile.AxeShield,"HACHA + ESCUDO",HighflyWeaponKind.Axe,HighflyWeaponKind.Shield,HighflyGuardStyle.ShieldGuard,false,
            P("AS_DDR",HighflyStrikeDirection.DiagonalDownRight,HighflyHandUsage.Right,"Axe_A",.94f,.24f,.52f,.18f,130f,29f,.068f),
            P("AS_HRL",HighflyStrikeDirection.HorizontalRightToLeft,HighflyHandUsage.Right,"Axe_B",.92f,.24f,.55f,.20f,125f,31f,.072f),
            P("AS_VDOWN",HighflyStrikeDirection.VerticalDown,HighflyHandUsage.Right,"Axe_C",.88f,.28f,.66f,.24f,110f,39f,.092f,.88f));

        private static readonly HighflyLoadoutDefinition DualDaggers = new HighflyLoadoutDefinition(
            HighflyLoadoutProfile.DualDaggers,"DOBLE DAGA",HighflyWeaponKind.Dagger,HighflyWeaponKind.Dagger,HighflyGuardStyle.CrossGuard,true,
            P("DG_RUP",HighflyStrikeDirection.DiagonalUpRight,HighflyHandUsage.Right,"Dagger_A",1.24f,.16f,.40f,.30f,300f,17f,.032f),
            P("DG_LDOWN",HighflyStrikeDirection.DiagonalDownLeft,HighflyHandUsage.Left,"Dagger_B",1.28f,.15f,.40f,.32f,310f,18f,.034f),
            P("DG_X",HighflyStrikeDirection.Cross,HighflyHandUsage.Both,"Dagger_C",1.20f,.21f,.57f,.40f,275f,24f,.050f,.80f));

        private static readonly HighflyLoadoutDefinition Spear2H = new HighflyLoadoutDefinition(
            HighflyLoadoutProfile.Spear2H,"LANZA 2H",HighflyWeaponKind.Spear,null,HighflyGuardStyle.PoleGuard,false,
            P("SP_THRUST",HighflyStrikeDirection.Thrust,HighflyHandUsage.Both,"Spear_A",1.02f,.22f,.50f,.48f,135f,27f,.055f),
            P("SP_SWEEP",HighflyStrikeDirection.HorizontalLeftToRight,HighflyHandUsage.Both,"Spear_B",.98f,.24f,.58f,.32f,120f,25f,.052f),
            P("SP_HEAVY",HighflyStrikeDirection.Thrust,HighflyHandUsage.Both,"Spear_C",.92f,.28f,.66f,.62f,100f,38f,.085f,.88f));

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
                case HighflyLoadoutProfile.DualDaggers: return DualDaggers;
                case HighflyLoadoutProfile.Spear2H: return Spear2H;
                default: return Unarmed;
            }
        }
    }
}
