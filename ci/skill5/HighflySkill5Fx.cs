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
                0.27f,
                blue);
        }

        public static void SpawnFinalCross(Vector3 position, Vector3 forward)
        {
            Color blue = new Color(0.28f, 0.82f, 1f, 1f);
            SpawnTexturedScar("HF_SKILL5_FINAL_X_A", position, forward, 45f, 1.82f, 0.17f, 0.17f, blue);
            SpawnTexturedScar("HF_SKILL5_FINAL_X_B", position + forward.normalized * 0.02f, forward, -45f, 1.76f, 0.14f, 0.15f, Color.white);
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
