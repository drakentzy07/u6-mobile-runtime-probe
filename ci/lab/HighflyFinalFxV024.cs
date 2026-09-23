using System.Collections;
using UnityEngine;

namespace Highfly.SkillLab
{
    // Asset-backed VFX for the FINAL / NEAR-FINAL Skill Lab gate.
    // No primitive spheres/cylinders are used as visible skill presentation.
    public static class HighflyFinalFxV024
    {
        private const string Root = "HIGHFLY/FinalVFX/";

        public static GameObject SpawnSlash(
            Vector3 origin,
            Vector3 forward,
            Color color,
            float rollDegrees,
            float length,
            float width,
            float duration)
        {
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            forward.Normalize();

            string texture = Mathf.Abs(rollDegrees) > 40f ? "slash_03" : "slash_02";
            GameObject go = CreateTexturedQuad("HF_FINAL_SLASH", texture, color);
            if (go == null) return null;

            go.transform.position = origin;
            go.transform.rotation =
                Quaternion.LookRotation(forward, Vector3.up) *
                Quaternion.Euler(0f, 0f, rollDegrees);
            go.transform.localScale = new Vector3(
                Mathf.Max(0.2f, length),
                Mathf.Max(0.05f, width),
                1f);

            var life = go.AddComponent<HighflyFinalQuadLifetimeV024>();
            life.Initialize(Mathf.Max(0.08f, duration), 0.16f, 0f);

            SpawnImpact(origin + forward * Mathf.Min(length * 0.42f, 1.35f),
                Mathf.Max(0.35f, width * 1.4f),
                color);

            return go;
        }

        public static GameObject SpawnPulse(Vector3 position, float size, Color color, float life)
        {
            GameObject go = new GameObject("HF_FINAL_AURA");
            go.transform.position = position;

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.duration = Mathf.Max(0.15f, life);
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.34f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.04f, 0.28f);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.22f, size * 0.58f);
            main.startColor = color;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var emission = ps.emission;
            emission.rateOverTime = Mathf.Clamp(20f + size * 14f, 18f, 60f);

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = Mathf.Max(0.08f, size * 0.42f);
            shape.radiusThickness = 0.35f;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = CreateFxMaterial("magic_03", color);

            ps.Play();
            Object.Destroy(go, Mathf.Max(0.16f, life));
            return go;
        }

        public static GameObject SpawnField(Vector3 position, float radius, Color color, float life)
        {
            GameObject go = CreateTexturedQuad("HF_FINAL_FIELD", "circle_05", color);
            if (go == null) return null;

            go.transform.position = position + Vector3.up * 0.035f;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(
                Mathf.Max(0.5f, radius * 2f),
                Mathf.Max(0.5f, radius * 2f),
                1f);

            var lifeFx = go.AddComponent<HighflyFinalQuadLifetimeV024>();
            lifeFx.Initialize(Mathf.Max(0.15f, life), 0.04f, 38f);

            SpawnParticleBurst(
                position + Vector3.up * 0.12f,
                "twirl_02",
                color,
                Mathf.Max(0.45f, radius * 0.28f),
                12,
                Mathf.Max(0.4f, radius * 0.45f),
                0.42f);

            return go;
        }

        public static void SpawnImpact(Vector3 position, float radius, Color color)
        {
            SpawnParticleBurst(
                position,
                "spark_04",
                color,
                Mathf.Max(0.16f, radius * 0.36f),
                18,
                Mathf.Max(1.2f, radius * 4.5f),
                0.34f);
        }

        public static GameObject CreateProjectile(
            string label,
            Vector3 position,
            float radius,
            Color color)
        {
            if (!string.IsNullOrEmpty(label) &&
                label.ToUpperInvariant().Contains("ARROW"))
            {
                GameObject arrowPrefab = Resources.Load<GameObject>(Root + "KayKitArrow");
                if (arrowPrefab != null)
                {
                    GameObject arrow = Object.Instantiate(arrowPrefab, position, Quaternion.identity);
                    arrow.name = label + "_FINAL_ARROW";
                    arrow.transform.localScale = Vector3.one * 0.82f;

                    foreach (Collider c in arrow.GetComponentsInChildren<Collider>(true))
                        c.enabled = false;

                    AttachTrail(arrow, color, Mathf.Max(0.05f, radius * 0.28f));
                    return arrow;
                }
            }

            GameObject go = new GameObject(label + "_FINAL_PROJECTILE");
            go.transform.position = position;

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.duration = 3f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.11f, 0.24f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(
                Mathf.Max(0.08f, radius * 0.55f),
                Mathf.Max(0.12f, radius * 1.15f));
            main.startColor = color;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 44f;

            var shape = ps.shape;
            shape.enabled = false;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = CreateFxMaterial("magic_03", color);

            AttachTrail(go, color, Mathf.Max(0.045f, radius * 0.24f));
            ps.Play();
            return go;
        }

        public static Material CreateFxMaterial(string textureName, Color color)
        {
            Shader shader =
                Shader.Find("Sprites/Default") ??
                Shader.Find("Unlit/Transparent") ??
                Shader.Find("Unlit/Texture") ??
                Shader.Find("Standard");

            if (shader == null) return null;

            Material mat = new Material(shader);
            Texture2D texture = Resources.Load<Texture2D>(Root + textureName);
            if (texture != null) mat.mainTexture = texture;
            mat.color = color;
            mat.renderQueue = 3000;
            return mat;
        }

        private static void AttachTrail(GameObject go, Color color, float width)
        {
            if (go == null) return;

            TrailRenderer trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.18f;
            trail.minVertexDistance = 0.04f;
            trail.startWidth = width;
            trail.endWidth = 0f;
            trail.textureMode = LineTextureMode.Stretch;
            trail.material = CreateFxMaterial("slash_01", color);
        }

        private static void SpawnParticleBurst(
            Vector3 position,
            string textureName,
            Color color,
            float size,
            int count,
            float speed,
            float lifetime)
        {
            GameObject go = new GameObject("HF_FINAL_PARTICLE_BURST");
            go.transform.position = position;

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = false;
            main.duration = 0.12f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.65f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.55f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.45f, size);
            main.startColor = color;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, (short)Mathf.Clamp(count, 1, 64))
            });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = Mathf.Max(0.03f, size * 0.15f);

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = CreateFxMaterial(textureName, color);

            ps.Play();
            Object.Destroy(go, Mathf.Max(0.3f, lifetime + 0.2f));
        }

        private static GameObject CreateTexturedQuad(string name, string textureName, Color color)
        {
            GameObject go = new GameObject(name);
            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();

            Mesh mesh = new Mesh();
            mesh.name = name + "_MESH";
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3( 0.5f, -0.5f, 0f),
                new Vector3(-0.5f,  0.5f, 0f),
                new Vector3( 0.5f,  0.5f, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateBounds();

            mf.sharedMesh = mesh;
            mr.sharedMaterial = CreateFxMaterial(textureName, color);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go;
        }
    }

    [DisallowMultipleComponent]
    public sealed class HighflyFinalQuadLifetimeV024 : MonoBehaviour
    {
        private float _end;
        private float _duration;
        private float _grow;
        private float _spin;
        private Vector3 _startScale;
        private Renderer _renderer;
        private Material _material;
        private Color _startColor;

        public void Initialize(float duration, float grow, float spinDegreesPerSecond)
        {
            _duration = Mathf.Max(0.05f, duration);
            _end = Time.unscaledTime + _duration;
            _grow = grow;
            _spin = spinDegreesPerSecond;
            _startScale = transform.localScale;
            _renderer = GetComponent<Renderer>();
            if (_renderer != null)
            {
                _material = _renderer.material;
                _startColor = _material.color;
            }
        }

        private void Update()
        {
            float remaining = _end - Time.unscaledTime;
            if (remaining <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            float t = 1f - Mathf.Clamp01(remaining / _duration);
            transform.localScale = _startScale * (1f + t * _grow);
            if (Mathf.Abs(_spin) > 0.01f)
                transform.Rotate(0f, 0f, _spin * Time.unscaledDeltaTime, Space.Self);

            if (_material != null)
            {
                Color c = _startColor;
                c.a *= 1f - t;
                _material.color = c;
            }
        }
    }
}
