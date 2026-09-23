using UnityEngine;

namespace Highfly.SkillLab
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController))]
    public sealed class HighflyLabActionGuardV026 : MonoBehaviour
    {
        public static HighflyLabActionGuardV026 Instance { get; private set; }

        private PlayerController _player;
        private CharacterController _cc;
        private Vector3 _baselineScale;
        private int _token;
        private bool _locked;
        private string _owner = "-";
        private float _deadline;
        private Vector3 _lastPosition;
        private float _nextSampleAt;

        public bool IsLocked => _locked;
        public string Owner => _owner;

        public static HighflyLabActionGuardV026 Install(PlayerController player)
        {
            if (player == null) return null;
            var existing = player.GetComponent<HighflyLabActionGuardV026>();
            if (existing != null) return existing;

            var guard = player.gameObject.AddComponent<HighflyLabActionGuardV026>();
            guard.Initialize(player);
            return guard;
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Initialize(PlayerController player)
        {
            _player = player;
            _cc = player.GetComponent<CharacterController>();
            _baselineScale = player.transform.localScale;
            _lastPosition = player.transform.position;
            _nextSampleAt = Time.unscaledTime + 0.1f;
            HighflyLabTelemetryV026.Record("GUARD", "instalado; baselineScale=" + Vec(_baselineScale));
        }

        public int Begin(string owner, float timeoutSeconds = 1.25f, bool interruptExisting = true)
        {
            if (_player == null) return 0;

            if (_locked)
            {
                if (!interruptExisting)
                {
                    HighflyLabTelemetryV026.Record("BLOCK", owner + " rechazado; dueño=" + _owner);
                    return 0;
                }

                ForceRecover("preempt " + _owner + " -> " + owner);
            }

            HighflyAerialMobility.Instance?.ForceResetMotion("action:" + owner);
            HighflyParkourAnimationV010.Instance?.StopNow();
            _player.HighflyLabForceLocomotion();

            _token++;
            _locked = true;
            _owner = string.IsNullOrEmpty(owner) ? "UNKNOWN" : owner;
            _deadline = Time.unscaledTime + Mathf.Max(0.15f, timeoutSeconds);
            _lastPosition = _player.transform.position;

            if (_player.animator != null)
                _player.animator.applyRootMotion = false;

            RestoreRootScaleIfNeeded("begin");
            HighflyLabTelemetryV026.Record("BEGIN", _owner + " token=" + _token);
            return _token;
        }

        public void End(int token, string result = "ok")
        {
            if (!_locked || token == 0 || token != _token)
                return;

            string finished = _owner;
            RecoverCore(false, "end:" + result);
            HighflyLabTelemetryV026.Record("END", finished + " • " + result);
        }

        public bool BlocksManualInput(string input)
        {
            if (!_locked) return false;
            HighflyLabTelemetryV026.Record("INPUT BLOCK", input + " durante " + _owner);
            return true;
        }

        public void ForceRecover(string reason)
        {
            string previous = _owner;
            RecoverCore(true, reason);
            HighflyLabTelemetryV026.Record("RECOVER", previous + " • " + reason);
        }

        private void Update()
        {
            if (_player == null) return;

            if (_locked && Time.unscaledTime > _deadline)
            {
                ForceRecover("watchdog timeout");
                return;
            }

            if (Time.unscaledTime < _nextSampleAt) return;
            _nextSampleAt = Time.unscaledTime + 0.10f;

            Vector3 p = _player.transform.position;
            if (!Finite(p) || !Finite(_player.transform.localScale))
            {
                ForceRecover("non-finite transform");
                return;
            }

            RestoreRootScaleIfNeeded("watchdog");

            if (_cc != null && !_cc.enabled)
            {
                _cc.enabled = true;
                HighflyLabTelemetryV026.Record("FIX", "CharacterController reactivado");
            }

            if (_locked)
            {
                float distance = Vector3.Distance(p, _lastPosition);
                if (distance > 7.0f)
                {
                    HighflyLabTelemetryV026.Record("WARN", "salto de posición " + distance.ToString("0.00") + "m en " + _owner);
                }
            }

            _lastPosition = p;
        }

        private void RecoverCore(bool hard, string reason)
        {
            if (_player == null) return;

            if (hard)
            {
                AbortTransientLabCoroutines();
            }

            _locked = false;
            _owner = "-";
            _deadline = 0f;

            HighflyParkourAnimationV010.Instance?.StopNow();
            HighflyAerialMobility.Instance?.ForceResetMotion("recover:" + reason);

            if (_cc != null && !_cc.enabled)
                _cc.enabled = true;

            _player.HighflyLabForceLocomotion();

            if (_player.animator != null)
                _player.animator.applyRootMotion = false;

            RestoreRootScaleIfNeeded(hard ? "hard-recover" : "release");
            _lastPosition = _player.transform.position;
        }

        private void AbortTransientLabCoroutines()
        {
            _player.GetComponent<HighflyPremiumSkillRuntime>()?.StopAllCoroutines();
            _player.GetComponent<HighflyAdvancedSkillRuntime>()?.StopAllCoroutines();
            _player.GetComponent<HighflyReferenceSkillRuntime>()?.StopAllCoroutines();
            _player.GetComponent<HighflyApexPassSkillRuntime>()?.StopAllCoroutines();
            _player.GetComponent<HighflyDonorExpandedRuntimeV022>()?.StopAllCoroutines();
            _player.GetComponent<HighflyDonorWeaponRuntimeV021>()?.StopAllCoroutines();

            HighflyLabTelemetryV026.Record("ABORT", "coroutines transitorias detenidas");
        }

        private void RestoreRootScaleIfNeeded(string source)
        {
            if (_player == null) return;

            Vector3 current = _player.transform.localScale;
            if (Vector3.Distance(current, _baselineScale) < 0.001f)
                return;

            HighflyLabTelemetryV026.Record(
                "FIX",
                "player scale drift " + Vec(current) + " -> " + Vec(_baselineScale) + " @" + source);

            _player.transform.localScale = _baselineScale;
        }

        private static bool Finite(Vector3 v)
        {
            return !(float.IsNaN(v.x) || float.IsInfinity(v.x) ||
                     float.IsNaN(v.y) || float.IsInfinity(v.y) ||
                     float.IsNaN(v.z) || float.IsInfinity(v.z));
        }

        private static string Vec(Vector3 v)
        {
            return "(" + v.x.ToString("0.00") + "," + v.y.ToString("0.00") + "," + v.z.ToString("0.00") + ")";
        }
    }
}
