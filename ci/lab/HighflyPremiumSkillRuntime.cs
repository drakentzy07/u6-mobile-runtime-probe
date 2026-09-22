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
        ShadowCall = 5,

        // Tanda II — visible in the loadout menu, implemented in the next LAB pass.
        PhantomTwinDance = 6,
        ShadowLink = 7,
        AbyssalShackle = 8,
        VitalDomain = 9,
        ShadowJudgment = 10,

        EclipseRend = 11,
        ReturnWall = 12,
        VoraciousEcho = 13,

        // v0.9 APEX PASS — distinct mechanical prototypes.
        GravityZero = 14,
        MomentSight = 15,
        FormulaDrift = 16,
        SevenSinker = 17,
        ScrapBuild = 18,
        VictimArts = 19,
        BeastPossession = 20,
        DemonStrike = 21,
        SpiritArmament = 22,
        ShadowCreation = 23,
        TemporalCut = 24,
        MemoryRelease = 25,
        BoundlessMassacre = 26,
        TheFool = 27,
        ReserveSpell = 28
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
        private bool _twinQueued;

        private float _cdTwin;
        private float _cdStep;
        private float _cdShackle;
        private float _cdPact;
        private float _cdSummon;

        private float _vitalPactUntil;
        private bool _hitStopRunning;

        private readonly Collider[] _hits = new Collider[48];
        private readonly List<HighflyShadowMinion> _summons = new List<HighflyShadowMinion>();

        private const float TwinReset = 1.25f;
        private const float TwinCadence = 0.24f;
        private const float TwinFinisherCooldown = 1.35f;

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

        private void Update()
        {
            if (_twinQueued &&
                Time.unscaledTime >= _cdTwin &&
                Time.unscaledTime - _lastTwinInput <= TwinReset)
            {
                _twinQueued = false;
                TriggerTwinDance();
            }
        }

        public void ForcePreview(HighflyPremiumSkillId id)
        {
            switch (id)
            {
                case HighflyPremiumSkillId.TwinDance:
                    _cdTwin = -99f;
                    _twinQueued = false;
                    _twinStage = 0;
                    break;
                case HighflyPremiumSkillId.PhantomStep:
                    _cdStep = -99f;
                    break;
                case HighflyPremiumSkillId.ShadowShackle:
                    _cdShackle = -99f;
                    break;
                case HighflyPremiumSkillId.VitalPact:
                    _cdPact = -99f;
                    break;
                case HighflyPremiumSkillId.ShadowCall:
                    _cdSummon = -99f;
                    break;
            }

            if ((int)id <= 5)
            {
                Trigger(id);
                return;
            }

            if ((int)id <= 10)
            {
                HighflyAdvancedSkillRuntime.Instance?.ForcePreview(id);
                return;
            }

            if ((int)id <= 13)
            {
                HighflyReferenceSkillRuntime.Instance?.ForcePreview(id);
                return;
            }

            HighflyApexPassSkillRuntime.Instance?.ForcePreview(id);
        }

        public void ClearPreviewCooldown(HighflyPremiumSkillId id)
        {
            switch (id)
            {
                case HighflyPremiumSkillId.TwinDance:
                    _cdTwin = -99f;
                    _twinQueued = false;
                    break;
                case HighflyPremiumSkillId.PhantomStep:
                    _cdStep = -99f;
                    break;
                case HighflyPremiumSkillId.ShadowShackle:
                    _cdShackle = -99f;
                    break;
                case HighflyPremiumSkillId.VitalPact:
                    _cdPact = -99f;
                    break;
                case HighflyPremiumSkillId.ShadowCall:
                    _cdSummon = -99f;
                    break;
                default:
                    if ((int)id <= 10)
                        HighflyAdvancedSkillRuntime.Instance?.ClearPreviewCooldown(id);
                    else if ((int)id <= 13)
                        HighflyReferenceSkillRuntime.Instance?.ClearPreviewCooldown(id);
                    else
                        HighflyApexPassSkillRuntime.Instance?.ClearPreviewCooldown(id);
                    break;
            }
        }

        public float GetCooldownRemaining(HighflyPremiumSkillId id)
        {
            float now = Time.unscaledTime;

            switch (id)
            {
                case HighflyPremiumSkillId.TwinDance:
                    return Mathf.Max(0f, _cdTwin - now);
                case HighflyPremiumSkillId.PhantomStep:
                    return Mathf.Max(0f, _cdStep - now);
                case HighflyPremiumSkillId.ShadowShackle:
                    return Mathf.Max(0f, _cdShackle - now);
                case HighflyPremiumSkillId.VitalPact:
                    return Mathf.Max(0f, _cdPact - now);
                case HighflyPremiumSkillId.ShadowCall:
                    return Mathf.Max(0f, _cdSummon - now);
            }

            return 0f;
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

                case HighflyPremiumSkillId.PhantomTwinDance:
                case HighflyPremiumSkillId.ShadowLink:
                case HighflyPremiumSkillId.AbyssalShackle:
                case HighflyPremiumSkillId.VitalDomain:
                case HighflyPremiumSkillId.ShadowJudgment:
                    HighflyAdvancedSkillRuntime.Instance?.Trigger(id);
                    break;

                case HighflyPremiumSkillId.EclipseRend:
                case HighflyPremiumSkillId.ReturnWall:
                case HighflyPremiumSkillId.VoraciousEcho:
                    HighflyReferenceSkillRuntime.Instance?.Trigger(id);
                    break;

                case HighflyPremiumSkillId.GravityZero:
                case HighflyPremiumSkillId.MomentSight:
                case HighflyPremiumSkillId.FormulaDrift:
                case HighflyPremiumSkillId.SevenSinker:
                case HighflyPremiumSkillId.ScrapBuild:
                case HighflyPremiumSkillId.VictimArts:
                case HighflyPremiumSkillId.BeastPossession:
                case HighflyPremiumSkillId.DemonStrike:
                case HighflyPremiumSkillId.SpiritArmament:
                case HighflyPremiumSkillId.ShadowCreation:
                case HighflyPremiumSkillId.TemporalCut:
                case HighflyPremiumSkillId.MemoryRelease:
                case HighflyPremiumSkillId.BoundlessMassacre:
                case HighflyPremiumSkillId.TheFool:
                case HighflyPremiumSkillId.ReserveSpell:
                    HighflyApexPassSkillRuntime.Instance?.Trigger(id);
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
            if (!CanAct()) return;

            if (Time.unscaledTime < _cdTwin)
            {
                // One buffered press keeps the three-stage chain reliable on touch.
                _twinQueued = true;
                return;
            }

            if (Time.unscaledTime - _lastTwinInput > TwinReset)
                _twinStage = 0;

            _twinStage = (_twinStage % 3) + 1;
            _lastTwinInput = Time.unscaledTime;
            // Stages 1-2 use a short input cadence, not a fake full cooldown.
            // The real cooldown starts only after the third/finisher activation.
            _cdTwin = Time.unscaledTime +
                (_twinStage >= 3 ? TwinFinisherCooldown : TwinCadence);

            HighflySkillLabMetrics.RecordAction("S1 • DANZA GEMELA", _twinStage);
            StartCoroutine(TwinDanceRoutine(_twinStage));
        }

        private IEnumerator TwinDanceRoutine(int stage)
        {
            Vector3 dir = FacingDirection();
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            _player.HighflyMobileAttack();

            Color cyan = new Color(0.07f, 0.74f, 1f, 1f);
            Color violet = new Color(0.56f, 0.16f, 1f, 1f);

            HighflyPremiumFx.AttachWeaponTrail(
                _player,
                Color.white,
                stage == 2 ? violet : cyan,
                stage == 3 ? 0.52f : 0.42f,
                stage == 3 ? 0.30f : 0.24f);

            EchoTwinDance(stage);

            if (_cc != null)
                _cc.Move(dir * (stage == 3 ? 0.72f : 0.48f));

            yield return new WaitForSecondsRealtime(0.035f);

            Vector3 origin =
                transform.position +
                Vector3.up * 1.02f +
                dir * 1.22f;

            // Every activation is a true dual cut. No stage is allowed to look
            // like one lonely crescent anymore.
            float firstRoll =
                stage == 1 ? -31f :
                stage == 2 ? 34f :
                -48f;

            float secondRoll =
                stage == 1 ? 31f :
                stage == 2 ? -34f :
                48f;

            Color firstColor = stage == 2 ? violet : cyan;
            Color secondColor = stage == 2 ? cyan : violet;

            HighflyAnimeFx.SpawnBladeScar(
                origin,
                dir,
                firstColor,
                firstRoll,
                stage == 3 ? 3.65f : 3.15f,
                stage == 3 ? 0.34f : 0.28f,
                stage == 3 ? 0.32f : 0.27f);

            DealConeDamage(
                stage == 3 ? 22f : 18f,
                stage == 3 ? 3.25f : 2.85f,
                1.30f,
                dir,
                "Gemela-I");

            yield return new WaitForSecondsRealtime(stage == 3 ? 0.050f : 0.070f);

            HighflyAnimeFx.SpawnBladeScar(
                origin + dir * 0.10f,
                dir,
                secondColor,
                secondRoll,
                stage == 3 ? 3.70f : 3.22f,
                stage == 3 ? 0.36f : 0.30f,
                stage == 3 ? 0.34f : 0.29f);

            DealConeDamage(
                stage == 3 ? 24f : 20f,
                stage == 3 ? 3.30f : 2.95f,
                1.34f,
                dir,
                "Gemela-II");

            // Stage 2 adds a fast central blade. Stage 3 becomes the signature
            // four-cut X finisher so the chain escalates visually and mechanically.
            if (stage >= 2)
            {
                yield return new WaitForSecondsRealtime(0.045f);

                HighflyAnimeFx.SpawnBladeScar(
                    origin + dir * 0.18f,
                    dir,
                    Color.white,
                    0f,
                    stage == 3 ? 3.45f : 2.80f,
                    stage == 3 ? 0.22f : 0.18f,
                    0.23f);

                DealConeDamage(
                    stage == 3 ? 20f : 16f,
                    stage == 3 ? 3.25f : 2.80f,
                    1.18f,
                    dir,
                    "Gemela-Core");
            }

            if (stage == 3)
            {
                yield return new WaitForSecondsRealtime(0.040f);

                HighflyAnimeFx.SpawnBladeScar(
                    origin + dir * 0.22f,
                    dir,
                    new Color(0.86f, 0.64f, 1f, 1f),
                    -8f,
                    4.05f,
                    0.42f,
                    0.36f);

                DealConeDamage(44f, 3.55f, 1.52f, dir, "Gemela-Final");

                Vector3 finish =
                    transform.position +
                    dir * 2.20f +
                    Vector3.up * 0.88f;

                HighflyAnimeFx.SpawnImpactCross(
                    finish,
                    dir,
                    violet,
                    3.25f);

                StartCoroutine(FovPunch(5.8f, 0.11f));
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

            // Lucid Roll supplies the real i-frame contract.
            _player.HighflyMobileRoll();

            Color ghost = new Color(0.16f, 0.66f, 1f, 1f);
            float start = Time.unscaledTime;
            float duration = 0.24f;
            float nextEchoAt = 0f;
            Vector3 lastFxPosition = transform.position + Vector3.up * 0.85f;

            HighflyPremiumFx.SpawnResource(
                "ElectricalSparks",
                transform.position + Vector3.up * 0.85f,
                Quaternion.LookRotation(dir),
                0.55f,
                0.75f);

            while (Time.unscaledTime - start < duration)
            {
                float elapsed = Time.unscaledTime - start;

                if (_cc != null)
                    _cc.Move(dir * (9.2f * Time.unscaledDeltaTime));

                if (elapsed >= nextEchoAt)
                {
                    nextEchoAt += 0.052f;

                    Vector3 currentFxPosition = transform.position + Vector3.up * 0.85f;
                    HighflyPremiumFx.SpawnAfterImage(transform, ghost, 0.26f);

                    HighflyAnimeFx.SpawnLightningBurst(
                        lastFxPosition,
                        currentFxPosition,
                        ghost,
                        4,
                        0.18f);

                    lastFxPosition = currentFxPosition;
                }

                yield return null;
            }

            HighflyPremiumFx.SpawnResource(
                "ElectricalSparks",
                transform.position + Vector3.up * 0.85f,
                Quaternion.LookRotation(dir),
                0.70f,
                0.85f);

            HighflyPremiumFx.AttachWeaponTrail(_player, Color.white, ghost, 0.30f, 0.20f);
            SpawnSlash(dir, ghost, 0f, 2.2f);
            DealConeDamage(31f, 2.75f, 1.35f, dir, "Paso-Fantasma");
            SpawnImpactRing(transform.position + dir * 1.2f, ghost, 1.45f);
            StartCoroutine(FovPunch(3.8f, 0.09f));
        }

        // ------------------------------------------------------------------
        // S3 — GRILLETE UMBRÍO
        // Targeting / CC / Debuff / Shadow Mark family.
        // ------------------------------------------------------------------
        private void TriggerShadowShackle()
        {
            if (!CanAct() || Time.unscaledTime < _cdShackle) return;

            CharacterStats target = FindBestTarget(11.5f, 170f);
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

            Color shadow = new Color(0.44f, 0.08f, 0.82f, 1f);

            // Premium identity: a readable articulated five-finger shadow hand
            // closes around the target, then one visible chain retracts it.
            HighflyAnimeFx.SpawnShadowClaw(target.transform, 1.28f);
            HighflyAnimeFx.SpawnShadowChain(transform, target.transform, 1.08f);

            var status = target.GetComponent<HighflyLabStatusReceiver>();
            if (status == null)
                status = target.gameObject.AddComponent<HighflyLabStatusReceiver>();

            status.ApplyRoot(2.80f);
            status.ApplyShadowMark(5.8f);

            Vector3 lockPoint =
                target.transform.position +
                Vector3.up * 0.88f;

            HighflyPremiumFx.SpawnResource(
                "Sparks",
                lockPoint,
                Quaternion.identity,
                0.34f,
                0.38f,
                new Color(0.62f, 0.18f, 1f, 1f));

            DealDirect(target, 24f, "Grillete-Agarre");

            // Let the claw visibly close before the pull starts.
            yield return new WaitForSecondsRealtime(0.22f);

            yield return StartCoroutine(
                PullTargetTowardPlayer(
                    target,
                    0.52f,
                    2.05f));

            if (target != null)
            {
                Vector3 end =
                    target.transform.position +
                    Vector3.up * 0.88f;

                HighflyAnimeFx.SpawnImpactCross(
                    end,
                    FacingDirection(),
                    shadow,
                    1.85f);

                DealDirect(target, 18f, "Grillete-Pull");
            }
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

            Color life = new Color(0.16f, 0.92f, 0.52f, 1f);
            SpawnImpactRing(transform.position + Vector3.up * 0.15f, life, 3.2f);
            HighflyPremiumFx.SpawnResource(
                "ParticlesLight",
                transform.position + Vector3.up * 0.6f,
                Quaternion.identity,
                0.82f,
                2.25f,
                new Color(0.58f, 1f, 0.70f, 1f));
            HighflyPremiumFx.SpawnVitalAura(transform, 8f);
            HighflyAnimeFx.SpawnGrandMagicCircle(
                transform,
                new Color(0.10f, 0.78f, 0.42f, 1f),
                2.25f,
                8f);
            StartCoroutine(VitalAuraRoutine(life));
        }

        private IEnumerator VitalAuraRoutine(Color c)
        {
            float end = _vitalPactUntil;

            while (Time.unscaledTime < end)
            {
                HighflyPremiumFx.SpawnResource(
                    "ParticlesLight",
                    transform.position + Vector3.up * 0.95f,
                    Quaternion.identity,
                    0.34f,
                    0.85f,
                    new Color(0.68f, 1f, 0.76f, 0.78f));

                yield return new WaitForSecondsRealtime(1.25f);
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
                Vector3 spawnPos =
                    transform.position +
                    transform.right * (i == 0 ? -1.45f : 1.45f) +
                    transform.forward * 1.65f;

                HighflyPremiumFx.SpawnResource(
                    "Sparks",
                    spawnPos + Vector3.up * 0.20f,
                    Quaternion.identity,
                    0.34f,
                    0.50f,
                    new Color(0.34f, 0.08f, 0.62f, 1f));

                GameObject go = HighflyPremiumFx.SpawnShadowKnight(
                    spawnPos,
                    transform.rotation);

                if (go == null)
                {
                    go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    go.name = "HIGHFLY_SHADOW_FALLBACK_" + (i + 1);
                    Destroy(go.GetComponent<Collider>());
                    go.transform.position = spawnPos + Vector3.up * 0.9f;
                    go.transform.localScale = new Vector3(0.45f, 0.90f, 0.45f);
                }

                var minion = go.AddComponent<HighflyShadowMinion>();
                minion.Initialize(this, transform, i == 0 ? -1f : 1f, 9.5f);
                _summons.Add(minion);
            }
        }

        public void EnsureShadowFormation()
        {
            bool anyActive = false;

            for (int i = _summons.Count - 1; i >= 0; i--)
            {
                if (_summons[i] == null)
                {
                    _summons.RemoveAt(i);
                    continue;
                }

                anyActive = true;
            }

            if (anyActive)
                return;

            _cdSummon = -99f;
            TriggerShadowCall();
        }

        public void CommandShadowFormationStrike(bool heavy)
        {
            if (_summons.Count == 0)
            {
                EnsureShadowFormation();
                return;
            }

            CharacterStats target = FindBestTarget(10.5f, 200f);

            for (int i = 0; i < _summons.Count; i++)
            {
                HighflyShadowMinion minion = _summons[i];
                if (minion == null) continue;

                minion.CommandMirrorAttack(
                    target,
                    0.035f + i * 0.050f,
                    heavy);
            }
        }

        public void EchoBasicAttack()
        {
            if (_summons.Count == 0) return;

            CharacterStats target = FindBestTarget(8.5f, 180f);

            for (int i = 0; i < _summons.Count; i++)
            {
                HighflyShadowMinion minion = _summons[i];
                if (minion == null) continue;

                minion.CommandMirrorAttack(
                    target,
                    0.045f + i * 0.055f,
                    false);
            }
        }

        private void EchoTwinDance(int stage)
        {
            if (_summons.Count == 0) return;

            CharacterStats target = FindBestTarget(9.5f, 180f);

            for (int i = 0; i < _summons.Count; i++)
            {
                HighflyShadowMinion minion = _summons[i];
                if (minion == null) continue;

                minion.CommandMirrorAttack(
                    target,
                    0.055f + i * 0.060f,
                    stage >= 3);
            }
        }

        public CharacterStats FindBestTarget(float radius, float angle)
        {
            CharacterStats[] all =
                UnityEngine.Object.FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);

            Vector3 viewDir = transform.forward;
            if (_player != null && _player.cameraTransform != null)
            {
                viewDir = _player.cameraTransform.forward;
                viewDir.y = 0f;
                if (viewDir.sqrMagnitude > 0.001f)
                    viewDir.Normalize();
                else
                    viewDir = transform.forward;
            }

            CharacterStats bestInView = null;
            float bestViewScore = float.MaxValue;

            CharacterStats nearestFallback = null;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < all.Length; i++)
            {
                CharacterStats stats = all[i];
                if (!IsValidTarget(stats)) continue;

                Vector3 delta = stats.transform.position - transform.position;
                delta.y = 0f;
                float distance = delta.magnitude;

                if (distance <= 0.01f || distance > radius)
                    continue;

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestFallback = stats;
                }

                float a = Vector3.Angle(viewDir, delta / distance);
                if (a > angle * 0.5f)
                    continue;

                // Center-screen alignment matters more than tiny distance differences.
                float score = a * 0.11f + distance * 0.32f;
                if (score < bestViewScore)
                {
                    bestViewScore = score;
                    bestInView = stats;
                }
            }

            return bestInView != null ? bestInView : nearestFallback;
        }

        public CharacterStats FindBestTargetStrict(float radius, float angle)
        {
            CharacterStats[] all =
                UnityEngine.Object.FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);

            Vector3 viewDir = transform.forward;
            if (_player != null && _player.cameraTransform != null)
            {
                viewDir = _player.cameraTransform.forward;
                viewDir.y = 0f;

                if (viewDir.sqrMagnitude > 0.001f)
                    viewDir.Normalize();
                else
                    viewDir = transform.forward;
            }

            CharacterStats best = null;
            float bestScore = float.MaxValue;

            for (int i = 0; i < all.Length; i++)
            {
                CharacterStats stats = all[i];
                if (!IsValidTarget(stats)) continue;

                Vector3 delta =
                    stats.transform.position -
                    transform.position;
                delta.y = 0f;

                float distance = delta.magnitude;
                if (distance <= 0.01f || distance > radius)
                    continue;

                float a =
                    Vector3.Angle(
                        viewDir,
                        delta / distance);

                if (a > angle * 0.5f)
                    continue;

                float score =
                    a * 0.15f +
                    distance * 0.28f;

                if (score < bestScore)
                {
                    bestScore = score;
                    best = stats;
                }
            }

            return best;
        }

        private bool IsValidTarget(CharacterStats stats)
        {
            if (stats == null || stats.transform == transform)
                return false;

            if (stats is HighflyLabDummyStats)
                return true;

            try { return stats.CompareTag("Enemy"); }
            catch { return false; }
        }

        public void DealDirect(CharacterStats target, float damage, string source)
        {
            if (target == null) return;

            var status = target.GetComponent<HighflyLabStatusReceiver>();
            if (status != null && status.IsShadowMarked)
                damage *= 1.20f;

            target.TakeDamage(damage, 25f, transform);

            // Lab dummies record every hit inside TakeDamage so basic ATQ and
            // premium skills share one telemetry path. Real enemies are counted here.
            if (!(target is HighflyLabDummyStats))
                HighflySkillLabMetrics.RecordHit(damage);

            HighflyPremiumFx.SpawnResource(
                "Sparks",
                target.transform.position + Vector3.up * 0.85f,
                Quaternion.identity,
                0.48f,
                0.55f);

            if (VitalPactActive && _stats != null)
                _stats.RestoreEgo(damage * 0.22f);

            if (HighflyAdvancedSkillRuntime.Instance != null &&
                HighflyAdvancedSkillRuntime.Instance.ShadowLinkActive &&
                _stats != null)
            {
                _stats.RestoreEgo(damage * 0.18f);
            }

            StartCoroutine(HitStop(0.035f));
        }

        private void DealConeDamage(float damage, float reach, float radius, Vector3 dir, string source)
        {
            CharacterStats[] all =
                UnityEngine.Object.FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);

            float maxDistance = reach + radius * 0.95f;
            float minForwardDot = -0.05f;

            for (int i = 0; i < all.Length; i++)
            {
                CharacterStats stats = all[i];
                if (!IsValidTarget(stats)) continue;

                Vector3 to = stats.transform.position - transform.position;
                float vertical = Mathf.Abs(to.y);
                to.y = 0f;

                float distance = to.magnitude;
                if (distance <= 0.01f || distance > maxDistance || vertical > 2.5f)
                    continue;

                Vector3 n = to / distance;
                float forwardDot = Vector3.Dot(dir, n);
                if (forwardDot < minForwardDot)
                    continue;

                // Wider up close, tighter at the outer edge: feels like a sword sweep.
                float lateralAllowance =
                    Mathf.Lerp(radius * 1.45f, radius * 0.80f, distance / maxDistance);

                float lateral =
                    Vector3.Cross(dir, n).magnitude * distance;

                if (lateral > lateralAllowance)
                    continue;

                DealDirect(stats, damage, source);
            }
        }

        private IEnumerator PullTargetTowardPlayer(
            CharacterStats target,
            float duration,
            float stopDistance)
        {
            if (target == null) yield break;

            Transform targetTransform = target.transform;
            NavMeshAgent agent = target.GetComponentInParent<NavMeshAgent>();
            bool agentUsable = agent != null && agent.enabled;
            bool oldUpdatePosition = false;

            if (agentUsable)
            {
                oldUpdatePosition = agent.updatePosition;
                agent.updatePosition = false;
                agent.isStopped = true;
            }

            Vector3 start = targetTransform.position;
            Vector3 delta = start - transform.position;
            delta.y = 0f;

            Vector3 end =
                transform.position +
                (delta.sqrMagnitude > 0.001f ? delta.normalized : transform.forward) *
                stopDistance;

            end.y = start.y;

            float begin = Time.unscaledTime;

            while (target != null && Time.unscaledTime - begin < duration)
            {
                float t = (Time.unscaledTime - begin) / Mathf.Max(0.001f, duration);
                float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);

                targetTransform.position = Vector3.Lerp(start, end, eased);
                yield return null;
            }

            if (target != null)
                targetTransform.position = end;

            if (agentUsable && agent != null)
            {
                agent.Warp(end);
                agent.updatePosition = oldUpdatePosition;
                agent.isStopped = true; // status receiver releases it when root expires
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

            HighflyTimeDilationManager.RequestHitStop(duration, 0.08f);
            yield return new WaitForSecondsRealtime(duration);
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
            HighflyPremiumFx.SpawnCrescentSlash(
                transform.position + Vector3.up * 1.05f + dir * 1.10f,
                dir,
                color,
                Mathf.Max(1.25f, length * 0.72f),
                Mathf.Max(0.28f, length * 0.22f),
                roll,
                0.20f);
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
        private float _phase;
        private bool _attacking;
        private Vector3 _formationVelocity;
        private Animator _animator;

        public void Initialize(
            HighflyPremiumSkillRuntime owner,
            Transform master,
            float side,
            float lifeSeconds)
        {
            _owner = owner;
            _master = master;
            _side = side;
            _expiresAt = Time.unscaledTime + lifeSeconds;
            _phase = side > 0f ? 0f : Mathf.PI;
            _animator = GetComponentInChildren<Animator>(true);
        }

        private void Update()
        {
            if (_owner == null || _master == null || Time.unscaledTime >= _expiresAt)
            {
                Destroy(gameObject);
                return;
            }

            if (_attacking)
                return;

            // V formation: hunter stays at the rear point, shadows lead ahead.
            Vector3 desired =
                _master.position +
                _master.right * (_side * 1.45f) +
                _master.forward * 1.55f;

            transform.position = Vector3.SmoothDamp(
                transform.position,
                desired,
                ref _formationVelocity,
                0.075f,
                25f,
                Time.unscaledDeltaTime);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                _master.rotation,
                1f - Mathf.Exp(-16f * Time.unscaledDeltaTime));
        }

        public void CommandMirrorAttack(
            CharacterStats target,
            float delay,
            bool heavy)
        {
            if (_attacking || !isActiveAndEnabled)
                return;

            StartCoroutine(MirrorAttack(target, delay, heavy));
        }

        private IEnumerator MirrorAttack(
            CharacterStats target,
            float delay,
            bool heavy)
        {
            _attacking = true;

            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);

            if (_owner == null || _master == null)
            {
                _attacking = false;
                yield break;
            }

            if (target == null)
                target = _owner.FindBestTarget(9.5f, 180f);

            Vector3 attackDir = _master.forward;

            if (target != null)
            {
                Vector3 to = target.transform.position - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.001f)
                    attackDir = to.normalized;
            }

            transform.rotation = Quaternion.LookRotation(attackDir, Vector3.up);

            if (_animator != null)
            {
                bool hasAttack = false;
                bool hasCombo = false;

                foreach (AnimatorControllerParameter p in _animator.parameters)
                {
                    if (p.name == "doAttack") hasAttack = true;
                    if (p.name == "ComboStep") hasCombo = true;
                }

                if (hasCombo)
                    _animator.SetInteger("ComboStep", heavy ? 3 : 1);

                if (hasAttack)
                    _animator.SetTrigger("doAttack");
            }

            Vector3 start = transform.position;
            Vector3 lungeTarget = start + attackDir * (heavy ? 1.35f : 0.95f);

            if (target != null)
            {
                Vector3 toTarget = target.transform.position - start;
                toTarget.y = 0f;

                if (toTarget.sqrMagnitude > 0.001f)
                {
                    float stop = heavy ? 1.35f : 1.55f;
                    lungeTarget =
                        target.transform.position -
                        toTarget.normalized * stop;
                    lungeTarget.y = start.y;
                }
            }

            float begin = Time.unscaledTime;
            const float lungeTime = 0.095f;

            while (Time.unscaledTime - begin < lungeTime)
            {
                float t = (Time.unscaledTime - begin) / lungeTime;
                float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
                transform.position = Vector3.Lerp(start, lungeTarget, eased);
                yield return null;
            }

            Color shadow = heavy
                ? new Color(0.82f, 0.42f, 1f, 1f)
                : new Color(0.52f, 0.18f, 0.96f, 1f);

            HighflyAnimeFx.SpawnBladeCut(
                transform.position + Vector3.up * 1.05f + attackDir * 0.90f,
                attackDir,
                shadow,
                _side > 0f ? 42f : -42f,
                heavy ? 3.0f : 2.45f,
                heavy ? 0.46f : 0.36f,
                heavy ? 0.21f : 0.17f);

            HighflyPremiumFx.SpawnResource(
                "Sparks",
                transform.position + Vector3.up * 0.95f + attackDir * 0.85f,
                Quaternion.LookRotation(attackDir),
                heavy ? 0.50f : 0.34f,
                0.55f,
                shadow);

            if (target != null)
                _owner.DealDirect(target, heavy ? 22f : 12f, "Eco-Sombra");

            yield return new WaitForSecondsRealtime(heavy ? 0.11f : 0.075f);
            _attacking = false;
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
