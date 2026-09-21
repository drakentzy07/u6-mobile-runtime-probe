using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Highfly.SkillLab
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController))]
    public sealed class HighflyAdvancedSkillRuntime : MonoBehaviour
    {
        public static HighflyAdvancedSkillRuntime Instance { get; private set; }

        private PlayerController _player;
        private PlayerStats _stats;
        private CharacterController _cc;
        private HighflyPremiumSkillRuntime _premium;

        private float _cdPhantomDance;
        private float _cdShadowLink;
        private float _cdAbyssal;
        private float _cdVitalDomain;
        private float _cdJudgment;

        private float _shadowLinkUntil;
        private bool _hitStopRunning;

        public bool ShadowLinkActive => Time.unscaledTime < _shadowLinkUntil;

        private void Awake()
        {
            Instance = this;
            _player = GetComponent<PlayerController>();
            _stats = GetComponent<PlayerStats>();
            _cc = GetComponent<CharacterController>();
            _premium = GetComponent<HighflyPremiumSkillRuntime>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void ForcePreview(HighflyPremiumSkillId id)
        {
            switch (id)
            {
                case HighflyPremiumSkillId.PhantomTwinDance:
                    _cdPhantomDance = -99f;
                    break;
                case HighflyPremiumSkillId.ShadowLink:
                    _cdShadowLink = -99f;
                    break;
                case HighflyPremiumSkillId.AbyssalShackle:
                    _cdAbyssal = -99f;
                    break;
                case HighflyPremiumSkillId.VitalDomain:
                    _cdVitalDomain = -99f;
                    break;
                case HighflyPremiumSkillId.ShadowJudgment:
                    _cdJudgment = -99f;
                    break;
            }

            Trigger(id);
        }

        public float GetCooldownRemaining(HighflyPremiumSkillId id)
        {
            float now = Time.unscaledTime;

            switch (id)
            {
                case HighflyPremiumSkillId.PhantomTwinDance:
                    return Mathf.Max(0f, _cdPhantomDance - now);
                case HighflyPremiumSkillId.ShadowLink:
                    return Mathf.Max(0f, _cdShadowLink - now);
                case HighflyPremiumSkillId.AbyssalShackle:
                    return Mathf.Max(0f, _cdAbyssal - now);
                case HighflyPremiumSkillId.VitalDomain:
                    return Mathf.Max(0f, _cdVitalDomain - now);
                case HighflyPremiumSkillId.ShadowJudgment:
                    return Mathf.Max(0f, _cdJudgment - now);
            }

            return 0f;
        }

        public void Trigger(HighflyPremiumSkillId id)
        {
            switch (id)
            {
                case HighflyPremiumSkillId.PhantomTwinDance:
                    TriggerPhantomTwinDance();
                    break;
                case HighflyPremiumSkillId.ShadowLink:
                    TriggerShadowLink();
                    break;
                case HighflyPremiumSkillId.AbyssalShackle:
                    TriggerAbyssalShackle();
                    break;
                case HighflyPremiumSkillId.VitalDomain:
                    TriggerVitalDomain();
                    break;
                case HighflyPremiumSkillId.ShadowJudgment:
                    TriggerShadowJudgment();
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
        // S6 — DANZA FANTASMA
        // Fusion: Twin Dance + Phantom Step.
        // Three spatial cuts, each hit leaves an afterimage and lightning seam.
        // ------------------------------------------------------------------
        private void TriggerPhantomTwinDance()
        {
            if (!CanAct() || Time.unscaledTime < _cdPhantomDance || _premium == null)
                return;

            CharacterStats target = _premium.FindBestTargetStrict(10.5f, 140f);
            if (target == null)
            {
                HighflySkillLabMetrics.RecordAction("S6 • DANZA FANTASMA (SIN OBJETIVO)", 0);
                return;
            }

            _cdPhantomDance = Time.unscaledTime + 5.0f;
            HighflySkillLabMetrics.RecordAction("S6 • DANZA FANTASMA", 0);
            StartCoroutine(PhantomTwinDanceRoutine(target));
        }

        private IEnumerator PhantomTwinDanceRoutine(CharacterStats target)
        {
            if (target == null) yield break;

            var status = EnsureStatus(target);
            status?.ApplyRoot(1.20f);

            Color cyan = new Color(0.10f, 0.74f, 1f, 1f);
            Color violet = new Color(0.62f, 0.20f, 1f, 1f);

            _player.HighflyMobileAttack();

            // Snapshot the target once. The dance now orbits the SAME target
            // instead of feeling like it re-acquires something mid-sequence.
            Transform lockedTarget = target.transform;

            for (int strike = 0; strike < 3; strike++)
            {
                if (lockedTarget == null || target == null)
                    yield break;

                Vector3 old = transform.position;
                Vector3 targetPos = lockedTarget.position;

                Vector3 radial =
                    transform.position -
                    targetPos;
                radial.y = 0f;

                if (radial.sqrMagnitude < 0.01f)
                    radial = -lockedTarget.forward;

                radial.Normalize();

                Vector3 side =
                    Vector3.Cross(Vector3.up, radial).normalized;

                Vector3 desired;
                if (strike == 0)
                    desired = targetPos + side * 1.85f - radial * 0.30f;
                else if (strike == 1)
                    desired = targetPos - side * 1.85f - radial * 0.22f;
                else
                    desired = targetPos + radial * 1.95f;

                desired.y = old.y;

                Color strikeColor =
                    strike == 1 ? violet : cyan;

                HighflyPremiumFx.SpawnAfterImage(
                    transform,
                    strikeColor,
                    0.36f);

                Warp(desired);

                Vector3 fxFrom = old + Vector3.up * 0.92f;
                Vector3 fxTo = transform.position + Vector3.up * 0.92f;

                HighflyAnimeFx.SpawnLightningBurst(
                    fxFrom,
                    fxTo,
                    strikeColor,
                    4,
                    0.22f);

                FaceTarget(target);

                Vector3 dir =
                    FlatDirection(
                        transform.position,
                        lockedTarget.position);

                float roll =
                    strike == 0 ? -40f :
                    strike == 1 ? 40f :
                    0f;

                HighflyPremiumFx.AttachWeaponTrail(
                    _player,
                    Color.white,
                    strikeColor,
                    0.36f,
                    0.24f);

                HighflyAnimeFx.SpawnBladeScar(
                    transform.position + Vector3.up * 1.02f + dir * 1.04f,
                    dir,
                    strikeColor,
                    roll,
                    strike == 2 ? 3.85f : 3.10f,
                    strike == 2 ? 0.42f : 0.30f,
                    strike == 2 ? 0.34f : 0.29f);

                _premium.DealDirect(
                    target,
                    strike == 2 ? 54f : 29f,
                    "Danza-Fantasma");

                // Slightly slower than v0.6 so every teleport/cut can actually
                // be read on a phone screen.
                yield return new WaitForSecondsRealtime(
                    strike == 2 ? 0.115f : 0.155f);
            }

            if (target != null)
            {
                Vector3 impact =
                    target.transform.position +
                    Vector3.up * 0.92f;

                HighflyAnimeFx.SpawnImpactCross(
                    impact,
                    transform.forward,
                    violet,
                    3.35f);

                HighflyPremiumFx.SpawnResource(
                    "EnergyExplosion",
                    impact,
                    Quaternion.identity,
                    0.42f,
                    1.18f);
            }

            yield return HitStop(0.060f);
        }

        // ------------------------------------------------------------------
        // S7 — VÍNCULO UMBRÍO
        // Fusion: Shadow Call + Vital Pact.
        // Keeps the shadow formation, grants drain and immediately orders a
        // coordinated strike. Basic/S1 echoes remain synchronized.
        // ------------------------------------------------------------------
        private void TriggerShadowLink()
        {
            if (!CanAct() || Time.unscaledTime < _cdShadowLink || _premium == null)
                return;

            _cdShadowLink = Time.unscaledTime + 11f;
            _shadowLinkUntil = Time.unscaledTime + 9f;

            HighflySkillLabMetrics.RecordAction("S7 • VÍNCULO UMBRÍO", 0);

            _premium.EnsureShadowFormation();

            Color shadowLife = new Color(0.54f, 0.15f, 0.86f, 1f);
            HighflyAnimeFx.SpawnGrandMagicCircle(transform, shadowLife, 2.45f, 9f);
            HighflyPremiumFx.SpawnResource(
                "PlasmaExplosion",
                transform.position + Vector3.up * 0.7f,
                Quaternion.identity,
                0.36f,
                1.2f,
                shadowLife);

            _premium.CommandShadowFormationStrike(true);

            if (_stats != null)
                _stats.RestoreEgo(_stats.maxEgo * 0.08f);
        }

        // ------------------------------------------------------------------
        // S8 — GRILLETE ABISAL
        // Evolution of Shadow Shackle: multi-target shadow hands + convergence.
        // ------------------------------------------------------------------
        private void TriggerAbyssalShackle()
        {
            if (!CanAct() || Time.unscaledTime < _cdAbyssal || _premium == null)
                return;

            List<CharacterStats> targets = FindTargets(12f, 3);
            if (targets.Count == 0)
            {
                HighflySkillLabMetrics.RecordAction("S8 • GRILLETE ABISAL (SIN OBJETIVO)", 0);
                return;
            }

            _cdAbyssal = Time.unscaledTime + 7f;
            HighflySkillLabMetrics.RecordAction("S8 • GRILLETE ABISAL", 0);
            StartCoroutine(AbyssalShackleRoutine(targets));
        }

        private IEnumerator AbyssalShackleRoutine(List<CharacterStats> targets)
        {
            Color abyss = new Color(0.34f, 0.04f, 0.60f, 1f);
            Vector3 convergence =
                transform.position +
                FlatForward() * 3.0f;

            for (int i = 0; i < targets.Count; i++)
            {
                CharacterStats target = targets[i];
                if (target == null) continue;

                var status = EnsureStatus(target);
                status?.ApplyRoot(3.0f);
                status?.ApplyShadowMark(6.0f);

                HighflyAnimeFx.SpawnShadowClaw(target.transform, 1.34f);
                HighflyAnimeFx.SpawnShadowChain(transform, target.transform, 1.12f);

                _premium.DealDirect(target, 24f, "Grillete-Abisal");
            }

            yield return new WaitForSecondsRealtime(0.18f);

            float begin = Time.unscaledTime;
            const float pullTime = 0.48f;

            Vector3[] starts = new Vector3[targets.Count];
            for (int i = 0; i < targets.Count; i++)
                starts[i] = targets[i] != null ? targets[i].transform.position : Vector3.zero;

            while (Time.unscaledTime - begin < pullTime)
            {
                float t = (Time.unscaledTime - begin) / pullTime;
                float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);

                for (int i = 0; i < targets.Count; i++)
                {
                    CharacterStats target = targets[i];
                    if (target == null) continue;

                    Vector3 slot =
                        convergence +
                        transform.right * ((i - (targets.Count - 1) * 0.5f) * 1.05f);
                    slot.y = starts[i].y;

                    target.transform.position = Vector3.Lerp(starts[i], slot, eased);
                }

                yield return null;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                CharacterStats target = targets[i];
                if (target == null) continue;

                Vector3 p = target.transform.position + Vector3.up * 0.85f;
                _premium.DealDirect(target, 18f, "Colapso-Abisal");
                HighflyPremiumFx.SpawnResource("Sparks", p, Quaternion.identity, 0.48f, 0.55f, abyss);
            }

            HighflyPremiumFx.SpawnResource(
                "PlasmaExplosion",
                convergence + Vector3.up * 0.55f,
                Quaternion.identity,
                0.52f,
                1.15f,
                abyss);

            yield return HitStop(0.045f);
        }

        // ------------------------------------------------------------------
        // S9 — DOMINIO VITAL
        // Evolution of Vital Pact. A sustained ritual field that drains enemies
        // and restores the hunter while inside the circle.
        // ------------------------------------------------------------------
        private void TriggerVitalDomain()
        {
            if (!CanAct() || Time.unscaledTime < _cdVitalDomain || _premium == null)
                return;

            _cdVitalDomain = Time.unscaledTime + 15f;
            HighflySkillLabMetrics.RecordAction("S9 • DOMINIO VITAL", 0);
            StartCoroutine(VitalDomainRoutine());
        }

        private IEnumerator VitalDomainRoutine()
        {
            const float radius = 4.6f;
            const float duration = 8.5f;

            Color vital = new Color(0.12f, 0.78f, 0.40f, 1f);
            Vector3 center = transform.position;

            var anchor = new GameObject("HF_VITAL_DOMAIN_ANCHOR");
            anchor.transform.position = center;

            HighflyAnimeFx.SpawnGrandMagicCircle(anchor.transform, vital, radius, duration);

            HighflyPremiumFx.SpawnResource(
                "ParticlesLight",
                center + Vector3.up * 0.15f,
                Quaternion.identity,
                1.10f,
                2.0f,
                new Color(0.72f, 1f, 0.78f, 0.82f));

            float end = Time.unscaledTime + duration;
            while (Time.unscaledTime < end)
            {
                if (_stats != null)
                    _stats.RestoreEgo(_stats.maxEgo * 0.025f);

                List<CharacterStats> targets = FindTargetsFrom(center, radius, 8);
                for (int i = 0; i < targets.Count; i++)
                {
                    CharacterStats target = targets[i];
                    if (target == null) continue;

                    _premium.DealDirect(target, 8f, "Dominio-Vital");

                    Vector3 from = target.transform.position + Vector3.up * 0.75f;
                    Vector3 to = transform.position + Vector3.up * 1.0f;
                    HighflyAnimeFx.SpawnLightningBurst(
                        from,
                        to,
                        new Color(0.36f, 0.94f, 0.56f, 1f),
                        2,
                        0.13f);
                }

                yield return new WaitForSecondsRealtime(0.95f);
            }

            if (anchor != null)
                Destroy(anchor);
        }

        // ------------------------------------------------------------------
        // S10 — JUICIO DE LA SOMBRA
        // Shadow finisher: bind -> V formation -> synchronized three-way cut.
        // ------------------------------------------------------------------
        private void TriggerShadowJudgment()
        {
            if (!CanAct() || Time.unscaledTime < _cdJudgment || _premium == null)
                return;

            CharacterStats target = _premium.FindBestTarget(12f, 210f);
            if (target == null)
            {
                HighflySkillLabMetrics.RecordAction("S10 • JUICIO DE LA SOMBRA (SIN OBJETIVO)", 0);
                return;
            }

            _cdJudgment = Time.unscaledTime + 14f;
            HighflySkillLabMetrics.RecordAction("S10 • JUICIO DE LA SOMBRA", 0);
            StartCoroutine(ShadowJudgmentRoutine(target));
        }

        private IEnumerator ShadowJudgmentRoutine(CharacterStats target)
        {
            if (target == null) yield break;

            Color shadow = new Color(0.44f, 0.08f, 0.78f, 1f);

            EnsureStatus(target)?.ApplyRoot(1.4f);
            HighflyAnimeFx.SpawnShadowClaw(target.transform, 1.30f);
            HighflyAnimeFx.SpawnShadowChain(transform, target.transform, 1.05f);

            Vector3 targetPos = target.transform.position;
            Vector3 back = -FlatDirectionTo(targetPos);
            Vector3 side = Vector3.Cross(Vector3.up, back).normalized;

            GameObject left = HighflyPremiumFx.SpawnShadowKnight(
                targetPos + back * 2.4f - side * 2.0f,
                Quaternion.LookRotation(-back, Vector3.up));

            GameObject right = HighflyPremiumFx.SpawnShadowKnight(
                targetPos + back * 2.4f + side * 2.0f,
                Quaternion.LookRotation(-back, Vector3.up));

            HighflyPremiumFx.SpawnResource(
                "Sparks",
                targetPos + Vector3.up * 0.78f,
                Quaternion.identity,
                0.36f,
                0.46f,
                shadow);

            yield return new WaitForSecondsRealtime(0.22f);

            Vector3 hunterStart = transform.position;
            Vector3 hunterEnd = targetPos - back * 1.55f;
            hunterEnd.y = hunterStart.y;

            HighflyPremiumFx.SpawnAfterImage(transform, shadow, 0.30f);
            Warp(hunterEnd);
            FaceTarget(target);

            if (left != null)
                left.transform.position = targetPos - side * 1.35f + Vector3.up * 0.02f;
            if (right != null)
                right.transform.position = targetPos + side * 1.35f + Vector3.up * 0.02f;

            Vector3 toward = FlatDirectionTo(targetPos);

            HighflyAnimeFx.SpawnBladeCut(
                transform.position + Vector3.up * 1.0f + toward * 0.8f,
                toward,
                Color.white,
                0f,
                3.0f,
                0.46f,
                0.18f);

            if (left != null)
            {
                Vector3 ld = FlatDirection(left.transform.position, targetPos);
                HighflyAnimeFx.SpawnBladeCut(
                    left.transform.position + Vector3.up * 1.0f + ld * 0.75f,
                    ld,
                    shadow,
                    42f,
                    2.7f,
                    0.40f,
                    0.19f);
            }

            if (right != null)
            {
                Vector3 rd = FlatDirection(right.transform.position, targetPos);
                HighflyAnimeFx.SpawnBladeCut(
                    right.transform.position + Vector3.up * 1.0f + rd * 0.75f,
                    rd,
                    shadow,
                    -42f,
                    2.7f,
                    0.40f,
                    0.19f);
            }

            _premium.DealDirect(target, 32f, "Juicio-I");
            yield return new WaitForSecondsRealtime(0.055f);
            _premium.DealDirect(target, 32f, "Juicio-II");
            yield return new WaitForSecondsRealtime(0.055f);
            _premium.DealDirect(target, 72f, "Juicio-Final");

            HighflyAnimeFx.SpawnImpactCross(
                targetPos + Vector3.up * 0.90f,
                toward,
                shadow,
                3.6f);

            HighflyPremiumFx.SpawnResource(
                "EnergyExplosion",
                targetPos + Vector3.up * 0.75f,
                Quaternion.identity,
                0.62f,
                1.6f);

            yield return HitStop(0.075f);

            if (left != null) Destroy(left, 0.12f);
            if (right != null) Destroy(right, 0.12f);
        }

        // --------------------------- helpers ------------------------------

        private HighflyLabStatusReceiver EnsureStatus(CharacterStats target)
        {
            if (target == null) return null;

            var status = target.GetComponent<HighflyLabStatusReceiver>();
            if (status == null)
                status = target.gameObject.AddComponent<HighflyLabStatusReceiver>();

            return status;
        }

        private List<CharacterStats> FindTargets(float radius, int maxCount)
        {
            return FindTargetsFrom(transform.position, radius, maxCount);
        }

        private List<CharacterStats> FindTargetsFrom(Vector3 center, float radius, int maxCount)
        {
            CharacterStats[] all =
                UnityEngine.Object.FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);

            var scored = new List<Tuple<float, CharacterStats>>();

            for (int i = 0; i < all.Length; i++)
            {
                CharacterStats stats = all[i];
                if (!IsValidTarget(stats)) continue;

                float sqr = (stats.transform.position - center).sqrMagnitude;
                if (sqr > radius * radius) continue;

                scored.Add(new Tuple<float, CharacterStats>(sqr, stats));
            }

            scored.Sort((a, b) => a.Item1.CompareTo(b.Item1));

            var result = new List<CharacterStats>();
            for (int i = 0; i < scored.Count && i < maxCount; i++)
                result.Add(scored[i].Item2);

            return result;
        }

        private bool IsValidTarget(CharacterStats stats)
        {
            if (stats == null || stats.transform == transform) return false;
            if (stats is HighflyLabDummyStats) return true;

            try { return stats.CompareTag("Enemy"); }
            catch { return false; }
        }

        private void FaceTarget(CharacterStats target)
        {
            if (target == null) return;

            Vector3 dir = FlatDirectionTo(target.transform.position);
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }

        private Vector3 FlatDirectionTo(Vector3 position)
        {
            return FlatDirection(transform.position, position);
        }

        private static Vector3 FlatDirection(Vector3 from, Vector3 to)
        {
            Vector3 dir = to - from;
            dir.y = 0f;
            return dir.sqrMagnitude > 0.001f ? dir.normalized : Vector3.forward;
        }

        private Vector3 FlatForward()
        {
            Vector3 f = transform.forward;
            f.y = 0f;
            return f.sqrMagnitude > 0.001f ? f.normalized : Vector3.forward;
        }

        private void Warp(Vector3 position)
        {
            bool enabled = _cc != null && _cc.enabled;
            if (_cc != null && enabled) _cc.enabled = false;
            transform.position = position;
            if (_cc != null && enabled) _cc.enabled = true;
        }

        private IEnumerator HitStop(float duration)
        {
            if (_hitStopRunning) yield break;
            _hitStopRunning = true;

            float old = Time.timeScale;
            Time.timeScale = 0.06f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = old <= 0f ? 1f : old;
            _hitStopRunning = false;
        }
    }
}
