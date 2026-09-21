using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Highfly.SkillLab
{
    public enum HighflyPremiumSkillId
    {
        TwinDance = 1,
        PhantomStep = 2,
        ShadowShackle = 3,
        VitalPact = 4,
        ShadowCall = 5
    }

    [DisallowMultipleComponent]
    public sealed class HighflyPremiumSkillRuntime : MonoBehaviour
    {
        public static HighflyPremiumSkillRuntime Instance { get; private set; }

        private PlayerController _player;
        private PlayerStats _stats;
        private CharacterController _cc;

        private int _twinStage;
        private float _lastTwinInput = -99f;

        private float _cdTwin;
        private float _cdStep;
        private float _cdShackle;
        private float _cdPact;
        private float _cdSummon;

        private float _vitalPactUntil;
        private bool _hitStopRunning;

        private readonly Collider[] _hits = new Collider[48];
        private readonly List<HighflyShadowMinion> _summons = new List<HighflyShadowMinion>();

        private const float TwinReset = 0.95f;

        public bool VitalPactActive => Time.unscaledTime < _vitalPactUntil;

        private void Awake()
        {
            Instance = this;
            _player = GetComponent<PlayerController>();
            _stats = GetComponent<PlayerStats>();
            _cc = GetComponent<CharacterController>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Trigger(HighflyPremiumSkillId id)
        {
            switch (id)
            {
                case HighflyPremiumSkillId.TwinDance:
                    TriggerTwinDance();
                    break;
                case HighflyPremiumSkillId.PhantomStep:
                    TriggerPhantomStep();
                    break;
                case HighflyPremiumSkillId.ShadowShackle:
                    TriggerShadowShackle();
                    break;
                case HighflyPremiumSkillId.VitalPact:
                    TriggerVitalPact();
                    break;
                case HighflyPremiumSkillId.ShadowCall:
                    TriggerShadowCall();
                    break;
            }
        }

        private bool CanAct()
        {
            return _player != null &&
                   _player.currentState != PlayerState.Die &&
                   _player.currentState != PlayerState.Interact &&
                   _player.currentState != PlayerState.UseItem;
        }

        // ------------------------------------------------------------------
        // S1 — DANZA GEMELA
        // Melee / Dual / Multi-hit / Combo / Cancel family.
        // ------------------------------------------------------------------
        private void TriggerTwinDance()
        {
            if (!CanAct() || Time.unscaledTime < _cdTwin) return;

            if (Time.unscaledTime - _lastTwinInput > TwinReset)
                _twinStage = 0;

            _twinStage = (_twinStage % 3) + 1;
            _lastTwinInput = Time.unscaledTime;
            _cdTwin = Time.unscaledTime + 0.18f;

            HighflySkillLabMetrics.RecordAction("S1 • DANZA GEMELA", _twinStage);
            StartCoroutine(TwinDanceRoutine(_twinStage));
        }

        private IEnumerator TwinDanceRoutine(int stage)
        {
            Vector3 dir = FacingDirection();
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            // Reuse Lucid's real sword animation ownership instead of inventing a LAB animator.
            _player.HighflyMobileAttack();

            Color c1 = new Color(0.10f, 0.90f, 1f, 1f);
            Color c2 = new Color(0.55f, 0.22f, 1f, 1f);

            SpawnTelegraphArc(transform.position + Vector3.up * 1.0f, dir, c1, 1.5f + stage * 0.2f);
            yield return new WaitForSecondsRealtime(stage == 3 ? 0.055f : 0.075f);

            if (_cc != null)
                _cc.Move(dir * (0.32f + 0.14f * stage));

            if (stage == 1)
            {
                SpawnSlash(dir, c1, -32f, 1.9f);
                DealConeDamage(19f, 2.25f, 1.20f, dir, "Gemela-I");
                yield return new WaitForSecondsRealtime(0.07f);
                SpawnSlash(dir, c2, 34f, 2.0f);
                DealConeDamage(21f, 2.35f, 1.20f, dir, "Gemela-II");
            }
            else if (stage == 2)
            {
                SpawnSlash(dir, c2, 65f, 2.25f);
                DealConeDamage(29f, 2.55f, 1.35f, dir, "Gemela-Cruz");
                yield return new WaitForSecondsRealtime(0.055f);
                SpawnImpactRing(transform.position + dir * 1.7f, c1, 1.7f);
            }
            else
            {
                SpawnSlash(dir, Color.white, 0f, 2.7f);
                SpawnSlash(dir, c2, 90f, 2.55f);
                DealConeDamage(48f, 3.05f, 1.55f, dir, "Gemela-Finisher");
                SpawnImpactRing(transform.position + dir * 2.0f, c2, 2.8f);
                StartCoroutine(FovPunch(5.5f, 0.10f));
            }
        }

        // ------------------------------------------------------------------
        // S2 — PASO FANTASMA
        // Mobility / Rush / Perfect-Dodge family.
        // ------------------------------------------------------------------
        private void TriggerPhantomStep()
        {
            if (!CanAct() || Time.unscaledTime < _cdStep) return;

            _cdStep = Time.unscaledTime + 1.15f;
            HighflySkillLabMetrics.RecordAction("S2 • PASO FANTASMA", 0);
            StartCoroutine(PhantomStepRoutine());
        }

        private IEnumerator PhantomStepRoutine()
        {
            Vector3 dir = FacingDirection();
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            // Lucid Roll supplies the actual i-frame contract.
            _player.HighflyMobileRoll();

            Color ghost = new Color(0.22f, 0.78f, 1f, 1f);
            float start = Time.unscaledTime;
            float duration = 0.22f;
            float nextEcho = 0f;

            while (Time.unscaledTime - start < duration)
            {
                float t = (Time.unscaledTime - start) / duration;

                if (_cc != null)
                    _cc.Move(dir * (7.5f * Time.unscaledDeltaTime));

                if (t >= nextEcho)
                {
                    nextEcho += 0.18f;
                    SpawnGhostEcho(transform.position, ghost);
                }

                yield return null;
            }

            SpawnSlash(dir, ghost, 0f, 2.2f);
            DealConeDamage(27f, 2.45f, 1.15f, dir, "Paso-Fantasma");
            SpawnImpactRing(transform.position + dir * 1.2f, ghost, 1.45f);
            StartCoroutine(FovPunch(3.2f, 0.08f));
        }

        // ------------------------------------------------------------------
        // S3 — GRILLETE UMBRÍO
        // Targeting / CC / Debuff / Shadow Mark family.
        // ------------------------------------------------------------------
        private void TriggerShadowShackle()
        {
            if (!CanAct() || Time.unscaledTime < _cdShackle) return;

            CharacterStats target = FindBestTarget(9.0f, 95f);
            if (target == null)
            {
                HighflySkillLabMetrics.RecordAction("S3 • GRILLETE UMBRÍO (SIN OBJETIVO)", 0);
                return;
            }

            _cdShackle = Time.unscaledTime + 4.5f;
            HighflySkillLabMetrics.RecordAction("S3 • GRILLETE UMBRÍO", 0);
            StartCoroutine(ShadowShackleRoutine(target));
        }

        private IEnumerator ShadowShackleRoutine(CharacterStats target)
        {
            if (target == null) yield break;

            Color shadow = new Color(0.43f, 0.12f, 0.92f, 1f);
            Vector3 origin = transform.position + Vector3.up * 1.05f;
            Vector3 end = target.transform.position + Vector3.up * 0.9f;

            for (int i = 0; i < 3; i++)
            {
                Vector3 side = transform.right * ((i - 1) * 0.18f);
                SpawnChain(origin + side, end, shadow, 0.36f);
            }

            DealDirect(target, 24f, "Grillete");
            SpawnImpactRing(end, shadow, 1.35f);

            var status = target.GetComponent<HighflyLabStatusReceiver>();
            if (status == null)
                status = target.gameObject.AddComponent<HighflyLabStatusReceiver>();

            status.ApplyRoot(2.2f);
            status.ApplyShadowMark(5.0f);

            yield return new WaitForSecondsRealtime(0.12f);
            SpawnChain(origin, end, Color.white, 0.18f);
        }

        // ------------------------------------------------------------------
        // S4 — PACTO VITAL
        // Healing / Sustain / Passive-conversion family.
        // ------------------------------------------------------------------
        private void TriggerVitalPact()
        {
            if (!CanAct() || Time.unscaledTime < _cdPact) return;

            _cdPact = Time.unscaledTime + 10f;
            _vitalPactUntil = Time.unscaledTime + 8f;

            HighflySkillLabMetrics.RecordAction("S4 • PACTO VITAL", 0);

            if (_stats != null)
            {
                float missing = Mathf.Max(0f, _stats.maxEgo - _stats.currentEgo);
                float heal = Mathf.Max(_stats.maxEgo * 0.08f, missing * 0.30f);
                _stats.RestoreEgo(heal);
            }

            Color life = new Color(0.16f, 1f, 0.62f, 1f);
            SpawnImpactRing(transform.position + Vector3.up * 0.15f, life, 3.2f);
            StartCoroutine(VitalAuraRoutine(life));
        }

        private IEnumerator VitalAuraRoutine(Color c)
        {
            float end = _vitalPactUntil;
            while (Time.unscaledTime < end)
            {
                SpawnOrbitSpark(c);
                yield return new WaitForSecondsRealtime(0.32f);
            }
        }

        // ------------------------------------------------------------------
        // Slot 5 — LLAMADO DE LA SOMBRA
        // Summon / AI / System-interaction family.
        // ------------------------------------------------------------------
        private void TriggerShadowCall()
        {
            if (!CanAct() || Time.unscaledTime < _cdSummon) return;

            _cdSummon = Time.unscaledTime + 12f;
            HighflySkillLabMetrics.RecordAction("S5 • LLAMADO DE LA SOMBRA", 0);

            CleanupSummons();

            SpawnImpactRing(transform.position + Vector3.up * 0.1f, new Color(0.28f, 0.08f, 0.72f, 1f), 4f);

            for (int i = 0; i < 2; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = "HIGHFLY_SHADOW_" + (i + 1);
                Destroy(go.GetComponent<Collider>());

                go.transform.position = transform.position + transform.right * (i == 0 ? -1.6f : 1.6f);
                go.transform.localScale = new Vector3(0.48f, 0.90f, 0.48f);

                var renderer = go.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.sharedMaterial = HighflyLabVisuals.CreateMaterial(
                        new Color(0.10f, 0.02f, 0.22f, 1f),
                        new Color(0.35f, 0.08f, 1f, 1f));

                var minion = go.AddComponent<HighflyShadowMinion>();
                minion.Initialize(this, transform, i == 0 ? -1f : 1f, 9.5f);
                _summons.Add(minion);
            }
        }

        public CharacterStats FindBestTarget(float radius, float angle)
        {
            Vector3 origin = transform.position + Vector3.up * 0.8f;
            int count = Physics.OverlapSphereNonAlloc(origin, radius, _hits, ~0, QueryTriggerInteraction.Collide);

            CharacterStats best = null;
            float bestScore = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider hit = _hits[i];
                if (hit == null) continue;

                CharacterStats stats = hit.GetComponentInParent<CharacterStats>();
                if (stats == null || stats.transform == transform) continue;

                bool valid = stats is HighflyLabDummyStats;
                if (!valid)
                {
                    try { valid = stats.CompareTag("Enemy"); } catch { }
                }
                if (!valid) continue;

                Vector3 delta = stats.transform.position - transform.position;
                delta.y = 0f;
                if (delta.sqrMagnitude < 0.01f) continue;

                float a = Vector3.Angle(transform.forward, delta.normalized);
                if (a > angle * 0.5f) continue;

                float score = delta.sqrMagnitude + a * 0.06f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = stats;
                }
            }

            return best;
        }

        public void DealDirect(CharacterStats target, float damage, string source)
        {
            if (target == null) return;

            var status = target.GetComponent<HighflyLabStatusReceiver>();
            if (status != null && status.IsShadowMarked)
                damage *= 1.20f;

            target.TakeDamage(damage, 25f, transform);
            HighflySkillLabMetrics.RecordHit(damage);

            if (VitalPactActive && _stats != null)
                _stats.RestoreEgo(damage * 0.22f);

            StartCoroutine(HitStop(0.035f));
        }

        private void DealConeDamage(float damage, float reach, float radius, Vector3 dir, string source)
        {
            Vector3 center = transform.position + Vector3.up * 0.9f + dir * reach;
            int count = Physics.OverlapSphereNonAlloc(center, radius, _hits, ~0, QueryTriggerInteraction.Collide);

            var seen = new HashSet<CharacterStats>();

            for (int i = 0; i < count; i++)
            {
                Collider hit = _hits[i];
                if (hit == null) continue;

                CharacterStats stats = hit.GetComponentInParent<CharacterStats>();
                if (stats == null || stats.transform == transform || seen.Contains(stats)) continue;

                bool valid = stats is HighflyLabDummyStats;
                if (!valid)
                {
                    try { valid = stats.CompareTag("Enemy"); } catch { }
                }
                if (!valid) continue;

                Vector3 to = stats.transform.position - transform.position;
                to.y = 0f;
                if (Vector3.Dot(dir, to.normalized) < 0.12f) continue;

                seen.Add(stats);
                DealDirect(stats, damage, source);
            }
        }

        private Vector3 FacingDirection()
        {
            Vector2 move = _player != null ? _player.HighflyMobileMoveInput : Vector2.zero;

            if (move.sqrMagnitude > 0.025f && _player.cameraTransform != null)
            {
                Vector3 forward = _player.cameraTransform.forward;
                Vector3 right = _player.cameraTransform.right;
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();

                Vector3 world = forward * move.y + right * move.x;
                if (world.sqrMagnitude > 0.001f)
                    return world.normalized;
            }

            if (_player != null && _player.IsLockOn && _player.LockOnTarget != null)
            {
                Vector3 lockDir = _player.LockOnTarget.position - transform.position;
                lockDir.y = 0f;
                if (lockDir.sqrMagnitude > 0.001f)
                    return lockDir.normalized;
            }

            return transform.forward;
        }

        private IEnumerator HitStop(float duration)
        {
            if (_hitStopRunning) yield break;
            _hitStopRunning = true;

            float previous = Time.timeScale;
            Time.timeScale = 0.08f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = previous <= 0f ? 1f : previous;
            _hitStopRunning = false;
        }

        private IEnumerator FovPunch(float amount, float duration)
        {
            Camera cam = Camera.main;
            if (cam == null) yield break;

            float original = cam.fieldOfView;
            float half = duration * 0.5f;
            float start = Time.unscaledTime;

            while (Time.unscaledTime - start < half)
            {
                float t = (Time.unscaledTime - start) / Mathf.Max(0.001f, half);
                cam.fieldOfView = Mathf.Lerp(original, original - amount, t);
                yield return null;
            }

            start = Time.unscaledTime;
            while (Time.unscaledTime - start < half)
            {
                float t = (Time.unscaledTime - start) / Mathf.Max(0.001f, half);
                cam.fieldOfView = Mathf.Lerp(original - amount, original, t);
                yield return null;
            }

            cam.fieldOfView = original;
        }

        private void CleanupSummons()
        {
            for (int i = _summons.Count - 1; i >= 0; i--)
            {
                if (_summons[i] != null)
                    Destroy(_summons[i].gameObject);
            }
            _summons.Clear();
        }

        // ------------------------- VFX helpers -----------------------------
        private void SpawnTelegraphArc(Vector3 position, Vector3 dir, Color color, float scale)
        {
            SpawnSlash(dir, new Color(color.r, color.g, color.b, 0.35f), 0f, scale);
        }

        private void SpawnSlash(Vector3 dir, Color color, float roll, float length)
        {
            var go = new GameObject("HF_SLASH");
            go.transform.position = transform.position + Vector3.up * 1.05f + dir * 1.05f;
            go.transform.rotation = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(0f, 0f, roll);

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.positionCount = 5;
            lr.widthMultiplier = 0.12f;
            lr.numCapVertices = 4;
            lr.sharedMaterial = HighflyLabVisuals.CreateFxMaterial(color);

            float h = 0.72f;
            lr.SetPosition(0, new Vector3(-length * 0.45f, -h, 0f));
            lr.SetPosition(1, new Vector3(-length * 0.22f, h * 0.25f, 0f));
            lr.SetPosition(2, new Vector3(0f, h, 0f));
            lr.SetPosition(3, new Vector3(length * 0.22f, h * 0.25f, 0f));
            lr.SetPosition(4, new Vector3(length * 0.45f, -h, 0f));

            StartCoroutine(FadeLine(lr, 0.16f));
        }

        private void SpawnChain(Vector3 from, Vector3 to, Color color, float life)
        {
            var go = new GameObject("HF_SHADOW_CHAIN");
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 9;
            lr.widthMultiplier = 0.055f;
            lr.numCapVertices = 3;
            lr.sharedMaterial = HighflyLabVisuals.CreateFxMaterial(color);

            Vector3 delta = to - from;
            for (int i = 0; i < 9; i++)
            {
                float t = i / 8f;
                Vector3 p = Vector3.Lerp(from, to, t);
                p += Vector3.up * Mathf.Sin(t * Mathf.PI * 4f) * 0.08f;
                p += transform.right * Mathf.Sin(t * Mathf.PI * 6f) * 0.05f;
                lr.SetPosition(i, p);
            }

            Destroy(go, life);
        }

        private void SpawnGhostEcho(Vector3 position, Color color)
        {
            var echo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            echo.name = "HF_PHANTOM_ECHO";
            Destroy(echo.GetComponent<Collider>());
            echo.transform.position = position + Vector3.up * 0.9f;
            echo.transform.rotation = transform.rotation;
            echo.transform.localScale = new Vector3(0.38f, 0.82f, 0.38f);

            var r = echo.GetComponent<Renderer>();
            if (r != null)
                r.sharedMaterial = HighflyLabVisuals.CreateMaterial(
                    new Color(color.r * 0.16f, color.g * 0.16f, color.b * 0.16f, 1f),
                    color);

            Destroy(echo, 0.22f);
        }

        private void SpawnImpactRing(Vector3 position, Color color, float radius)
        {
            var go = new GameObject("HF_IMPACT_RING");
            go.transform.position = position;

            var lr = go.AddComponent<LineRenderer>();
            lr.loop = true;
            lr.useWorldSpace = false;
            lr.positionCount = 48;
            lr.widthMultiplier = 0.065f;
            lr.sharedMaterial = HighflyLabVisuals.CreateFxMaterial(color);

            for (int i = 0; i < 48; i++)
            {
                float a = (i / 48f) * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0.04f, Mathf.Sin(a) * radius));
            }

            Destroy(go, 0.20f);
        }

        private void SpawnOrbitSpark(Color color)
        {
            float a = Time.unscaledTime * 6f;
            Vector3 p = transform.position + Vector3.up * (0.6f + Mathf.Sin(a * 0.7f) * 0.35f);
            p += new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 1.15f;

            var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "HF_VITAL_ORB";
            Destroy(orb.GetComponent<Collider>());
            orb.transform.position = p;
            orb.transform.localScale = Vector3.one * 0.10f;

            var r = orb.GetComponent<Renderer>();
            if (r != null)
                r.sharedMaterial = HighflyLabVisuals.CreateMaterial(color * 0.22f, color);

            Destroy(orb, 0.34f);
        }

        private IEnumerator FadeLine(LineRenderer lr, float duration)
        {
            if (lr == null) yield break;

            Color baseColor = lr.startColor;
            float start = Time.unscaledTime;

            while (lr != null && Time.unscaledTime - start < duration)
            {
                float t = (Time.unscaledTime - start) / duration;
                float alpha = 1f - t;
                lr.startColor = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                lr.endColor = lr.startColor;
                yield return null;
            }

            if (lr != null)
                Destroy(lr.gameObject);
        }
    }

    public sealed class HighflyLabStatusReceiver : MonoBehaviour
    {
        private NavMeshAgent _agent;
        private float _rootUntil;
        private float _markUntil;
        private bool _cachedWasStopped;

        public bool IsShadowMarked => Time.unscaledTime < _markUntil;

        private void Awake()
        {
            _agent = GetComponentInParent<NavMeshAgent>();
        }

        public void ApplyRoot(float seconds)
        {
            _rootUntil = Mathf.Max(_rootUntil, Time.unscaledTime + seconds);

            if (_agent != null && _agent.enabled)
            {
                _cachedWasStopped = _agent.isStopped;
                _agent.isStopped = true;
            }
        }

        public void ApplyShadowMark(float seconds)
        {
            _markUntil = Mathf.Max(_markUntil, Time.unscaledTime + seconds);
        }

        private void Update()
        {
            if (_agent != null && _agent.enabled && _rootUntil > 0f && Time.unscaledTime >= _rootUntil)
            {
                _rootUntil = 0f;
                _agent.isStopped = _cachedWasStopped;
            }
        }
    }

    public sealed class HighflyShadowMinion : MonoBehaviour
    {
        private HighflyPremiumSkillRuntime _owner;
        private Transform _master;
        private float _side;
        private float _expiresAt;
        private float _nextAttack;
        private float _phase;

        public void Initialize(HighflyPremiumSkillRuntime owner, Transform master, float side, float lifeSeconds)
        {
            _owner = owner;
            _master = master;
            _side = side;
            _expiresAt = Time.unscaledTime + lifeSeconds;
            _phase = side > 0f ? 0f : Mathf.PI;
        }

        private void Update()
        {
            if (_owner == null || _master == null || Time.unscaledTime >= _expiresAt)
            {
                Destroy(gameObject);
                return;
            }

            float a = Time.unscaledTime * 1.7f + _phase;
            Vector3 desired = _master.position +
                              _master.right * (Mathf.Cos(a) * 1.8f) +
                              _master.forward * (Mathf.Sin(a) * 1.1f) +
                              Vector3.up * 0.9f;

            transform.position = Vector3.Lerp(
                transform.position,
                desired,
                1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));

            CharacterStats target = _owner.FindBestTarget(10f, 180f);
            if (target != null)
            {
                Vector3 look = target.transform.position - transform.position;
                look.y = 0f;
                if (look.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);

                if (Time.unscaledTime >= _nextAttack)
                {
                    _nextAttack = Time.unscaledTime + 0.82f;
                    StartCoroutine(Attack(target));
                }
            }
        }

        private IEnumerator Attack(CharacterStats target)
        {
            if (target == null) yield break;

            Vector3 from = transform.position + Vector3.up * 0.45f;
            Vector3 to = target.transform.position + Vector3.up * 0.8f;

            var go = new GameObject("HF_SHADOW_BOLT");
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.widthMultiplier = 0.075f;
            lr.sharedMaterial = HighflyLabVisuals.CreateFxMaterial(new Color(0.48f, 0.18f, 1f, 1f));
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);

            yield return new WaitForSecondsRealtime(0.08f);

            if (target != null)
                _owner.DealDirect(target, 13f, "Sombra");

            Destroy(go, 0.10f);
        }
    }

    public static class HighflyLabVisuals
    {
        public static Material CreateMaterial(Color baseColor, Color emission)
        {
            Shader shader =
                Shader.Find("Universal Render Pipeline/Lit") ??
                Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("Sprites/Default");

            var mat = new Material(shader);
            SetColor(mat, "_BaseColor", baseColor);
            SetColor(mat, "_Color", baseColor);

            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission * 2.2f);
            }

            return mat;
        }

        public static Material CreateFxMaterial(Color color)
        {
            Shader shader =
                Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("Sprites/Default");

            var mat = new Material(shader);
            SetColor(mat, "_BaseColor", color);
            SetColor(mat, "_Color", color);

            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 3f);
            }

            return mat;
        }

        private static void SetColor(Material mat, string property, Color color)
        {
            if (mat != null && mat.HasProperty(property))
                mat.SetColor(property, color);
        }
    }
}
