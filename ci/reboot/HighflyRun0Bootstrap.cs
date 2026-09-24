using System.Collections;
using UnityEngine;

namespace Highfly.SkillLab
{
    public static class HighflySkillLabMode
    {
        public static bool IsActive => true;
    }

    public sealed class HighflyRun0Dummy : CharacterStats
    {
        public static HighflyRun0Dummy Instance { get; private set; }

        private Vector3 _baseScale;
        private Renderer _renderer;
        private Color _baseColor;

        public override void Start()
        {
            Instance = this;
            maxEgo = 999999f;
            currentEgo = maxEgo;
            _baseScale = transform.localScale;
            _renderer = GetComponentInChildren<Renderer>(true);

            if (_renderer != null)
            {
                Material m = _renderer.material;
                if (m.HasProperty("_BaseColor"))
                    _baseColor = m.GetColor("_BaseColor");
                else if (m.HasProperty("_Color"))
                    _baseColor = m.GetColor("_Color");
                else
                    _baseColor = Color.gray;
            }
        }

        public override void TakeDamage(
            float damage,
            float composureDamage = 10f,
            Transform attacker = null)
        {
            currentEgo = maxEgo;
            StopAllCoroutines();
            StartCoroutine(Pulse());
        }

        private IEnumerator Pulse()
        {
            transform.localScale =
                new Vector3(
                    _baseScale.x * 1.05f,
                    _baseScale.y * 0.96f,
                    _baseScale.z * 1.05f);

            if (_renderer != null)
            {
                Material m = _renderer.material;
                if (m.HasProperty("_BaseColor"))
                    m.SetColor("_BaseColor", Color.white);
                if (m.HasProperty("_Color"))
                    m.SetColor("_Color", Color.white);
            }

            yield return new WaitForSecondsRealtime(0.05f);

            transform.localScale = _baseScale;

            if (_renderer != null)
            {
                Material m = _renderer.material;
                if (m.HasProperty("_BaseColor"))
                    m.SetColor("_BaseColor", _baseColor);
                if (m.HasProperty("_Color"))
                    m.SetColor("_Color", _baseColor);
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class HighflySkillLabBootstrap : MonoBehaviour
    {
        public const float LabY = 120f;

        public static readonly Vector3 Run0Spawn =
            new Vector3(0f, LabY + 0.92f, -4.4f);

        private PlayerController _player;
        private bool _setup;
        private float _nextBoundsCheck;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (UnityEngine.Object.FindFirstObjectByType<HighflySkillLabBootstrap>() != null)
                return;

            GameObject root =
                new GameObject("HIGHFLY_COMBAT_REBOOT_RUN0");

            DontDestroyOnLoad(root);
            root.AddComponent<HighflySkillLabBootstrap>();
        }

        private void Update()
        {
            if (!_setup)
            {
                PlayerController player =
                    UnityEngine.Object.FindFirstObjectByType<PlayerController>();

                if (player != null)
                    Setup(player);
            }

            if (_setup &&
                _player != null &&
                Time.unscaledTime >= _nextBoundsCheck)
            {
                _nextBoundsCheck = Time.unscaledTime + 0.15f;

                Vector3 p = _player.transform.position;
                if (p.y < LabY - 2f ||
                    p.y > LabY + 8f ||
                    Mathf.Abs(p.x) > 10.8f ||
                    p.z < -8f ||
                    p.z > 11f)
                {
                    HighflyRun0SwordBakeoff.Instance?.ResetRun0();
                }
            }
        }

        private void Setup(PlayerController player)
        {
            _setup = true;
            _player = player;

            BuildRoom();

            CharacterController cc =
                player.GetComponent<CharacterController>();

            if (cc != null) cc.enabled = false;
            player.transform.position = Run0Spawn;
            player.transform.rotation = Quaternion.identity;
            if (cc != null) cc.enabled = true;

            BuildDummy();

            HighflyRun0Hunter.Install(player);
            HighflyRun0SwordBakeoff.Install(player, transform);

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor =
                    new Color(0.72f, 0.76f, 0.81f, 1f);
            }

            if (!Application.isMobilePlatform)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            StartCoroutine(SnapCamera());
            Debug.Log("[RUN0] BOOTSTRAP READY");
        }

        private IEnumerator SnapCamera()
        {
            yield return null;
            yield return null;
            HighflyThirdPersonMobileCamera.Instance?.SnapBehindPlayer(11f);
        }

        private static void BuildRoom()
        {
            Material floor = Material(
                new Color(0.58f, 0.61f, 0.64f, 1f));

            Material wall = Material(
                new Color(0.78f, 0.80f, 0.82f, 1f));

            Material line = Material(
                new Color(0.20f, 0.23f, 0.27f, 1f));

            Block(
                "RUN0_FLOOR",
                new Vector3(0f, LabY - 0.35f, 1.5f),
                new Vector3(22f, 0.7f, 20f),
                floor);

            Block(
                "RUN0_BACK",
                new Vector3(0f, LabY + 4f, 11.0f),
                new Vector3(22f, 8f, 0.45f),
                wall);

            Block(
                "RUN0_LEFT",
                new Vector3(-10.8f, LabY + 4f, 1.5f),
                new Vector3(0.45f, 8f, 20f),
                wall);

            Block(
                "RUN0_RIGHT",
                new Vector3(10.8f, LabY + 4f, 1.5f),
                new Vector3(0.45f, 8f, 20f),
                wall);

            for (int z = -4; z <= 8; z += 1)
            {
                Block(
                    "RUN0_METER_" + z,
                    new Vector3(0f, LabY + 0.02f, z),
                    new Vector3(18f, 0.025f, 0.018f),
                    z == 0 ? line : wall);
            }

            for (int x = -8; x <= 8; x += 2)
            {
                Block(
                    "RUN0_X_" + x,
                    new Vector3(x, LabY + 0.021f, 2f),
                    new Vector3(0.018f, 0.025f, 14f),
                    line);
            }

            GameObject key = new GameObject("RUN0_KEY_LIGHT");
            key.transform.rotation = Quaternion.Euler(50f, 25f, 0f);
            Light l = key.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.2f;

            GameObject rim = new GameObject("RUN0_RIM_LIGHT");
            rim.transform.position =
                new Vector3(0f, LabY + 4.5f, 5f);
            Light rl = rim.AddComponent<Light>();
            rl.type = LightType.Point;
            rl.range = 14f;
            rl.intensity = 0.45f;
        }

        private static void BuildDummy()
        {
            GameObject root = new GameObject("RUN0_DUMMY");
            root.transform.position =
                new Vector3(0f, LabY + 1.0f, 2.6f);

            GameObject body =
                GameObject.CreatePrimitive(PrimitiveType.Capsule);

            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale =
                new Vector3(0.72f, 0.90f, 0.72f);

            Renderer r = body.GetComponent<Renderer>();
            if (r != null)
                r.sharedMaterial =
                    Material(new Color(0.32f, 0.08f, 0.08f, 1f));

            root.AddComponent<HighflyRun0Dummy>();
        }

        private static Material Material(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material m = new Material(shader);
            if (m.HasProperty("_BaseColor"))
                m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color"))
                m.SetColor("_Color", color);
            return m;
        }

        private static GameObject Block(
            string name,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            GameObject go =
                GameObject.CreatePrimitive(PrimitiveType.Cube);

            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;

            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = material;

            return go;
        }
    }
}
