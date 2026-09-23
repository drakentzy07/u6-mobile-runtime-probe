using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Highfly.SkillLab
{
    [DisallowMultipleComponent]
    public sealed class HighflyDonorBrowserV022 : MonoBehaviour
    {
        private RectTransform _content;
        private Text _info;
        private Text _status;
        private readonly List<GameObject> _rows = new List<GameObject>();

        public static HighflyDonorBrowserV022 Install(Transform parent)
        {
            var existing = Object.FindFirstObjectByType<HighflyDonorBrowserV022>();
            if (existing != null) return existing;
            var go = new GameObject("HIGHFLY_DONOR_BROWSER_v022");
            go.transform.SetParent(parent, false);
            return go.AddComponent<HighflyDonorBrowserV022>();
        }

        private void Awake() => Build();

        private void Build()
        {
            var cg = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cg.transform.SetParent(transform, false);
            var canvas = cg.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9900;

            var scaler = cg.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.35f;

            var panel = new GameObject("DONOR_BROWSER_PANEL", typeof(RectTransform), typeof(Image), typeof(Outline));
            panel.transform.SetParent(cg.transform, false);
            var pr = panel.GetComponent<RectTransform>();
            pr.anchorMin = new Vector2(0f, 0f);
            pr.anchorMax = new Vector2(0.40f, 1f);
            pr.offsetMin = new Vector2(12f, 14f);
            pr.offsetMax = new Vector2(-8f, -14f);
            panel.GetComponent<Image>().color = new Color(0.010f, 0.016f, 0.028f, 0.95f);
            panel.GetComponent<Outline>().effectColor = new Color(0.42f, 0.28f, 0.90f, 0.95f);

            var title = Label(panel.transform, "HIGHFLY • DONOR LAB 2.2", 26, TextAnchor.UpperLeft, Color.white);
            SetTop(title.rectTransform, 22f, 18f, 22f, 48f);

            _status = Label(panel.transform, "8 SKILLS • TOCÁ = EJECUTA EN EL PERSONAJE", 15, TextAnchor.MiddleLeft, new Color(0.35f, 0.82f, 1f, 1f));
            SetTop(_status.rectTransform, 22f, 72f, 22f, 34f);

            BuildScroll(panel.transform);

            _info = Label(panel.transform,
                "DONOR = mecánica trazada al proyecto fuente.\nRAW VERIFIED = ejecutado en donor original.\nASSET REPLACED = asset tercero no redistribuible.",
                14, TextAnchor.UpperLeft, new Color(0.80f, 0.87f, 0.94f, 1f));
            var ir = _info.rectTransform;
            ir.anchorMin = new Vector2(0f, 0f);
            ir.anchorMax = new Vector2(1f, 0f);
            ir.pivot = new Vector2(0.5f, 0f);
            ir.offsetMin = new Vector2(22f, 18f);
            ir.offsetMax = new Vector2(-22f, 170f);

            Populate();
        }

        private void BuildScroll(Transform parent)
        {
            var viewport = new GameObject("ScrollViewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            viewport.transform.SetParent(parent, false);
            var vr = viewport.GetComponent<RectTransform>();
            vr.anchorMin = new Vector2(0f, 0f);
            vr.anchorMax = new Vector2(1f, 1f);
            vr.offsetMin = new Vector2(18f, 182f);
            vr.offsetMax = new Vector2(-18f, -118f);
            viewport.GetComponent<Image>().color = new Color(0.006f, 0.012f, 0.024f, 0.80f);

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            _content = content.GetComponent<RectTransform>();
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            _content.anchoredPosition = Vector2.zero;

            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 7f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = viewport.GetComponent<ScrollRect>();
            scroll.content = _content;
            scroll.viewport = vr;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.inertia = true;
            scroll.scrollSensitivity = 48f;
        }

        private void Populate()
        {
            Section("KEEP / CONTROL");
            Add("DRIFT DE FÓRMULA", "KEEP • HIGHFLY", () => HighflyAcceptedSkillRuntimeV016.Instance?.Preview(HighflyAcceptedSkillV016.Drift));
            Add("JUMP SMASH", "KEEP • HIGHFLY", () => HighflyAcceptedSkillRuntimeV016.Instance?.Preview(HighflyAcceptedSkillV016.JumpSmash));

            Section("SIGIL COMBAT • MIT");
            Add("MELEE ATTACK", "SOURCE VERIFIED • 0.30s • r1.0 • DMG18 • POISE1.5 • CD0.5",
                () => HighflyDonorExpandedRuntimeV022.Instance?.Preview(HighflyDonorExpandedSkillV022.SigilMelee));
            Add("DASH ATTACK", "RAW VERIFIED • 2.5m • 0.35s • r1.2 • DMG34 • CD1.2",
                () => HighflyDonorWeaponRuntimeV021.Instance?.Preview(HighflyDonorWeaponSkillV021.SigilDashAttack));
            Add("RANGED SHOT", "SOURCE VERIFIED • 26m/s • r0.35 • DMG12 • CD0.4",
                () => HighflyDonorExpandedRuntimeV022.Instance?.Preview(HighflyDonorExpandedSkillV022.SigilRanged));
            Add("FLASH", "SOURCE VERIFIED • 5m • obstacle r0.4 • COST20 • CD3",
                () => HighflyDonorExpandedRuntimeV022.Instance?.Preview(HighflyDonorExpandedSkillV022.SigilFlash));
            Add("FIREBALL", "SOURCE VERIFIED • charge1.2 • r0.6→2.5 • stun0.8→2.5 • x2.5 • CD4",
                () => HighflyDonorExpandedRuntimeV022.Instance?.Preview(HighflyDonorExpandedSkillV022.SigilFireball));

            Section("DRAGON SOULS • MIT CODE");
            Add("SWORD THROW / EMBED / RECALL",
                "SOURCE VERIFIED • SEMI-FULL • original 3rd-party anim/mesh/SFX not redistributed",
                () => HighflyDonorWeaponRuntimeV021.Instance?.Preview(HighflyDonorWeaponSkillV021.DragonSwordThrowRecall));
        }

        private void Section(string text)
        {
            var t = Label(_content, text, 15, TextAnchor.MiddleLeft, new Color(0.47f, 0.78f, 1f, 1f));
            t.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;
            _rows.Add(t.gameObject);
        }

        private void Add(string name, string note, UnityEngine.Events.UnityAction action)
        {
            var b = Button(_content, "●  " + name + "\n    " + note);
            b.gameObject.AddComponent<LayoutElement>().preferredHeight = 76f;
            b.onClick.AddListener(() =>
            {
                _info.text = name + "\n" + note + "\n\nTOCÁ OTRA SKILL: el panel queda abierto para comparar rápido.";
                action();
            });
            _rows.Add(b.gameObject);
        }

        private static Button Button(Transform parent, string value)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.030f, 0.045f, 0.072f, 0.98f);
            var ol = go.GetComponent<Outline>();
            ol.effectColor = new Color(0.32f, 0.22f, 0.78f, 0.95f);
            ol.effectDistance = new Vector2(1f, -1f);

            var t = Label(go.transform, value, 14, TextAnchor.MiddleLeft, new Color(0.92f, 0.96f, 1f, 1f));
            var tr = t.rectTransform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(14f, 5f);
            tr.offsetMax = new Vector2(-10f, -5f);
            return go.GetComponent<Button>();
        }

        private static Text Label(Transform parent, string value, int size, TextAnchor align, Color color)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = value;
            t.fontSize = size;
            t.alignment = align;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private static void SetTop(RectTransform r, float left, float top, float right, float height)
        {
            r.anchorMin = new Vector2(0f, 1f);
            r.anchorMax = new Vector2(1f, 1f);
            r.pivot = new Vector2(0.5f, 1f);
            r.offsetMin = new Vector2(left, -top - height);
            r.offsetMax = new Vector2(-right, -top);
        }
    }
}
