using UnityEngine;
using UnityEngine.UI;

namespace Highfly.SkillLab
{
    [DisallowMultipleComponent]
    public sealed class HighflyCuratedGalleryV015 : MonoBehaviour
    {
        public static HighflyCuratedGalleryV015 Install(Transform parent)
        {
            HighflyCuratedGalleryV015 existing = Object.FindFirstObjectByType<HighflyCuratedGalleryV015>();
            if (existing != null) return existing;

            GameObject root = new GameObject("HIGHFLY_V015_CURATED_GALLERY");
            root.transform.SetParent(parent, false);
            HighflyCuratedGalleryV015 gallery = root.AddComponent<HighflyCuratedGalleryV015>();
            gallery.Build();
            return gallery;
        }

        private void Build()
        {
            GameObject canvasGo = new GameObject("CuratedGalleryCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9800;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject panel = new GameObject("CuratedPanel", typeof(RectTransform), typeof(Image), typeof(Outline));
            panel.transform.SetParent(canvasGo.transform, false);
            RectTransform pr = panel.GetComponent<RectTransform>();
            pr.anchorMin = new Vector2(0f, 0.08f);
            pr.anchorMax = new Vector2(0.34f, 0.96f);
            pr.offsetMin = new Vector2(18f, 0f);
            pr.offsetMax = new Vector2(-8f, 0f);
            panel.GetComponent<Image>().color = new Color(0.012f, 0.020f, 0.034f, 0.96f);
            panel.GetComponent<Outline>().effectColor = new Color(0.10f, 0.70f, 0.95f, 0.9f);

            Text title = Label(panel.transform, "HIGHFLY v0.15 • CURATED DONOR PASS", 25, TextAnchor.MiddleLeft);
            RectTransform tr = title.rectTransform;
            tr.anchorMin = new Vector2(0f, 1f);
            tr.anchorMax = new Vector2(1f, 1f);
            tr.pivot = new Vector2(0.5f, 1f);
            tr.offsetMin = new Vector2(18f, -62f);
            tr.offsetMax = new Vector2(-18f, -12f);

            Text subtitle = Label(panel.transform,
                "SOLO MECÁNICAS AUDITADAS • SIN RELLENO • TOCÁ = PREVIEW",
                14, TextAnchor.MiddleLeft);
            RectTransform sr = subtitle.rectTransform;
            sr.anchorMin = new Vector2(0f, 1f);
            sr.anchorMax = new Vector2(1f, 1f);
            sr.pivot = new Vector2(0.5f, 1f);
            sr.offsetMin = new Vector2(18f, -102f);
            sr.offsetMax = new Vector2(-18f, -64f);

            AddButton(panel.transform, 0, "DRIFT DE FÓRMULA", "HIGHFLY • probado", HighflyCuratedDonorSkill.Drift);
            AddButton(panel.transform, 1, "ARTE DEL SACRIFICIO", "HIGHFLY + Dragon Souls • rework VFX", HighflyCuratedDonorSkill.ArteSacrificio);
            AddButton(panel.transform, 2, "JUMP SMASH", "Lucid nativa • trigger doSmash", HighflyCuratedDonorSkill.JumpSmash);
            AddButton(panel.transform, 3, "EMBER BOLT", "SubspaceHunter Fire → HIGHFLY", HighflyCuratedDonorSkill.EmberBolt);
            AddButton(panel.transform, 4, "THUNDER MARK", "SubspaceHunter Thunder → HIGHFLY", HighflyCuratedDonorSkill.ThunderMark);
            AddButton(panel.transform, 5, "FROST LANCE", "SubspaceHunter Ice → HIGHFLY", HighflyCuratedDonorSkill.FrostLance);
            AddButton(panel.transform, 6, "METEOR BREAK", "SubspaceHunter Meteor → HIGHFLY", HighflyCuratedDonorSkill.MeteorBreak);
            AddButton(panel.transform, 7, "AEGIS", "SubspaceHunter Shield → HIGHFLY • absorbe daño", HighflyCuratedDonorSkill.Aegis);

            Text note = Label(panel.transform,
                "FUERA DE ESTE BUILD: Danza, Cadena, Impacto Dual, geometría repetida y previews no aprobados.",
                13, TextAnchor.UpperLeft);
            RectTransform nr = note.rectTransform;
            nr.anchorMin = new Vector2(0f, 0f);
            nr.anchorMax = new Vector2(1f, 0f);
            nr.pivot = new Vector2(0.5f, 0f);
            nr.offsetMin = new Vector2(18f, 16f);
            nr.offsetMax = new Vector2(-18f, 74f);
        }

        private static void AddButton(
            Transform parent,
            int index,
            string name,
            string origin,
            HighflyCuratedDonorSkill skill)
        {
            GameObject go = new GameObject("Skill_" + name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
            go.transform.SetParent(parent, false);

            RectTransform r = go.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, 1f);
            r.anchorMax = new Vector2(1f, 1f);
            r.pivot = new Vector2(0.5f, 1f);
            float top = 118f + index * 91f;
            r.offsetMin = new Vector2(16f, -(top + 80f));
            r.offsetMax = new Vector2(-16f, -top);

            go.GetComponent<Image>().color = new Color(0.035f, 0.052f, 0.080f, 0.98f);
            Outline outline = go.GetComponent<Outline>();
            outline.effectColor = new Color(0.11f, 0.48f, 0.72f, 0.95f);
            outline.effectDistance = new Vector2(1f, -1f);

            Text txt = Label(go.transform, name + "\n" + origin, 16, TextAnchor.MiddleLeft);
            RectTransform tx = txt.rectTransform;
            tx.anchorMin = Vector2.zero;
            tx.anchorMax = Vector2.one;
            tx.offsetMin = new Vector2(16f, 6f);
            tx.offsetMax = new Vector2(-12f, -6f);

            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                HighflyCuratedDonorRuntimeV015.Instance?.Preview(skill);
            });
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
