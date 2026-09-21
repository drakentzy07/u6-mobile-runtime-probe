using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Highfly.SkillLab
{
    public static class HighflyPremiumFx
    {
        private const string Root = "HIGHFLY/SkillVFX/";

        public static GameObject SpawnResource(
            string resourceName,
            Vector3 position,
            Quaternion rotation,
            float scale = 1f,
            float lifetime = 2.5f,
            Color? tint = null)
        {
            GameObject prefab = Resources.Load<GameObject>(Root + resourceName);
            if (prefab == null)
            {
                Debug.LogWarning("[HIGHFLY LAB] VFX resource missing: " + resourceName);
                return null;
            }

            GameObject go = UnityEngine.Object.Instantiate(prefab, position, rotation);
            go.name = "HF_FX_" + resourceName;
            go.transform.localScale *= scale;

            if (tint.HasValue)
                TintParticles(go, tint.Value);

            UnityEngine.Object.Destroy(go, lifetime);
            return go;
        }

        public static GameObject SpawnShadowKnight(Vector3 position, Quaternion rotation)
        {
            GameObject prefab = Resources.Load<GameObject>(Root + "ShadowKnight");
            if (prefab == null)
                return null;

            GameObject go = UnityEngine.Object.Instantiate(prefab, position, rotation);
            go.name = "HIGHFLY_SHADOW_KNIGHT";

            foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.Destroy(c);

            foreach (Rigidbody rb in go.GetComponentsInChildren<Rigidbody>(true))
                UnityEngine.Object.Destroy(rb);

            Material ghost = CreateTransparentMaterial(
                new Color(0.045f, 0.012f, 0.09f, 0.90f),
                new Color(0.45f, 0.08f, 0.95f, 1f));

            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                var mats = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = ghost;
                r.sharedMaterials = mats;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            Animator animator = go.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                animator.enabled = true;
                animator.applyRootMotion = false;
            }

            return go;
        }

        public static void AttachWeaponTrail(
            PlayerController player,
            Color start,
            Color end,
            float duration = 0.32f,
            float width = 0.22f)
        {
            if (player == null || player.myWeapon == null) return;

            Transform weapon = player.myWeapon.transform;

            var trailGo = new GameObject("HF_PREMIUM_WEAPON_TRAIL");
            trailGo.transform.SetParent(weapon, false);
            trailGo.transform.localPosition = Vector3.zero;
            trailGo.transform.localRotation = Quaternion.identity;

            TrailRenderer trail = trailGo.AddComponent<TrailRenderer>();
            trail.time = 0.16f;
            trail.minVertexDistance = 0.025f;
            trail.numCapVertices = 5;
            trail.numCornerVertices = 5;
            trail.textureMode = LineTextureMode.Stretch;
            trail.alignment = LineAlignment.View;
            trail.widthCurve = new AnimationCurve(
                new Keyframe(0f, width * 0.25f),
                new Keyframe(0.28f, width),
                new Keyframe(1f, 0f));

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(start, 0f),
                    new GradientColorKey(Color.white, 0.42f),
                    new GradientColorKey(end, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.9f, 0f),
                    new GradientAlphaKey(0.75f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                });
            trail.colorGradient = gradient;
            trail.sharedMaterial = CreateTransparentMaterial(Color.white, end * 2.5f);

            HighflyFxLifetime life = trailGo.AddComponent<HighflyFxLifetime>();
            life.Initialize(duration, trail);
        }

        public static void SpawnAfterImage(
            Transform playerRoot,
            Color color,
            float lifetime = 0.26f)
        {
            if (playerRoot == null) return;

            var ghostRoot = new GameObject("HF_PHANTOM_AFTERIMAGE");
            ghostRoot.transform.position = Vector3.zero;
            ghostRoot.transform.rotation = Quaternion.identity;
            ghostRoot.transform.localScale = Vector3.one;

            Material ghostMat = CreateTransparentMaterial(
                new Color(color.r, color.g, color.b, 0.32f),
                color * 2.2f);

            foreach (SkinnedMeshRenderer smr in playerRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr == null || !smr.enabled) continue;

                Mesh baked = new Mesh();
                smr.BakeMesh(baked);

                var part = new GameObject("Ghost_" + smr.name);
                part.transform.SetParent(ghostRoot.transform, false);
                part.transform.position = smr.transform.position;
                part.transform.rotation = smr.transform.rotation;
                part.transform.localScale = smr.transform.lossyScale;

                var mf = part.AddComponent<MeshFilter>();
                mf.sharedMesh = baked;

                var mr = part.AddComponent<MeshRenderer>();
                int count = Mathf.Max(1, smr.sharedMaterials.Length);
                var mats = new Material[count];
                for (int i = 0; i < count; i++) mats[i] = ghostMat;
                mr.sharedMaterials = mats;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            foreach (MeshFilter source in playerRoot.GetComponentsInChildren<MeshFilter>(true))
            {
                if (source == null || source.sharedMesh == null) continue;
                if (source.GetComponent<SkinnedMeshRenderer>() != null) continue;

                Renderer sourceRenderer = source.GetComponent<Renderer>();
                if (sourceRenderer == null || !sourceRenderer.enabled) continue;

                var part = new GameObject("GhostMesh_" + source.name);
                part.transform.SetParent(ghostRoot.transform, false);
                part.transform.position = source.transform.position;
                part.transform.rotation = source.transform.rotation;
                part.transform.localScale = source.transform.lossyScale;

                part.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
                var mr = part.AddComponent<MeshRenderer>();
                mr.sharedMaterial = ghostMat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            HighflyGhostFade fade = ghostRoot.AddComponent<HighflyGhostFade>();
            fade.Initialize(lifetime);
        }

        public static void SpawnLightningSegment(Vector3 from, Vector3 to, Color color, float life = 0.12f)
        {
            if ((to - from).sqrMagnitude < 0.015f) return;

            var root = new GameObject("HF_PHANTOM_LIGHTNING");

            LineRenderer glow = CreateBoltLine(root.transform, "Glow", color, 0.11f, 16);
            LineRenderer core = CreateBoltLine(root.transform, "Core", Color.white, 0.032f, 16);

            Vector3 delta = to - from;
            Vector3 tangent = delta.normalized;
            Vector3 side = Vector3.Cross(tangent, Vector3.up);
            if (side.sqrMagnitude < 0.01f)
                side = Vector3.Cross(tangent, Vector3.right);
            side.Normalize();
            Vector3 up = Vector3.Cross(side, tangent).normalized;

            for (int i = 0; i < 16; i++)
            {
                float t = i / 15f;
                Vector3 p = Vector3.Lerp(from, to, t);

                float envelope = Mathf.Sin(t * Mathf.PI);
                float n1 = Mathf.Sin((t * 37f + Time.unscaledTime * 31f) * 2.1f);
                float n2 = Mathf.Cos((t * 29f + Time.unscaledTime * 23f) * 1.7f);
                p += side * n1 * 0.08f * envelope;
                p += up * n2 * 0.055f * envelope;

                glow.SetPosition(i, p);
                core.SetPosition(i, p);
            }

            root.AddComponent<HighflySimpleLifetime>().Initialize(life);
        }

        public static void SpawnShadowTether(
            Transform source,
            Transform target,
            Vector3 sourceOffset,
            Vector3 targetOffset,
            float duration,
            int strandIndex)
        {
            if (source == null || target == null) return;

            var root = new GameObject("HF_SHADOW_TETHER_" + strandIndex);
            var fx = root.AddComponent<HighflyShadowTetherFx>();
            fx.Initialize(source, target, sourceOffset, targetOffset, duration, strandIndex);
        }

        public static void SpawnVitalAura(Transform target, float duration)
        {
            if (target == null) return;

            var root = new GameObject("HF_VITAL_AURA");
            var aura = root.AddComponent<HighflyVitalAuraFx>();
            aura.Initialize(target, duration);
        }

        private static LineRenderer CreateBoltLine(
            Transform parent,
            string name,
            Color color,
            float width,
            int points)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = points;
            lr.numCapVertices = 4;
            lr.numCornerVertices = 3;
            lr.widthMultiplier = width;
            lr.sharedMaterial = CreateTransparentMaterial(color, color * 2.5f);

            Gradient g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(color, 0.45f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.12f),
                    new GradientAlphaKey(0.82f, 0.8f),
                    new GradientAlphaKey(0f, 1f)
                });
            lr.colorGradient = g;
            return lr;
        }

        private static void TintParticles(GameObject go, Color tint)
        {
            foreach (ParticleSystem ps in go.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                main.startColor = new ParticleSystem.MinMaxGradient(tint);
            }
        }

        public static void SpawnCrescentSlash(
            Vector3 origin,
            Vector3 forward,
            Color color,
            float radius,
            float thickness,
            float rollDegrees,
            float duration = 0.20f)
        {
            var go = new GameObject("HF_CRESCENT_SLASH");
            go.transform.position = origin;
            go.transform.rotation =
                Quaternion.LookRotation(forward, Vector3.up) *
                Quaternion.Euler(0f, 0f, rollDegrees);

            const int segments = 22;
            float inner = Mathf.Max(0.05f, radius - thickness);
            float outer = radius;
            float startAngle = -58f;
            float endAngle = 58f;

            Vector3[] vertices = new Vector3[(segments + 1) * 2];
            int[] triangles = new int[segments * 6];
            Vector2[] uv = new Vector2[vertices.Length];

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float angle = Mathf.Lerp(startAngle, endAngle, t) * Mathf.Deg2Rad;

                Vector3 dir =
                    new Vector3(
                        Mathf.Sin(angle),
                        Mathf.Cos(angle),
                        0f);

                vertices[i * 2] = dir * inner;
                vertices[i * 2 + 1] = dir * outer;

                uv[i * 2] = new Vector2(t, 0f);
                uv[i * 2 + 1] = new Vector2(t, 1f);

                if (i < segments)
                {
                    int v = i * 2;
                    int ti = i * 6;
                    triangles[ti + 0] = v;
                    triangles[ti + 1] = v + 3;
                    triangles[ti + 2] = v + 1;
                    triangles[ti + 3] = v;
                    triangles[ti + 4] = v + 2;
                    triangles[ti + 5] = v + 3;
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = "HF_CrescentMesh";
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = CreateTransparentMaterial(
                new Color(color.r, color.g, color.b, 0.78f),
                color * 3.5f);

            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            HighflyCrescentFade fade = go.AddComponent<HighflyCrescentFade>();
            fade.Initialize(duration, mesh, mr);
        }

        public static void SpawnVitalSigil(Transform target, float duration)
        {
            if (target == null) return;

            var root = new GameObject("HF_VITAL_SIGIL");
            var sigil = root.AddComponent<HighflyVitalSigilFx>();
            sigil.Initialize(target, duration);
        }

        public static Material CreateTransparentMaterial(Color baseColor, Color emission)
        {
            Shader shader =
                Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("Sprites/Default");

            Material mat = new Material(shader);

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);

            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission);
            }

            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);

            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
            return mat;
        }
    }

    public sealed class HighflyCrescentFade : MonoBehaviour
    {
        private float _start;
        private float _duration;
        private Mesh _mesh;
        private MeshRenderer _renderer;

        public void Initialize(float duration, Mesh mesh, MeshRenderer renderer)
        {
            _start = Time.unscaledTime;
            _duration = Mathf.Max(0.05f, duration);
            _mesh = mesh;
            _renderer = renderer;
        }

        private void Update()
        {
            float t = (Time.unscaledTime - _start) / _duration;
            if (t >= 1f)
            {
                if (_mesh != null) Destroy(_mesh);
                Destroy(gameObject);
                return;
            }

            transform.localScale = Vector3.one * Mathf.Lerp(0.86f, 1.08f, t);

            if (_renderer != null)
            {
                foreach (Material m in _renderer.materials)
                {
                    if (m == null) continue;

                    float a = Mathf.Lerp(0.82f, 0f, t);

                    if (m.HasProperty("_BaseColor"))
                    {
                        Color cc = m.GetColor("_BaseColor");
                        cc.a = a;
                        m.SetColor("_BaseColor", cc);
                    }

                    if (m.HasProperty("_Color"))
                    {
                        Color cc = m.GetColor("_Color");
                        cc.a = a;
                        m.SetColor("_Color", cc);
                    }
                }
            }
        }
    }

    public sealed class HighflyVitalSigilFx : MonoBehaviour
    {
        private Transform _target;
        private float _end;
        private Transform _ringA;
        private Transform _ringB;
        private Transform _ringC;

        public void Initialize(Transform target, float duration)
        {
            _target = target;
            _end = Time.unscaledTime + duration;

            transform.position = target.position + Vector3.up * 0.035f;

            _ringA = CreateRing("VitalOuter", 1.65f, 0.055f, 72);
            _ringB = CreateRing("VitalInner", 1.05f, 0.035f, 60);
            _ringC = CreateRuneSpokes();
        }

        private Transform CreateRing(string name, float radius, float width, int points)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);

            var lr = go.AddComponent<LineRenderer>();
            lr.loop = true;
            lr.useWorldSpace = false;
            lr.positionCount = points;
            lr.widthMultiplier = width;
            lr.sharedMaterial = HighflyPremiumFx.CreateTransparentMaterial(
                new Color(0.18f, 0.82f, 0.44f, 0.78f),
                new Color(0.32f, 1f, 0.58f, 1f));

            for (int i = 0; i < points; i++)
            {
                float a = (i / (float)points) * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }

            return go.transform;
        }

        private Transform CreateRuneSpokes()
        {
            var root = new GameObject("VitalRunes");
            root.transform.SetParent(transform, false);

            for (int i = 0; i < 6; i++)
            {
                var go = new GameObject("Rune_" + i);
                go.transform.SetParent(root.transform, false);

                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = false;
                lr.positionCount = 3;
                lr.widthMultiplier = 0.032f;
                lr.sharedMaterial = HighflyPremiumFx.CreateTransparentMaterial(
                    new Color(0.20f, 0.78f, 0.46f, 0.72f),
                    new Color(0.34f, 1f, 0.62f, 1f));

                float a = (i / 6f) * Mathf.PI * 2f;
                Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 p = new Vector3(-d.z, 0f, d.x);

                lr.SetPosition(0, d * 0.48f - p * 0.16f);
                lr.SetPosition(1, d * 1.12f);
                lr.SetPosition(2, d * 0.48f + p * 0.16f);
            }

            return root.transform;
        }

        private void LateUpdate()
        {
            if (_target == null || Time.unscaledTime >= _end)
            {
                Destroy(gameObject);
                return;
            }

            transform.position = _target.position + Vector3.up * 0.035f;

            float dt = Time.unscaledDeltaTime;
            if (_ringA != null) _ringA.Rotate(0f, 42f * dt, 0f, Space.Self);
            if (_ringB != null) _ringB.Rotate(0f, -72f * dt, 0f, Space.Self);
            if (_ringC != null) _ringC.Rotate(0f, 26f * dt, 0f, Space.Self);

            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 4.5f) * 0.035f;
            transform.localScale = Vector3.one * pulse;
        }
    }

    public sealed class HighflyFxLifetime : MonoBehaviour
    {
        private float _end;
        private TrailRenderer _trail;

        public void Initialize(float duration, TrailRenderer trail)
        {
            _end = Time.unscaledTime + duration;
            _trail = trail;
        }

        private void Update()
        {
            if (Time.unscaledTime < _end) return;

            if (_trail != null)
                _trail.emitting = false;

            Destroy(gameObject, 0.30f);
            enabled = false;
        }
    }

    public sealed class HighflySimpleLifetime : MonoBehaviour
    {
        private float _end;

        public void Initialize(float duration)
        {
            _end = Time.unscaledTime + duration;
        }

        private void Update()
        {
            if (Time.unscaledTime >= _end)
                Destroy(gameObject);
        }
    }

    public sealed class HighflyGhostFade : MonoBehaviour
    {
        private float _start;
        private float _duration;
        private Renderer[] _renderers;

        public void Initialize(float duration)
        {
            _start = Time.unscaledTime;
            _duration = Mathf.Max(0.05f, duration);
            _renderers = GetComponentsInChildren<Renderer>(true);
        }

        private void Update()
        {
            float t = (Time.unscaledTime - _start) / _duration;
            if (t >= 1f)
            {
                foreach (MeshFilter mf in GetComponentsInChildren<MeshFilter>(true))
                {
                    if (mf.sharedMesh != null && mf.gameObject.name.StartsWith("Ghost_"))
                        Destroy(mf.sharedMesh);
                }

                Destroy(gameObject);
                return;
            }

            float alpha = Mathf.Lerp(0.42f, 0f, t);
            foreach (Renderer r in _renderers)
            {
                if (r == null) continue;
                foreach (Material m in r.materials)
                {
                    if (m == null) continue;

                    if (m.HasProperty("_BaseColor"))
                    {
                        Color c = m.GetColor("_BaseColor");
                        c.a = alpha;
                        m.SetColor("_BaseColor", c);
                    }

                    if (m.HasProperty("_Color"))
                    {
                        Color c = m.GetColor("_Color");
                        c.a = alpha;
                        m.SetColor("_Color", c);
                    }
                }
            }
        }
    }

    public sealed class HighflyShadowTetherFx : MonoBehaviour
    {
        private Transform _source;
        private Transform _target;
        private Vector3 _sourceOffset;
        private Vector3 _targetOffset;
        private float _start;
        private float _duration;
        private float _phase;

        private LineRenderer _glow;
        private LineRenderer _core;

        public void Initialize(
            Transform source,
            Transform target,
            Vector3 sourceOffset,
            Vector3 targetOffset,
            float duration,
            int strandIndex)
        {
            _source = source;
            _target = target;
            _sourceOffset = sourceOffset;
            _targetOffset = targetOffset;
            _start = Time.unscaledTime;
            _duration = duration;
            _phase = strandIndex * 1.71f;

            _glow = CreateLine(
                "Glow",
                new Color(0.20f, 0.02f, 0.38f, 0.60f),
                0.12f);

            _core = CreateLine(
                "Core",
                new Color(0.72f, 0.28f, 1f, 0.92f),
                0.035f);
        }

        private LineRenderer CreateLine(string name, Color color, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);

            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 24;
            lr.widthMultiplier = width;
            lr.numCapVertices = 5;
            lr.numCornerVertices = 4;
            lr.sharedMaterial = HighflyPremiumFx.CreateTransparentMaterial(color, color * 2f);
            return lr;
        }

        private void Update()
        {
            if (_source == null || _target == null)
            {
                Destroy(gameObject);
                return;
            }

            float age = Time.unscaledTime - _start;
            if (age >= _duration)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 a = _source.position + _sourceOffset;
            Vector3 b = _target.position + _targetOffset;
            Vector3 delta = b - a;

            Vector3 side = Vector3.Cross(delta.normalized, Vector3.up);
            if (side.sqrMagnitude < 0.01f) side = Vector3.right;
            side.Normalize();

            for (int i = 0; i < 24; i++)
            {
                float t = i / 23f;
                Vector3 p = Vector3.Lerp(a, b, t);
                float envelope = Mathf.Sin(t * Mathf.PI);

                p += Vector3.up *
                     Mathf.Sin(t * Mathf.PI * 3f + _phase + age * 16f) *
                     0.17f * envelope;

                p += side *
                     Mathf.Sin(t * Mathf.PI * 5f + _phase * 1.3f + age * 11f) *
                     0.12f * envelope;

                _glow.SetPosition(i, p);
                _core.SetPosition(i, p);
            }
        }
    }

    public sealed class HighflyVitalAuraFx : MonoBehaviour
    {
        private Transform _target;
        private float _end;
        private ParticleSystem _particles;

        public void Initialize(Transform target, float duration)
        {
            _target = target;
            _end = Time.unscaledTime + duration;

            transform.position = target.position + Vector3.up * 0.8f;

            _particles = gameObject.AddComponent<ParticleSystem>();
            var main = _particles.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.65f, 1.25f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.12f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.55f, 1f, 0.72f, 0.95f),
                new Color(0.95f, 1f, 0.90f, 0.85f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = _particles.emission;
            emission.rateOverTime = 34f;

            var shape = _particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 1.05f;

            var velocity = _particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.y = new ParticleSystem.MinMaxCurve(0.35f, 1.1f);

            ParticleSystemRenderer renderer = _particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = HighflyPremiumFx.CreateTransparentMaterial(
                new Color(0.45f, 1f, 0.68f, 0.72f),
                new Color(0.25f, 1f, 0.54f, 1f));
        }

        private void LateUpdate()
        {
            if (_target == null || Time.unscaledTime >= _end)
            {
                if (_particles != null)
                {
                    var emission = _particles.emission;
                    emission.enabled = false;
                }

                Destroy(gameObject, 1.5f);
                enabled = false;
                return;
            }

            transform.position = _target.position + Vector3.up * 0.8f;
        }
    }
}
