using UnityEngine;

namespace Highfly.SkillLab
{
    [DisallowMultipleComponent]
    public sealed class HighflyLabMotionGuardV026 : MonoBehaviour
    {
        public static HighflyLabMotionGuardV026 Instance { get; private set; }

        private PlayerController _player;
        private Vector3 _baselineRootScale;
        private float _skillLockUntil;
        private string _skillOwner = "-";
        private Vector3 _lastSafePosition;
        private float _nextSanityCheck;

        public bool SkillOwnsMotion => Time.unscaledTime < _skillLockUntil;
        public string Owner => SkillOwnsMotion ? _skillOwner : "LOCOMOTION";
        public float Remaining => Mathf.Max(0f, _skillLockUntil - Time.unscaledTime);

        public static HighflyLabMotionGuardV026 Install(PlayerController player)
        {
            if (player == null) return null;
            var g = player.GetComponent<HighflyLabMotionGuardV026>();
            if (g == null) g = player.gameObject.AddComponent<HighflyLabMotionGuardV026>();
            g.Bind(player);
            return g;
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Bind(PlayerController player)
        {
            _player = player;
            _baselineRootScale = player.transform.localScale;
            _lastSafePosition = player.transform.position;
        }

        public bool TryClaimSkill(string owner, float seconds)
        {
            if (_player == null) return false;
            if (_player.currentState == PlayerState.Die ||
                _player.currentState == PlayerState.Interact ||
                _player.currentState == PlayerState.UseItem)
                return false;

            HighflyParkourAnimationV010.Instance?.StopNow();
            _skillOwner = string.IsNullOrEmpty(owner) ? "SKILL" : owner;
            _skillLockUntil = Mathf.Max(_skillLockUntil, Time.unscaledTime + Mathf.Max(0.08f, seconds));
            HighflyLabTestHistoryV026.Log("CLAIM " + _skillOwner + " • " + seconds.ToString("0.00") + "s");
            return true;
        }

        public void ForceRelease(string reason)
        {
            _skillLockUntil = 0f;
            _skillOwner = "-";
            HighflyCombatFacingV027.Instance?.ResetVisualFacing(reason);
            if (_player != null)
                _player.HighflyLabForceLocomotion();
            HighflyLabTestHistoryV026.Log("RELEASE • " + reason);
        }

        private void Update()
        {
            if (_player == null) return;

            if (Time.unscaledTime >= _skillLockUntil && _skillOwner != "-")
            {
                _skillOwner = "-";
                HighflyCombatFacingV027.Instance?.ResetVisualFacing("skill lease expired");
            }

            if (Time.unscaledTime < _nextSanityCheck) return;
            _nextSanityCheck = Time.unscaledTime + 0.10f;

            Transform root = _player.transform;
            Vector3 p = root.position;
            if (!Finite(p))
            {
                root.position = _lastSafePosition;
                ForceRelease("NON-FINITE POSITION");
                HighflyLabTestHistoryV026.Flag("ROOT POSITION INVALID • RECOVERED");
            }
            else
            {
                _lastSafePosition = p;
            }

            Vector3 s = root.localScale;
            if ((s - _baselineRootScale).sqrMagnitude > 0.0004f)
            {
                root.localScale = _baselineRootScale;
                HighflyLabTestHistoryV026.Flag(
                    "ROOT SCALE DRIFT " + s.ToString("F2") + " -> " + _baselineRootScale.ToString("F2"));
            }
        }

        private static bool Finite(Vector3 v)
        {
            return !(float.IsNaN(v.x) || float.IsInfinity(v.x) ||
                     float.IsNaN(v.y) || float.IsInfinity(v.y) ||
                     float.IsNaN(v.z) || float.IsInfinity(v.z));
        }
    }
}
