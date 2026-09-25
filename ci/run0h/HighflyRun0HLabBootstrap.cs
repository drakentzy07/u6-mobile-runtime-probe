using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Highfly.Mobile;
using Highfly.Run0I2;

namespace Highfly.Run0H
{
    [DisallowMultipleComponent]
    public sealed class HighflyRun0HDummyStats : CharacterStats
    {
        public override void Start()
        {
            maxEgo = 999999f;
            currentEgo = maxEgo;
        }

        public override void TakeDamage(float damage, float composureDamage = 10f, Transform attacker = null)
        {
            currentEgo = maxEgo;

            StopAllCoroutines();
            StartCoroutine(HitPulse());
        }

        private IEnumerator HitPulse()
        {
            Vector3 baseScale = transform.localScale;
            transform.localScale = new Vector3(
                baseScale.x * 1.08f,
                baseScale.y * 0.93f,
                baseScale.z * 1.08f);

            yield return new WaitForSecondsRealtime(0.06f);
            transform.localScale = baseScale;
        }
    }

    [DisallowMultipleComponent]
    public sealed class HighflyRun0HLabBootstrap : MonoBehaviour
    {
        private const float LabY = 120f;
        private static readonly Vector3 LabSpawn = new Vector3(0f, LabY + 0.9f, -7.2f);

        private PlayerController _player;
        private bool _setup;
        private float _nextSafetyCheck;
        private Text _characterStatus;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (FindAnyObjectByType<HighflyRun0HLabBootstrap>() != null) return;

            var root = new GameObject("HIGHFLY_RUN0H_LAB_BOOTSTRAP");
            DontDestroyOnLoad(root);
            root.AddComponent<HighflyRun0HLabBootstrap>();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (!_setup)
            {
                PlayerController player = FindAnyObjectByType<PlayerController>();
                if (player != null)
                    SetupLab(player);
            }

            if (_setup && _player != null && Time.unscaledTime >= _nextSafetyCheck)
            {
                _nextSafetyCheck = Time.unscaledTime + 0.15f;
                CheckBounds();
                RefreshCharacterStatus();
            }
        }

        private void SetupLab(PlayerController player)
        {
            _setup = true;
            _player = player;

            BuildRoom();
            BuildDummies();
            MovePlayerToLab();
            HideLucidStoryUi();
            BuildCleanLabUi();

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.055f, 0.065f, 0.085f, 1f);
            }

            StartCoroutine(SnapCamera());

            Debug.Log("[RUN0H] LAB READY • DIRECT BOOT • NO PROLOGUE.");
        }

        private void MovePlayerToLab()
        {
            CharacterController cc = _player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            _player.transform.position = LabSpawn;
            _player.transform.rotation = Quaternion.identity;
            _player.SetHighflyMobileMove(Vector2.zero);

            if (cc != null) cc.enabled = true;
        }

        private IEnumerator SnapCamera()
        {
            yield return null;
            yield return null;
            HighflyThirdPersonMobileCamera.Instance?.SnapBehindPlayer(13f);
        }

        private void HideLucidStoryUi()
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);

            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null) continue;

                string n = canvas.gameObject.name;
                bool keep =
                    n.Contains("HIGHFLY Mobile HUD") ||
                    n.Contains("RUN0H");

                if (!keep)
                    canvas.gameObject.SetActive(false);
            }
        }

        private static void BuildRoom()
        {
            Material floor = CreateMaterial(new Color(0.20f, 0.23f, 0.28f, 1f));
            Material wall = CreateMaterial(new Color(0.12f, 0.14f, 0.18f, 1f));
            Material grid = CreateMaterial(new Color(0.30f, 0.36f, 0.43f, 1f));
            Material accent = CreateMaterial(new Color(0.08f, 0.42f, 0.60f, 1f));

            CreateBlock("RUN0H_FLOOR",
                new Vector3(0f, LabY - 0.35f, 3f),
                new Vector3(28f, 0.7f, 28f),
                floor);

            CreateBlock("RUN0H_BACK",
                new Vector3(0f, LabY + 6.5f, 16.5f),
                new Vector3(28f, 13f, 0.55f),
                wall);

            CreateBlock("RUN0H_LEFT",
                new Vector3(-13.7f, LabY + 6.5f, 3f),
                new Vector3(0.55f, 13f, 28f),
                wall);

            CreateBlock("RUN0H_RIGHT",
                new Vector3(13.7f, LabY + 6.5f, 3f),
                new Vector3(0.55f, 13f, 28f),
                wall);

            CreateBlock("RUN0H_FRONT",
                new Vector3(0f, LabY + 6.5f, -10.65f),
                new Vector3(28f, 13f, 0.55f),
                wall);

            CreateBlock("RUN0H_CEILING",
                new Vector3(0f, LabY + 13f, 3f),
                new Vector3(28f, 0.45f, 28f),
                wall);

            for (int z = -4; z <= 14; z += 2)
            {
                CreateBlock(
                    "RUN0H_GRID_Z_" + z,
                    new Vector3(0f, LabY + 0.025f, z),
                    new Vector3(24f, 0.03f, 0.025f),
                    z % 4 == 0 ? accent : grid);
            }

            for (int x = -10; x <= 10; x += 2)
            {
                CreateBlock(
                    "RUN0H_GRID_X_" + x,
                    new Vector3(x, LabY + 0.026f, 4f),
                    new Vector3(0.025f, 0.03f, 20f),
                    x == 0 ? accent : grid);
            }

            GameObject key = new GameObject("RUN0H_KEY_LIGHT");
            key.transform.position = new Vector3(-3f, LabY + 7f, -2f);
            key.transform.rotation = Quaternion.Euler(48f, 28f, 0f);
            Light keyLight = key.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.intensity = 1.25f;
            keyLight.color = new Color(0.92f, 0.96f, 1f, 1f);

            GameObject rim = new GameObject("RUN0H_RIM_LIGHT");
            rim.transform.position = new Vector3(0f, LabY + 5f, 9f);
            Light rimLight = rim.AddComponent<Light>();
            rimLight.type = LightType.Point;
            rimLight.range = 18f;
            rimLight.intensity = 1.0f;
            rimLight.color = new Color(0.15f, 0.65f, 1f, 1f);
        }

        private static void BuildDummies()
        {
            CreateDummy(new Vector3(0f, LabY + 1.15f, 4.5f), "DUMMY_CENTRO");
            CreateDummy(new Vector3(-4.0f, LabY + 1.15f, 8.5f), "DUMMY_IZQ");
            CreateDummy(new Vector3(4.0f, LabY + 1.15f, 8.5f), "DUMMY_DER");
        }

        private static void CreateDummy(Vector3 position, string name)
        {
            GameObject root = new GameObject(name);
            root.transform.position = position;

            try { root.tag = "Enemy"; } catch { }

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.90f, 1.10f, 0.90f);

            Renderer r = body.GetComponent<Renderer>();
            if (r != null)
                r.sharedMaterial = CreateMaterial(new Color(0.50f, 0.13f, 0.17f, 1f));

            root.AddComponent<HighflyRun0HDummyStats>();
        }

        private void BuildCleanLabUi()
        {
            GameObject canvasGo = new GameObject(
                "RUN0H_LAB_UI",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 6500;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject panel = new GameObject(
                "RUN0H_SKILL_PANEL",
                typeof(RectTransform),
                typeof(Image));

            panel.transform.SetParent(canvasGo.transform, false);

            RectTransform pr = panel.GetComponent<RectTransform>();
            pr.anchorMin = pr.anchorMax = new Vector2(0f, 1f);
            pr.pivot = new Vector2(0f, 1f);
            pr.anchoredPosition = new Vector2(24f, -24f);
            pr.sizeDelta = new Vector2(430f, 730f);

            panel.GetComponent<Image>().color =
                new Color(0.018f, 0.026f, 0.045f, 0.93f);

            Text title = CreateText(
                panel.transform,
                "HIGHFLY • SKILLS LAB SUPREMO\nRUN0I.3 • SINGLE HUNTER + GUARD",
                24,
                TextAnchor.UpperLeft);

            RectTransform tr = title.rectTransform;
            tr.anchorMin = tr.anchorMax = new Vector2(0f, 1f);
            tr.pivot = new Vector2(0f, 1f);
            tr.anchoredPosition = new Vector2(18f, -16f);
            tr.sizeDelta = new Vector2(390f, 68f);

            Text charLabel = CreateText(
                panel.transform,
                "LOADOUT DE COMBATE",
                18,
                TextAnchor.MiddleLeft);

            RectTransform clr = charLabel.rectTransform;
            clr.anchorMin = clr.anchorMax = new Vector2(0f, 1f);
            clr.pivot = new Vector2(0f, 1f);
            clr.anchoredPosition = new Vector2(18f, -95f);
            clr.sizeDelta = new Vector2(390f, 30f);

            Button unarmed = CreateButton(panel.transform,"SIN ARMAS",new Vector2(18f,-135f),new Vector2(382f,42f));
            unarmed.onClick.AddListener(()=>HighflyRun0HCharacterVisual.Instance?.EquipPreset(HighflyLoadoutProfile.Unarmed));

            Button sword1 = CreateButton(panel.transform,"ESPADA 1H",new Vector2(18f,-185f),new Vector2(185f,48f));
            sword1.onClick.AddListener(()=>HighflyRun0HCharacterVisual.Instance?.UseLoadout(HighflyLoadoutProfile.Sword1H));

            Button dualSword = CreateButton(panel.transform,"DOBLE ESPADA",new Vector2(215f,-185f),new Vector2(185f,48f));
            dualSword.onClick.AddListener(()=>HighflyRun0HCharacterVisual.Instance?.UseLoadout(HighflyLoadoutProfile.DualSword));

            Button swordShield = CreateButton(panel.transform,"ESPADA + ESC",new Vector2(18f,-241f),new Vector2(185f,48f));
            swordShield.onClick.AddListener(()=>HighflyRun0HCharacterVisual.Instance?.UseLoadout(HighflyLoadoutProfile.SwordShield));

            Button daggers = CreateButton(panel.transform,"DOBLE DAGA",new Vector2(215f,-241f),new Vector2(185f,48f));
            daggers.onClick.AddListener(()=>HighflyRun0HCharacterVisual.Instance?.UseLoadout(HighflyLoadoutProfile.DualDaggers));

            Button axe1 = CreateButton(panel.transform,"HACHA 1H",new Vector2(18f,-297f),new Vector2(185f,48f));
            axe1.onClick.AddListener(()=>HighflyRun0HCharacterVisual.Instance?.UseLoadout(HighflyLoadoutProfile.Axe1H));

            Button dualAxe = CreateButton(panel.transform,"DOBLE HACHA",new Vector2(215f,-297f),new Vector2(185f,48f));
            dualAxe.onClick.AddListener(()=>HighflyRun0HCharacterVisual.Instance?.UseLoadout(HighflyLoadoutProfile.DualAxe));

            Button axeShield = CreateButton(panel.transform,"HACHA + ESC",new Vector2(18f,-353f),new Vector2(185f,48f));
            axeShield.onClick.AddListener(()=>HighflyRun0HCharacterVisual.Instance?.UseLoadout(HighflyLoadoutProfile.AxeShield));

            Button spear = CreateButton(panel.transform,"LANZA 2H",new Vector2(215f,-353f),new Vector2(185f,48f));
            spear.onClick.AddListener(()=>HighflyRun0HCharacterVisual.Instance?.UseLoadout(HighflyLoadoutProfile.Spear2H));

            _characterStatus = CreateText(
                panel.transform,
                "Cargando personaje...",
                16,
                TextAnchor.MiddleLeft);

            RectTransform csr = _characterStatus.rectTransform;
            csr.anchorMin = csr.anchorMax = new Vector2(0f, 1f);
            csr.pivot = new Vector2(0f, 1f);
            csr.anchoredPosition = new Vector2(18f, -414f);
            csr.sizeDelta = new Vector2(390f, 34f);

            Text skillsHeader = CreateText(
                panel.transform,
                "GRAMÁTICA DIRECCIONAL",
                18,
                TextAnchor.MiddleLeft);

            RectTransform shr = skillsHeader.rectTransform;
            shr.anchorMin = shr.anchorMax = new Vector2(0f, 1f);
            shr.pivot = new Vector2(0f, 1f);
            shr.anchoredPosition = new Vector2(18f, -460f);
            shr.sizeDelta = new Vector2(390f, 30f);

            string[] slots =
            {
                "ATQ • ComboRecipe según arma",
                "DIRECCIÓN • ↓ ↑ ← → / \\ X T S",
                "GUARD • Cross real / Shield real / Pole / Weapon",
                "S1 legacy test • S2 OFF • PARKOUR intacto"
            };

            for (int i = 0; i < slots.Length; i++)
            {
                Text row = CreateText(
                    panel.transform,
                    slots[i],
                    16,
                    TextAnchor.MiddleLeft);

                RectTransform rr = row.rectTransform;
                rr.anchorMin = rr.anchorMax = new Vector2(0f, 1f);
                rr.pivot = new Vector2(0f, 1f);
                rr.anchoredPosition = new Vector2(18f, -502f - i * 44f);
                rr.sizeDelta = new Vector2(390f, 40f);
            }

            Text footer = CreateText(
                panel.transform,
                "CORE: EQUIPMENT → RESOLVER → MELEE\n1 Hunter fijo • guard por equipo • 3 dummies",
                14,
                TextAnchor.LowerLeft);

            RectTransform fr = footer.rectTransform;
            fr.anchorMin = fr.anchorMax = new Vector2(0f, 0f);
            fr.pivot = new Vector2(0f, 0f);
            fr.anchoredPosition = new Vector2(18f, 14f);
            fr.sizeDelta = new Vector2(390f, 52f);
        }

        private void RefreshCharacterStatus()
        {
            if (_characterStatus == null) return;

            HighflyRun0HCharacterVisual visual = HighflyRun0HCharacterVisual.Instance;
            _characterStatus.text = visual != null && visual.IsBound
                ? visual.CurrentLabel + " • ALTURA 1.72m"
                : "Cargando personaje...";
        }

        private void CheckBounds()
        {
            Vector3 p = _player.transform.position;

            bool escaped =
                p.y < LabY - 2.5f ||
                p.y > LabY + 15.5f ||
                Mathf.Abs(p.x) > 12.8f ||
                p.z < -9.9f ||
                p.z > 15.8f;

            if (!escaped) return;

            MovePlayerToLab();
            HighflyThirdPersonMobileCamera.Instance?.SnapBehindPlayer(13f);
        }

        private static Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            Material mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            return mat;
        }

        private static GameObject CreateBlock(
            string name,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;

            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = material;

            return go;
        }

        private static Button CreateButton(
            Transform parent,
            string label,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            GameObject go = new GameObject(
                label + "_BUTTON",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));

            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            go.GetComponent<Image>().color =
                new Color(0.045f, 0.10f, 0.16f, 0.98f);

            Text text = CreateText(
                go.transform,
                label,
                17,
                TextAnchor.MiddleCenter);

            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;

            return go.GetComponent<Button>();
        }

        private static Text CreateText(
            Transform parent,
            string value,
            int size,
            TextAnchor anchor)
        {
            GameObject go = new GameObject(
                "Text",
                typeof(RectTransform),
                typeof(Text));

            go.transform.SetParent(parent, false);

            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = new Color(0.90f, 0.96f, 1f, 1f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            return text;
        }
    }
}
