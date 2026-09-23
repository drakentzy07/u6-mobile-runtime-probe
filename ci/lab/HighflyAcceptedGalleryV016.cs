using UnityEngine;
using UnityEngine.UI;

namespace Highfly.SkillLab
{
    [DisallowMultipleComponent]
    public sealed class HighflyAcceptedGalleryV016 : MonoBehaviour
    {
        private GameObject _panel;

        public static HighflyAcceptedGalleryV016 Install(Transform parent)
        {
            HighflyAcceptedGalleryV016 existing = Object.FindFirstObjectByType<HighflyAcceptedGalleryV016>();
            if (existing != null) return existing;

            GameObject root = new GameObject("HIGHFLY_V016_ACCEPTED_GALLERY");
            root.transform.SetParent(parent, false);
            HighflyAcceptedGalleryV016 gallery = root.AddComponent<HighflyAcceptedGalleryV016>();
            gallery.Build();
            return gallery;
        }

        private void Build()
        {
            GameObject canvasGo = new GameObject("AcceptedGalleryCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9800;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Button toggle = Button(canvasGo.transform, "SKILLS • 2 KEEP");
            RectTransform tg = toggle.GetComponent<RectTransform>();
            tg.anchorMin = tg.anchorMax = new Vector2(0.5f, 1f);
            tg.anchoredPosition = new Vector2(0f, -52f);
            tg.sizeDelta = new Vector2(270f, 64f);

            _panel = new GameObject("AcceptedPanel", typeof(RectTransform), typeof(Image), typeof(Outline));
            _panel.transform.SetParent(canvasGo.transform, false);
            RectTransform pr = _panel.GetComponent<RectTransform>();
            pr.anchorMin = new Vector2(0.5f, 0.5f);
            pr.anchorMax = new Vector2(0.5f, 0.5f);
            pr.sizeDelta = new Vector2(620f, 390f);
            pr.anchoredPosition = new Vector2(0f, 40f);
            _panel.GetComponent<Image>().color = new Color(0.012f, 0.020f, 0.034f, 0.96f);
            _panel.GetComponent<Outline>().effectColor = new Color(0.10f, 0.70f, 0.95f, 0.9f);

            Text title = Label(_panel.transform, "HIGHFLY v0.16 • ACCEPTED CORE", 28, TextAnchor.MiddleCenter);
            RectTransform tr = title.rectTransform;
            tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 1f);
            tr.anchoredPosition = new Vector2(0f, -46f);
            tr.sizeDelta = new Vector2(560f, 52f);

            Text sub = Label(_panel.transform, "KEEP = se mejora. REJECT = rollback. Sin skills falsas.", 16, TextAnchor.MiddleCenter);
            RectTransform sr = sub.rectTransform;
            sr.anchorMin = sr.anchorMax = new Vector2(0.5f, 1f);
            sr.anchoredPosition = new Vector2(0f, -92f);
            sr.sizeDelta = new Vector2(560f, 44f);

            AddSkill(_panel.transform, new Vector2(0f, -170f), "DRIFT DE FÓRMULA", "KEEP • si se bloquea otra vez, el bug es interno y no UI", HighflyAcceptedSkillV016.Drift);
            AddSkill(_panel.transform, new Vector2(0f, -270f), "JUMP SMASH", "KEEP • limpia trigger básico antes de doSmash", HighflyAcceptedSkillV016.JumpSmash);

            toggle.onClick.AddListener(() => _panel.SetActive(!_panel.activeSelf));
        }

        private void AddSkill(Transform parent, Vector2 pos, string name, string note, HighflyAcceptedSkillV016 skill)
        {
            Button b = Button(parent, name + "\n" + note);
            RectTransform r = b.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
            r.anchoredPosition = pos;
            r.sizeDelta = new Vector2(540f, 82f);

            b.onClick.AddListener(() =>
            {
                // Hide immediately so the mobile joystick is never covered after activation.
                _panel.SetActive(false);
                HighflyAcceptedSkillRuntimeV016.Instance?.Preview(skill);
            });
        }

        private static Button Button(Transform parent, string text)
        {
            GameObject go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.035f, 0.052f, 0.080f, 0.98f);
            Outline outline = go.GetComponent<Outline>();
            outline.effectColor = new Color(0.11f, 0.48f, 0.72f, 0.95f);
            outline.effectDistance = new Vector2(1f, -1f);

            Text t = Label(go.transform, text, 17, TextAnchor.MiddleCenter);
            RectTransform tr = t.rectTransform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(12f, 6f);
            tr.offsetMax = new Vector2(-12f, -6f);

            return go.GetComponent<Button>();
        }

        private static Text Label(Transform parent, string value, int size, TextAnchor anchor)
        {
            GameObject go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text t = go.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = value;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }
    }
}
