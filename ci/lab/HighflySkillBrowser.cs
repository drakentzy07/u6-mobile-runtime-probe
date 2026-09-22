using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Highfly.SkillLab
{
    [DisallowMultipleComponent]
    public sealed class HighflySkillBrowser : MonoBehaviour
    {
        public static HighflySkillBrowser Instance { get; private set; }

        private Canvas _canvas;
        private GameObject _panel;
        private RectTransform _content;
        private Text _info;
        private Text _equipLabel;
        private Text _countLabel;
        private readonly Text[] _slotLabels = new Text[5];
        private readonly Button[] _slotButtons = new Button[5];

        private int _selectedSlot;
        private HighflyPremiumSkillId _selectedRuntime = HighflyPremiumSkillId.TwinDance;
        private string _selectedMasterId = "DUAL-02";
        private Coroutine _previewRoutine;
        private HighflyPremiumSkillId _previewingId;

        public static HighflySkillBrowser Install(Transform parent)
        {
            HighflySkillBrowser existing =
                Object.FindFirstObjectByType<HighflySkillBrowser>();

            if (existing != null)
                return existing;

            var go = new GameObject("HIGHFLY_MASTER_SKILL_BROWSER");
            go.transform.SetParent(parent, false);
            var browser = go.AddComponent<HighflySkillBrowser>();
            browser.Build();
            return browser;
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            HighflySkillLoadout.Changed -= RefreshSlots;
            if (Instance == this)
                Instance = null;
        }

        public void OpenForSlot(int slot)
        {
            _selectedSlot = Mathf.Clamp(slot, 0, 4);
            if (_panel != null)
            {
                _panel.SetActive(true);
                RefreshSlots();
            }
        }

        private void Build()
        {
            HighflySkillLoadout.Changed += RefreshSlots;

            var canvasGo = new GameObject(
                "MasterSkillBrowserCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 8600;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Button open = CreateButton(
                canvasGo.transform,
                "CODEX • 28",
                new Vector2(1f, 1f),
                new Vector2(-238f, -58f),
                new Vector2(238f, 70f),
                new Color(0.055f, 0.075f, 0.11f, 0.94f),
                new Color(0.15f, 0.82f, 1f, 1f));

            open.onClick.AddListener(Toggle);

            Text versionStamp = CreateLabel(
                canvasGo.transform,
                "HIGHFLY v0.14.1 • ALL SKILLS GALLERY • 28 PREVIEWS",
                Vector2.zero,
                new Vector2(720f, 44f),
                18,
                TextAnchor.MiddleCenter,
                new Color(0.82f, 0.96f, 1f, 1f),
                false);
            RectTransform stampRect = versionStamp.rectTransform;
            stampRect.anchorMin = stampRect.anchorMax = new Vector2(0.5f, 1f);
            stampRect.anchoredPosition = new Vector2(0f, -24f);

            BuildPanel(canvasGo.transform);
            _panel.SetActive(true);
            RefreshSlots();
        }

        private void BuildPanel(Transform parent)
        {
            _panel = new GameObject(
                "MasterSkillBrowserPanel",
                typeof(RectTransform),
                typeof(Image));

            _panel.transform.SetParent(parent, false);

            RectTransform panel = _panel.GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0f, 0.03f);
            panel.anchorMax = new Vector2(0.36f, 0.97f);
            panel.offsetMin = new Vector2(18f, 0f);
            panel.offsetMax = new Vector2(-6f, 0f);

            _panel.GetComponent<Image>().color =
                new Color(0.018f, 0.028f, 0.045f, 0.94f);

            Text title = CreateLabel(
                _panel.transform,
                "HIGHFLY v0.14.1 • 28 PREVIEWS • MASTER SKILL TREE",
                new Vector2(0f, -20f),
                new Vector2(-150f, 48f),
                25,
                TextAnchor.MiddleLeft,
                new Color(0.90f, 0.97f, 1f, 1f),
                true);
            SetTopStretch(title.rectTransform, 18f, 168f, 12f, 58f);

            Button close = CreateButton(
                _panel.transform,
                "X",
                new Vector2(1f, 1f),
                new Vector2(-35f, -34f),
                new Vector2(52f, 46f),
                new Color(0.12f, 0.13f, 0.16f, 1f),
                new Color(0.55f, 0.62f, 0.72f, 1f));
            close.onClick.AddListener(() => _panel.SetActive(false));

            _countLabel = CreateLabel(
                _panel.transform,
                "",
                Vector2.zero,
                Vector2.zero,
                14,
                TextAnchor.MiddleLeft,
                new Color(0.52f, 0.72f, 0.82f, 1f),
                true);
            SetTopStretch(_countLabel.rectTransform, 18f, 18f, 60f, 88f);

            var slotsRoot = new GameObject("Slots", typeof(RectTransform));
            slotsRoot.transform.SetParent(_panel.transform, false);
            RectTransform slotsRect = slotsRoot.GetComponent<RectTransform>();
            slotsRect.anchorMin = new Vector2(0f, 1f);
            slotsRect.anchorMax = new Vector2(1f, 1f);
            slotsRect.pivot = new Vector2(0.5f, 1f);
            slotsRect.offsetMin = new Vector2(14f, -152f);
            slotsRect.offsetMax = new Vector2(-14f, -92f);

            for (int i = 0; i < 5; i++)
            {
                int slot = i;
                Button b = CreateButton(
                    slotsRoot.transform,
                    "S" + (i + 1),
                    new Vector2(0f, 0.5f),
                    new Vector2(62f + i * 124f, 0f),
                    new Vector2(114f, 54f),
                    new Color(0.045f, 0.065f, 0.095f, 1f),
                    new Color(0.11f, 0.68f, 0.92f, 1f));

                _slotButtons[i] = b;
                _slotLabels[i] = b.GetComponentInChildren<Text>();
                b.onClick.AddListener(() =>
                {
                    _selectedSlot = slot;
                    RefreshSlots();
                });
            }

            BuildScrollArea();

            _info = CreateLabel(
                _panel.transform,
                "",
                Vector2.zero,
                Vector2.zero,
                15,
                TextAnchor.UpperLeft,
                new Color(0.76f, 0.84f, 0.91f, 1f),
                true);
            RectTransform infoRect = _info.rectTransform;
            infoRect.anchorMin = new Vector2(0f, 0f);
            infoRect.anchorMax = new Vector2(1f, 0f);
            infoRect.pivot = new Vector2(0.5f, 0f);
            infoRect.offsetMin = new Vector2(18f, 76f);
            infoRect.offsetMax = new Vector2(-18f, 184f);

            Button equip = CreateButton(
                _panel.transform,
                "EQUIPAR",
                new Vector2(0.5f, 0f),
                new Vector2(0f, 38f),
                new Vector2(300f, 58f),
                new Color(0.04f, 0.20f, 0.28f, 1f),
                new Color(0.10f, 0.84f, 1f, 1f));

            _equipLabel = equip.GetComponentInChildren<Text>();
            equip.onClick.AddListener(EquipSelectedRuntime);

            PopulateScroll();
            SelectRuntime(HighflyPremiumSkillId.TwinDance, false);
        }

        private void BuildScrollArea()
        {
            var viewport = new GameObject(
                "ScrollViewport",
                typeof(RectTransform),
                typeof(Image),
                typeof(RectMask2D),
                typeof(ScrollRect));

            viewport.transform.SetParent(_panel.transform, false);
            RectTransform vr = viewport.GetComponent<RectTransform>();
            vr.anchorMin = new Vector2(0f, 0f);
            vr.anchorMax = new Vector2(1f, 1f);
            vr.offsetMin = new Vector2(14f, 194f);
            vr.offsetMax = new Vector2(-14f, -160f);

            viewport.GetComponent<Image>().color =
                new Color(0.025f, 0.038f, 0.060f, 0.86f);

            var contentGo = new GameObject(
                "Content",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));

            contentGo.transform.SetParent(viewport.transform, false);
            _content = contentGo.GetComponent<RectTransform>();
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            _content.anchoredPosition = Vector2.zero;
            _content.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 7f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = viewport.GetComponent<ScrollRect>();
            scroll.content = _content;
            scroll.viewport = vr;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 34f;
            scroll.inertia = true;
        }

        private void PopulateScroll()
        {
            CreateSection("● PREVIEW RUNTIME • TOCÁ = EJECUTAR");

            for (int i = 0; i < HighflySkillCatalog.All.Length; i++)
            {
                HighflySkillDefinitionLite def = HighflySkillCatalog.All[i];
                HighflyPremiumSkillId id = def.Id;

                Button b = CreateListButton(
                    "●  " + def.Name +
                    "    [" + def.Family + "]    CD " +
                    def.CooldownSeconds.ToString("0.#") + "s");

                b.onClick.AddListener(() => SelectRuntime(id, true));
            }

            CreateSection("◇ MASTER TREE • BASE → EVOLUCIÓN → FUSIÓN → APEX");

            for (int i = 0; i < HighflyMasterSkillTree.All.Length; i++)
            {
                HighflyMasterSkillNode node = HighflyMasterSkillTree.All[i];
                string nodeId = node.Id;

                Button b = CreateListButton(
                    "◇  " + node.Name +
                    "    " + node.Tier.ToString().ToUpperInvariant() +
                    "    [" + node.Weapon + "]");

                b.onClick.AddListener(() => SelectMaster(nodeId));
            }

            if (_countLabel != null)
                _countLabel.text =
                    HighflySkillCatalog.All.Length +
                    " PREVIEWS ACTIVOS  •  " +
                    HighflyMasterSkillTree.All.Length +
                    " NODOS MAESTROS  •  LISTA SCROLL";
        }

        private void SelectRuntime(HighflyPremiumSkillId id, bool preview)
        {
            _selectedRuntime = id;
            HighflySkillDefinitionLite def = HighflySkillCatalog.Get(id);

            if (_info != null && def != null)
            {
                _info.text =
                    "PREVIEW • " + def.Name + "\n" +
                    def.Role + "\n" +
                    "EVOLUCIÓN: " + def.Evolution;
            }

            if (_equipLabel != null)
                _equipLabel.text = "EQUIPAR " +
                    (def != null ? def.Name : "SKILL") +
                    " EN S" + (_selectedSlot + 1);

            if (preview)
                StartPreview(id);
        }

        private void SelectMaster(string id)
        {
            _selectedMasterId = id;
            HighflyMasterSkillNode node = HighflyMasterSkillTree.Get(id);
            if (node == null || _info == null)
                return;

            _info.text =
                node.Tier.ToString().ToUpperInvariant() +
                " • " + node.Name +
                " • " + node.Weapon + "\n" +
                node.Role + " / " + node.MechanicKey + "\n" +
                "VIENE DE: " + node.Parent +
                "  →  DESTINO: " + node.Destination;

            if (_equipLabel != null)
                _equipLabel.text = node.RuntimeReady
                    ? "VERSIÓN RUNTIME DISPONIBLE ARRIBA"
                    : "BASE DEFINIDA • AÚN SIN VFX RUNTIME";
        }

        private void StartPreview(HighflyPremiumSkillId id)
        {
            if (_previewRoutine != null)
            {
                StopCoroutine(_previewRoutine);
                HighflyPremiumSkillRuntime.Instance?.ClearPreviewCooldown(_previewingId);
            }

            _previewingId = id;
            _previewRoutine = StartCoroutine(PreviewRoutine(id));
        }

        private IEnumerator PreviewRoutine(HighflyPremiumSkillId id)
        {
            yield return new WaitForSecondsRealtime(0.035f);

            HighflyPremiumSkillRuntime runtime = HighflyPremiumSkillRuntime.Instance;
            if (runtime != null && NeedsTarget(id))
            {
                CharacterStats target = runtime.FindBestTarget(13f, 360f);
                if (target != null)
                {
                    Vector3 dir = target.transform.position - runtime.transform.position;
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.001f)
                        runtime.transform.rotation =
                            Quaternion.LookRotation(dir.normalized, Vector3.up);
                }
            }

            runtime?.ForcePreview(id);
            yield return new WaitForSecondsRealtime(PreviewDuration(id));
            runtime?.ClearPreviewCooldown(id);
            _previewRoutine = null;
        }

        private static bool NeedsTarget(HighflyPremiumSkillId id)
        {
            return
                id == HighflyPremiumSkillId.ShadowShackle ||
                id == HighflyPremiumSkillId.PhantomTwinDance ||
                id == HighflyPremiumSkillId.AbyssalShackle ||
                id == HighflyPremiumSkillId.ShadowJudgment ||
                id == HighflyPremiumSkillId.EclipseRend ||
                id == HighflyPremiumSkillId.FormulaDrift ||
                id == HighflyPremiumSkillId.SevenSinker ||
                id == HighflyPremiumSkillId.VictimArts ||
                id == HighflyPremiumSkillId.DemonStrike ||
                id == HighflyPremiumSkillId.ShadowCreation ||
                id == HighflyPremiumSkillId.TemporalCut ||
                id == HighflyPremiumSkillId.MemoryRelease ||
                id == HighflyPremiumSkillId.BoundlessMassacre;
        }

        private static float PreviewDuration(HighflyPremiumSkillId id)
        {
            switch (id)
            {
                case HighflyPremiumSkillId.ShadowCall:
                case HighflyPremiumSkillId.ShadowLink:
                    return 2.15f;
                case HighflyPremiumSkillId.ShadowJudgment:
                case HighflyPremiumSkillId.EclipseRend:
                    return 1.95f;
                case HighflyPremiumSkillId.VitalDomain:
                    return 1.70f;
                case HighflyPremiumSkillId.MemoryRelease:
                case HighflyPremiumSkillId.SevenSinker:
                case HighflyPremiumSkillId.VictimArts:
                    return 2.20f;
                case HighflyPremiumSkillId.BoundlessMassacre:
                    return 2.45f;
                default:
                    return 1.55f;
            }
        }

        private void EquipSelectedRuntime()
        {
            HighflySkillDefinitionLite def = HighflySkillCatalog.Get(_selectedRuntime);
            if (def == null || !def.Implemented)
                return;

            HighflySkillLoadout.Assign(_selectedSlot, _selectedRuntime);
            RefreshSlots();
        }

        private void Toggle()
        {
            if (_panel == null)
                return;

            _panel.SetActive(!_panel.activeSelf);
            if (_panel.activeSelf)
                RefreshSlots();
        }

        private void RefreshSlots()
        {
            for (int i = 0; i < 5; i++)
            {
                HighflySkillDefinitionLite def =
                    HighflySkillCatalog.Get(HighflySkillLoadout.Get(i));

                if (_slotLabels[i] != null)
                    _slotLabels[i].text =
                        "S" + (i + 1) + "\n" +
                        CompactName(def != null ? def.Name : "-");

                if (_slotButtons[i] != null)
                {
                    Image image = _slotButtons[i].GetComponent<Image>();
                    if (image != null)
                        image.color = i == _selectedSlot
                            ? new Color(0.07f, 0.34f, 0.46f, 1f)
                            : new Color(0.045f, 0.065f, 0.095f, 1f);
                }
            }

            HighflySkillDefinitionLite selected =
                HighflySkillCatalog.Get(_selectedRuntime);
            if (_equipLabel != null && selected != null)
                _equipLabel.text =
                    "EQUIPAR " + selected.Name +
                    " EN S" + (_selectedSlot + 1);
        }

        private void CreateSection(string label)
        {
            Text t = CreateLabel(
                _content,
                label,
                Vector2.zero,
                new Vector2(0f, 42f),
                16,
                TextAnchor.MiddleLeft,
                new Color(0.42f, 0.82f, 1f, 1f),
                false);

            LayoutElement le = t.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 42f;
        }

        private Button CreateListButton(string label)
        {
            Button b = CreateButton(
                _content,
                label,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(0f, 58f),
                new Color(0.045f, 0.060f, 0.088f, 1f),
                new Color(0.14f, 0.32f, 0.48f, 1f));

            RectTransform rect = b.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(0f, 58f);

            LayoutElement le = b.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 58f;

            Text text = b.GetComponentInChildren<Text>();
            text.fontSize = 14;
            text.alignment = TextAnchor.MiddleLeft;
            return b;
        }

        private static string CompactName(string value)
        {
            if (string.IsNullOrEmpty(value)) return "-";
            string[] parts = value.Split(' ');
            if (parts.Length <= 2) return value;
            return parts[0] + " " + parts[1];
        }

        private static void SetTopStretch(
            RectTransform rect,
            float left,
            float right,
            float top,
            float bottom)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static Button CreateButton(
            Transform parent,
            string label,
            Vector2 anchor,
            Vector2 position,
            Vector2 size,
            Color background,
            Color border)
        {
            var go = new GameObject(
                "Button_" + label.Replace("\n", "_"),
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(Outline));

            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            go.GetComponent<Image>().color = background;
            Outline outline = go.GetComponent<Outline>();
            outline.effectColor = border;
            outline.effectDistance = new Vector2(1f, -1f);

            Text text = CreateLabel(
                go.transform,
                label,
                Vector2.zero,
                size,
                17,
                TextAnchor.MiddleCenter,
                Color.white,
                false);

            RectTransform tr = text.rectTransform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(10f, 4f);
            tr.offsetMax = new Vector2(-10f, -4f);

            return go.GetComponent<Button>();
        }

        private static Text CreateLabel(
            Transform parent,
            string label,
            Vector2 position,
            Vector2 size,
            int fontSize,
            TextAnchor alignment,
            Color color,
            bool ignored)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = label;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }
    }
}
