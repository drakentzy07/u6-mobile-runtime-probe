using System.Collections;
using UnityEngine;

namespace Highfly.SkillLab
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController))]
    public sealed class HighflyAerialMobility : MonoBehaviour
    {
        public static HighflyAerialMobility Instance { get; private set; }

        [Header("LAB aerial movement")]
        [SerializeField] private float doubleJumpSpeed = 8.2f;
        [SerializeField] private float wallJumpVerticalSpeed = 9.4f;
        [SerializeField] private float wallJumpHorizontalSpeed = 7.2f;
        [SerializeField] private float wallProbeDistance = 1.18f;
        [SerializeField] private float wallProbeRadius = 0.18f;
        [SerializeField] private float wallTrickChance = 1.0f;
        [SerializeField] private float wallTrickDuration = 0.42f;

        private PlayerController _player;
        private CharacterController _cc;

        private bool _airJumpUsed;
        private bool _wallJumpUsed;
        private float _lastGroundedAt;
        private bool _wallImpulseActive;
        private bool _wallTrickActive;
        private bool _wasGrounded;
        private float _lastAirVerticalSpeed;
        private Transform _wallTrickVisual;
        private Quaternion _wallTrickBaseRotation = Quaternion.identity;

        private readonly RaycastHit[] _wallHits = new RaycastHit[16];

        public string DebugState
        {
            get
            {
                if (_player == null) return "-";
                if (_player.HighflyIsGrounded) return "GROUND";
                if (_wallJumpUsed && _airJumpUsed) return "AIR • DJ+WJ usados";
                if (_wallJumpUsed) return "AIR • wall usado";
                if (_airJumpUsed) return "AIR • double usado";
                return "AIR • recursos listos";
            }
        }

        private void Awake()
        {
            Instance = this;
            _player = GetComponent<PlayerController>();
            _cc = GetComponent<CharacterController>();
            _wasGrounded = _player != null && _player.HighflyIsGrounded;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (_player == null) return;

            bool grounded = _player.HighflyIsGrounded;

            if (grounded)
            {
                _lastGroundedAt = Time.unscaledTime;
                _airJumpUsed = false;
                _wallJumpUsed = false;

                if (!_wasGrounded)
                {
                    HighflyParkourAnimationV010.Instance?.Play(
                        "NinjaJump_Land",
                        1.18f,
                        0.30f);
                    SoftenLightLanding(_lastAirVerticalSpeed);
                }
            }
            else
            {
                _lastAirVerticalSpeed = _player.HighflyVerticalSpeed;
            }

            _wasGrounded = grounded;
        }

        public void RequestJump()
        {
            if (_player == null) return;
            if (HighflyLabActionGuardV026.Instance != null &&
                HighflyLabActionGuardV026.Instance.BlocksManualInput("JUMP / PARKOUR"))
                return;
            if (_player.currentState != PlayerState.Locomotion) return;

            if (_player.HighflyIsGrounded)
            {
                HighflyParkourAnimationV010.Instance?.Play(
                    "NinjaJump_Start",
                    1.10f,
                    0.42f);
                _player.HighflyPerformBaseJump();
                SpawnJumpRing(
                    transform.position + Vector3.up * 0.04f,
                    new Color(0.76f, 0.84f, 0.92f, 0.72f),
                    0.72f);
                return;
            }

            Vector3 wallNormal;
            Vector3 wallPoint;

            if (!_wallJumpUsed && TryFindWall(out wallNormal, out wallPoint))
            {
                PerformWallJump(wallNormal, wallPoint);
                return;
            }

            if (!_airJumpUsed)
            {
                PerformDoubleJump();
                return;
            }
        }

        private void PerformDoubleJump()
        {
            _airJumpUsed = true;
            HighflyParkourAnimationV010.Instance?.Play(
                "NinjaJump_Start",
                1.18f,
                0.38f);
            _player.HighflyLabSetVerticalSpeed(doubleJumpSpeed, false);

            Color air = new Color(0.18f, 0.72f, 1f, 1f);

            SpawnJumpRing(
                transform.position + Vector3.up * 0.10f,
                air,
                1.05f);

            HighflyPremiumFx.SpawnAfterImage(transform, air, 0.20f);
            HighflyPremiumFx.SpawnResource(
                "ElectricalSparks",
                transform.position + Vector3.up * 0.20f,
                Quaternion.identity,
                0.38f,
                0.55f,
                air);

            HighflySkillLabMetrics.RecordAction("MOVILIDAD • DOBLE SALTO", 0);
        }

        private void PerformWallJump(Vector3 normal, Vector3 point)
        {
            _wallJumpUsed = true;

            Vector3 outward = normal;
            outward.y = 0f;
            if (outward.sqrMagnitude < 0.01f)
                outward = -transform.forward;
            outward.Normalize();

            Vector3 inputWorld = DesiredMoveDirection();
            Vector3 tangent = Vector3.ProjectOnPlane(inputWorld, outward);
            if (tangent.sqrMagnitude > 0.01f)
                tangent.Normalize();

            // Input along the wall is preserved so the move reads as parkour,
            // not as an automatic bounce. No input = strong rebound.
            float tangentWeight = tangent.sqrMagnitude > 0.01f ? 0.58f : 0f;
            Vector3 horizontal =
                (outward * 0.82f + tangent * tangentWeight).normalized;

            transform.rotation =
                Quaternion.LookRotation(horizontal, Vector3.up);

            HighflyParkourAnimationV010.Instance?.Play(
                "NinjaJump_Start",
                1.22f,
                0.42f);
            _player.HighflyLabSetVerticalSpeed(wallJumpVerticalSpeed, false);

            Color wall = new Color(0.72f, 0.32f, 1f, 1f);

            HighflyPremiumFx.SpawnResource(
                "Sparks",
                point + normal * 0.04f,
                Quaternion.LookRotation(normal),
                0.55f,
                0.60f,
                wall);

            HighflyAnimeFx.SpawnLightningBurst(
                point + Vector3.up * 0.30f,
                transform.position + Vector3.up * 0.85f,
                wall,
                3,
                0.14f);

            HighflyPremiumFx.SpawnAfterImage(transform, wall, 0.24f);
            SpawnJumpRing(point + normal * 0.03f, wall, 0.70f, normal);

            if (!_wallImpulseActive)
                StartCoroutine(WallImpulse(horizontal));

            TryWallTrick(inputWorld, tangent);
            HighflySkillLabMetrics.RecordAction(
                _wallTrickActive ? "MOVILIDAD • WALL TRICK" : "MOVILIDAD • WALL JUMP",
                0);
        }

        private IEnumerator WallImpulse(Vector3 direction)
        {
            _wallImpulseActive = true;

            float start = Time.unscaledTime;
            const float duration = 0.18f;

            while (Time.unscaledTime - start < duration)
            {
                if (_cc != null && _cc.enabled)
                    _cc.Move(direction * wallJumpHorizontalSpeed * Time.unscaledDeltaTime);

                yield return null;
            }

            _wallImpulseActive = false;
        }

        private void TryWallTrick(Vector3 inputWorld, Vector3 tangent)
        {
            // v0.10: parkour is deterministic. Reaching a valid wall should never
            // randomly decide whether the hunter performs the trick.
            if (_wallTrickActive || wallTrickChance <= 0f)
                return;

            if (_player == null || _player.animator == null)
                return;

            Transform visual = _player.animator.transform;

            // Never rotate the physics/player root: the trick is presentation only.
            if (visual == null || visual == transform)
                return;

            float lateral =
                tangent.sqrMagnitude > 0.01f && inputWorld.sqrMagnitude > 0.01f
                    ? Vector3.Dot(inputWorld.normalized, tangent.normalized)
                    : 0f;

            StartCoroutine(WallTrickVisual(visual, lateral));
        }

        private IEnumerator WallTrickVisual(Transform visual, float lateral)
        {
            _wallTrickActive = true;
            _wallTrickVisual = visual;
            _wallTrickBaseRotation = visual.localRotation;

            Quaternion baseRotation = _wallTrickBaseRotation;
            bool sideFlip = Mathf.Abs(lateral) > 0.45f;
            Vector3 axis = sideFlip ? Vector3.forward : Vector3.right;
            float direction = sideFlip && lateral < 0f ? -1f : 1f;

            float start = Time.unscaledTime;
            while (visual != null &&
                   Time.unscaledTime - start < wallTrickDuration)
            {
                float t =
                    (Time.unscaledTime - start) /
                    Mathf.Max(0.01f, wallTrickDuration);

                float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                visual.localRotation =
                    baseRotation *
                    Quaternion.AngleAxis(360f * eased * direction, axis);

                yield return null;
            }

            if (visual != null)
                visual.localRotation = baseRotation;

            _wallTrickVisual = null;
            _wallTrickActive = false;
        }

        public void ForceResetMotion(string reason)
        {
            StopAllCoroutines();

            if (_wallTrickVisual != null)
                _wallTrickVisual.localRotation = _wallTrickBaseRotation;

            _wallTrickVisual = null;
            _wallImpulseActive = false;
            _wallTrickActive = false;

            HighflyLabTelemetryV026.Record("PARKOUR RESET", reason ?? "-");
        }

        private void SoftenLightLanding(float lastVerticalSpeed)
        {
            if (_player == null || _player.animator == null)
                return;

            float impactSpeed = Mathf.Abs(Mathf.Min(0f, lastVerticalSpeed));

            // Preserve deliberate heavy-landing poses; only remove the tiny
            // crouched residue after ordinary jumps and parkour.
            if (impactSpeed > 13.5f)
                return;

            Animator a = _player.animator;
            int locomotion =
                Animator.StringToHash("Base Layer.Locomotion");

            if (a.HasState(0, locomotion))
            {
                a.CrossFadeInFixedTime(locomotion, 0.035f, 0);
                return;
            }

            locomotion = Animator.StringToHash("Locomotion");
            if (a.HasState(0, locomotion))
                a.CrossFadeInFixedTime(locomotion, 0.035f, 0);
        }

        private bool TryFindWall(out Vector3 normal, out Vector3 point)
        {
            Vector3 origin =
                transform.position +
                Vector3.up * Mathf.Max(0.85f, _cc != null ? _cc.height * 0.46f : 0.9f);

            Vector3 desired = DesiredMoveDirection();

            Vector3[] directions =
            {
                desired,
                transform.forward,
                transform.right,
                -transform.right,
                -transform.forward
            };

            float bestDistance = float.MaxValue;
            bool found = false;
            normal = Vector3.zero;
            point = Vector3.zero;

            for (int d = 0; d < directions.Length; d++)
            {
                Vector3 dir = directions[d];
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.01f) continue;
                dir.Normalize();

                int count = Physics.SphereCastNonAlloc(
                    origin,
                    wallProbeRadius,
                    dir,
                    _wallHits,
                    wallProbeDistance,
                    ~0,
                    QueryTriggerInteraction.Ignore);

                for (int i = 0; i < count; i++)
                {
                    RaycastHit hit = _wallHits[i];
                    if (hit.collider == null) continue;

                    Transform ht = hit.collider.transform;
                    if (ht == transform || ht.IsChildOf(transform))
                        continue;

                    if (Mathf.Abs(hit.normal.y) > 0.42f)
                        continue;

                    if (hit.distance < bestDistance)
                    {
                        bestDistance = hit.distance;
                        normal = hit.normal;
                        point = hit.point;
                        found = true;
                    }
                }
            }

            return found;
        }

        private Vector3 DesiredMoveDirection()
        {
            Vector2 input = _player != null
                ? _player.HighflyMobileMoveInput
                : Vector2.zero;

            if (input.sqrMagnitude > 0.02f &&
                _player != null &&
                _player.cameraTransform != null)
            {
                Vector3 forward = _player.cameraTransform.forward;
                Vector3 right = _player.cameraTransform.right;
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();

                Vector3 world = forward * input.y + right * input.x;
                if (world.sqrMagnitude > 0.01f)
                    return world.normalized;
            }

            return transform.forward;
        }

        private static void SpawnJumpRing(
            Vector3 position,
            Color color,
            float radius,
            Vector3? surfaceNormal = null)
        {
            var go = new GameObject("HF_JUMP_RING");
            go.transform.position = position;

            Vector3 normal = surfaceNormal ?? Vector3.up;
            go.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);

            var lr = go.AddComponent<LineRenderer>();
            lr.loop = true;
            lr.useWorldSpace = false;
            lr.positionCount = 40;
            lr.widthMultiplier = 0.045f;
            lr.sharedMaterial =
                HighflyPremiumFx.CreateTransparentMaterial(
                    new Color(color.r, color.g, color.b, 0.78f),
                    color * 2.8f);

            for (int i = 0; i < 40; i++)
            {
                float a = i / 40f * Mathf.PI * 2f;
                lr.SetPosition(
                    i,
                    new Vector3(
                        Mathf.Cos(a) * radius,
                        0f,
                        Mathf.Sin(a) * radius));
            }

            go.AddComponent<HighflySimpleLifetime>().Initialize(0.34f);
        }
    }
}
