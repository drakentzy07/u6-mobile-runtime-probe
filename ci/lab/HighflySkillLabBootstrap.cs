using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Highfly.Mobile;

namespace Highfly.SkillLab
{
    public static class HighflySkillLabMode
    {
        // v0.10 is a dedicated LAB artifact. It must never fall through to Lucid's story/prologue.
        public static bool IsActive => true;
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
        private Renderer[] _renderers;
        private Color[] _baseColors;

        public override void Start()
        {
            maxEgo = 999999f;
            currentEgo = maxEgo;
            _baseScale = transform.localScale;

            _renderers = GetComponentsInChildren<Renderer>(true);
            _baseColors = new Color[_renderers.Length];

            for (int i = 0; i < _renderers.Length; i++)
            {
                Material mat = _renderers[i] != null ? _renderers[i].material : null;
                if (mat == null)
                {
                    _baseColors[i] = Color.gray;
                    continue;
                }

                if (mat.HasProperty("_BaseColor"))
                    _baseColors[i] = mat.GetColor("_BaseColor");
                else if (mat.HasProperty("_Color"))
                    _baseColors[i] = mat.GetColor("_Color");
                else
                    _baseColors[i] = Color.gray;
            }
        }

        public override void TakeDamage(float damage, float composureDamage = 10f, Transform attacker = null)
        {
            currentEgo = maxEgo;
            HighflySkillLabMetrics.RecordHit(damage);

            StopAllCoroutines();
            StartCoroutine(HitPulse());
        }

        private IEnumerator HitPulse()
        {
            transform.localScale = new Vector3(
                _baseScale.x * 1.09f,
                _baseScale.y * 0.93f,
                _baseScale.z * 1.09f);

            SetFlash(new Color(1f, 0.32f, 0.22f, 1f));
            yield return new WaitForSecondsRealtime(0.045f);

            SetFlash(Color.white);
            yield return new WaitForSecondsRealtime(0.035f);

            RestoreColors();
            transform.localScale = _baseScale;
        }

        private void SetFlash(Color color)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer r = _renderers[i];
                if (r == null) continue;

                Material mat = r.material;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", color * 1.8f);
                }
            }
        }

        private void RestoreColors()
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer r = _renderers[i];
                if (r == null) continue;

                Material mat = r.material;
                Color color = i < _baseColors.Length ? _baseColors[i] : Color.gray;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.black);
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class HighflySkillLabBootstrap : MonoBehaviour
    {
        private const float LabY = 120f;
        private static readonly Vector3 LabSpawn = new Vector3(0f, LabY + 0.9f, -7.2f);

        private bool _setup;
        private Text _metricsText;
        private PlayerController _player;
        private float _nextSafetyCheck;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (UnityEngine.Object.FindFirstObjectByType<HighflySkillLabBootstrap>() != null) return;

            var root = new GameObject("HIGHFLY_SKILL_LAB_v0.10");
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

            if (_setup && _player != null && Time.unscaledTime >= _nextSafetyCheck)
            {
                _nextSafetyCheck = Time.unscaledTime + 0.10f;
                CheckPlayerBounds();
            }

            if (_metricsText != null)
            {
                float age = HighflySkillLabMetrics.LastHitAt > 0f
                    ? Time.unscaledTime - HighflySkillLabMetrics.LastHitAt
                    : 0f;

                _metricsText.text =
                    "HIGHFLY • COMBAT FEEL LAB v0.10 • SLF CORE\n" +
                    "20 CORE SKILLS • SHADOW IDENTITY • SKILL EXPRESSION\n" +
                    "PC: WASD + arrastre derecho + R recentrar\n" +
                    "SPACE salto/doble/wall | dodge/parry/lock siguen disponibles\n" +
                    "Movilidad: " + (HighflyAerialMobility.Instance != null ? HighflyAerialMobility.Instance.DebugState : "-") + "\n\n" +
                    "Panel izquierdo: DAÑO / MOV / CONTROL / SOMBRA / BUFF / DEF / TÁCTICA\n" +
                    "TOCÁ una skill = preview automático • PROBAR MANUAL = ejecución real\n" +
                    "Danza Gemela exige timing • Vista del Instante sólo ante golpe letal\n\n" +
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
            _player = player;
            HighflyTimeDilationManager.ForceReset();
            HighflySkillLabMetrics.Reset();

            BuildRoom();

            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.position = LabSpawn;
            player.transform.rotation = Quaternion.identity;

            if (cc != null) cc.enabled = true;

            if (player.GetComponent<HighflyPremiumSkillRuntime>() == null)
                player.gameObject.AddComponent<HighflyPremiumSkillRuntime>();

            if (player.GetComponent<HighflyAdvancedSkillRuntime>() == null)
                player.gameObject.AddComponent<HighflyAdvancedSkillRuntime>();

            if (player.GetComponent<HighflyReferenceSkillRuntime>() == null)
                player.gameObject.AddComponent<HighflyReferenceSkillRuntime>();

            if (player.GetComponent<HighflyApexPassSkillRuntime>() == null)
                player.gameObject.AddComponent<HighflyApexPassSkillRuntime>();

            if (player.GetComponent<HighflyAerialMobility>() == null)
                player.gameObject.AddComponent<HighflyAerialMobility>();

            if (player.GetComponent<HighflyCombatLabV010>() == null)
                player.gameObject.AddComponent<HighflyCombatLabV010>();

            Animator hunterAnimator = player.animator;
            if (hunterAnimator != null &&
                hunterAnimator.GetComponent<HighflyParkourAnimationV010>() == null)
                hunterAnimator.gameObject.AddComponent<HighflyParkourAnimationV010>();

            if (player.GetComponent<HighflyLabDesktopControls>() == null)
                player.gameObject.AddComponent<HighflyLabDesktopControls>();

            CreateDummy(new Vector3(0f, LabY + 1.0f, 3.5f));
            CreateDummy(new Vector3(-3.4f, LabY + 1.0f, 5.3f));
            CreateDummy(new Vector3(3.4f, LabY + 1.0f, 5.3f));

            CreateMetricsHud();
            HighflyCombatLabBrowserV010.Install(transform);

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.78f, 0.80f, 0.83f, 1f);
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
                new Color(0.64f, 0.67f, 0.70f, 1f),
                new Color(0.015f, 0.018f, 0.022f, 1f));

            Material wallMat = HighflyLabVisuals.CreateMaterial(
                new Color(0.82f, 0.84f, 0.86f, 1f),
                new Color(0.01f, 0.01f, 0.01f, 1f));

            Material gridMat = HighflyLabVisuals.CreateMaterial(
                new Color(0.28f, 0.30f, 0.33f, 1f),
                new Color(0.03f, 0.03f, 0.035f, 1f));

            Material accentMat = HighflyLabVisuals.CreateMaterial(
                new Color(0.42f, 0.44f, 0.47f, 1f),
                new Color(0.04f, 0.04f, 0.05f, 1f));

            CreateBlock(
                "LAB_FLOOR",
                new Vector3(0f, LabY - 0.35f, 3f),
                new Vector3(28f, 0.7f, 28f),
                floorMat);

            CreateBlock(
                "LAB_BACK_WALL",
                new Vector3(0f, LabY + 6.5f, 16.5f),
                new Vector3(28f, 13f, 0.55f),
                wallMat);

            CreateBlock(
                "LAB_LEFT_WALL",
                new Vector3(-13.7f, LabY + 6.5f, 3f),
                new Vector3(0.55f, 13f, 28f),
                wallMat);

            CreateBlock(
                "LAB_RIGHT_WALL",
                new Vector3(13.7f, LabY + 6.5f, 3f),
                new Vector3(0.55f, 13f, 28f),
                wallMat);

            CreateBlock(
                "LAB_FRONT_WALL",
                new Vector3(0f, LabY + 6.5f, -10.65f),
                new Vector3(28f, 13f, 0.55f),
                wallMat);

            CreateBlock(
                "LAB_CEILING",
                new Vector3(0f, LabY + 13.0f, 3f),
                new Vector3(28f, 0.45f, 28f),
                wallMat);

            // Distance lanes every 2m.
            for (int z = -4; z <= 14; z += 2)
            {
                CreateBlock(
                    "LAB_GRID_Z_" + z,
                    new Vector3(0f, LabY + 0.025f, z),
                    new Vector3(24f, 0.03f, 0.025f),
                    z % 4 == 0 ? gridMat : wallMat);
            }

            for (int x = -10; x <= 10; x += 2)
            {
                CreateBlock(
                    "LAB_GRID_X_" + x,
                    new Vector3(x, LabY + 0.026f, 4f),
                    new Vector3(0.025f, 0.03f, 20f),
                    x == 0 ? gridMat : wallMat);
            }

            // Portal-like lab pylons.
            CreateBlock("LAB_PYLON_L", new Vector3(-6.5f, LabY + 2.2f, 10f), new Vector3(0.45f, 4.4f, 0.45f), accentMat);
            CreateBlock("LAB_PYLON_R", new Vector3(6.5f, LabY + 2.2f, 10f), new Vector3(0.45f, 4.4f, 0.45f), accentMat);

            // PARKOUR TEST LANE — separated from combat dummies.
            // Two parallel walls support alternating wall jumps; staggered platforms
            // give clear height targets for double-jump validation.
            CreateBlock(
                "PARKOUR_WALL_LEFT",
                new Vector3(-10.3f, LabY + 3.2f, 5.5f),
                new Vector3(0.55f, 6.4f, 10.0f),
                wallMat);

            CreateBlock(
                "PARKOUR_WALL_RIGHT",
                new Vector3(-5.7f, LabY + 3.2f, 5.5f),
                new Vector3(0.55f, 6.4f, 10.0f),
                wallMat);

            CreateBlock(
                "PARKOUR_STEP_A",
                new Vector3(-8.0f, LabY + 0.55f, -0.5f),
                new Vector3(3.0f, 0.35f, 2.2f),
                accentMat);

            CreateBlock(
                "PARKOUR_STEP_B",
                new Vector3(-8.0f, LabY + 1.55f, 3.0f),
                new Vector3(3.0f, 0.35f, 2.2f),
                accentMat);

            CreateBlock(
                "PARKOUR_STEP_C",
                new Vector3(-8.0f, LabY + 2.85f, 7.0f),
                new Vector3(3.0f, 0.35f, 2.2f),
                accentMat);

            var key = new GameObject("LAB_KEY_LIGHT");
            key.transform.position = new Vector3(-3f, LabY + 7f, -2f);
            key.transform.rotation = Quaternion.Euler(48f, 28f, 0f);
            var keyLight = key.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.intensity = 1.32f;
            keyLight.color = new Color(1.0f, 0.985f, 0.96f, 1f);

            var rim = new GameObject("LAB_RIM_LIGHT");
            rim.transform.position = new Vector3(0f, LabY + 5.5f, 9f);
            var rimLight = rim.AddComponent<Light>();
            rimLight.type = LightType.Point;
            rimLight.range = 18f;
            rimLight.intensity = 0.55f;
            rimLight.color = new Color(0.88f, 0.92f, 1f, 1f);
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
                    new Color(0.12f, 0.13f, 0.15f, 1f),
                    new Color(0.015f, 0.015f, 0.02f, 1f));

            root.AddComponent<HighflyLabDummyStats>();

            var ring = new GameObject("DummyRing");
            ring.transform.SetParent(root.transform, false);
            ring.transform.localPosition = new Vector3(0f, -0.95f, 0f);

            var lr = ring.AddComponent<LineRenderer>();
            lr.loop = true;
            lr.useWorldSpace = false;
            lr.positionCount = 40;
            lr.widthMultiplier = 0.045f;
            lr.sharedMaterial = HighflyLabVisuals.CreateFxMaterial(new Color(0.22f, 0.24f, 0.27f, 1f));

            for (int i = 0; i < 40; i++)
            {
                float a = (i / 40f) * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.90f, 0f, Mathf.Sin(a) * 0.90f));
            }
        }

        private static string SkillName(int slot)
        {
            HighflySkillDefinitionLite def =
                HighflySkillCatalog.Get(HighflySkillLoadout.Get(slot));

            return def != null ? def.Name : "-";
        }

        private void CheckPlayerBounds()
        {
            if (_player == null) return;

            Vector3 p = _player.transform.position;

            bool escaped =
                p.y < LabY - 2.5f ||
                p.y > LabY + 15.5f ||
                Mathf.Abs(p.x) > 12.8f ||
                p.z < -9.9f ||
                p.z > 15.8f;

            if (escaped)
                ResetPlayerToLab();
        }

        private void ResetPlayerToLab()
        {
            if (_player == null) return;

            CharacterController cc = _player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            _player.transform.position = LabSpawn;
            _player.transform.rotation = Quaternion.identity;

            if (cc != null) cc.enabled = true;

            _player.SetHighflyMobileMove(Vector2.zero);
            HighflyThirdPersonMobileCamera.Instance?.SnapBehindPlayer(13f);

            HighflySkillLabMetrics.RecordAction("SISTEMA • REINGRESO AL LAB", 0);
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
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-24f, -24f);
            rect.sizeDelta = new Vector2(520f, 300f);

            var image = panel.GetComponent<Image>();
            image.color = new Color(0.06f, 0.065f, 0.075f, 0.84f);

            var accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(panel.transform, false);

            var accentRect = accent.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.pivot = new Vector2(0f, 0.5f);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(8f, 0f);

            accent.GetComponent<Image>().color = new Color(0.82f, 0.84f, 0.88f, 1f);

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
