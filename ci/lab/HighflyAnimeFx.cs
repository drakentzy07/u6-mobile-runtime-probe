using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Highfly.SkillLab
{
    public static class HighflyAnimeFx
    {
        public static void SpawnBladeCut(
            Vector3 origin,
            Vector3 forward,
            Color color,
            float rollDegrees,
            float length = 2.6f,
            float width = 0.42f,
            float duration = 0.18f)
        {
            SpawnBladeLayer(origin, forward, color, rollDegrees, length, width, duration, 1.0f, 0.74f);
            SpawnBladeLayer(origin, forward, Color.white, rollDegrees, length * 0.94f, width * 0.26f, duration * 0.82f, 1.035f, 0.96f);
        }

        private static void SpawnBladeLayer(
            Vector3 origin,
            Vector3 forward,
            Color color,
            float rollDegrees,
            float length,
            float width,
            float duration,
            float scale,
            float alpha)
        {
            var go = new GameObject("HF_ANIME_BLADE_CUT");
            go.transform.position = origin;
            go.transform.rotation =
                Quaternion.LookRotation(forward.normalized, Vector3.up) *
                Quaternion.Euler(0f, 0f, rollDegrees);
            go.transform.localScale = Vector3.one * scale;

            const int segments = 20;
            Vector3[] vertices = new Vector3[(segments + 1) * 2];
            int[] triangles = new int[segments * 6];

            float halfArc = 0.95f;

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float x = Mathf.Lerp(-length * 0.5f, length * 0.5f, t);
                float arch = Mathf.Sin(t * Mathf.PI);
                float centerY = arch * halfArc - halfArc * 0.34f;

                // Taper to knife points at both ends.
                float taper = Mathf.Pow(Mathf.Sin(t * Mathf.PI), 0.62f);
                float localWidth = Mathf.Max(0.012f, width * taper);

                vertices[i * 2] = new Vector3(x, centerY - localWidth * 0.5f, 0f);
                vertices[i * 2 + 1] = new Vector3(x, centerY + localWidth * 0.5f, 0f);

                if (i < segments)
                {
                    int v = i * 2;
                    int tri = i * 6;
                    triangles[tri + 0] = v;
                    triangles[tri + 1] = v + 3;
                    triangles[tri + 2] = v + 1;
                    triangles[tri + 3] = v;
                    triangles[tri + 4] = v + 2;
                    triangles[tri + 5] = v + 3;
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = "HF_AnimeBladeMesh";
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = HighflyPremiumFx.CreateTransparentMaterial(
                new Color(color.r, color.g, color.b, alpha),
                color * 4.2f);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var life = go.AddComponent<HighflyBladeCutLifetime>();
            life.Initialize(mesh, mr, duration);
        }

        public static void SpawnLightningBurst(
            Vector3 from,
            Vector3 to,
            Color color,
            int strands = 3,
            float life = 0.16f)
        {
            Vector3 delta = to - from;
            Vector3 side = Vector3.Cross(delta.normalized, Vector3.up);
            if (side.sqrMagnitude < 0.01f)
                side = Vector3.right;
            side.Normalize();

            for (int i = 0; i < strands; i++)
            {
                float offset = (i - (strands - 1) * 0.5f) * 0.11f;
                HighflyPremiumFx.SpawnLightningSegment(
                    from + side * offset,
                    to - side * offset,
                    i == strands / 2 ? Color.white : color,
                    life + i * 0.018f);
            }

            HighflyPremiumFx.SpawnResource(
                "ElectricalSparks",
                to,
                Quaternion.identity,
                0.48f,
                0.72f,
                color);
        }

        public static HighflyShadowHandFx SpawnShadowHand(
            Transform target,
            float duration = 1.15f,
            Color? color = null)
        {
            if (target == null) return null;

            var go = new GameObject("HF_SHADOW_HAND");
            var hand = go.AddComponent<HighflyShadowHandFx>();
            hand.Initialize(
                target,
                duration,
                color ?? new Color(0.34f, 0.05f, 0.62f, 1f));
            return hand;
        }

        public static HighflyGrandMagicCircleFx SpawnGrandMagicCircle(
            Transform target,
            Color color,
            float radius,
            float duration)
        {
            if (target == null) return null;

            var go = new GameObject("HF_GRAND_MAGIC_CIRCLE");
            var fx = go.AddComponent<HighflyGrandMagicCircleFx>();
            fx.Initialize(target, color, radius, duration);
            return fx;
        }

        public static HighflyReflectWallFx SpawnReflectWall(
            Transform owner,
            Color color,
            float duration)
        {
            if (owner == null) return null;

            var go = new GameObject("HF_REFLECT_WALL");
            var fx = go.AddComponent<HighflyReflectWallFx>();
            fx.Initialize(owner, color, duration);
            return fx;
        }

        public static GameObject SpawnDecoyClone(
            Transform playerRoot,
            Color color,
            float duration)
        {
            if (playerRoot == null) return null;

            var ghostRoot = new GameObject("HF_DECOY_CLONE");
            ghostRoot.transform.position = Vector3.zero;
            ghostRoot.transform.rotation = Quaternion.identity;
            ghostRoot.transform.localScale = Vector3.one;

            Material ghostMat =
                HighflyPremiumFx.CreateTransparentMaterial(
                    new Color(color.r, color.g, color.b, 0.62f),
                    color * 2.4f);

            foreach (SkinnedMeshRenderer smr in
                     playerRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr == null || !smr.enabled) continue;

                Mesh baked = new Mesh();
                smr.BakeMesh(baked);

                var part = new GameObject("Decoy_" + smr.name);
                part.transform.SetParent(ghostRoot.transform, false);
                part.transform.position = smr.transform.position;
                part.transform.rotation = smr.transform.rotation;
                part.transform.localScale = smr.transform.lossyScale;

                part.AddComponent<MeshFilter>().sharedMesh = baked;

                var mr = part.AddComponent<MeshRenderer>();
                int count = Mathf.Max(1, smr.sharedMaterials.Length);
                var mats = new Material[count];
                for (int i = 0; i < mats.Length; i++) mats[i] = ghostMat;
                mr.sharedMaterials = mats;
                mr.shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            foreach (MeshFilter source in
                     playerRoot.GetComponentsInChildren<MeshFilter>(true))
            {
                if (source == null || source.sharedMesh == null) continue;

                Renderer sourceRenderer = source.GetComponent<Renderer>();
                if (sourceRenderer == null || !sourceRenderer.enabled) continue;

                var part = new GameObject("DecoyMesh_" + source.name);
                part.transform.SetParent(ghostRoot.transform, false);
                part.transform.position = source.transform.position;
                part.transform.rotation = source.transform.rotation;
                part.transform.localScale = source.transform.lossyScale;

                part.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;

                var mr = part.AddComponent<MeshRenderer>();
                mr.sharedMaterial = ghostMat;
                mr.shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            var pulse = ghostRoot.AddComponent<HighflyDecoyPulseFx>();
            pulse.Initialize(duration);

            return ghostRoot;
        }

        public static void SpawnImpactCross(
            Vector3 position,
            Vector3 forward,
            Color color,
            float size = 2.4f)
        {
            SpawnBladeCut(position, forward, color, 45f, size, 0.38f, 0.16f);
            SpawnBladeCut(position, forward, Color.white, -45f, size * 0.96f, 0.24f, 0.14f);
            HighflyPremiumFx.SpawnResource("Sparks", position, Quaternion.identity, 0.52f, 0.58f, color);
        }
    }

    public sealed class HighflyReflectWallFx : MonoBehaviour
    {
        private Transform _owner;
        private float _end;
        private readonly List<Transform> _layers = new List<Transform>();

        public void Initialize(Transform owner, Color color, float duration)
        {
            _owner = owner;
            _end = Time.unscaledTime + duration;

            transform.SetParent(owner, false);
            transform.localPosition = new Vector3(0f, 1.05f, 1.45f);
            transform.localRotation = Quaternion.identity;

            _layers.Add(CreateArc("Outer", color, 1.65f, 0.080f, 54, 145f));
            _layers.Add(CreateArc("Mid", Color.white, 1.28f, 0.036f, 46, 138f));
            _layers.Add(CreateArc("Inner", color, 0.88f, 0.030f, 38, 132f));

            CreateSpokes(color);
        }

        private Transform CreateArc(
            string name,
            Color color,
            float radius,
            float width,
            int points,
            float degrees)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.positionCount = points;
            lr.widthMultiplier = width;
            lr.numCapVertices = 5;
            lr.sharedMaterial =
                HighflyPremiumFx.CreateTransparentMaterial(
                    new Color(color.r, color.g, color.b, 0.76f),
                    color * 3.1f);

            float half = degrees * 0.5f;

            for (int i = 0; i < points; i++)
            {
                float t = i / (float)(points - 1);
                float a = Mathf.Lerp(-half, half, t) * Mathf.Deg2Rad;

                lr.SetPosition(
                    i,
                    new Vector3(
                        Mathf.Sin(a) * radius,
                        Mathf.Cos(a) * radius - radius * 0.25f,
                        0f));
            }

            return go.transform;
        }

        private void CreateSpokes(Color color)
        {
            for (int i = -3; i <= 3; i++)
            {
                var go = new GameObject("RuneSpoke_" + i);
                go.transform.SetParent(transform, false);

                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = false;
                lr.positionCount = 3;
                lr.widthMultiplier = 0.024f;
                lr.sharedMaterial =
                    HighflyPremiumFx.CreateTransparentMaterial(
                        new Color(color.r, color.g, color.b, 0.62f),
                        color * 2.5f);

                float x = i * 0.25f;
                lr.SetPosition(0, new Vector3(x * 0.45f, -0.95f, 0f));
                lr.SetPosition(1, new Vector3(x, 0.0f, 0f));
                lr.SetPosition(2, new Vector3(x * 0.45f, 0.95f, 0f));
            }
        }

        private void LateUpdate()
        {
            if (_owner == null || Time.unscaledTime >= _end)
            {
                Destroy(gameObject);
                return;
            }

            float dt = Time.unscaledDeltaTime;

            for (int i = 0; i < _layers.Count; i++)
            {
                if (_layers[i] == null) continue;
                _layers[i].localScale =
                    Vector3.one *
                    (1f + Mathf.Sin(Time.unscaledTime * 7f + i) * 0.020f);
            }

            transform.localRotation =
                Quaternion.Euler(
                    0f,
                    Mathf.Sin(Time.unscaledTime * 4.5f) * 1.6f,
                    0f);
        }
    }

    public sealed class HighflyDecoyPulseFx : MonoBehaviour
    {
        private float _start;
        private float _duration;
        private Renderer[] _renderers;

        public void Initialize(float duration)
        {
            _start = Time.unscaledTime;
            _duration = Mathf.Max(0.15f, duration);
            _renderers = GetComponentsInChildren<Renderer>(true);
        }

        private void Update()
        {
            float t = Mathf.Clamp01(
                (Time.unscaledTime - _start) / _duration);

            float pulse = 0.94f + Mathf.Sin(Time.unscaledTime * 18f) * 0.035f;
            transform.localScale = Vector3.one * pulse;

            float alpha =
                Mathf.Lerp(0.68f, 0.12f, Mathf.Pow(t, 1.5f));

            foreach (Renderer r in _renderers)
            {
                if (r == null) continue;

                foreach (Material m in r.materials)
                {
                    if (m == null) continue;

                    if (m.HasProperty("_BaseColor"))
                    {
                        Color col = m.GetColor("_BaseColor");
                        col.a = alpha;
                        m.SetColor("_BaseColor", col);
                    }

                    if (m.HasProperty("_Color"))
                    {
                        Color col = m.GetColor("_Color");
                        col.a = alpha;
                        m.SetColor("_Color", col);
                    }
                }
            }

            if (t >= 1f)
                Destroy(gameObject);
        }
    }

    public sealed class HighflyBladeCutLifetime : MonoBehaviour
    {
        private Mesh _mesh;
        private MeshRenderer _renderer;
        private float _start;
        private float _duration;

        public void Initialize(Mesh mesh, MeshRenderer renderer, float duration)
        {
            _mesh = mesh;
            _renderer = renderer;
            _start = Time.unscaledTime;
            _duration = Mathf.Max(0.05f, duration);
        }

        private void Update()
        {
            float t = Mathf.Clamp01((Time.unscaledTime - _start) / _duration);

            transform.localScale = Vector3.Lerp(
                Vector3.one * 0.72f,
                Vector3.one * 1.18f,
                1f - Mathf.Pow(1f - t, 2f));

            if (_renderer != null)
            {
                foreach (Material m in _renderer.materials)
                {
                    if (m == null) continue;
                    float a = Mathf.Lerp(0.92f, 0f, t);

                    if (m.HasProperty("_BaseColor"))
                    {
                        Color c = m.GetColor("_BaseColor");
                        c.a = a;
                        m.SetColor("_BaseColor", c);
                    }

                    if (m.HasProperty("_Color"))
                    {
                        Color c = m.GetColor("_Color");
                        c.a = a;
                        m.SetColor("_Color", c);
                    }
                }
            }

            if (t >= 1f)
            {
                if (_mesh != null) Destroy(_mesh);
                Destroy(gameObject);
            }
        }
    }

    public sealed class HighflyShadowHandFx : MonoBehaviour
    {
        private Transform _target;
        private float _start;
        private float _duration;
        private Color _color;
        private LineRenderer[] _fingers;
        private LineRenderer _palm;

        public void Initialize(Transform target, float duration, Color color)
        {
            _target = target;
            _duration = Mathf.Max(0.2f, duration);
            _start = Time.unscaledTime;
            _color = color;

            _palm = CreateLine("Palm", 0.18f, color);
            _palm.positionCount = 8;

            _fingers = new LineRenderer[5];
            for (int i = 0; i < _fingers.Length; i++)
            {
                _fingers[i] = CreateLine("Finger_" + i, 0.105f - i * 0.006f, color);
                _fingers[i].positionCount = 10;
            }
        }

        private LineRenderer CreateLine(string name, float width, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.widthMultiplier = width;
            lr.numCapVertices = 6;
            lr.numCornerVertices = 4;
            lr.sharedMaterial = HighflyPremiumFx.CreateTransparentMaterial(
                new Color(color.r * 0.35f, color.g * 0.28f, color.b * 0.42f, 0.78f),
                color * 2.8f);
            return lr;
        }

        private void Update()
        {
            if (_target == null)
            {
                Destroy(gameObject);
                return;
            }

            float age = Time.unscaledTime - _start;
            float t = Mathf.Clamp01(age / _duration);
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 center = _target.position + Vector3.up * 0.78f;
            Vector3 back = -_target.forward;
            Vector3 right = _target.right;

            float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.28f));
            float grip = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.18f) / 0.28f));

            Vector3 palmCenter = center + back * Mathf.Lerp(1.65f, 0.62f, rise);

            for (int i = 0; i < 8; i++)
            {
                float a = (i / 7f - 0.5f) * 1.35f;
                _palm.SetPosition(
                    i,
                    palmCenter +
                    right * Mathf.Sin(a) * 0.62f +
                    Vector3.up * Mathf.Cos(a) * 0.28f);
            }

            for (int f = 0; f < _fingers.Length; f++)
            {
                float spread = (f - 2) * 0.28f;
                Vector3 fingerStart =
                    palmCenter +
                    right * spread +
                    Vector3.up * (0.28f - Mathf.Abs(spread) * 0.16f);

                Vector3 gripTarget =
                    center +
                    right * spread * 0.34f +
                    Vector3.up * Mathf.Lerp(0.55f, -0.34f, f / 4f);

                for (int p = 0; p < 10; p++)
                {
                    float u = p / 9f;
                    Vector3 point = Vector3.Lerp(fingerStart, gripTarget, u * grip);
                    float curl = Mathf.Sin(u * Mathf.PI) * (0.26f + 0.05f * f);
                    point += back * curl * (1f - grip * 0.25f);
                    _fingers[f].SetPosition(p, point);
                }
            }
        }
    }

    public sealed class HighflyGrandMagicCircleFx : MonoBehaviour
    {
        private Transform _target;
        private Color _color;
        private float _radius;
        private float _end;
        private readonly List<Transform> _rings = new List<Transform>();
        private Transform _glyphRoot;

        public void Initialize(Transform target, Color color, float radius, float duration)
        {
            _target = target;
            _color = color;
            _radius = radius;
            _end = Time.unscaledTime + duration;

            transform.position = target.position + Vector3.up * 0.035f;

            _rings.Add(CreateRing("Outer", radius, 0.055f, 96));
            _rings.Add(CreateRing("Mid", radius * 0.73f, 0.035f, 80));
            _rings.Add(CreateRing("Inner", radius * 0.42f, 0.028f, 64));

            CreateStar(radius * 0.70f, 8);
            CreateStar(radius * 0.42f, 5);
            CreateGlyphs();
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
                new Color(_color.r, _color.g, _color.b, 0.66f),
                _color * 2.7f);

            for (int i = 0; i < points; i++)
            {
                float a = (i / (float)points) * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }

            return go.transform;
        }

        private void CreateStar(float radius, int points)
        {
            var go = new GameObject("Star_" + points);
            go.transform.SetParent(transform, false);

            var lr = go.AddComponent<LineRenderer>();
            lr.loop = true;
            lr.useWorldSpace = false;
            lr.positionCount = points * 2;
            lr.widthMultiplier = 0.026f;
            lr.sharedMaterial = HighflyPremiumFx.CreateTransparentMaterial(
                new Color(_color.r, _color.g, _color.b, 0.58f),
                _color * 2.2f);

            for (int i = 0; i < points * 2; i++)
            {
                float a = (i / (float)(points * 2)) * Mathf.PI * 2f;
                float r = (i % 2 == 0) ? radius : radius * 0.40f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * r, 0.005f, Mathf.Sin(a) * r));
            }
        }

        private void CreateGlyphs()
        {
            _glyphRoot = new GameObject("Runes").transform;
            _glyphRoot.SetParent(transform, false);

            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2f;
                Vector3 radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 tangent = new Vector3(-radial.z, 0f, radial.x);

                var rune = new GameObject("Rune_" + i);
                rune.transform.SetParent(_glyphRoot, false);

                var lr = rune.AddComponent<LineRenderer>();
                lr.useWorldSpace = false;
                lr.positionCount = 4;
                lr.widthMultiplier = 0.026f;
                lr.sharedMaterial = HighflyPremiumFx.CreateTransparentMaterial(
                    new Color(_color.r, _color.g, _color.b, 0.78f),
                    _color * 3f);

                Vector3 basePos = radial * (_radius * 0.86f);
                lr.SetPosition(0, basePos - tangent * 0.11f);
                lr.SetPosition(1, basePos + radial * 0.16f);
                lr.SetPosition(2, basePos + tangent * 0.11f);
                lr.SetPosition(3, basePos - radial * 0.08f);
            }
        }

        private void LateUpdate()
        {
            if (_target == null || Time.unscaledTime >= _end)
            {
                Destroy(gameObject);
                return;
            }

            transform.position = _target.position + Vector3.up * 0.035f;

            if (_rings.Count > 0 && _rings[0] != null)
                _rings[0].Rotate(0f, 28f * Time.unscaledDeltaTime, 0f);
            if (_rings.Count > 1 && _rings[1] != null)
                _rings[1].Rotate(0f, -46f * Time.unscaledDeltaTime, 0f);
            if (_rings.Count > 2 && _rings[2] != null)
                _rings[2].Rotate(0f, 68f * Time.unscaledDeltaTime, 0f);
            if (_glyphRoot != null)
                _glyphRoot.Rotate(0f, -22f * Time.unscaledDeltaTime, 0f);

            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.022f;
            transform.localScale = Vector3.one * pulse;
        }
    }
}
