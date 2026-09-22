using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Highfly.SkillLab
{
    public sealed partial class HighflyCombatLabV010 : MonoBehaviour
    {
        private IEnumerator SovereignLiftRoutine()
        {
            CharacterStats target = Target(10f);
            if (target == null) yield break;

            Color c = new Color(0.35f, 0.82f, 1f, 1f);
            SpawnRing(target.transform.position + Vector3.up * 0.05f, c, 1.2f, 0.40f);
            HighflyAnimeFx.SpawnLightningBurst(
                transform.position + Vector3.up * 1.1f,
                target.transform.position + Vector3.up * 0.8f,
                c, 3, 0.18f);

            NavMeshAgent agent = target.GetComponent<NavMeshAgent>();
            bool agentWas = agent != null && agent.enabled;
            if (agentWas) agent.enabled = false;

            CharacterController tc = target.GetComponent<CharacterController>();
            bool ccWas = tc != null && tc.enabled;
            if (ccWas) tc.enabled = false;

            Vector3 start = target.transform.position;
            float begin = Time.unscaledTime;
            const float rise = 0.35f;

            while (Time.unscaledTime - begin < rise)
            {
                float t = (Time.unscaledTime - begin) / rise;
                target.transform.position =
                    start + Vector3.up * Mathf.SmoothStep(0f, 3.0f, t);
                yield return null;
            }

            Deal(target, 10f, "SOVEREIGN LIFT • LAUNCH", 25f);
            yield return new WaitForSecondsRealtime(0.72f);

            begin = Time.unscaledTime;
            Vector3 high = target.transform.position;
            while (Time.unscaledTime - begin < 0.25f)
            {
                float t = (Time.unscaledTime - begin) / 0.25f;
                target.transform.position = Vector3.Lerp(high, start, t * t);
                yield return null;
            }

            target.transform.position = start;
            SpawnShock(start + Vector3.up * 0.12f, c, 2.6f);
            Deal(target, 16f, "SOVEREIGN LIFT • SLAM", 28f);

            if (ccWas && tc != null) tc.enabled = true;
            if (agentWas && agent != null) agent.enabled = true;
        }

        private IEnumerator AbyssalShackleRoutine()
        {
            CharacterStats target = Target(11f);
            if (target == null) yield break;

            Color c = new Color(0.52f, 0.08f, 0.92f, 1f);
            GameObject arm =
                SpawnHunterArmGhost(
                    target.transform.position + Vector3.up * 0.85f,
                    c,
                    2.2f);

            Vector3 start =
                arm != null
                    ? arm.transform.position
                    : transform.position + Vector3.up * 1f;

            Vector3 end = target.transform.position + Vector3.up * 0.85f;

            if (arm != null)
            {
                float begin = Time.unscaledTime;
                while (Time.unscaledTime - begin < 0.22f)
                {
                    float t = (Time.unscaledTime - begin) / 0.22f;
                    arm.transform.position =
                        Vector3.Lerp(start, end, 1f - Mathf.Pow(1f - t, 3f));
                    arm.transform.localScale =
                        Vector3.one * Mathf.Lerp(1.25f, 2.35f, t);
                    yield return null;
                }
            }

            SpawnRing(end, c, 1.05f, 0.38f);

            Vector3 targetStart = target.transform.position;
            Vector3 pullEnd =
                transform.position + transform.forward * 2.1f;
            pullEnd.y = targetStart.y;

            float pullBegin = Time.unscaledTime;
            while (Time.unscaledTime - pullBegin < 0.30f)
            {
                float t = (Time.unscaledTime - pullBegin) / 0.30f;
                target.transform.position =
                    Vector3.Lerp(
                        targetStart,
                        pullEnd,
                        Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            Deal(target, 20f, "GRILLETE ABISAL • ATRACCIÓN", 30f);
            if (arm != null) Destroy(arm, 0.18f);
        }

        private IEnumerator SevenSinkerRoutine()
        {
            CharacterStats target = Target(13f);
            if (target == null) yield break;

            Color c = new Color(0.20f, 0.82f, 1f, 1f);
            Vector3 center =
                target.transform.position + Vector3.up * 0.75f;

            var blades = new List<GameObject>();

            for (int i = 0; i < 7; i++)
            {
                float a = i / 7f * Mathf.PI * 2f;

                Vector3 p =
                    center +
                    new Vector3(
                        Mathf.Cos(a),
                        0.2f + (i % 2) * 0.35f,
                        Mathf.Sin(a)) * 2.55f;

                GameObject blade =
                    CreateBlade(
                        "HF_SEVEN_SINKER_BLADE",
                        p,
                        Quaternion.LookRotation(
                            (center - p).normalized,
                            Vector3.up),
                        c,
                        1.0f);

                blades.Add(blade);
                SpawnRing(
                    p - Vector3.up * 0.2f,
                    c,
                    0.32f,
                    0.22f);

                yield return new WaitForSecondsRealtime(0.055f);
            }

            yield return new WaitForSecondsRealtime(0.32f);

            float begin = Time.unscaledTime;
            while (Time.unscaledTime - begin < 0.28f)
            {
                float t =
                    (Time.unscaledTime - begin) / 0.28f;

                for (int i = 0; i < blades.Count; i++)
                {
                    if (blades[i] == null) continue;

                    blades[i].transform.position =
                        Vector3.Lerp(
                            blades[i].transform.position,
                            center,
                            0.18f + t * 0.28f);
                }

                yield return null;
            }

            for (int i = 0; i < 7; i++)
                Deal(
                    target,
                    8f,
                    "SEVEN SINKER • " + (i + 1),
                    7f);

            SpawnShock(center, c, 3.4f);

            for (int i = 0; i < blades.Count; i++)
                if (blades[i] != null)
                    Destroy(blades[i], 0.12f);
        }

        private IEnumerator ShadowCallMirrorRoutine()
        {
            HighflyPremiumSkillRuntime old =
                HighflyPremiumSkillRuntime.Instance;

            if (old != null)
            {
                old.ClearPreviewCooldown(
                    HighflyPremiumSkillId.ShadowCall);
                old.Trigger(
                    HighflyPremiumSkillId.ShadowCall);
            }

            yield return new WaitForSecondsRealtime(0.35f);

            CharacterStats target = Target(11f);

            HighflyShadowMinion[] shadows =
                UnityEngine.Object.FindObjectsByType<HighflyShadowMinion>(
                    FindObjectsSortMode.None);

            if (_animator != null)
            {
                _animator.SetInteger("ComboStep", 2);
                _animator.SetTrigger("doAttack");
            }

            SpawnSlash(
                transform.position +
                Vector3.up * 1f +
                transform.forward * 1.4f,
                transform.forward,
                new Color(0.72f, 0.28f, 1f, 1f),
                48f,
                2.8f,
                0.17f);

            for (int i = 0; i < shadows.Length; i++)
            {
                if (shadows[i] != null)
                    shadows[i].CommandMirrorAttack(
                        target,
                        0.12f + i * 0.13f,
                        i == shadows.Length - 1);
            }

            yield return new WaitForSecondsRealtime(0.55f);
            HighflySkillLabMetrics.RecordAction(
                "ECO DE SOMBRA • SINCRONÍA",
                0);
        }

        private IEnumerator ShadowRelayRoutine()
        {
            Color c = new Color(0.48f, 0.08f, 0.88f, 1f);

            if (!_shadowRelayPoint.HasValue)
            {
                _shadowRelayPoint = transform.position;
                SpawnGroundMark(
                    transform.position + Vector3.up * 0.03f,
                    c,
                    1.25f,
                    3.8f);

                HighflySkillLabMetrics.RecordAction(
                    "RELEVO UMBRÍO • ANCLA",
                    0);

                yield break;
            }

            Vector3 old = transform.position;
            Vector3 dest = _shadowRelayPoint.Value;
            _shadowRelayPoint = old;

            HighflyPremiumFx.SpawnAfterImage(
                transform,
                c,
                0.28f);

            if (_cc != null) _cc.enabled = false;
            transform.position = dest;
            if (_cc != null) _cc.enabled = true;

            HighflyPremiumFx.SpawnAfterImage(
                transform,
                c,
                0.28f);

            SpawnRing(
                transform.position + Vector3.up * 0.1f,
                c,
                1.3f,
                0.35f);

            MarkEmergencyEscape();

            HighflySkillLabMetrics.RecordAction(
                "RELEVO UMBRÍO • SWAP",
                0);

            yield return null;
        }

        private IEnumerator ShadowCreationBladeRoutine()
        {
            CharacterStats target = Target(11f);

            Vector3 floor =
                transform.position +
                transform.forward * 1.4f +
                Vector3.up * 0.05f;

            Color c = new Color(0.56f, 0.08f, 0.92f, 1f);

            SpawnGroundMark(
                floor,
                c,
                0.9f,
                0.75f);

            yield return new WaitForSecondsRealtime(0.25f);

            GameObject blade =
                CreateBlade(
                    "HF_SHADOW_CREATED_BLADE",
                    floor + Vector3.up * 0.75f,
                    Quaternion.LookRotation(
                        transform.forward,
                        Vector3.up) *
                    Quaternion.Euler(75f, 0f, 0f),
                    c,
                    1.2f);

            yield return new WaitForSecondsRealtime(0.22f);

            if (target != null && blade != null)
            {
                Vector3 start = blade.transform.position;
                Vector3 end =
                    target.transform.position +
                    Vector3.up * 0.9f;

                float begin = Time.unscaledTime;

                while (Time.unscaledTime - begin < 0.22f)
                {
                    float t =
                        (Time.unscaledTime - begin) / 0.22f;

                    blade.transform.position =
                        Vector3.Lerp(start, end, t);

                    yield return null;
                }

                Vector3 dir = end - start;
                if (dir.sqrMagnitude < 0.01f)
                    dir = transform.forward;

                SpawnSlash(
                    end,
                    dir.normalized,
                    c,
                    35f,
                    2.6f,
                    0.17f);

                Deal(
                    target,
                    31f,
                    "CREACIÓN DE SOMBRA • BLADE",
                    22f);
            }

            if (blade != null)
                Destroy(blade, 0.12f);
        }
    }
}
