using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Highfly.SkillLab
{
    public sealed partial class HighflyCombatLabV010 : MonoBehaviour
    {
        private IEnumerator VitalPactRoutine()
        {
            Color c = new Color(0.76f, 0.18f, 1f, 1f);

            for (int i = 0; i < 4; i++)
            {
                SpawnRuneCircle(
                    transform.position + Vector3.up * 0.05f,
                    c,
                    1.25f + i * 0.22f,
                    0.55f + i * 0.08f);

                yield return new WaitForSecondsRealtime(0.09f);
            }

            if (_stats != null)
                _stats.RestoreEgo(
                    Mathf.Max(1f, _stats.maxEgo * 0.10f));

            HighflySkillLabMetrics.RecordAction(
                "PACTO VITAL • LUZ EMANANTE",
                0);
        }

        private IEnumerator VitalDomainRoutine()
        {
            Color c = new Color(0.56f, 0.12f, 0.94f, 1f);

            Vector3 center =
                transform.position + transform.forward * 1.2f;

            SpawnRuneCircle(
                center + Vector3.up * 0.03f,
                c,
                4.2f,
                2.2f);

            float start = Time.unscaledTime;
            float nextDrain = 0f;

            while (Time.unscaledTime - start < 1.65f)
            {
                if (Time.unscaledTime >= nextDrain)
                {
                    nextDrain = Time.unscaledTime + 0.18f;

                    int count = Physics.OverlapSphereNonAlloc(
                        center,
                        4.2f,
                        _hits,
                        ~0,
                        QueryTriggerInteraction.Ignore);

                    var seen = new HashSet<CharacterStats>();

                    for (int i = 0; i < count; i++)
                    {
                        CharacterStats cs =
                            _hits[i] != null
                                ? _hits[i].GetComponentInParent<CharacterStats>()
                                : null;

                        if (cs == null || seen.Contains(cs))
                            continue;

                        seen.Add(cs);

                        if (cs == _stats)
                        {
                            if (_stats != null)
                                _stats.RestoreEgo(
                                    Mathf.Max(
                                        0.2f,
                                        _stats.maxEgo * 0.004f));
                        }
                        else
                        {
                            cs.TakeDamage(
                                1.8f,
                                1.5f,
                                transform);

                            HighflySkillLabMetrics.RecordHit(1.8f);
                        }
                    }

                    HighflyShadowMinion[] shadows =
                        UnityEngine.Object.FindObjectsByType<HighflyShadowMinion>(
                            FindObjectsSortMode.None);

                    for (int i = 0; i < shadows.Length; i++)
                    {
                        HighflyShadowMinion sh = shadows[i];
                        if (sh == null) continue;
                        if (Vector3.Distance(sh.transform.position, center) > 4.2f)
                            continue;

                        SpawnRing(
                            sh.transform.position + Vector3.up * 0.15f,
                            new Color(0.45f, 1f, 0.72f, 0.9f),
                            0.55f,
                            0.25f);
                    }
                }

                if (UnityEngine.Random.value < 0.10f)
                {
                    SpawnRing(
                        center + Vector3.up * 0.08f,
                        c,
                        UnityEngine.Random.Range(1.2f, 3.8f),
                        0.28f);
                }

                yield return null;
            }

            HighflySkillLabMetrics.RecordAction(
                "DOMINIO VITAL • DRENA / CURA / BUFF",
                0);
        }

        private void TriggerReserveArcana()
        {
            Color c = new Color(0.18f, 0.84f, 1f, 1f);

            if (!_reserveReady)
            {
                _reserveReady = true;

                _reservePoint =
                    transform.position +
                    Vector3.up * 1.55f -
                    transform.right * 0.65f;

                SpawnRuneCircle(
                    _reservePoint,
                    c,
                    0.55f,
                    4.0f,
                    Quaternion.Euler(90f, 0f, 0f));

                HighflySkillLabMetrics.RecordAction(
                    "RESERVA ARCANA • HECHIZO GUARDADO",
                    0);

                return;
            }

            _reserveReady = false;

            CharacterStats target = Target(13f);

            Vector3 end =
                target != null
                    ? target.transform.position + Vector3.up * 0.9f
                    : transform.position +
                      transform.forward * 8f +
                      Vector3.up;

            HighflyAnimeFx.SpawnLightningBurst(
                _reservePoint,
                end,
                c,
                5,
                0.18f);

            SpawnShock(end, c, 2.8f);

            if (target != null)
                Deal(
                    target,
                    35f,
                    "RESERVA ARCANA • LIBERACIÓN",
                    25f);
        }

        private IEnumerator ReturnWallRoutine()
        {
            Color c = new Color(0.18f, 0.72f, 1f, 1f);

            Transform wall =
                new GameObject("HF_RETURN_WALL_V010").transform;

            wall.position =
                transform.position +
                transform.forward * 1.7f +
                Vector3.up * 1.35f;

            wall.rotation = transform.rotation;

            for (int row = 0; row < 4; row++)
            {
                for (int col = -2; col <= 2; col++)
                {
                    GameObject brick =
                        GameObject.CreatePrimitive(PrimitiveType.Cube);

                    brick.name = "ArcaneBrick";
                    brick.transform.SetParent(wall, false);

                    brick.transform.localPosition =
                        new Vector3(
                            col * 0.58f +
                            ((row & 1) == 1 ? 0.28f : 0f),
                            row * 0.48f - 0.75f,
                            0f);

                    brick.transform.localScale =
                        new Vector3(0.54f, 0.42f, 0.18f);

                    Renderer r = brick.GetComponent<Renderer>();
                    if (r != null)
                    {
                        r.sharedMaterial =
                            MakeMaterial(
                                new Color(
                                    0.12f,
                                    0.24f,
                                    0.38f,
                                    0.66f),
                                c * 2.2f);
                    }

                    Collider co = brick.GetComponent<Collider>();
                    if (co != null) co.enabled = false;
                }
            }

            HighflyReferenceSkillRuntime reference =
                HighflyReferenceSkillRuntime.Instance;

            if (reference != null)
            {
                HighflyReflectiveWallState.Activate(
                    reference,
                    transform,
                    1.10f,
                    72f);
            }

            SpawnRuneCircle(
                wall.position,
                c,
                1.8f,
                1.10f,
                wall.rotation * Quaternion.Euler(90f, 0f, 0f));

            HighflySkillLabMetrics.RecordAction(
                "MURALLA DE RETORNO • REFLECT",
                0);

            yield return new WaitForSecondsRealtime(1.10f);

            if (wall != null)
                Destroy(wall.gameObject);
        }

        private void ArmMomentSight(bool demo)
        {
            _momentArmed = true;
            _momentArmedUntil =
                Time.unscaledTime + (demo ? 4.0f : 7.0f);

            _momentPending = false;
            _momentEscaped = false;

            Color c = new Color(0.82f, 0.95f, 1f, 1f);

            SpawnRing(
                transform.position + Vector3.up * 1.05f,
                c,
                1.1f,
                0.55f);

            HighflySkillLabMetrics.RecordAction(
                "VISTA DEL INSTANTE • ARMADA",
                0);
        }

        public static bool TryInterceptLethal(
            PlayerStats stats,
            float damage,
            Transform attacker)
        {
            HighflyCombatLabV010 inst = Instance;

            if (inst == null ||
                stats == null ||
                !inst.MomentSightArmed ||
                inst._momentPending)
                return false;

            if (damage < stats.currentEgo)
                return false;

            inst._momentArmed = false;

            inst.StartCoroutine(
                inst.MomentSightWindow(
                    stats,
                    damage,
                    attacker));

            return true;
        }

        private IEnumerator MomentSightWindow(
            PlayerStats stats,
            float pendingDamage,
            Transform attacker)
        {
            _momentPending = true;
            _momentEscaped = false;
            _momentStartPosition = transform.position;

            Color c = new Color(0.86f, 0.98f, 1f, 1f);

            HighflySkillLabMetrics.RecordAction(
                "VISTA DEL INSTANTE • ¡LETAL! REACCIONÁ",
                0);

            SpawnThreatTelegraph(
                attacker != null
                    ? attacker.position
                    : transform.position + transform.forward * 4f,
                c);

            float start = Time.unscaledTime;
            const float window = 0.48f;

            while (Time.unscaledTime - start < window)
            {
                if (_player != null &&
                    (_player.currentState == PlayerState.Roll ||
                     _player.currentState == PlayerState.Parry))
                {
                    _momentEscaped = true;
                }

                if (Vector3.Distance(
                        transform.position,
                        _momentStartPosition) > 1.35f)
                {
                    _momentEscaped = true;
                }

                yield return null;
            }

            if (!_momentEscaped && stats != null)
            {
                stats.HighflyLabApplyRawDamage(
                    pendingDamage,
                    attacker);
            }
            else
            {
                SpawnShock(
                    transform.position + Vector3.up * 0.9f,
                    c,
                    1.9f);

                HighflySkillLabMetrics.RecordAction(
                    "VISTA DEL INSTANTE • PERFECT ESCAPE",
                    0);
            }

            _momentPending = false;
        }

        private IEnumerator MomentSightDemoThreat()
        {
            if (_stats == null)
                yield break;

            float lethal =
                Mathf.Max(1f, _stats.currentEgo + 1f);

            Transform fake =
                new GameObject(
                    "HF_MOMENT_FAKE_ATTACKER").transform;

            fake.position =
                transform.position +
                transform.forward * 4f;

            _stats.TakeDamage(
                lethal,
                10f,
                fake);

            yield return new WaitForSecondsRealtime(0.18f);

            if (_momentPending)
            {
                Vector3 side =
                    transform.right * 2.1f;

                if (_cc != null && _cc.enabled)
                    _cc.Move(side);

                MarkEmergencyEscape();
            }

            Destroy(fake.gameObject, 0.8f);
        }

        public void MarkEmergencyEscape()
        {
            if (_momentPending)
                _momentEscaped = true;
        }

        private IEnumerator FutureCutRoutine()
        {
            CharacterStats target = Target(13f);

            Vector3 point =
                target != null
                    ? target.transform.position
                    : transform.position +
                      transform.forward * 5f;

            Color mark =
                new Color(1f, 0.82f, 0.26f, 1f);

            SpawnGroundMark(
                point + Vector3.up * 0.05f,
                mark,
                1.05f,
                0.95f);

            HighflySkillLabMetrics.RecordAction(
                "CORTE FUTURO • MARCA",
                0);

            yield return new WaitForSecondsRealtime(0.82f);

            Vector3 dir =
                target != null
                    ? target.transform.position - transform.position
                    : transform.forward;

            if (dir.sqrMagnitude < 0.01f)
                dir = transform.forward;

            dir.Normalize();

            for (int i = 0; i < 3; i++)
            {
                SpawnSlash(
                    point +
                    Vector3.up * (0.55f + i * 0.35f),
                    dir,
                    mark,
                    i == 1 ? -75f : 75f,
                    3.4f + i * 0.35f,
                    0.19f);

                yield return new WaitForSecondsRealtime(0.045f);
            }

            SpawnShock(
                point + Vector3.up * 0.75f,
                mark,
                2.5f);

            if (target != null &&
                Vector3.Distance(
                    target.transform.position,
                    point) <= 1.9f)
            {
                Deal(
                    target,
                    48f,
                    "CORTE FUTURO • PREDICCIÓN",
                    28f);
            }
        }

        private IEnumerator MovePlayer(
            Vector3 direction,
            float distance,
            float duration)
        {
            if (distance <= 0.02f)
                yield break;

            direction.y = 0f;

            if (direction.sqrMagnitude < 0.01f)
                yield break;

            direction.Normalize();

            float speed =
                distance /
                Mathf.Max(0.02f, duration);

            float start = Time.unscaledTime;

            while (Time.unscaledTime - start < duration)
            {
                if (_cc != null && _cc.enabled)
                {
                    _cc.Move(
                        direction *
                        speed *
                        Time.unscaledDeltaTime);
                }

                yield return null;
            }
        }

        private static void SpawnSlash(
            Vector3 position,
            Vector3 forward,
            Color color,
            float roll,
            float length,
            float life)
        {
            HighflyAnimeFx.SpawnBladeCut(
                position,
                forward,
                color,
                roll,
                length,
                0.42f,
                life);
        }

        private static void SpawnShock(
            Vector3 position,
            Color color,
            float scale)
        {
            HighflyPremiumFx.SpawnResource(
                "EnergyExplosion",
                position,
                Quaternion.identity,
                scale,
                0.58f,
                color);

            SpawnRing(
                position + Vector3.up * 0.05f,
                color,
                Mathf.Max(
                    0.7f,
                    scale * 0.72f),
                0.35f);
        }

        private static void SpawnRing(
            Vector3 position,
            Color color,
            float radius,
            float life)
        {
            SpawnRuneCircle(
                position,
                color,
                radius,
                life,
                Quaternion.identity);
        }

        private static void SpawnRuneCircle(
            Vector3 position,
            Color color,
            float radius,
            float life,
            Quaternion? rotation = null)
        {
            GameObject go =
                new GameObject(
                    "HF_V010_RUNE_CIRCLE");

            go.transform.position = position;
            go.transform.rotation =
                rotation ?? Quaternion.identity;

            LineRenderer lr =
                go.AddComponent<LineRenderer>();

            lr.loop = true;
            lr.useWorldSpace = false;
            lr.positionCount = 64;
            lr.widthMultiplier = 0.035f;

            lr.sharedMaterial =
                MakeMaterial(
                    new Color(
                        color.r,
                        color.g,
                        color.b,
                        0.70f),
                    color * 3.2f);

            for (int i = 0; i < 64; i++)
            {
                float a =
                    i / 64f *
                    Mathf.PI *
                    2f;

                float pulse =
                    1f +
                    0.08f *
                    Mathf.Sin(a * 8f);

                lr.SetPosition(
                    i,
                    new Vector3(
                        Mathf.Cos(a) *
                        radius *
                        pulse,
                        0f,
                        Mathf.Sin(a) *
                        radius *
                        pulse));
            }

            HighflySimpleLifetime lt =
                go.AddComponent<HighflySimpleLifetime>();

            lt.Initialize(life);

            HighflyV010Pulse pulseFx =
                go.AddComponent<HighflyV010Pulse>();

            pulseFx.Initialize(radius, life);
        }

        private static void SpawnGroundMark(
            Vector3 position,
            Color color,
            float radius,
            float life)
        {
            SpawnRuneCircle(
                position,
                color,
                radius,
                life);

            GameObject cross =
                new GameObject(
                    "HF_V010_GROUND_CROSS");

            cross.transform.position =
                position + Vector3.up * 0.02f;

            LineRenderer lr =
                cross.AddComponent<LineRenderer>();

            lr.positionCount = 4;
            lr.useWorldSpace = false;
            lr.widthMultiplier = 0.045f;

            lr.sharedMaterial =
                MakeMaterial(
                    color,
                    color * 2.6f);

            lr.SetPosition(
                0,
                new Vector3(-radius, 0f, 0f));

            lr.SetPosition(
                1,
                new Vector3(radius, 0f, 0f));

            lr.SetPosition(
                2,
                new Vector3(0f, 0f, -radius));

            lr.SetPosition(
                3,
                new Vector3(0f, 0f, radius));

            HighflySimpleLifetime lt =
                cross.AddComponent<HighflySimpleLifetime>();

            lt.Initialize(life);
        }

        private static void SpawnThreatTelegraph(
            Vector3 source,
            Color color)
        {
            GameObject go =
                new GameObject(
                    "HF_V010_LETHAL_VECTOR");

            LineRenderer lr =
                go.AddComponent<LineRenderer>();

            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.widthMultiplier = 0.08f;

            lr.sharedMaterial =
                MakeMaterial(
                    color,
                    color * 4f);

            lr.SetPosition(
                0,
                source + Vector3.up * 1.0f);

            Vector3 end =
                Instance != null
                    ? Instance.transform.position +
                      Vector3.up * 1.0f
                    : source + Vector3.forward * 3f;

            lr.SetPosition(1, end);

            HighflySimpleLifetime lt =
                go.AddComponent<HighflySimpleLifetime>();

            lt.Initialize(0.52f);
        }

        private GameObject SpawnHunterArmGhost(
            Vector3 targetPoint,
            Color color,
            float initialScale)
        {
            if (_animator == null)
                return null;

            Transform upper =
                _animator.GetBoneTransform(
                    HumanBodyBones.RightUpperArm);

            Transform lower =
                _animator.GetBoneTransform(
                    HumanBodyBones.RightLowerArm);

            Transform hand =
                _animator.GetBoneTransform(
                    HumanBodyBones.RightHand);

            if (hand == null)
                return null;

            SkinnedMeshRenderer[] skins =
                _animator.GetComponentsInChildren<SkinnedMeshRenderer>(
                    true);

            for (int s = 0; s < skins.Length; s++)
            {
                SkinnedMeshRenderer src = skins[s];

                if (src == null ||
                    src.sharedMesh == null)
                    continue;

                Transform[] bones = src.bones;

                int iu = Array.IndexOf(bones, upper);
                int il = Array.IndexOf(bones, lower);
                int ih = Array.IndexOf(bones, hand);

                if (ih < 0 && il < 0 && iu < 0)
                    continue;

                Mesh baked = new Mesh();
                src.BakeMesh(baked);

                Mesh shared = src.sharedMesh;
                BoneWeight[] weights = shared.boneWeights;
                int[] tris = shared.triangles;

                if (weights == null ||
                    weights.Length == 0 ||
                    tris == null ||
                    tris.Length == 0)
                    continue;

                var keep = new List<int>();

                for (int i = 0; i + 2 < tris.Length; i += 3)
                {
                    int a = tris[i];
                    int b = tris[i + 1];
                    int d = tris[i + 2];

                    if (a >= weights.Length ||
                        b >= weights.Length ||
                        d >= weights.Length)
                        continue;

                    int score = 0;

                    if (ArmVertex(weights[a], iu, il, ih))
                        score++;

                    if (ArmVertex(weights[b], iu, il, ih))
                        score++;

                    if (ArmVertex(weights[d], iu, il, ih))
                        score++;

                    if (score >= 2)
                    {
                        keep.Add(a);
                        keep.Add(b);
                        keep.Add(d);
                    }
                }

                if (keep.Count < 6)
                    continue;

                Vector3 pivot = hand.position;
                Vector3[] verts = baked.vertices;

                for (int i = 0; i < verts.Length; i++)
                {
                    verts[i] =
                        src.transform.TransformPoint(
                            verts[i]) -
                        pivot;
                }

                Mesh m = new Mesh();

                if (verts.Length > 65535)
                {
                    m.indexFormat =
                        UnityEngine.Rendering.IndexFormat.UInt32;
                }

                m.vertices = verts;
                m.triangles = keep.ToArray();

                Vector3[] normals = baked.normals;

                if (normals != null &&
                    normals.Length == verts.Length)
                {
                    for (int i = 0; i < normals.Length; i++)
                    {
                        normals[i] =
                            src.transform.TransformDirection(
                                normals[i]);
                    }

                    m.normals = normals;
                }
                else
                {
                    m.RecalculateNormals();
                }

                Vector2[] uv = baked.uv;

                if (uv != null &&
                    uv.Length == verts.Length)
                {
                    m.uv = uv;
                }

                m.RecalculateBounds();

                GameObject go =
                    new GameObject(
                        "HF_HUNTER_SHADOW_ARM");

                go.transform.position = pivot;

                Vector3 dir =
                    targetPoint - pivot;

                dir.y *= 0.35f;

                if (dir.sqrMagnitude > 0.01f)
                {
                    go.transform.rotation =
                        Quaternion.FromToRotation(
                            transform.forward,
                            dir.normalized);
                }

                go.transform.localScale =
                    Vector3.one * initialScale;

                MeshFilter mf =
                    go.AddComponent<MeshFilter>();

                mf.sharedMesh = m;

                MeshRenderer mr =
                    go.AddComponent<MeshRenderer>();

                mr.sharedMaterial =
                    MakeMaterial(
                        new Color(
                            0.12f,
                            0.015f,
                            0.20f,
                            0.88f),
                        color * 3.5f);

                HighflySimpleLifetime lt =
                    go.AddComponent<HighflySimpleLifetime>();

                lt.Initialize(1.15f);

                HighflyPremiumFx.SpawnResource(
                    "ElectricalSparks",
                    pivot,
                    Quaternion.identity,
                    0.75f,
                    0.55f,
                    color);

                return go;
            }

            return null;
        }

        private static bool ArmVertex(
            BoneWeight w,
            int upper,
            int lower,
            int hand)
        {
            return
                BoneHit(
                    w.boneIndex0,
                    w.weight0,
                    upper,
                    lower,
                    hand) ||
                BoneHit(
                    w.boneIndex1,
                    w.weight1,
                    upper,
                    lower,
                    hand) ||
                BoneHit(
                    w.boneIndex2,
                    w.weight2,
                    upper,
                    lower,
                    hand) ||
                BoneHit(
                    w.boneIndex3,
                    w.weight3,
                    upper,
                    lower,
                    hand);
        }

        private static bool BoneHit(
            int idx,
            float weight,
            int upper,
            int lower,
            int hand)
        {
            return
                weight > 0.18f &&
                idx >= 0 &&
                (idx == upper ||
                 idx == lower ||
                 idx == hand);
        }

        private static GameObject CreateBlade(
            string name,
            Vector3 position,
            Quaternion rotation,
            Color color,
            float scale)
        {
            GameObject root = new GameObject(name);

            root.transform.position = position;
            root.transform.rotation = rotation;
            root.transform.localScale = Vector3.one * scale;

            GameObject blade =
                GameObject.CreatePrimitive(PrimitiveType.Cube);

            blade.transform.SetParent(
                root.transform,
                false);

            blade.transform.localPosition =
                new Vector3(
                    0f,
                    0f,
                    0.65f);

            blade.transform.localScale =
                new Vector3(
                    0.10f,
                    0.045f,
                    1.3f);

            Renderer r =
                blade.GetComponent<Renderer>();

            if (r != null)
            {
                r.sharedMaterial =
                    MakeMaterial(
                        new Color(
                            color.r * 0.22f,
                            color.g * 0.22f,
                            color.b * 0.22f,
                            0.95f),
                        color * 2.8f);
            }

            Collider collider =
                blade.GetComponent<Collider>();

            if (collider != null)
                collider.enabled = false;

            return root;
        }

        private static Material MakeMaterial(
            Color baseColor,
            Color emission)
        {
            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Unlit") ??
                Shader.Find(
                    "Universal Render Pipeline/Lit") ??
                Shader.Find("Sprites/Default");

            Material mat =
                new Material(shader);

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor(
                    "_BaseColor",
                    baseColor);

            if (mat.HasProperty("_Color"))
                mat.SetColor(
                    "_Color",
                    baseColor);

            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");

                mat.SetColor(
                    "_EmissionColor",
                    emission);
            }

            return mat;
        }
    }

    public sealed class HighflyV010Pulse : MonoBehaviour
    {
        private float _start;
        private float _life;
        private Vector3 _base;

        public void Initialize(
            float radius,
            float life)
        {
            _start = Time.unscaledTime;
            _life = Mathf.Max(0.05f, life);
            _base = transform.localScale;
        }

        private void Update()
        {
            float t =
                Mathf.Clamp01(
                    (Time.unscaledTime - _start) /
                    _life);

            float p =
                1f +
                Mathf.Sin(t * Mathf.PI) *
                0.12f;

            transform.localScale =
                _base * p;
        }
    }
}
