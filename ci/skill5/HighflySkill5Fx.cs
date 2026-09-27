using UnityEngine;
using UnityEngine.Rendering;

namespace Highfly.Skill5
{
    // NO_PROXY_TEXTURED_SCARS
    // Uses the already-audited slash texture and real SparksEffect shipped by the frozen run.
    public static class HighflySkill5Fx
    {
        private const string SlashTexture = "HIGHFLY/Run0I/slash_02";

        public static void SpawnSquareScar(
            int index,
            Vector3 center,
            Vector3 forward,
            Vector3 right)
        {
            Color blue = new Color(0.18f, 0.72f, 1f, 0.92f);
            Vector3 position = center;
            float roll = 0f;
            float length = 1.48f;

            switch (index)
            {
                case 0:
                    position += Vector3.up * 0.58f;
                    roll = 0f;
                    break;
                case 1:
                    position += right * 0.68f;
                    roll = 90f;
                    length = 1.18f;
                    break;
                case 2:
                    position -= Vector3.up * 0.58f;
                    roll = 180f;
                    break;
                default:
                    position -= right * 0.68f;
                    roll = -90f;
                    length = 1.18f;
                    break;
            }

            SpawnTexturedScar(
                "HF_SKILL5_SQUARE_SCAR_" + (index + 1),
                position,
                forward,
                roll,
                length,
                0.13f,
                0.24f,
                blue);
        }

        public static void SpawnFinalCross(Vector3 position, Vector3 forward)
        {
            SpawnImpactCross(position, forward, new Color(0.28f, 0.82f, 1f, 1f), 1.82f);
        }

        public static void SpawnSonicSlash(Vector3 position, Vector3 forward)
        {
            Color blue = new Color(0.18f, 0.70f, 1f, 0.96f);
            SpawnTexturedScar("HF_SKILL5_SONIC_DOWN", position, forward, 90f, 2.45f, 0.20f, 0.24f, blue);
            SpawnTexturedScar("HF_SKILL5_SONIC_CORE", position + forward.normalized * 0.018f, forward, 90f, 2.12f, 0.075f, 0.20f, Color.white);
        }

        public static void SpawnDualSlash(int handIndex, Vector3 position, Vector3 forward, Vector3 right)
        {
            bool first = handIndex == 0;
            Color color = first
                ? new Color(0.14f, 0.78f, 1f, 0.94f)
                : new Color(0.66f, 0.28f, 1f, 0.94f);
            float side = first ? 0.18f : -0.18f;
            float roll = first ? -12f : 12f;
            SpawnTexturedScar(
                first ? "HF_SKILL5_DUAL_RIGHT" : "HF_SKILL5_DUAL_LEFT",
                position + right * side,
                forward,
                roll,
                2.55f,
                0.18f,
                0.25f,
                color);
        }

        public static void SpawnApocalypseSlash(Vector3 position, Vector3 forward)
        {
            Color crimson = new Color(1f, 0.12f, 0.06f, 0.98f);
            Color fire = new Color(1f, 0.48f, 0.08f, 0.92f);
            SpawnTexturedScar("HF_SKILL5_APOCALYPSE_FIRE", position, forward, -38f, 3.85f, 0.38f, 0.28f, crimson);
            SpawnTexturedScar("HF_SKILL5_APOCALYPSE_CORE", position + forward.normalized * 0.02f, forward, -38f, 3.35f, 0.14f, 0.24f, fire);
        }

        public static void SpawnShieldSpiral(Vector3 center, Vector3 forward, Vector3 right, int pulse)
        {
            Color cyan = new Color(0.22f, 0.86f, 1f, 0.88f);
            float sign = pulse == 0 ? 1f : -1f;
            SpawnTexturedScar("HF_SKILL5_SHIELD_ARC_A", center + right * 0.42f * sign, forward, 72f * sign, 1.65f, 0.14f, 0.23f, cyan);
            SpawnTexturedScar("HF_SKILL5_SHIELD_ARC_B", center - right * 0.38f * sign + Vector3.up * 0.18f, forward, -58f * sign, 1.52f, 0.12f, 0.21f, Color.white);
        }

        public static void SpawnVerticalScar(
            int index,
            Vector3 center,
            Vector3 forward,
            Vector3 right)
        {
            Color cyan = new Color(0.20f, 0.80f, 1f, 0.94f);
            Vector3 position = center;
            float roll;
            float length = 1.95f;

            switch (index)
            {
                case 0:
                    position += right * 0.34f;
                    roll = 90f;
                    break;
                case 1:
                    position -= right * 0.34f;
                    roll = -90f;
                    break;
                case 2:
                    position -= right * 0.12f;
                    position += Vector3.up * 0.08f;
                    roll = -90f;
                    break;
                default:
                    position += right * 0.12f;
                    roll = 90f;
                    break;
            }

            SpawnTexturedScar(
                "HF_SKILL5_VERTICAL_" + (index + 1),
                position,
                forward,
                roll,
                length,
                index == 3 ? 0.18f : 0.13f,
                index == 3 ? 0.27f : 0.22f,
                index == 3 ? Color.white : cyan);
        }

        public static void SpawnSavageScar(
            int index,
            Vector3 center,
            Vector3 forward,
            Vector3 right)
        {
            Color gold = new Color(1f, 0.72f, 0.18f, 0.96f);
            if (index == 0)
            {
                SpawnTexturedScar(
                    "HF_SKILL5_FULCRUM_H",
                    center,
                    forward,
                    0f,
                    2.45f,
                    0.18f,
                    0.24f,
                    gold);
            }
            else if (index == 1)
            {
                SpawnTexturedScar(
                    "HF_SKILL5_FULCRUM_UP",
                    center + right * 0.15f,
                    forward,
                    -88f,
                    2.25f,
                    0.17f,
                    0.24f,
                    gold);
            }
            else
            {
                SpawnTexturedScar(
                    "HF_SKILL5_FULCRUM_DOWN",
                    center - right * 0.12f,
                    forward,
                    88f,
                    2.65f,
                    0.22f,
                    0.28f,
                    Color.white);
                SpawnImpactCross(
                    center + forward.normalized * 0.04f,
                    forward,
                    gold,
                    2.30f);
            }
        }

        public static void SpawnVorpalThrust(Vector3 position, Vector3 forward)
        {
            Color blue = new Color(0.22f, 0.70f, 1f, 0.98f);

            // Narrow layered streak: keeps the contact readable without a fake AoE.
            SpawnTexturedScar(
                "HF_SKILL5_VORPAL_STREAK_OUTER",
                position,
                forward,
                0f,
                3.10f,
                0.12f,
                0.20f,
                blue);
            SpawnTexturedScar(
                "HF_SKILL5_VORPAL_STREAK_CORE",
                position + forward.normalized * 0.025f,
                forward,
                0f,
                2.65f,
                0.055f,
                0.16f,
                Color.white);
        }

        public static void SpawnImpactCross(Vector3 position, Vector3 forward, Color color, float size)
        {
            SpawnTexturedScar("HF_SKILL5_IMPACT_X_A", position, forward, 45f, size, 0.17f, 0.17f, color);
            SpawnTexturedScar("HF_SKILL5_IMPACT_X_B", position + forward.normalized * 0.02f, forward, -45f, size * 0.96f, 0.14f, 0.15f, Color.white);
        }

        private static GameObject SpawnTexturedScar(
            string name,
            Vector3 position,
            Vector3 forward,
            float roll,
            float length,
            float width,
            float duration,
            Color color)
        {
            Texture2D texture = Resources.Load<Texture2D>(SlashTexture);
            if (texture == null)
            {
                Debug.LogError("[SKILL5] Quality gate: audited slash_02 texture missing. Refusing proxy scar.");
                return null;
            }

            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            forward.Normalize();

            GameObject go = new GameObject(name);
            go.transform.position = position;
            go.transform.rotation =
                Quaternion.LookRotation(forward, Vector3.up) *
                Quaternion.Euler(0f, 0f, roll);

            Mesh mesh = BuildQuadMesh(length, width);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;

            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = CreateMaterial(texture, color);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            HighflySkill5FxLifetime life = go.AddComponent<HighflySkill5FxLifetime>();
            life.Initialize(mesh, renderer.sharedMaterial, duration, color.a);
            return go;
        }

        private static Mesh BuildQuadMesh(float length, float width)
        {
            float hx = Mathf.Max(0.05f, length) * 0.5f;
            float hy = Mathf.Max(0.02f, width) * 0.5f;

            Mesh mesh = new Mesh();
            mesh.name = "HF_SKILL5_TEXTURED_SCAR_MESH";
            mesh.vertices = new[]
            {
                new Vector3(-hx,-hy,0f),
                new Vector3(-hx, hy,0f),
                new Vector3( hx, hy,0f),
                new Vector3( hx,-hy,0f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f,0f),
                new Vector2(0f,1f),
                new Vector2(1f,1f),
                new Vector2(1f,0f)
            };
            mesh.triangles = new[] { 0,1,2, 0,2,3, 2,1,0, 3,2,0 };
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Material CreateMaterial(Texture2D texture, Color color)
        {
            Shader shader =
                Shader.Find("Sprites/Default") ??
                Shader.Find("Unlit/Transparent") ??
                Shader.Find("Unlit/Texture");

            if (shader == null)
            {
                Debug.LogError("[SKILL5] Quality gate: no transparent shader for premium scar.");
                return null;
            }

            Material material = new Material(shader);
            material.mainTexture = texture;
            material.color = color;
            material.renderQueue = 3000;
            return material;
        }
    }

    public sealed class HighflySkill5FxLifetime : MonoBehaviour
    {
        private Mesh _mesh;
        private Material _material;
        private float _born;
        private float _life;
        private float _alpha;

        public void Initialize(Mesh mesh, Material material, float life, float alpha)
        {
            _mesh = mesh;
            _material = material;
            _born = Time.unscaledTime;
            _life = Mathf.Clamp(life, 0.08f, 0.29f);
            _alpha = alpha;
        }

        private void Update()
        {
            float t = Mathf.Clamp01((Time.unscaledTime - _born) / _life);
            if (_material != null)
            {
                Color c = _material.color;
                c.a = _alpha * (1f - t);
                _material.color = c;
            }

            if (t < 1f) return;
            if (_mesh != null) Destroy(_mesh);
            if (_material != null) Destroy(_material);
            Destroy(gameObject);
        }
    }
}
