using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Highfly.SkillLab
{
    public enum HighflyDonorWeaponSkillV021
    {
        SigilDashAttack,
        DragonSwordThrowRecall
    }

    /// <summary>
    /// DONOR LAB 2.1 weapon transplant runtime.
    /// This class deliberately keeps donor mechanics/timing separate from HIGHFLY core.
    ///
    /// SIGIL DASH ATTACK:
    ///   source: forestlii/sigil-combat @ aeee8dcee73e447f577e64201d651829efe2d5c1
    ///   preserved: 2.5m lunge, trace index-1 semantics, 1.2m trace radius,
    ///              0.35s trace window, 34 damage, 3 poise damage, 1.2s cooldown.
    ///   replacement: sample ships capsule/no authored humanoid choreography, so
    ///                CC0 UAL2 Sword_Dash supplies presentation only.
    ///
    /// DRAGON SOULS SWORD THROW / EMBED / RECALL:
    ///   source: btuhany/DragonSouls-Unity3D @ f54824255517801d5d3443848e1e4275d8d5066d
    ///   preserved: 0.47619s real throw-event timing (0.666667 clip event / 1.4 animator speed),
    ///              20m/s target approach, 200 impulse fallback, 25 in-air damage,
    ///              250m max range, embed-on-hit, recall curve point
    ///              (3.743,2.68,3.267), 40m/s first recall leg, Ease.InCubic,
    ///              0.3 Slerp hand-return factor and <1m catch threshold.
    ///   replacements: Asset-Store/free-pack animation + donor sword mesh/SFX are
    ///                 not redistributed here; CC0 UAL2 OverhandThrow + a neutral
    ///                 lab blade visual replace presentation only.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HighflyDonorWeaponRuntimeV021 : MonoBehaviour
    {
        public static HighflyDonorWeaponRuntimeV021 Instance { get; private set; }

        private const float SigilLungeDistance = 2.5f;
        private const float SigilTraceRadius = 1.2f;
        private const float SigilTraceWindow = 0.35f;
        private const float SigilDamage = 34f;
        private const float SigilPoiseDamage = 3f;
        private const float SigilCooldown = 1.2f;

        private const float DragonThrowEventRealSeconds = 0.6666667f / 1.4f;
        private const float DragonTargetSpeed = 20f;
        private const float DragonImpulseSpeed = 200f;
        private const float DragonDamage = 25f;
        private const float DragonMaxRange = 250f;
        private const float DragonRecallFirstLegSpeed = 40f;
        private const float DragonRecallHandFactor = 0.3f;
        private const float DragonCatchDistance = 1f;
        private static readonly Vector3 DragonCurvePointLocal = new Vector3(3.743f, 2.68f, 3.267f);

        private PlayerController _player;
        private CharacterController _cc;
        private Animator _animator;
        private bool _busy;
        private float _sigilReadyAt;

        private GameObject _dragonBlade;
        private bool _dragonThrown;
        private bool _dragonEmbedded;
        private bool _dragonReturning;
        private CharacterStats _dragonEmbeddedTarget;
        private Vector3 _dragonVelocity;
        private Vector3 _dragonThrowOrigin;

        public string DragonState
        {
            get
            {
                if (_dragonReturning) return "RECALLING";
                if (_dragonEmbedded) return "EMBEDDED";
                if (_dragonThrown) return "IN FLIGHT";
                return "READY";
            }
        }

        private void Awake()
        {
            Instance = this;
            _player = GetComponent<PlayerController>();
            _cc = GetComponent<CharacterController>();
            _animator = _player != null ? _player.animator : GetComponentInChildren<Animator>();
        }

        private void OnDestroy()
        {
            if (_dragonBlade != null)
                Destroy(_dragonBlade);
            if (Instance == this) Instance = null;
        }

        public void Preview(HighflyDonorWeaponSkillV021 skill)
        {
            if (skill == HighflyDonorWeaponSkillV021.DragonSwordThrowRecall)
            {
                if (_dragonThrown || _dragonEmbedded || _dragonReturning)
                {
                    if (!_dragonReturning)
                        StartCoroutine(DragonRecall());
                    return;
                }

                if (!_busy)
                    StartCoroutine(DragonThrow());
                return;
            }

            if (_busy) return;
            if (Time.unscaledTime < _sigilReadyAt)
            {
                float left = Mathf.Max(0f, _sigilReadyAt - Time.unscaledTime);
                HighflySkillLabMetrics.RecordAction("SIGIL DASH • CD " + left.ToString("0.0") + "s", 0);
                return;
            }

            StartCoroutine(SigilDashAttack());
        }

        private IEnumerator SigilDashAttack()
        {
            _busy = true;
            _sigilReadyAt = Time.unscaledTime + SigilCooldown;

            try
            {
                FaceNearestTarget(12f);
                HighflySkillLabMetrics.RecordAction("SIGIL DASH ATTACK • SEMI-FULL", 0);
                HighflyParkourAnimationV010.Instance?.Play("Sword_Dash", 1f, SigilTraceWindow + 0.12f);

                Vector3 fwd = transform.forward;
                fwd.y = 0f;
                if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
                fwd.Normalize();

                SafeMove(transform.position + fwd * SigilLungeDistance);

                HashSet<CharacterStats> hit = new HashSet<CharacterStats>();
                Vector3 previousSocket = SigilSocketPosition();
                float end = Time.unscaledTime + SigilTraceWindow;

                while (Time.unscaledTime < end)
                {
                    Vector3 currentSocket = SigilSocketPosition();
                    TraceSigilSweep(previousSocket, currentSocket, hit);
                    previousSocket = currentSocket;
                    yield return null;
                }

                HighflySkillLabMetrics.RecordAction(
                    "SIGIL DASH • trace 0.35s • hits " + hit.Count,
                    hit.Count);
            }
            finally
            {
                _busy = false;
            }
        }

        private Vector3 SigilSocketPosition()
        {
            return transform.position + Vector3.up * 1.0f + transform.forward * 1.2f;
        }

        private void TraceSigilSweep(Vector3 from, Vector3 to, HashSet<CharacterStats> hit)
        {
            Collider[] cols = Physics.OverlapCapsule(from, to, SigilTraceRadius, ~0, QueryTriggerInteraction.Ignore);
            CharacterStats owner = GetComponent<CharacterStats>();

            for (int i = 0; i < cols.Length; i++)
            {
                Collider col = cols[i];
                if (col == null) continue;

                CharacterStats target = col.GetComponentInParent<CharacterStats>();
                if (target == null || target == owner || hit.Contains(target)) continue;

                hit.Add(target);
                target.TakeDamage(SigilDamage, SigilPoiseDamage, transform);
            }
        }

        private IEnumerator DragonThrow()
        {
            _busy = true;
            try
            {
                FaceNearestTarget(30f);
                EnsureDragonBlade();
                SetBladeAtHand();

                HighflySkillLabMetrics.RecordAction(
                    "DRAGON SOULS • THROW WINDUP • SEMI-FULL",
                    0);

                // Original animation event = 0.6666667 clip seconds, original state speed = 1.4.
                // The animation asset itself is replaced; the donor event timing is not.
                HighflyParkourAnimationV010.Instance?.Play("OverhandThrow", 1f, 0.72f);
                yield return new WaitForSecondsRealtime(DragonThrowEventRealSeconds);

                ReleaseDragonBlade();
                HighflySkillLabMetrics.RecordAction(
                    "DRAGON SOULS • SWORD IN FLIGHT",
                    0);
            }
            finally
            {
                _busy = false;
            }
        }

        private void ReleaseDragonBlade()
        {
            if (_dragonBlade == null) return;

            _dragonBlade.transform.SetParent(null, true);
            _dragonThrowOrigin = _dragonBlade.transform.position;
            _dragonThrown = true;
            _dragonEmbedded = false;
            _dragonReturning = false;
            _dragonEmbeddedTarget = null;

            Vector3 direction = Camera.main != null ? Camera.main.transform.forward : transform.forward;
            direction.Normalize();

            RaycastHit hitInfo;
            if (Camera.main != null &&
                Physics.Raycast(
                    Camera.main.transform.position,
                    Camera.main.transform.forward,
                    out hitInfo,
                    DragonMaxRange,
                    ~0,
                    QueryTriggerInteraction.Ignore))
            {
                CharacterStats maybeTarget = hitInfo.collider != null
                    ? hitInfo.collider.GetComponentInParent<CharacterStats>()
                    : null;

                if (maybeTarget != null && maybeTarget != GetComponent<CharacterStats>())
                {
                    Vector3 toHit = hitInfo.point - _dragonBlade.transform.position;
                    _dragonVelocity = toHit.sqrMagnitude > 0.0001f
                        ? toHit.normalized * DragonTargetSpeed
                        : direction * DragonTargetSpeed;
                }
                else
                {
                    _dragonVelocity = direction * DragonImpulseSpeed;
                }
            }
            else
            {
                _dragonVelocity = direction * DragonImpulseSpeed;
            }

            StartCoroutine(DragonFlight());
        }

        private IEnumerator DragonFlight()
        {
            Vector3 previous = _dragonBlade != null ? _dragonBlade.transform.position : transform.position;

            while (_dragonThrown && !_dragonReturning && _dragonBlade != null)
            {
                float dt = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
                Vector3 next = previous + _dragonVelocity * dt;

                CharacterStats target = FindSegmentTarget(previous, next, 0.24f);
                if (target != null)
                {
                    _dragonBlade.transform.position = ClosestPointOnSegment(previous, next, target.transform.position);
                    EmbedDragonBlade(target);
                    yield break;
                }

                _dragonBlade.transform.position = next;
                SpinDragonBlade(dt);

                if (Vector3.Distance(_dragonBlade.transform.position, transform.position) >= DragonMaxRange)
                {
                    _dragonThrown = false;
                    _dragonVelocity = Vector3.zero;
                    HighflySkillLabMetrics.RecordAction("DRAGON SOULS • MAX RANGE 250m", 0);
                    yield break;
                }

                previous = next;
                yield return null;
            }
        }

        private void EmbedDragonBlade(CharacterStats target)
        {
            if (target == null || _dragonBlade == null) return;

            _dragonThrown = false;
            _dragonEmbedded = true;
            _dragonVelocity = Vector3.zero;
            _dragonEmbeddedTarget = target;
            _dragonBlade.transform.SetParent(target.transform, true);

            target.TakeDamage(DragonDamage, 10f, transform);
            HighflySkillLabMetrics.RecordAction(
                "DRAGON SOULS • EMBED • 25 DMG • TAP RECALL",
                0);
        }

        private IEnumerator DragonRecall()
        {
            if (_dragonBlade == null)
                yield break;

            _busy = true;
            _dragonReturning = true;
            _dragonThrown = false;
            _dragonEmbedded = false;
            _dragonEmbeddedTarget = null;
            _dragonBlade.transform.SetParent(null, true);

            HighflySkillLabMetrics.RecordAction("DRAGON SOULS • RECALL", 0);

            Vector3 start = _dragonBlade.transform.position;
            Vector3 curvePoint = transform.TransformPoint(DragonCurvePointLocal);
            float distance = Vector3.Distance(start, curvePoint);
            float duration = distance / DragonRecallFirstLegSpeed;
            float elapsed = 0f;

            while (elapsed < duration && _dragonBlade != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = duration <= 0.0001f ? 1f : Mathf.Clamp01(elapsed / duration);
                float eased = t * t * t; // DOTween Ease.InCubic donor behavior.
                _dragonBlade.transform.position = Vector3.LerpUnclamped(start, curvePoint, eased);
                SpinDragonBlade(Time.unscaledDeltaTime);
                yield return null;
            }

            // Original FixedUpdate leg: position = Vector3.Slerp(current, hand, 0.3)
            // until distance < 1m, then snap/re-parent to hand.
            WaitForFixedUpdate fixedWait = new WaitForFixedUpdate();
            while (_dragonBlade != null)
            {
                Vector3 hand = DragonHandPosition();
                _dragonBlade.transform.position = Vector3.Slerp(
                    _dragonBlade.transform.position,
                    hand,
                    DragonRecallHandFactor);
                SpinDragonBlade(Time.unscaledDeltaTime);

                if (Vector3.Distance(_dragonBlade.transform.position, hand) < DragonCatchDistance)
                    break;

                yield return fixedWait;
            }

            SetBladeAtHand();
            _dragonReturning = false;
            _busy = false;
            HighflySkillLabMetrics.RecordAction("DRAGON SOULS • CAUGHT / READY", 0);
        }

        private void EnsureDragonBlade()
        {
            if (_dragonBlade != null) return;

            _dragonBlade = new GameObject("DONOR_DRAGON_SOULS_SWORD_PROXY");

            GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "Blade";
            blade.transform.SetParent(_dragonBlade.transform, false);
            blade.transform.localPosition = new Vector3(0f, 0.68f, 0f);
            blade.transform.localScale = new Vector3(0.10f, 1.25f, 0.055f);
            Destroy(blade.GetComponent<Collider>());

            GameObject guard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            guard.name = "Guard";
            guard.transform.SetParent(_dragonBlade.transform, false);
            guard.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            guard.transform.localScale = new Vector3(0.48f, 0.09f, 0.12f);
            Destroy(guard.GetComponent<Collider>());

            GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            grip.name = "Grip";
            grip.transform.SetParent(_dragonBlade.transform, false);
            grip.transform.localPosition = new Vector3(0f, -0.27f, 0f);
            grip.transform.localScale = new Vector3(0.07f, 0.28f, 0.07f);
            Destroy(grip.GetComponent<Collider>());

            Material bladeMat = HighflyLabVisuals.CreateMaterial(
                new Color(0.62f, 0.72f, 0.82f, 1f),
                new Color(0.08f, 0.18f, 0.28f, 1f));
            Material gripMat = HighflyLabVisuals.CreateMaterial(
                new Color(0.10f, 0.12f, 0.16f, 1f),
                new Color(0.01f, 0.02f, 0.03f, 1f));

            Renderer br = blade.GetComponent<Renderer>();
            Renderer gr = guard.GetComponent<Renderer>();
            Renderer rr = grip.GetComponent<Renderer>();
            if (br != null) br.sharedMaterial = bladeMat;
            if (gr != null) gr.sharedMaterial = bladeMat;
            if (rr != null) rr.sharedMaterial = gripMat;

            TrailRenderer trail = _dragonBlade.AddComponent<TrailRenderer>();
            trail.time = 0.16f;
            trail.startWidth = 0.12f;
            trail.endWidth = 0.015f;
            trail.sharedMaterial = HighflyLabVisuals.CreateFxMaterial(new Color(0.38f, 0.82f, 1f, 1f));
        }

        private void SetBladeAtHand()
        {
            if (_dragonBlade == null) return;

            _dragonBlade.transform.SetParent(transform, false);
            _dragonBlade.transform.localPosition = new Vector3(0.42f, 1.15f, 0.34f);
            _dragonBlade.transform.localRotation = Quaternion.Euler(0f, 0f, -12f);

            _dragonThrown = false;
            _dragonEmbedded = false;
            _dragonReturning = false;
            _dragonVelocity = Vector3.zero;
            _dragonEmbeddedTarget = null;
        }

        private Vector3 DragonHandPosition()
        {
            return transform.TransformPoint(new Vector3(0.42f, 1.15f, 0.34f));
        }

        private void SpinDragonBlade(float dt)
        {
            if (_dragonBlade == null) return;
            _dragonBlade.transform.Rotate(Vector3.up, -1800f * dt, Space.Self);
        }

        private CharacterStats FindSegmentTarget(Vector3 a, Vector3 b, float radius)
        {
            CharacterStats[] all = Object.FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);
            CharacterStats owner = GetComponent<CharacterStats>();
            CharacterStats best = null;
            float bestSq = float.MaxValue;

            for (int i = 0; i < all.Length; i++)
            {
                CharacterStats target = all[i];
                if (target == null || target == owner) continue;

                Vector3 p = target.transform.position;
                Vector3 closest = ClosestPointOnSegment(a, b, p);
                float sq = (p - closest).sqrMagnitude;
                float accept = radius + ApproxTargetRadius(target);

                if (sq <= accept * accept && sq < bestSq)
                {
                    bestSq = sq;
                    best = target;
                }
            }

            return best;
        }

        private static float ApproxTargetRadius(CharacterStats target)
        {
            if (target == null) return 0.5f;
            string n = target.gameObject.name;
            if (n.Contains("LARGE")) return 1.45f;
            if (n.Contains("SMALL")) return 0.72f;
            return 0.95f;
        }

        private static Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector3 ab = b - a;
            float denom = Vector3.Dot(ab, ab);
            if (denom <= 0.000001f) return a;
            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / denom);
            return a + ab * t;
        }

        private void FaceNearestTarget(float radius)
        {
            CharacterStats[] all = Object.FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);
            CharacterStats owner = GetComponent<CharacterStats>();
            CharacterStats best = null;
            float bestSq = radius * radius;

            for (int i = 0; i < all.Length; i++)
            {
                CharacterStats candidate = all[i];
                if (candidate == null || candidate == owner) continue;

                Vector3 d = candidate.transform.position - transform.position;
                d.y = 0f;
                float sq = d.sqrMagnitude;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best = candidate;
                }
            }

            if (best == null) return;
            Vector3 dir = best.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        private void SafeMove(Vector3 target)
        {
            if (_cc != null && _cc.enabled)
            {
                _cc.enabled = false;
                transform.position = target;
                _cc.enabled = true;
            }
            else
            {
                transform.position = target;
            }
        }
    }
}
