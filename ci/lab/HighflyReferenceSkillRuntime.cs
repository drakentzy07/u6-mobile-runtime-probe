using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Highfly.SkillLab
{
    public static class HighflyReflectiveWallState
    {
        private static float _activeUntil;
        private static Transform _owner;
        private static HighflyReferenceSkillRuntime _runtime;
        private static float _frontHalfAngle = 68f;

        public static bool Active =>
            _owner != null &&
            _runtime != null &&
            Time.unscaledTime < _activeUntil;

        public static void Activate(
            HighflyReferenceSkillRuntime runtime,
            Transform owner,
            float duration,
            float frontHalfAngle)
        {
            _runtime = runtime;
            _owner = owner;
            _activeUntil = Time.unscaledTime + Mathf.Max(0.1f, duration);
            _frontHalfAngle = Mathf.Clamp(frontHalfAngle, 15f, 89f);
        }

        public static bool TryReflect(
            PlayerStats defender,
            float damage,
            float composureDamage,
            Transform attacker)
        {
            if (!HighflySkillLabMode.IsActive || !Active || defender == null)
                return false;

            if (attacker == null)
                return false;

            Vector3 delta = attacker.position - _owner.position;
            delta.y = 0f;

            if (delta.sqrMagnitude < 0.001f)
                return false;

            float angle = Vector3.Angle(_owner.forward, delta.normalized);
            if (angle > _frontHalfAngle)
                return false;

            _runtime.OnSuccessfulReflect(
                attacker,
                Mathf.Max(1f, damage),
                Mathf.Max(1f, composureDamage));

            return true;
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController))]
    public sealed class HighflyReferenceSkillRuntime : MonoBehaviour
    {
        public static HighflyReferenceSkillRuntime Instance { get; private set; }

        private PlayerController _player;
        private PlayerStats _stats;
        private CharacterController _cc;
        private HighflyPremiumSkillRuntime _premium;

        private float _cdRend;
        private float _cdReflect;
        private float _cdDecoy;
        private bool _hitStopRunning;

        public float RendRemaining => Mathf.Max(0f, _cdRend - Time.unscaledTime);
        public float ReflectRemaining => Mathf.Max(0f, _cdReflect - Time.unscaledTime);
        public float DecoyRemaining => Mathf.Max(0f, _cdDecoy - Time.unscaledTime);

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

        public void Trigger(HighflyPremiumSkillId id)
        {
            switch (id)
            {
                case HighflyPremiumSkillId.EclipseRend:
                    TriggerEclipseRend();
                    break;

                case HighflyPremiumSkillId.ReturnWall:
                    TriggerReturnWall();
                    break;

                case HighflyPremiumSkillId.VoraciousEcho:
                    TriggerVoraciousEcho();
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

        // --------------------------------------------------------------
        // S11 — DESGARRO ECLIPSE
        // Reference family: ultra-fast multi-cut execution.
        // Seven visible cuts + delayed finisher. No smoke.
        // --------------------------------------------------------------
        private void TriggerEclipseRend()
        {
            if (!CanAct() || _premium == null || Time.unscaledTime < _cdRend)
                return;

            CharacterStats target = _premium.FindBestTarget(10.5f, 210f);
            if (target == null)
            {
                HighflySkillLabMetrics.RecordAction("S11 • DESGARRO ECLIPSE (SIN OBJETIVO)", 0);
                return;
            }

            _cdRend = Time.unscaledTime + 7.0f;
            HighflySkillLabMetrics.RecordAction("S11 • DESGARRO ECLIPSE", 0);
            StartCoroutine(EclipseRendRoutine(target));
        }

        private IEnumerator EclipseRendRoutine(CharacterStats target)
        {
            if (target == null) yield break;

            HighflyLabStatusReceiver status =
                target.GetComponent<HighflyLabStatusReceiver>();

            if (status == null)
                status = target.gameObject.AddComponent<HighflyLabStatusReceiver>();

            status.ApplyRoot(1.05f);

            Color core = new Color(0.52f, 0.10f, 0.92f, 1f);
            Color edge = new Color(0.10f, 0.72f, 1f, 1f);

            _player.HighflyMobileAttack();
            HighflyPremiumFx.AttachWeaponTrail(
                _player,
                Color.white,
                core,
                0.55f,
                0.24f);

            float[] rolls = { -58f, 34f, -22f, 66f, -44f, 18f, 0f };

            for (int i = 0; i < rolls.Length; i++)
            {
                if (target == null) yield break;

                Vector3 targetCenter =
                    target.transform.position + Vector3.up * 0.95f;

                Vector3 dir =
                    target.transform.position - transform.position;
                dir.y = 0f;

                if (dir.sqrMagnitude < 0.001f)
                    dir = transform.forward;

                dir.Normalize();
                transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

                Color cut = (i % 2 == 0) ? edge : core;

                HighflyAnimeFx.SpawnBladeCut(
                    targetCenter - dir * 0.18f,
                    dir,
                    cut,
                    rolls[i],
                    i == rolls.Length - 1 ? 3.9f : 3.0f,
                    i == rolls.Length - 1 ? 0.50f : 0.34f,
                    i == rolls.Length - 1 ? 0.21f : 0.14f);

                if (i == 1 || i == 4)
                {
                    HighflyPremiumFx.SpawnAfterImage(
                        transform,
                        cut,
                        0.18f);
                }

                _premium.DealDirect(
                    target,
                    i == rolls.Length - 1 ? 54f : 16f,
                    i == rolls.Length - 1 ? "Eclipse-Finisher" : "Eclipse-Cut");

                yield return new WaitForSecondsRealtime(
                    i == rolls.Length - 1 ? 0.060f : 0.048f);
            }

            if (target != null)
            {
                Vector3 impact =
                    target.transform.position + Vector3.up * 0.90f;

                // Delayed anime-style confirmation: cuts land, silence, then X impact.
                yield return new WaitForSecondsRealtime(0.070f);

                HighflyAnimeFx.SpawnImpactCross(
                    impact,
                    transform.forward,
                    core,
                    3.8f);

                HighflyAnimeFx.SpawnLightningBurst(
                    impact - transform.right * 0.8f,
                    impact + transform.right * 0.8f,
                    edge,
                    3,
                    0.14f);
            }

            yield return HitStop(0.070f);
        }

        // --------------------------------------------------------------
        // S12 — MURALLA DE RETORNO
        // Reference family: timed frontal reflect / reactive defense.
        // Real damage interception is hooked in PlayerStats.Mobile.cs.
        // --------------------------------------------------------------
        private void TriggerReturnWall()
        {
            if (!CanAct() || Time.unscaledTime < _cdReflect)
                return;

            _cdReflect = Time.unscaledTime + 9.0f;

            const float active = 1.35f;
            HighflyReflectiveWallState.Activate(
                this,
                transform,
                active,
                68f);

            HighflySkillLabMetrics.RecordAction("S12 • MURALLA DE RETORNO", 0);

            HighflyAnimeFx.SpawnReflectWall(
                transform,
                new Color(0.42f, 0.72f, 1f, 1f),
                active);
        }

        public void OnSuccessfulReflect(
            Transform attacker,
            float incomingDamage,
            float composureDamage)
        {
            if (attacker == null) return;

            CharacterStats enemy =
                attacker.GetComponentInParent<CharacterStats>();

            float reflected =
                Mathf.Clamp(incomingDamage * 1.25f + 12f, 18f, 120f);

            if (enemy != null && enemy != _stats)
                enemy.TakeDamage(reflected, composureDamage * 1.35f, transform);

            Vector3 hit =
                transform.position +
                transform.forward * 1.35f +
                Vector3.up * 1.0f;

            HighflyAnimeFx.SpawnImpactCross(
                hit,
                transform.forward,
                new Color(0.48f, 0.78f, 1f, 1f),
                2.4f);

            HighflyPremiumFx.SpawnResource(
                "Sparks",
                hit,
                Quaternion.LookRotation(transform.forward),
                0.58f,
                0.55f,
                new Color(0.62f, 0.90f, 1f, 1f));

            HighflySkillLabMetrics.RecordAction(
                "S12 • REFLEJO PERFECTO " + reflected.ToString("0"),
                0);

            StartCoroutine(HitStop(0.050f));
        }

        // --------------------------------------------------------------
        // S13 — ECO VORAZ
        // Reference family: decoy / displacement / delayed counter.
        // Leaves a full-pose visual clone, moves the hunter, then clone cuts.
        // --------------------------------------------------------------
        private void TriggerVoraciousEcho()
        {
            if (!CanAct() || _premium == null || Time.unscaledTime < _cdDecoy)
                return;

            _cdDecoy = Time.unscaledTime + 8.0f;
            HighflySkillLabMetrics.RecordAction("S13 • ECO VORAZ", 0);
            StartCoroutine(VoraciousEchoRoutine());
        }

        private IEnumerator VoraciousEchoRoutine()
        {
            CharacterStats target = _premium.FindBestTarget(9.5f, 220f);

            Vector3 decoyPosition = transform.position;
            Quaternion decoyRotation = transform.rotation;

            GameObject decoy =
                HighflyAnimeFx.SpawnDecoyClone(
                    transform,
                    new Color(0.22f, 0.72f, 1f, 1f),
                    1.55f);

            Vector3 escapeDir = -transform.right;
            if (_player.HighflyMobileMoveInput.x > 0.15f)
                escapeDir = transform.right;
            else if (_player.HighflyMobileMoveInput.y < -0.15f)
                escapeDir = -transform.forward;

            Vector3 destination =
                transform.position +
                escapeDir.normalized * 2.35f;

            HighflyPremiumFx.SpawnAfterImage(
                transform,
                new Color(0.32f, 0.76f, 1f, 1f),
                0.22f);

            Warp(destination);

            HighflyAnimeFx.SpawnLightningBurst(
                decoyPosition + Vector3.up * 0.85f,
                transform.position + Vector3.up * 0.85f,
                new Color(0.18f, 0.68f, 1f, 1f),
                3,
                0.16f);

            yield return new WaitForSecondsRealtime(0.82f);

            Vector3 dir = decoyRotation * Vector3.forward;
            if (target != null)
            {
                dir = target.transform.position - decoyPosition;
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.001f)
                    dir = decoyRotation * Vector3.forward;
                dir.Normalize();
            }

            HighflyAnimeFx.SpawnBladeCut(
                decoyPosition + Vector3.up * 1.0f + dir * 0.95f,
                dir,
                new Color(0.44f, 0.18f, 0.96f, 1f),
                -36f,
                3.0f,
                0.42f,
                0.20f);

            if (target != null)
                _premium.DealDirect(target, 42f, "Eco-Voraz");

            HighflyPremiumFx.SpawnResource(
                "Sparks",
                decoyPosition + Vector3.up * 0.85f,
                Quaternion.identity,
                0.42f,
                0.48f,
                new Color(0.50f, 0.22f, 1f, 1f));

            if (decoy != null)
                Destroy(decoy, 0.10f);
        }

        private void Warp(Vector3 position)
        {
            bool wasEnabled = _cc != null && _cc.enabled;

            if (_cc != null && wasEnabled)
                _cc.enabled = false;

            transform.position = position;

            if (_cc != null && wasEnabled)
                _cc.enabled = true;
        }

        private IEnumerator HitStop(float duration)
        {
            if (_hitStopRunning) yield break;

            _hitStopRunning = true;

            float oldScale = Time.timeScale;
            Time.timeScale = 0.06f;

            yield return new WaitForSecondsRealtime(duration);

            Time.timeScale = oldScale <= 0f ? 1f : oldScale;
            _hitStopRunning = false;
        }
    }
}
