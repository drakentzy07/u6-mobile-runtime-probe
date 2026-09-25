using UnityEngine;

namespace Highfly.SkillLab
{
    public static class HighflySkillLabMode { public static bool IsActive => true; }

    public sealed class HighflyAerialMobility : MonoBehaviour
    {
        public static HighflyAerialMobility Instance => null;
        public string DebugState => "RUN0I PARKOUR";
        public void RequestJump() { }
    }

    public sealed class HighflyPremiumSkillRuntime : MonoBehaviour
    {
        public static HighflyPremiumSkillRuntime Instance => null;
        public void EchoBasicAttack() { }
        public void Trigger(object ignored) { }
    }

    public static class HighflySkillLoadout { public static object Get(int index) => null; }

    public static class HighflyCombatLabV010
    {
        public static bool TryInterceptLethal(PlayerStats player,float damage,Transform attacker) => false;
    }

    public static class HighflyReflectiveWallState
    {
        public static bool TryReflect(PlayerStats player,float damage,float composureDamage,Transform attacker)
        {
            Highfly.Combat.HighflyLucidCombatBridge bridge=Highfly.Combat.HighflyLucidCombatBridge.Instance;
            if (bridge == null) return false;

            // Slide Move: HIGHFLY defensive i-frame is authoritative here.
            // Damage is intercepted only inside the explicit action window.
            if (bridge.IsDefensiveIFrame) return true;

            return bridge.TryParryIncoming(damage,composureDamage,attacker);
        }
    }

    public static class HighflyDonorDefenseState
    {
        public static bool TryModifyIncoming(PlayerStats player,ref float damage,ref float composureDamage) => false;
    }

    public sealed class HighflySkillCooldownVisual : MonoBehaviour
    {
        public void Configure(int skillSlot,Sprite discSprite) { }
    }

    public static class HighflyCombatFacingV027
    {
        public static void FaceVisual(Vector3 worldDirection)
        {
            Highfly.Run0H.HighflyRun0HCharacterVisual.Instance?.SetActionFacing(worldDirection);
        }

        public static Vector3 Forward(Transform fallbackRoot)
        {
            if (fallbackRoot==null) return Vector3.forward;
            Vector3 f=fallbackRoot.forward; f.y=0f;
            return f.sqrMagnitude>0.0001f?f.normalized:Vector3.forward;
        }
    }
}
