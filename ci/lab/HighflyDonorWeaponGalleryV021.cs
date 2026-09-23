using UnityEngine;
using UnityEngine.UI;

namespace Highfly.SkillLab
{
    [DisallowMultipleComponent]
    public sealed class HighflyDonorWeaponGalleryV021 : MonoBehaviour
    {
        private GameObject _panel;
        private Text _dragonState;

        public static HighflyDonorWeaponGalleryV021 Install(Transform parent)
        {
            HighflyDonorWeaponGalleryV021 existing =
                Object.FindFirstObjectByType<HighflyDonorWeaponGalleryV021>();

            if (existing != null) return existing;

            GameObject root = new GameObject("HIGHFLY_DONOR_LAB_V021_WEAPON_GALLERY");
            root.transform.SetParent(parent, false);

            HighflyDonorWeaponGalleryV021 gallery =
                root.AddComponent<HighflyDonorWeaponGalleryV021>();

            gallery.Build();
            return gallery;
        }

        private void Update()
        {
            if (_dragonState != null)
            {
                string state = HighflyDonorWeaponRuntimeV021.Instance != null
                    ? HighflyDonorWeaponRuntimeV021.Instance.DragonState
                    : "-";

                _dragonState.text =
                    "DRAGON SOULS • " + state +
                    "\nSEMI-FULL • tap: THROW / tap again: RECALL";
            }
        }

        private void Build()
        {
            GameObject canvasGo = new GameObject(
                "DonorWeaponCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9810;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Button toggle = Button(
                canvasGo.transform,
                "DONOR LAB 2.1 • WEAPONS");

            RectTransform tg = toggle.GetComponent<RectTransform>();
            tg.anchorMin = tg.anchorMax = new Vector2(1f, 1f);
            tg.pivot = new Vector2(1f, 1f);
            tg.anchoredPosition = new Vector2(-24f, -52f);
            tg.sizeDelta = new Vector2(340f, 64f);

            _panel = new GameObject(
                "DonorWeaponPanel",
                typeof(RectTransform),
                typeof(Image),
                typeof(Outline));

            _panel.transform.SetParent(canvasGo.transform, false);

            RectTransform pr = _panel.GetComponent<RectTransform>();
            pr.anchorMin = pr.anchorMax = new Vector2(1f, 0.5f);
            pr.pivot = new Vector2(1f, 0.5f);
            pr.sizeDelta = new Vector2(690f, 500f);
            pr.anchoredPosition = new Vector2(-28f, 10f);

            _panel.GetComponent<Image>().color =
                new Color(0.010f, 0.018f, 0.032f, 0.97f);

            Outline outline = _panel.GetComponent<Outline>();
            outline.effectColor = new Color(0.52f, 0.30f, 1f, 0.95f);
            outline.effectDistance = new Vector2(2f, -2f);

            Text title = Label(
                _panel.transform,
                "HIGHFLY • DONOR LAB 2.1 / WEAPON SKILLS",
                28,
                TextAnchor.MiddleCenter);

            RectTransform tr = title.rectTransform;
            tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 1f);
            tr.anchoredPosition = new Vector2(0f, -42f);
            tr.sizeDelta = new Vector2(640f, 50f);

            Text rule = Label(
                _panel.transform,
                "RAW DONOR → dependencia → adapter mínimo → retarget → KEEP / REJECT\n" +
                "NO INSPIRED • presentación reemplazada sólo cuando el asset original no es redistribuible",
                15,
                TextAnchor.MiddleCenter);

            RectTransform rr = rule.rectTransform;
            rr.anchorMin = rr.anchorMax = new Vector2(0.5f, 1f);
            rr.anchoredPosition = new Vector2(0f, -96f);
            rr.sizeDelta = new Vector2(630f, 60f);

            Button sigil = Button(
                _panel.transform,
                "SIGIL • DASH ATTACK\n" +
                "SEMI-FULL • 2.5m / trace 0.35s / r=1.2 / DMG 34 / poise 3 / CD 1.2");

            RectTransform sr = sigil.GetComponent<RectTransform>();
            sr.anchorMin = sr.anchorMax = new Vector2(0.5f, 1f);
            sr.anchoredPosition = new Vector2(0f, -190f);
            sr.sizeDelta = new Vector2(620f, 96f);

            sigil.onClick.AddListener(() =>
            {
                _panel.SetActive(false);
                HighflyDonorWeaponRuntimeV021.Instance?.Preview(
                    HighflyDonorWeaponSkillV021.SigilDashAttack);
            });

            Button dragon = Button(
                _panel.transform,
                "DRAGON SOULS • SWORD THROW / EMBED / RECALL\n" +
                "SEMI-FULL • donor timing + projectile + embed + 2-stage recall preserved");

            RectTransform dr = dragon.GetComponent<RectTransform>();
            dr.anchorMin = dr.anchorMax = new Vector2(0.5f, 1f);
            dr.anchoredPosition = new Vector2(0f, -310f);
            dr.sizeDelta = new Vector2(620f, 96f);

            dragon.onClick.AddListener(() =>
            {
                _panel.SetActive(false);
                HighflyDonorWeaponRuntimeV021.Instance?.Preview(
                    HighflyDonorWeaponSkillV021.DragonSwordThrowRecall);
            });

            _dragonState = Label(
                _panel.transform,
                "DRAGON SOULS • READY",
                15,
                TextAnchor.MiddleCenter);

            RectTransform ds = _dragonState.rectTransform;
            ds.anchorMin = ds.anchorMax = new Vector2(0.5f, 1f);
            ds.anchoredPosition = new Vector2(0f, -405f);
            ds.sizeDelta = new Vector2(620f, 56f);

            Text note = Label(
                _panel.transform,
                "Sigil raw sample = MIT / Unity 6000.4. Dragon Souls code = MIT; third-party animation/mesh/SFX stay outside public repo.\n" +
                "UAL2 CC0 supplies only replacement choreography/presentation.",
                13,
                TextAnchor.MiddleCenter);

            RectTransform nr = note.rectTransform;
            nr.anchorMin = nr.anchorMax = new Vector2(0.5f, 0f);
            nr.anchoredPosition = new Vector2(0f, 38f);
            nr.sizeDelta = new Vector2(630f, 60f);

            toggle.onClick.AddListener(() =>
                _panel.SetActive(!_panel.activeSelf));
        }

        private static Button Button(Transform parent, string text)
        {
            GameObject go = new GameObject(
                "Button",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(Outline));

            go.transform.SetParent(parent, false);

            go.GetComponent<Image>().color =
                new Color(0.040f, 0.046f, 0.078f, 0.98f);

            Outline outline = go.GetComponent<Outline>();
            outline.effectColor = new Color(0.42f, 0.27f, 0.82f, 0.95f);
            outline.effectDistance = new Vector2(1f, -1f);

            Text t = Label(go.transform, text, 16, TextAnchor.MiddleCenter);
            RectTransform tr = t.rectTransform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(12f, 7f);
            tr.offsetMax = new Vector2(-12f, -7f);

            return go.GetComponent<Button>();
        }

        private static Text Label(
            Transform parent,
            string value,
            int size,
            TextAnchor anchor)
        {
            GameObject go = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(Text));

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
