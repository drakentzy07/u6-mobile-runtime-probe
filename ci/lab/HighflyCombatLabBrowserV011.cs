using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Highfly.SkillLab
{
    [DisallowMultipleComponent]
    public sealed class HighflyCombatLabBrowserV011 : MonoBehaviour
    {
        private sealed class Entry
        {
            public string Name;
            public string Category;
            public string Status;
            public HighflyCombatSkillV010? RuntimeSkill;

            public Entry(string name, string category, string status, HighflyCombatSkillV010? runtimeSkill)
            {
                Name = name;
                Category = category;
                Status = status;
                RuntimeSkill = runtimeSkill;
            }
        }

        private static readonly Entry[] Core10 =
        {
            new Entry("DANZA GEMELA • REFORGED", "DAÑO", "PREMIUM PASS", HighflyCombatSkillV010.TwinDanceReforged),
            new Entry("DRIFT DE FÓRMULA", "MOVIMIENTO", "PREMIUM CANDIDATE", HighflyCombatSkillV010.FormulaDrift),
            new Entry("IMPACTO DUAL", "DAÑO", "SEMI-COCINADA", HighflyCombatSkillV010.DualImpact),
            new Entry("PILE BREAKER", "DAÑO", "SEMI-COCINADA", HighflyCombatSkillV010.PileBreaker),
            new Entry("CADENA SIN LÍMITE", "DAÑO", "SEMI-COCINADA", HighflyCombatSkillV010.BoundlessChain),
            new Entry("SEVEN SINKER • REFORGED", "CONTROL", "PROTOTIPO+", HighflyCombatSkillV010.SevenSinkerReforged),
            new Entry("SCRAP & BUILD • REFORGED", "TÁCTICA", "EN PREPARACIÓN", null),
            new Entry("ARTE DEL SACRIFICIO", "TÁCTICA", "EN PREPARACIÓN", null),
            new Entry("COUNTERFORGE", "DEFENSA", "EN PREPARACIÓN", null),
            new Entry("EDGE RUNNER", "MOVIMIENTO", "PARKOUR CORE", null)
        };

        private GameObject _panel;
        private Text _toggleLabel;
        private Text _title;
        private Text _info;
        private Button _manual;
        private Button _preview;
        private Text _targetModeLabel;
        private bool _expanded;
        private bool _threeTargets = true;
        private Entry _selected;
        private readonly List<GameObject> _rows = new List<GameObject>();

        public static HighflyCombatLabBrowserV011 Install(Transform parent)
        {
            HighflyCombatLabBrowserV011 existing = UnityEngine.Object.FindFirstObjectByType<HighflyCombatLabBrowserV011>();
            if (existing != null) return existing;

            GameObject go = new GameObject("HIGHFLY_COMBAT_BROWSER_v0.11");
            go.transform.SetParent(parent, false);
            return go.AddComponent<HighflyCombatLabBrowserV011>();
        }

        private void Awake()
        {
            Build();
        }

        private void Build()
        {
            GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9300;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Button toggle = CreateButton(canvasGo.transform, "SKILLS  ☰", new Color(0.025f, 0.045f, 0.075f, 0.96f), new Color(0.15f, 0.82f, 1f, 1f), 22);
            RectTransform toggleRect = toggle.GetComponent<RectTransform>();
            toggleRect.anchorMin = toggleRect.anchorMax = new Vector2(0f, 1f);
            toggleRect.pivot = new Vector2(0f, 1f);
            toggleRect.anchoredPosition = new Vector2(18f, -18f);
            toggleRect.sizeDelta = new Vector2(215f, 58f);
            _toggleLabel = toggle.GetComponentInChildren<Text>();
            toggle.onClick.AddListener(TogglePanel);

            _panel = new GameObject("SLF_CORE10_PANEL", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(canvasGo.transform, false);
            RectTransform panelRect = _panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(0.315f, 1f);
            panelRect.offsetMin = new Vector2(18f, 18f);
            panelRect.offsetMax = new Vector2(-8f, -92f);
            _panel.GetComponent<Image>().color = new Color(0.012f, 0.021f, 0.038f, 0.955f);

            _title = CreateLabel(_panel.transform,
                "HIGHFLY • SKILL LAB v0.11\nSLF PREMIUM CORE 10",
                25, TextAnchor.UpperLeft, new Color(0.89f, 0.97f, 1f, 1f));
            SetTopRect(_title.rectTransform, 22f, 18f, -22f, 72f);

            Text subtitle = CreateLabel(_panel.transform,
                "DISEÑO → LAB → 3 MONSTRUOS → PREMIUM",
                17, TextAnchor.MiddleLeft, new Color(0.26f, 0.79f, 1f, 1f));
            SetTopRect(subtitle.rectTransform, 22f, 92f, -22f, 40f);

            Button targetMode = CreateButton(_panel.transform, "TARGETS: 3", new Color(0.045f, 0.075f, 0.10f, 1f), new Color(0.28f, 0.95f, 0.78f, 1f), 18);
            RectTransform targetRect = targetMode.GetComponent<RectTransform>();
            targetRect.anchorMin = targetRect.anchorMax = new Vector2(0f, 1f);
            targetRect.pivot = new Vector2(0f, 1f);
            targetRect.anchoredPosition = new Vector2(22f, -142f);
            targetRect.sizeDelta = new Vector2(230f, 50f);
            _targetModeLabel = targetMode.GetComponentInChildren<Text>();
            targetMode.onClick.AddListener(ToggleTargets);

            Text hint = CreateLabel(_panel.transform,
                "TOCÁ UNA SKILL PARA PREVIEW • LAS GRISADAS AÚN NO SE EJECUTAN",
                15, TextAnchor.MiddleLeft, new Color(0.68f, 0.76f, 0.84f, 1f));
            RectTransform hr = hint.rectTransform;
            hr.anchorMin = hr.anchorMax = new Vector2(0f, 1f);
            hr.pivot = new Vector2(0f, 1f);
            hr.anchoredPosition = new Vector2(270f, -142f);
            hr.sizeDelta = new Vector2(300f, 54f);

            BuildScroll(_panel.transform);

            _info = CreateLabel(_panel.transform, "", 17, TextAnchor.UpperLeft, new Color(0.84f, 0.91f, 0.97f, 1f));
            RectTransform ir = _info.rectTransform;
            ir.anchorMin = new Vector2(0f, 0f);
            ir.anchorMax = new Vector2(1f, 0f);
            ir.pivot = new Vector2(0.5f, 0f);
            ir.offsetMin = new Vector2(22f, 86f);
            ir.offsetMax = new Vector2(-22f, 176f);

            _manual = CreateButton(_panel.transform, "PROBAR MANUAL", new Color(0.035f, 0.19f, 0.27f, 1f), new Color(0.18f, 0.90f, 1f, 1f), 18);
            RectTransform mr = _manual.GetComponent<RectTransform>();
            mr.anchorMin = new Vector2(0f, 0f);
            mr.anchorMax = new Vector2(0.5f, 0f);
            mr.pivot = new Vector2(0.5f, 0f);
            mr.offsetMin = new Vector2(22f, 20f);
            mr.offsetMax = new Vector2(-6f, 74f);
            _manual.onClick.AddListener(ManualSelected);

            _preview = CreateButton(_panel.transform, "PREVIEW AUTO", new Color(0.12f, 0.07f, 0.23f, 1f), new Color(0.72f, 0.38f, 1f, 1f), 18);
            RectTransform pr = _preview.GetComponent<RectTransform>();
            pr.anchorMin = new Vector2(0.5f, 0f);
            pr.anchorMax = new Vector2(1f, 0f);
            pr.pivot = new Vector2(0.5f, 0f);
            pr.offsetMin = new Vector2(6f, 20f);
            pr.offsetMax = new Vector2(-22f, 74f);
            _preview.onClick.AddListener(PreviewSelected);

            Select(Core10[0], false);
            SetExpanded(false);
            HighflyLabTargetsV011.SetThreeTargetMode(_threeTargets);
        }

        private void BuildScroll(Transform parent)
        {
            GameObject viewport = new GameObject("ScrollViewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            viewport.transform.SetParent(parent, false);
            RectTransform vr = viewport.GetComponent<RectTransform>();
            vr.anchorMin = new Vector2(0f, 0f);
            vr.anchorMax = new Vector2(1f, 1f);
            vr.offsetMin = new Vector2(18f, 192f);
            vr.offsetMax = new Vector2(-18f, -214f);
            viewport.GetComponent<Image>().color = new Color(0.007f, 0.014f, 0.026f, 0.80f);

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);

            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = viewport.GetComponent<ScrollRect>();
            scroll.content = contentRect;
            scroll.viewport = vr;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.inertia = true;
            scroll.scrollSensitivity = 46f;

            for (int i = 0; i < Core10.Length; i++)
            {
                Entry entry = Core10[i];
                bool playable = entry.RuntimeSkill.HasValue;
                Color accent = CategoryColor(entry.Category);
                Color bg = playable
                    ? new Color(0.028f, 0.045f, 0.072f, 0.97f)
                    : new Color(0.035f, 0.038f, 0.046f, 0.92f);

                Button row = CreateButton(content.transform,
                    (i + 1).ToString("00") + "  " + entry.Name + "\n      " + entry.Category + " • " + entry.Status,
                    bg, accent, 17);
                LayoutElement le = row.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = 72f;
                row.interactable = playable;
                Entry captured = entry;
                if (playable)
                    row.onClick.AddListener(() => Select(captured, true));
                _rows.Add(row.gameObject);
            }
        }

        private void TogglePanel()
        {
            SetExpanded(!_expanded);
        }

        private void SetExpanded(bool value)
        {
            _expanded = value;
            if (_panel != null) _panel.SetActive(value);
            if (_toggleLabel != null) _toggleLabel.text = value ? "SKILLS  ◀" : "SKILLS  ☰";
        }

        private void ToggleTargets()
        {
            _threeTargets = !_threeTargets;
            HighflyLabTargetsV011.SetThreeTargetMode(_threeTargets);
            if (_targetModeLabel != null)
                _targetModeLabel.text = _threeTargets ? "TARGETS: 3" : "TARGET: 1";
        }

        private void Select(Entry entry, bool autoPreview)
        {
            _selected = entry;
            bool playable = entry != null && entry.RuntimeSkill.HasValue;
            if (_manual != null) _manual.interactable = playable;
            if (_preview != null) _preview.interactable = playable;

            if (_info != null && entry != null)
            {
                _info.text = entry.Name + "\n" +
                             entry.Category + " • " + entry.Status + "\n" +
                             (playable ? "RUNTIME ACTIVO • lista para iterar" : "DISEÑO BLOQUEADO • aún sin runtime v0.11");
            }

            if (autoPreview && playable)
                HighflyCombatLabV010.Instance?.ForcePreview(entry.RuntimeSkill.Value);
        }

        private void ManualSelected()
        {
            if (_selected != null && _selected.RuntimeSkill.HasValue)
                HighflyCombatLabV010.Instance?.TriggerManual(_selected.RuntimeSkill.Value);
        }

        private void PreviewSelected()
        {
            if (_selected != null && _selected.RuntimeSkill.HasValue)
                HighflyCombatLabV010.Instance?.ForcePreview(_selected.RuntimeSkill.Value);
        }

        private static Color CategoryColor(string category)
        {
            switch (category)
            {
                case "DAÑO": return new Color(1f, 0.36f, 0.25f, 1f);
                case "MOVIMIENTO": return new Color(0.18f, 0.84f, 1f, 1f);
                case "CONTROL": return new Color(0.47f, 0.58f, 1f, 1f);
                case "DEFENSA": return new Color(0.98f, 0.88f, 0.42f, 1f);
                default: return new Color(0.72f, 0.39f, 1f, 1f);
            }
        }

        private static Text CreateLabel(Transform parent, string value, int size, TextAnchor anchor, Color color)
        {
            GameObject go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text t = go.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = value;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            return t;
        }

        private static Button CreateButton(Transform parent, string value, Color background, Color accent, int fontSize)
        {
            GameObject go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = background;

            Button button = go.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.05f, 1.05f, 1.05f, 1f);
            colors.pressedColor = new Color(0.80f, 0.90f, 1f, 1f);
            colors.disabledColor = new Color(0.44f, 0.44f, 0.48f, 0.72f);
            button.colors = colors;

            Text label = CreateLabel(go.transform, value, fontSize, TextAnchor.MiddleLeft, new Color(0.92f, 0.96f, 0.99f, 1f));
            RectTransform tr = label.rectTransform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(16f, 5f);
            tr.offsetMax = new Vector2(-10f, -5f);

            GameObject bar = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(go.transform, false);
            RectTransform ar = bar.GetComponent<RectTransform>();
            ar.anchorMin = new Vector2(0f, 0f);
            ar.anchorMax = new Vector2(0f, 1f);
            ar.pivot = new Vector2(0f, 0.5f);
            ar.sizeDelta = new Vector2(6f, 0f);
            bar.GetComponent<Image>().color = accent;

            return button;
        }

        private static void SetTopRect(RectTransform rect, float left, float top, float right, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(right, -top);
        }
    }
}
