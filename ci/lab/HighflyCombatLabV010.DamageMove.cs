using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Highfly.SkillLab
{
    public sealed partial class HighflyCombatLabV010 : MonoBehaviour
    {
        private void TriggerTwinDanceTap()
        {
            float now = Time.unscaledTime;
            float dt = now - _lastTwinTap;
            if (_twinStage > 0 && dt < TwinMinWindow)
            {
                HighflySkillLabMetrics.RecordAction("DANZA GEMELA • DEMASIADO PRONTO", _twinStage);
                return;
            }

            if (_twinStage == 0 || dt > TwinReset)
            {
                _twinStage = 1;
                _perfectTwinLinks = 0;
            }
            else
            {
                bool perfect = dt >= TwinPerfectMin && dt <= TwinPerfectMax;
                if (perfect) _perfectTwinLinks++;
                _twinStage = Mathf.Min(3, _twinStage + 1);
            }

            _lastTwinTap = now;
            StartCoroutine(TwinStageRoutine(_twinStage));
            if (_twinStage == 3)
            {
                _twinStage = 0;
                BeginCooldown(HighflyCombatSkillV010.TwinDanceReforged, 1.25f);
            }
        }

        private IEnumerator TwinStageRoutine(int stage)
        {
            FaceTarget();
            CharacterStats target = Target(7f);
            Color c = new Color(0.20f, 0.86f, 1f, 1f);
            int cuts = stage == 1 ? 2 : stage == 2 ? 3 : 5;
            float damage = stage == 1 ? 8f : stage == 2 ? 9f : 11f;
            string label = stage < 3 ? "DANZA GEMELA • CADENA " + stage : "DANZA GEMELA • FINISHER";

            // PRODUCTION PASS v0.13:
            // Use Lucid's native Animator for sword attacks. Direct UAL2 playback was
            // visually incompatible with this rig (the reported "break dance" issue).
            // UAL2 stays available only after an explicit retarget/validation pass.
            if (_animator != null)
            {
                _animator.SetInteger("ComboStep", Mathf.Clamp(stage - 1, 0, 2));
                _animator.SetTrigger("doAttack");
            }

            for (int i = 0; i < cuts; i++)
            {
                float yaw = Mathf.Lerp(-55f, 55f, cuts <= 1 ? 0.5f : i / (float)(cuts - 1));
                if ((i & 1) == 1) yaw = -yaw;
                Vector3 p = transform.position + Vector3.up * (0.85f + 0.10f * i) + transform.forward * (1.25f + 0.12f * i);
                SpawnSlash(p, transform.forward, c, yaw, 2.3f + stage * 0.35f, 0.18f);
                HighflyPremiumFx.SpawnAfterImage(transform, c, 0.12f);
                if (target != null) Deal(target, damage, label, 10f + stage * 4f);
                yield return new WaitForSecondsRealtime(stage == 3 ? 0.075f : 0.095f);
            }

            if (stage == 3 && _perfectTwinLinks >= 2 && target != null)
            {
                Color perfect = new Color(0.90f, 0.98f, 1f, 1f);
                for (int i = 0; i < 4; i++)
                {
                    SpawnSlash(target.transform.position + Vector3.up * (0.65f + i * 0.28f), transform.forward, perfect, i % 2 == 0 ? 68f : -68f, 3.4f, 0.16f);
                    Deal(target, 10f, "DANZA GEMELA • PERFECT CHAIN", 14f);
                    yield return new WaitForSecondsRealtime(0.055f);
                }
                SpawnShock(target.transform.position + Vector3.up * 0.9f, perfect, 2.4f);
                _perfectTwinLinks = 0;
            }
        }

        private IEnumerator DualImpactRoutine()
        {
            FaceTarget();
            CharacterStats target = Target(7f);
            if (target == null) yield break;
            Color mark = new Color(1f, 0.76f, 0.18f, 1f);
            SpawnSlash(target.transform.position + Vector3.up * 0.95f, transform.forward, mark, 25f, 2.1f, 0.18f);
            Deal(target, 15f, "IMPACTO DUAL • MARCA", 12f);
            SpawnRing(target.transform.position + Vector3.up * 0.08f, mark, 0.9f, 0.35f);
            yield return new WaitForSecondsRealtime(0.34f);
            SpawnShock(target.transform.position + Vector3.up * 0.75f, mark, 2.6f);
            Deal(target, 34f, "IMPACTO DUAL • DETONACIÓN", 32f);
        }

        private IEnumerator PileBreakerRoutine()
        {
            FaceTarget();
            Color c = new Color(1f, 0.34f, 0.10f, 1f);
            HighflySkillLabMetrics.RecordAction("PILE BREAKER • CARGANDO", 0);
            HighflyParkourAnimationV010.Instance?.Play(
                "Melee_Hook",
                0.88f,
                0.78f);
            for (int i = 0; i < 4; i++)
            {
                SpawnRing(transform.position + Vector3.up * 1.05f, c, 0.35f + i * 0.18f, 0.22f);
                yield return new WaitForSecondsRealtime(0.09f);
            }
            CharacterStats target = Target(7f);
            if (target != null)
            {
                Vector3 d = target.transform.position - transform.position; d.y = 0f;
                if (d.sqrMagnitude > 0.01f) yield return StartCoroutine(MovePlayer(d.normalized, Mathf.Max(0f, d.magnitude - 1.5f), 0.10f));
                SpawnShock(target.transform.position + Vector3.up * 0.85f, c, 3.8f);
                HighflyAnimeFx.SpawnLightningBurst(transform.position + Vector3.up * 1.1f, target.transform.position + Vector3.up * 0.8f, c, 4, 0.14f);
                Deal(target, 58f, "PILE BREAKER • BREAK", 62f);
            }
        }

        private IEnumerator BoundlessChainRoutine(bool preview)
        {
            FaceTarget();
            // PRODUCTION PASS v0.13:
            // Keep the chain on Lucid-native attacks until donor clips are properly retargeted.
            if (_animator != null)
            {
                _animator.SetInteger("ComboStep", 0);
                _animator.SetTrigger("doAttack");
            }
            CharacterStats target = Target(8f);
            if (target == null) yield break;
            Color c = new Color(0.58f, 0.18f, 1f, 1f);
            int count = preview ? 12 : 8;
            for (int i = 0; i < count; i++)
            {
                Vector3 around = Quaternion.Euler(0f, i * 137.5f, 0f) * Vector3.forward;
                Vector3 p = target.transform.position + Vector3.up * (0.55f + (i % 4) * 0.32f) + around * (0.35f + (i % 3) * 0.16f);
                // Advance through Lucid's native combo poses so each visible body motion
                // matches the chain cadence instead of reusing the incompatible UAL combo.
                if (_animator != null && i > 0 && (i % 3) == 0)
                {
                    _animator.SetInteger("ComboStep", (i / 3) % 3);
                    _animator.SetTrigger("doAttack");
                }

                SpawnSlash(p, around, c, (i % 2 == 0 ? 1f : -1f) * (25f + (i % 5) * 13f), 2.5f + (i % 3) * 0.25f, 0.14f);
                Deal(target, 7.5f, "CADENA SIN LÍMITE • " + (i + 1), 8f);
                if ((i % 3) == 2) HighflyPremiumFx.SpawnAfterImage(transform, c, 0.13f);
                yield return new WaitForSecondsRealtime(0.075f);
            }
            SpawnShock(target.transform.position + Vector3.up * 0.85f, c, 3.0f);
        }

        private IEnumerator DemonStrikeRoutine()
        {
            FaceTarget();
            HighflyParkourAnimationV010.Instance?.Play(
                "Melee_Hook",
                1.06f,
                0.62f);
            Color c = new Color(1f, 0.08f, 0.12f, 1f);
            Vector3 fist = transform.position + Vector3.up * 1.15f + transform.right * 0.35f + transform.forward * 0.25f;
            for (int i = 0; i < 5; i++)
            {
                SpawnRing(fist, c, 0.18f + i * 0.10f, 0.18f);
                yield return new WaitForSecondsRealtime(0.065f);
            }
            CharacterStats target = Target(7f);
            if (target != null)
            {
                Vector3 d = target.transform.position - transform.position; d.y = 0f;
                if (d.sqrMagnitude > 0.01f) yield return StartCoroutine(MovePlayer(d.normalized, Mathf.Max(0f, d.magnitude - 1.35f), 0.09f));
                SpawnShock(target.transform.position + Vector3.up * 0.75f, c, 4.2f);
                Deal(target, 52f, "GOLPE DEMONÍACO", 48f);
            }
        }

        private IEnumerator PhantomStepRoutine()
        {
            HighflyParkourAnimationV010.Instance?.Play(
                "Sword_Dash",
                1.18f,
                0.48f);
            CharacterStats target = Target(12f);
            Vector3 dir = target != null ? target.transform.position - transform.position : transform.forward;
            dir.y = 0f; if (dir.sqrMagnitude < 0.01f) dir = transform.forward; dir.Normalize();
            float distance = target != null ? Mathf.Clamp(Vector3.Distance(transform.position, target.transform.position) - 1.35f, 1.7f, 6.5f) : 4.5f;
            Color c = new Color(0.22f, 0.92f, 1f, 1f);
            for (int i = 0; i < 4; i++) HighflyPremiumFx.SpawnAfterImage(transform, c, 0.18f + i * 0.02f);
            yield return StartCoroutine(MovePlayer(dir, distance, 0.11f));
            if (target != null)
            {
                transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                SpawnSlash(target.transform.position + Vector3.up * 0.95f, dir, c, -38f, 3.0f, 0.19f);
                Deal(target, 25f, "PASO FANTASMA", 18f);
            }
            MarkEmergencyEscape();
        }

        private IEnumerator FormulaDriftRoutine()
        {
            CharacterStats target = Target(11f);
            if (target == null) yield break;
            Vector2 input = _player != null ? _player.HighflyMobileMoveInput : Vector2.zero;
            float side = Mathf.Abs(input.x) > 0.20f ? Mathf.Sign(input.x) : 1f;
            Color c = new Color(0.18f, 0.88f, 1f, 1f);
            float radius = Mathf.Clamp(Vector3.Distance(transform.position, target.transform.position), 2.4f, 4.2f);
            Vector3 rel = transform.position - target.transform.position; rel.y = 0f;
            if (rel.sqrMagnitude < 0.1f) rel = -target.transform.forward * radius;
            rel.Normalize();
            float start = Time.unscaledTime;
            const float duration = 0.72f;
            while (Time.unscaledTime - start < duration)
            {
                float t = (Time.unscaledTime - start) / duration;
                float angle = side * Mathf.Lerp(0f, 205f, Mathf.SmoothStep(0f, 1f, t));
                Vector3 desired = target.transform.position + Quaternion.Euler(0f, angle, 0f) * rel * radius;
                desired.y = transform.position.y;
                Vector3 delta = desired - transform.position;
                if (_cc != null && _cc.enabled) _cc.Move(delta * Mathf.Min(1f, 14f * Time.unscaledDeltaTime));
                Vector3 face = target.transform.position - transform.position; face.y = 0f;
                if (face.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(face.normalized, Vector3.up);
                if (UnityEngine.Random.value < 0.26f) HighflyPremiumFx.SpawnAfterImage(transform, c, 0.13f);
                yield return null;
            }
            SpawnSlash(target.transform.position + Vector3.up * 1f, transform.forward, c, side > 0f ? 55f : -55f, 2.8f, 0.18f);
            Deal(target, 22f, "DRIFT DE FÓRMULA • FLANCO", 16f);
        }

        private IEnumerator VoraciousEchoRoutine()
        {
            CharacterStats target = Target(10f);
            Vector2 input = _player != null ? _player.HighflyMobileMoveInput : Vector2.zero;
            float side = Mathf.Abs(input.x) > 0.15f ? Mathf.Sign(input.x) : -1f;
            Vector3 dashDir = transform.right * side;
            Color c = new Color(0.64f, 0.20f, 1f, 1f);
            for (int i = 0; i < 3; i++) HighflyPremiumFx.SpawnAfterImage(transform, c, 0.18f);
            yield return StartCoroutine(MovePlayer(dashDir, 3.1f, 0.12f));
            MarkEmergencyEscape();
            yield return new WaitForSecondsRealtime(0.16f);
            if (target != null)
            {
                Vector3 d = target.transform.position - transform.position; d.y = 0f; if (d.sqrMagnitude < 0.01f) d = transform.forward;
                SpawnSlash(transform.position + Vector3.up * 1f + d.normalized * 1.2f, d.normalized, c, side > 0f ? -70f : 70f, 3.4f, 0.19f);
                Deal(target, 28f, "ECO VORAZ • CORTE RETARDADO", 20f);
            }
        }
    }
}