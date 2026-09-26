using UnityEngine;

namespace Highfly.SkillLab
{
    // MONSTER RUN 0 is a dedicated lab artifact.
    // Reuse the exact Golden mobile HUD/camera path from Skill Lab Run0H.
    public static class HighflySkillLabMode
    {
        public static bool IsActive => true;
    }

    public sealed class HighflyAerialMobility : MonoBehaviour
    {
        public static HighflyAerialMobility Instance => null;
        public string DebugState => "BASE";
        public void RequestJump() { }
    }

    public sealed class HighflyPremiumSkillRuntime : MonoBehaviour
    {
        public static HighflyPremiumSkillRuntime Instance => null;
        public void EchoBasicAttack() { }
        public void Trigger(object ignored) { }
    }

    public static class HighflySkillLoadout
    {
        public static object Get(int index) => null;
    }

    public static class HighflyCombatLabV010
    {
        public static bool TryInterceptLethal(PlayerStats player, float damage, Transform attacker) => false;
    }

    public static class HighflyReflectiveWallState
    {
        public static bool TryReflect(PlayerStats player, float damage, float composureDamage, Transform attacker) => false;
    }

    public static class HighflyDonorDefenseState
    {
        public static bool TryModifyIncoming(PlayerStats player, ref float damage, ref float composureDamage) => false;
    }

    public sealed class HighflySkillCooldownVisual : MonoBehaviour
    {
        public void Configure(int skillSlot, Sprite discSprite) { }
    }

    public static class HighflyCombatFacingV027
    {
        public static void FaceVisual(Vector3 worldDirection) { }

        public static Vector3 Forward(Transform fallbackRoot)
        {
            if (fallbackRoot == null) return Vector3.forward;
            Vector3 f = fallbackRoot.forward;
            f.y = 0f;
            return f.sqrMagnitude > 0.0001f ? f.normalized : Vector3.forward;
        }
    }
}