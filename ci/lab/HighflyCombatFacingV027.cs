using UnityEngine;

namespace Highfly.SkillLab
{
    [DisallowMultipleComponent]
    public sealed class HighflyCombatFacingV027 : MonoBehaviour
    {
        public static HighflyCombatFacingV027 Instance { get; private set; }

        private PlayerController _player;
        private Transform _visualPivot;

        public Vector3 CombatForward
        {
            get
            {
                if (_visualPivot != null)
                {
                    Vector3 f = _visualPivot.forward;
                    f.y = 0f;
                    if (f.sqrMagnitude > 0.0001f)
                        return f.normalized;
                }

                if (_player != null)
                {
                    Vector3 f = _player.transform.forward;
                    f.y = 0f;
                    if (f.sqrMagnitude > 0.0001f)
                        return f.normalized;
                }

                return Vector3.forward;
            }
        }

        public static HighflyCombatFacingV027 Install(PlayerController player)
        {
            if (player == null) return null;

            var c = player.GetComponent<HighflyCombatFacingV027>();
            if (c == null)
                c = player.gameObject.AddComponent<HighflyCombatFacingV027>();

            c._player = player;
            Instance = c;
            return c;
        }

        private void Awake()
        {
            Instance = this;
            if (_player == null)
                _player = GetComponent<PlayerController>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void BindVisual(Transform visualPivot)
        {
            _visualPivot = visualPivot;
            ResetVisualFacing("bind");
            HighflyLabTestHistoryV026.Log("COMBAT FACING • visual pivot bound");
        }

        public void Face(Vector3 worldDirection)
        {
            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude < 0.0001f)
                return;

            worldDirection.Normalize();

            if (_visualPivot != null)
            {
                _visualPivot.rotation = Quaternion.LookRotation(worldDirection, Vector3.up);
                return;
            }

            // v2.7 rule: skills are never allowed to rotate the locomotion root.
            HighflyLabTestHistoryV026.Flag("COMBAT FACING requested without visual pivot");
        }

        public void ResetVisualFacing(string reason)
        {
            if (_visualPivot == null) return;
            _visualPivot.localRotation = Quaternion.identity;
            HighflyLabTestHistoryV026.Log("COMBAT FACING RESET • " + reason);
        }

        private void LateUpdate()
        {
            if (_player == null || _visualPivot == null)
                return;

            bool skillOwnsMotion =
                HighflyLabMotionGuardV026.Instance != null &&
                HighflyLabMotionGuardV026.Instance.SkillOwnsMotion;

            if (skillOwnsMotion)
                return;

            // Outside an owned skill the visual body must realign with LUCID's root.
            _visualPivot.localRotation = Quaternion.Slerp(
                _visualPivot.localRotation,
                Quaternion.identity,
                1f - Mathf.Exp(-24f * Time.unscaledDeltaTime));
        }

        public static void FaceVisual(Vector3 worldDirection)
        {
            if (Instance != null)
                Instance.Face(worldDirection);
        }

        public static Vector3 Forward(Transform fallbackRoot)
        {
            if (Instance != null)
                return Instance.CombatForward;

            if (fallbackRoot == null)
                return Vector3.forward;

            Vector3 f = fallbackRoot.forward;
            f.y = 0f;
            return f.sqrMagnitude > 0.0001f ? f.normalized : Vector3.forward;
        }
    }
}
