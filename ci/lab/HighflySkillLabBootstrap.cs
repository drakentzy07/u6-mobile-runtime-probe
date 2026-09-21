using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Highfly.Mobile;

namespace Highfly.SkillLab
{
    public static class HighflySkillLabMode
    {
        public static bool IsActive
        {
            get
            {
                string url = Application.absoluteURL ?? string.Empty;
                return url.IndexOf("lab=1", StringComparison.OrdinalIgnoreCase) >= 0 ||
                       url.IndexOf("skilllab", StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }
    }

    public static class HighflySkillLabMetrics
    {
        public static string LastAction = "-";
        public static float LastDamage;
        public static int TotalHits;
        public static float TotalDamage;
        public static int ComboStage;
        public static float LastHitAt;

        public static void RecordAction(string action, int comboStage)
        {
            LastAction = action;
            ComboStage = comboStage;
        }

        public static void RecordHit(float damage)
        {
            LastDamage = damage;
            TotalDamage += damage;
            TotalHits++;
            LastHitAt = Time.unscaledTime;
        }

        public static void Reset()
        {
            LastAction = "-";
            LastDamage = 0f;
            TotalHits = 0;
            TotalDamage = 0f;
            ComboStage = 0;
            LastHitAt = 0f;
        }
    }

    public sealed class HighflyLabDummyStats : CharacterStats
    {
        private Vector3 _baseScale;

        public override void Start()
        {
            maxEgo = 999999f;
            currentEgo = maxEgo;
            _baseScale = transform.localScale;
        }

        public override void TakeDamage(float damage, float composureDamage = 10f, Transform attacker = null)
        {
            currentEgo = maxEgo;
            StopAllCoroutines();
            StartCoroutine(HitPulse());
        }

        private IEnumerator HitPulse()
        {
            transform.localScale = new Vector3(
                _baseScale.x * 1.08f,
                _baseScale.y * 0.94f,
                _baseScale.z * 1.08f);

            yield return new WaitForSecondsRealtime(0.065f);
            transform.localScale = _baseScale;
        }
    }

    [DisallowMultipleComponent]
    public sealed class HighflySkillLabBootstrap : MonoBehaviour
    {
        private const float LabY = 120f;
        private bool _setup;
        private Text _metricsText;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (UnityEngine.Object.FindFirstObjectByType<HighflySkillLabBootstrap>() != null) return;

            var root = new GameObject("HIGHFLY_SKILL_LAB_v0.2");
            DontDestroyOnLoad(root);
            root.AddComponent<HighflySkillLabBootstrap>();
        }

        private void Update()
        {
            if (!HighflySkillLabMode.IsActive) return;

            if (!_setup)
            {
                var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
                if (player != null)
                    SetupLab(player);
            }

            if (_metricsText != null)
            {
                float age = HighflySkillLabMetrics.LastHitAt > 0f
                    ? Time.unscaledTime - HighflySkillLabMetrics.LastHitAt
                    : 0f;

                _metricsText.text =
                    "HIGHFLY • SKILL LAB v0.2\n" +
                    "GOLDEN CAMERA / GOLDEN MOBILE CORE\n" +
                    "PC: WASD + arrastre derecho | Mobile: joystick + derecha\n\n" +
                    "S1 DANZA GEMELA  •  S2 PASO FANTASMA\n" +
                    "S3 GRILLETE UMBRÍO  •  S4 PACTO VITAL\n" +
                    "S5 LLAMADO DE LA SOMBRA (slot ULT)\n\n" +
                    "Acción: " + HighflySkillLabMetrics.LastAction + "\n" +
                    "Combo: " + HighflySkillLabMetrics.ComboStage + "/3\n" +
                    "Último daño: " + HighflySkillLabMetrics.LastDamage.ToString("0") + "\n" +
                    "Golpes: " + HighflySkillLabMetrics.TotalHits + "\n" +
                    "Daño acumulado: " + HighflySkillLabMetrics.TotalDamage.ToString("0") + "\n" +
                    (age > 0f ? "Último impacto: " + age.ToString("0.00") + "s" : "");
            }
        }

        private void SetupLab(PlayerController player)
        {
            _setup = true;
            HighflySkillLabMetrics.Reset();

            BuildRoom();

            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.position = new Vector3(0f, LabY + 0.9f, -7.2f);
            player.transform.rotation = Quaternion.identity;

            if (cc != null) cc.enabled = true;

            if (player.GetComponent<HighflyPremiumSkillRuntime>() == null)
                player.gameObject.AddComponent<HighflyPremiumSkillRuntime>();

            if (player.GetComponent<HighflyLabDesktopControls>() == null)
                player.gameObject.AddComponent<HighflyLabDesktopControls>();

            CreateDummy(new Vector3(0f, LabY + 1.0f, 3.5f));
            CreateDummy(new Vector3(-3.4f, LabY + 1.0f, 5.3f));
            CreateDummy(new Vector3(3.4f, LabY + 1.0f, 5.3f));

            CreateMetricsHud();

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.008f, 0.014f, 0.028f, 1f);
            }

            if (!Application.isMobilePlatform)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            StartCoroutine(SnapCameraNextFrame());
            StartCoroutine(RelabelLabButtons());
        }

        private IEnumerator SnapCameraNextFrame()
        {
            yield return null;
            yield return null;

            HighflyThirdPersonMobileCamera.Instance?.SnapBehindPlayer(13f);
        }

        private IEnumerator RelabelLabButtons()
        {
            yield return null;
            yield return null;

            var allText = UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < allText.Length; i++)
            {
                if (allText[i] == null) continue;
                if (allText[i].text == "ULT")
                    allText[i].text = "S5";
            }
        }

        private static void BuildRoom()
        {
            Material floorMat = HighflyLabVisuals.CreateMaterial(
                new Color(0.025f, 0.035f, 0.055f, 1f),
                new Color(0.00f, 0.10f, 0.18f, 1f));

            Material wallMat = HighflyLabVisuals.CreateMaterial(
                new Color(0.012f, 0.018f, 0.032f, 1f),
                new Color(0.015f, 0.05f, 0.10f, 1f));

            Material cyanMat = HighflyLabVisuals.CreateMaterial(
                new Color(0.015f, 0.12f, 0.16f, 1f),
                new Color(0.05f, 0.92f, 1f, 1f));

            Material violetMat = HighflyLabVisuals.CreateMaterial(
                new Color(0.08f, 0.025f, 0.12f, 1f),
                new Color(0.55f, 0.10f, 1f, 1f));

            CreateBlock(
                "LAB_FLOOR",
                new Vector3(0f, LabY - 0.35f, 3f),
                new Vector3(28f, 0.7f, 28f),
                floorMat);

            CreateBlock(
                "LAB_BACK_WALL",
                new Vector3(0f, LabY + 4.5f, 16.5f),
                new Vector3(28f, 9f, 0.55f),
                wallMat);

            CreateBlock(
                "LAB_LEFT_WALL",
                new Vector3(-13.7f, LabY + 4.5f, 3f),
                new Vector3(0.55f, 9f, 28f),
                wallMat);

            CreateBlock(
                "LAB_RIGHT_WALL",
                new Vector3(13.7f, LabY + 4.5f, 3f),
                new Vector3(0.55f, 9f, 28f),
                wallMat);

            // Distance lanes every 2m.
            for (int z = -4; z <= 14; z += 2)
            {
                CreateBlock(
                    "LAB_GRID_Z_" + z,
                    new Vector3(0f, LabY + 0.025f, z),
                    new Vector3(24f, 0.03f, 0.025f),
                    z % 4 == 0 ? cyanMat : wallMat);
            }

            for (int x = -10; x <= 10; x += 2)
            {
                CreateBlock(
                    "LAB_GRID_X_" + x,
                    new Vector3(x, LabY + 0.026f, 4f),
                    new Vector3(0.025f, 0.03f, 20f),
                    x == 0 ? violetMat : wallMat);
            }

            // Portal-like lab pylons.
            CreateBlock("LAB_PYLON_L", new Vector3(-6.5f, LabY + 2.2f, 10f), new Vector3(0.45f, 4.4f, 0.45f), cyanMat);
            CreateBlock("LAB_PYLON_R", new Vector3(6.5f, LabY + 2.2f, 10f), new Vector3(0.45f, 4.4f, 0.45f), violetMat);

            var key = new GameObject("LAB_KEY_LIGHT");
            key.transform.position = new Vector3(-3f, LabY + 7f, -2f);
            key.transform.rotation = Quaternion.Euler(48f, 28f, 0f);
            var keyLight = key.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.intensity = 1.15f;
            keyLight.color = new Color(0.62f, 0.82f, 1f, 1f);

            var rim = new GameObject("LAB_RIM_LIGHT");
            rim.transform.position = new Vector3(0f, LabY + 5.5f, 9f);
            var rimLight = rim.AddComponent<Light>();
            rimLight.type = LightType.Point;
            rimLight.range = 18f;
            rimLight.intensity = 2.0f;
            rimLight.color = new Color(0.42f, 0.18f, 1f, 1f);
        }

        private static GameObject CreateBlock(string name, Vector3 position, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = mat;

            return go;
        }

        private static void CreateDummy(Vector3 position)
        {
            var root = new GameObject("LAB_DUMMY_INMORTAL");
            root.transform.position = position;

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.90f, 1.15f, 0.90f);

            try { root.tag = "Enemy"; } catch { }

            var bodyRenderer = body.GetComponent<Renderer>();
            if (bodyRenderer != null)
                bodyRenderer.sharedMaterial = HighflyLabVisuals.CreateMaterial(
                    new Color(0.025f, 0.065f, 0.10f, 1f),
                    new Color(0.08f, 0.78f, 1f, 1f));

            root.AddComponent<HighflyLabDummyStats>();

            var ring = new GameObject("DummyRing");
            ring.transform.SetParent(root.transform, false);
            ring.transform.localPosition = new Vector3(0f, -0.95f, 0f);

            var lr = ring.AddComponent<LineRenderer>();
            lr.loop = true;
            lr.useWorldSpace = false;
            lr.positionCount = 40;
            lr.widthMultiplier = 0.045f;
            lr.sharedMaterial = HighflyLabVisuals.CreateFxMaterial(new Color(0.08f, 0.78f, 1f, 1f));

            for (int i = 0; i < 40; i++)
            {
                float a = (i / 40f) * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.90f, 0f, Mathf.Sin(a) * 0.90f));
            }
        }

        private void CreateMetricsHud()
        {
            var canvasGo = new GameObject("LAB_DEV_HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 7000;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var panel = new GameObject("MetricsPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasGo.transform, false);

            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24f, -128f);
            rect.sizeDelta = new Vector2(590f, 390f);

            var image = panel.GetComponent<Image>();
            image.color = new Color(0.008f, 0.015f, 0.035f, 0.86f);

            var accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(panel.transform, false);

            var accentRect = accent.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.pivot = new Vector2(0f, 0.5f);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(8f, 0f);

            accent.GetComponent<Image>().color = new Color(0.05f, 0.85f, 1f, 1f);

            var textGo = new GameObject("MetricsText", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(panel.transform, false);

            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(22f, 16f);
            textRect.offsetMax = new Vector2(-18f, -16f);

            _metricsText = textGo.GetComponent<Text>();
            _metricsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _metricsText.fontSize = 20;
            _metricsText.alignment = TextAnchor.UpperLeft;
            _metricsText.color = new Color(0.86f, 0.96f, 1f, 1f);
        }
    }
}
