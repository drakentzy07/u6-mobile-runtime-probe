using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Highfly.SkillLab
{
    [DisallowMultipleComponent]
    public sealed class HighflyCombatLabBrowserV010 : MonoBehaviour
    {
        private Canvas _canvas;
        private RectTransform _content;
        private Text _title;
        private Text _info;
        private Text _status;
        private HighflyCombatCategoryV010? _filter;
        private HighflyCombatSkillV010 _selected = HighflyCombatSkillV010.TwinDanceReforged;
        private readonly List<GameObject> _rows = new List<GameObject>();

        public static HighflyCombatLabBrowserV010 Install(Transform parent)
        {
            var existing = Object.FindFirstObjectByType<HighflyCombatLabBrowserV010>();
            if (existing != null) return existing;
            GameObject go = new GameObject("HIGHFLY_COMBAT_BROWSER_v0.10");
            go.transform.SetParent(parent, false);
            return go.AddComponent<HighflyCombatLabBrowserV010>();
        }

        private void Awake()
        {
            Build();
        }

        private void Build()
        {
            GameObject cg = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cg.transform.SetParent(transform, false);
            _canvas = cg.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 9200;
            CanvasScaler scaler = cg.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.35f;

            GameObject panel = new GameObject("SLF_CORE_PANEL", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(cg.transform, false);
            RectTransform pr = panel.GetComponent<RectTransform>();
            pr.anchorMin = new Vector2(0f, 0f);
            pr.anchorMax = new Vector2(0.385f, 1f);
            pr.offsetMin = new Vector2(14f, 14f);
            pr.offsetMax = new Vector2(-8f, -14f);
            panel.GetComponent<Image>().color = new Color(0.018f, 0.024f, 0.040f, 0.94f);

            _title = Label(panel.transform, "HIGHFLY • COMBAT FEEL LAB v0.10\nSLF CORE + SHADOW IDENTITY", 25, TextAnchor.UpperLeft, new Color(0.86f, 0.97f, 1f, 1f));
            SetRect(_title.rectTransform, 22f, -18f, -22f, 100f, true);

            _status = Label(panel.transform, "20 SKILLS JUGABLES • TOCÁ = PREVIEW AUTO", 16, TextAnchor.MiddleLeft, new Color(0.34f, 0.78f, 0.96f, 1f));
            SetRect(_status.rectTransform, 22f, -112f, -22f, 38f, true);

            BuildFilters(panel.transform);
            BuildScroll(panel.transform);

            _info = Label(panel.transform, "", 16, TextAnchor.UpperLeft, new Color(0.78f, 0.86f, 0.93f, 1f));
            RectTransform ir = _info.rectTransform;
            ir.anchorMin = new Vector2(0f, 0f); ir.anchorMax = new Vector2(1f, 0f); ir.pivot = new Vector2(0.5f, 0f);
            ir.offsetMin = new Vector2(22f, 92f); ir.offsetMax = new Vector2(-22f, 222f);

            Button manual = Button(panel.transform, "PROBAR MANUAL", new Color(0.04f, 0.22f, 0.30f, 1f), new Color(0.10f, 0.86f, 1f, 1f));
            RectTransform mr = manual.GetComponent<RectTransform>();
            mr.anchorMin = new Vector2(0f, 0f); mr.anchorMax = new Vector2(0.5f, 0f); mr.pivot = new Vector2(0.5f, 0f);
            mr.offsetMin = new Vector2(22f, 22f); mr.offsetMax = new Vector2(-6f, 80f);
            manual.onClick.AddListener(() => HighflyCombatLabV010.Instance?.TriggerManual(_selected));

            Button repeat = Button(panel.transform, "PREVIEW AUTO", new Color(0.12f, 0.09f, 0.24f, 1f), new Color(0.66f, 0.35f, 1f, 1f));
            RectTransform rr = repeat.GetComponent<RectTransform>();
            rr.anchorMin = new Vector2(0.5f, 0f); rr.anchorMax = new Vector2(1f, 0f); rr.pivot = new Vector2(0.5f, 0f);
            rr.offsetMin = new Vector2(6f, 22f); rr.offsetMax = new Vector2(-22f, 80f);
            repeat.onClick.AddListener(() => HighflyCombatLabV010.Instance?.ForcePreview(_selected));

            Populate();
            Select(_selected, false);
        }

        private void BuildFilters(Transform parent)
        {
            string[] names = { "TODO", "DAÑO", "MOV", "CONTROL", "SOMBRA", "BUFF", "DEF", "TÁCTICA" };
            HighflyCombatCategoryV010?[] cats =
            {
                null,
                HighflyCombatCategoryV010.Damage,
                HighflyCombatCategoryV010.Movement,
                HighflyCombatCategoryV010.Control,
                HighflyCombatCategoryV010.Shadow,
                HighflyCombatCategoryV010.Buff,
                HighflyCombatCategoryV010.Defense,
                HighflyCombatCategoryV010.Tactical
            };

            for (int i = 0; i < names.Length; i++)
            {
                int col = i % 4;
                int row = i / 4;
                Button b = Button(parent, names[i], new Color(0.045f, 0.065f, 0.095f, 1f), new Color(0.22f, 0.66f, 0.95f, 1f));
                RectTransform br = b.GetComponent<RectTransform>();
                br.anchorMin = new Vector2(0f, 1f); br.anchorMax = new Vector2(0f, 1f); br.pivot = new Vector2(0f, 1f);
                br.anchoredPosition = new Vector2(22f + col * 164f, -160f - row * 54f);
                br.sizeDelta = new Vector2(152f, 44f);
                HighflyCombatCategoryV010? cat = cats[i];
                b.onClick.AddListener(() => { _filter = cat; Populate(); });
            }
        }

        private void BuildScroll(Transform parent)
        {
            GameObject viewport = new GameObject("ScrollViewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            viewport.transform.SetParent(parent, false);
            RectTransform vr = viewport.GetComponent<RectTransform>();
            vr.anchorMin = new Vector2(0f, 0f); vr.anchorMax = new Vector2(1f, 1f);
            vr.offsetMin = new Vector2(18f, 236f); vr.offsetMax = new Vector2(-18f, -274f);
            viewport.GetComponent<Image>().color = new Color(0.01f, 0.018f, 0.03f, 0.82f);

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            _content = content.GetComponent<RectTransform>();
            _content.anchorMin = new Vector2(0f, 1f); _content.anchorMax = new Vector2(1f, 1f); _content.pivot = new Vector2(0.5f, 1f); _content.anchoredPosition = Vector2.zero;
            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8); layout.spacing = 7f; layout.childControlWidth = true; layout.childControlHeight = true; layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ScrollRect scroll = viewport.GetComponent<ScrollRect>(); scroll.content = _content; scroll.viewport = vr; scroll.horizontal = false; scroll.vertical = true; scroll.inertia = true; scroll.scrollSensitivity = 38f;
        }

        private void Populate()
        {
            for (int i = 0; i < _rows.Count; i++) if (_rows[i] != null) Destroy(_rows[i]);
            _rows.Clear();
            HighflyCombatCategoryV010? last = null;
            for (int i = 0; i < HighflyCombatCatalogV010.All.Length; i++)
            {
                HighflyCombatSkillDefinitionV010 d = HighflyCombatCatalogV010.All[i];
                if (_filter.HasValue && d.Category != _filter.Value) continue;
                if (!last.HasValue || last.Value != d.Category)
                {
                    Text section = Label(_content, CategoryName(d.Category), 15, TextAnchor.MiddleLeft, CategoryColor(d.Category));
                    LayoutElement sle = section.gameObject.AddComponent<LayoutElement>(); sle.preferredHeight = 34f;
                    _rows.Add(section.gameObject);
                    last = d.Category;
                }
                Button b = Button(_content, "●  " + d.Name + "\n    " + d.Role, new Color(0.035f, 0.050f, 0.075f, 0.96f), CategoryColor(d.Category));
                LayoutElement le = b.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = 72f;
                HighflyCombatSkillV010 id = d.Id;
                b.onClick.AddListener(() => Select(id, true));
                _rows.Add(b.gameObject);
            }
            if (_status != null)
                _status.text = (_filter.HasValue ? CategoryName(_filter.Value) : "TODAS LAS CATEGORÍAS") + " • 20 CORE SKILLS • JUGABLE";
        }

        private void Select(HighflyCombatSkillV010 id, bool preview)
        {
            _selected = id;
            HighflyCombatSkillDefinitionV010 d = HighflyCombatCatalogV010.Get(id);
            if (d != null && _info != null)
            {
                _info.text = d.Name + "\n" + CategoryName(d.Category) + " • " + d.Role + "\nSKILL EXPRESSION: " + d.SkillExpression + "\nPREVIEW: " + d.PreviewSeconds.ToString("0.0") + "s";
            }
            if (preview) HighflyCombatLabV010.Instance?.ForcePreview(id);
        }

        private static string CategoryName(HighflyCombatCategoryV010 c)
        {
            switch (c)
            {
                case HighflyCombatCategoryV010.Damage: return "DAÑO";
                case HighflyCombatCategoryV010.Movement: return "MOVIMIENTO";
                case HighflyCombatCategoryV010.Control: return "CONTROL";
                case HighflyCombatCategoryV010.Shadow: return "SOMBRA / SUMMON";
                case HighflyCombatCategoryV010.Buff: return "BUFF / DOMINIO";
                case HighflyCombatCategoryV010.Defense: return "DEFENSA / COUNTER";
                default: return "TÁCTICA / PREDICCIÓN";
            }
        }

        private static Color CategoryColor(HighflyCombatCategoryV010 c)
        {
            switch (c)
            {
                case HighflyCombatCategoryV010.Damage: return new Color(1f, 0.36f, 0.24f, 1f);
                case HighflyCombatCategoryV010.Movement: return new Color(0.20f, 0.84f, 1f, 1f);
                case HighflyCombatCategoryV010.Control: return new Color(0.42f, 0.66f, 1f, 1f);
                case HighflyCombatCategoryV010.Shadow: return new Color(0.70f, 0.30f, 1f, 1f);
                case HighflyCombatCategoryV010.Buff: return new Color(0.42f, 1f, 0.62f, 1f);
                case HighflyCombatCategoryV010.Defense: return new Color(0.95f, 0.92f, 0.48f, 1f);
                default: return new Color(1f, 0.74f, 0.24f, 1f);
            }
        }

        private static Text Label(Transform parent, string value, int size, TextAnchor align, Color color)
        {
            GameObject go = new GameObject("Label", typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            Text t = go.GetComponent<Text>(); t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.text = value; t.fontSize = size; t.alignment = align; t.color = color; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private static Button Button(Transform parent, string value, Color bg, Color accent)
        {
            GameObject go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>(); img.color = bg;
            Button b = go.GetComponent<Button>(); ColorBlock cb = b.colors; cb.normalColor = Color.white; cb.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f); cb.pressedColor = new Color(0.82f, 0.90f, 1f, 1f); b.colors = cb;
            Text t = Label(go.transform, value, 15, TextAnchor.MiddleLeft, new Color(0.90f, 0.94f, 0.98f, 1f));
            RectTransform tr = t.rectTransform; tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = new Vector2(14f, 5f); tr.offsetMax = new Vector2(-10f, -5f);
            GameObject bar = new GameObject("Accent", typeof(RectTransform), typeof(Image)); bar.transform.SetParent(go.transform, false); RectTransform ar = bar.GetComponent<RectTransform>(); ar.anchorMin = new Vector2(0f, 0f); ar.anchorMax = new Vector2(0f, 1f); ar.pivot = new Vector2(0f, 0.5f); ar.sizeDelta = new Vector2(5f, 0f); bar.GetComponent<Image>().color = accent;
            return b;
        }

        private static void SetRect(RectTransform r, float left, float top, float right, float height, bool topAnchor)
        {
            if (topAnchor)
            {
                r.anchorMin = new Vector2(0f, 1f); r.anchorMax = new Vector2(1f, 1f); r.pivot = new Vector2(0.5f, 1f);
                r.offsetMin = new Vector2(left, -top - height); r.offsetMax = new Vector2(right, -top);
            }
        }
    }
}
