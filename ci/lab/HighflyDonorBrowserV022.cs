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
        private GameObject _panel;
        private Text _toggleText;
        private readonly List<GameObject> _rows = new List<GameObject>();

        public static HighflyDonorBrowserV022 Install(Transform parent)
        {
            var existing = Object.FindFirstObjectByType<HighflyDonorBrowserV022>();
            if (existing != null) return existing;

            var go = new GameObject("HIGHFLY_DONOR_BROWSER_v024_FINAL_VISUAL_PASS");
            go.transform.SetParent(parent, false);
            return go.AddComponent<HighflyDonorBrowserV022>();
        }

        private void Awake() => Build();

        private void Build()
        {
            var cg = new GameObject("FinalSkillVaultCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cg.transform.SetParent(transform, false);

            var canvas = cg.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9950;

            var scaler = cg.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.35f;

            _panel = new GameObject("FINAL_SKILL_VAULT_PANEL", typeof(RectTransform), typeof(Image), typeof(Outline));
            _panel.transform.SetParent(cg.transform, false);

            var pr = _panel.GetComponent<RectTransform>();
            // Leave the lower-left 28% free so the joystick is never covered.
            pr.anchorMin = new Vector2(0f, 0.28f);
            pr.anchorMax = new Vector2(0.34f, 1f);
            pr.offsetMin = new Vector2(10f, 8f);
            pr.offsetMax = new Vector2(-8f, -12f);

            _panel.GetComponent<Image>().color = new Color(0.008f, 0.014f, 0.026f, 0.965f);
            _panel.GetComponent<Outline>().effectColor = new Color(0.40f, 0.25f, 0.95f, 0.95f);

            BuildPersistentToggle(cg.transform);

            var title = Label(
                _panel.transform,
                "HIGHFLY • LAB 2.5 • COMBAT LINK",
                22,
                TextAnchor.UpperLeft,
                Color.white);
            SetTop(title.rectTransform, 16f, 12f, 14f, 40f);

            var status = Label(
                _panel.transform,
                "CONECTADAS • SIN COSTO/CD • SCROLL • TOCÁ = PREVIEW",
                13,
                TextAnchor.MiddleLeft,
                new Color(0.35f, 0.84f, 1f, 1f));
            SetTop(status.rectTransform, 16f, 52f, 14f, 30f);

            BuildScroll(_panel.transform);

            _info = Label(
                _panel.transform,
                "LUCID y KAYKIT = candidatos A/B reales.\n" +
                "FULL DONOR audiovisual = sólo cuando importemos la cadena original verificable.\n" +
                "PORT HF = mecánica donor conectada a cuerpo/VFX HIGHFLY; nunca se etiqueta como FULL.",
                12,
                TextAnchor.UpperLeft,
                new Color(0.80f, 0.87f, 0.94f, 1f));

            var ir = _info.rectTransform;
            ir.anchorMin = new Vector2(0f, 0f);
            ir.anchorMax = new Vector2(1f, 0f);
            ir.pivot = new Vector2(0.5f, 0f);
            ir.offsetMin = new Vector2(16f, 10f);
            ir.offsetMax = new Vector2(-14f, 105f);

            Populate();
        }

        private void BuildPersistentToggle(Transform canvasParent)
        {
            var b = Button(canvasParent, "SKILLS -");
            b.gameObject.name = "SkillVault_Minimize";

            var r = b.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.34f, 1f);
            r.pivot = new Vector2(1f, 1f);
            r.anchoredPosition = new Vector2(-14f, -16f);
            r.sizeDelta = new Vector2(128f, 44f);

            _toggleText = b.GetComponentInChildren<Text>();
            if (_toggleText != null)
            {
                _toggleText.fontSize = 13;
                _toggleText.alignment = TextAnchor.MiddleCenter;
            }

            b.onClick.AddListener(TogglePanel);

            var reset = Button(canvasParent, "RESET MOV");
            reset.gameObject.name = "SkillVault_ResetMovement";
            var rr = reset.GetComponent<RectTransform>();
            rr.anchorMin = rr.anchorMax = new Vector2(0.34f, 1f);
            rr.pivot = new Vector2(1f, 1f);
            rr.anchoredPosition = new Vector2(-150f, -16f);
            rr.sizeDelta = new Vector2(128f, 44f);
            Text rt = reset.GetComponentInChildren<Text>();
            if (rt != null)
            {
                rt.fontSize = 13;
                rt.alignment = TextAnchor.MiddleCenter;
            }
            reset.onClick.AddListener(() =>
            {
                HighflyParkourAnimationV010.Instance?.StopNow();
                PlayerController p = Object.FindFirstObjectByType<PlayerController>();
                HighflyLabMotionGuardV026.Instance?.ForceRelease("RESET MOV button");
                p?.HighflyLabForceLocomotion();
                HighflySkillLabMetrics.RecordAction("LAB • RESET MOVIMIENTO", 0);
                HighflyLabTestHistoryV026.Log("MANUAL RESET MOV");
            });
        }

        private void TogglePanel()
        {
            if (_panel == null) return;
            bool next = !_panel.activeSelf;
            _panel.SetActive(next);
            if (_toggleText != null)
                _toggleText.text = next ? "SKILLS -" : "SKILLS +";
        }

        private void BuildScroll(Transform parent)
        {
            var viewport = new GameObject(
                "SkillScrollViewport",
                typeof(RectTransform),
                typeof(Image),
                typeof(RectMask2D),
                typeof(ScrollRect));
            viewport.transform.SetParent(parent, false);

            var vr = viewport.GetComponent<RectTransform>();
            vr.anchorMin = new Vector2(0f, 0f);
            vr.anchorMax = new Vector2(1f, 1f);
            vr.offsetMin = new Vector2(14f, 112f);
            vr.offsetMax = new Vector2(-14f, -92f);

            viewport.GetComponent<Image>().color = new Color(0.004f, 0.010f, 0.020f, 0.84f);

            var content = new GameObject(
                "Content",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);

            _content = content.GetComponent<RectTransform>();
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0f, 1f);
            _content.anchoredPosition = Vector2.zero;
            _content.sizeDelta = Vector2.zero;

            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 6, 6);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperLeft;
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
            scroll.decelerationRate = 0.12f;
            scroll.scrollSensitivity = 68f;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        }

        private void Populate()
        {
            Section("HIGHFLY PREMIUM • CONECTADO");
            Add("DANZA GEMELA", "3 etapas • animación cuerpo + cortes + hit + recovery",
                () => Premium(HighflyPremiumSkillId.TwinDance));
            Add("PASO FANTASMA", "dash/roll • i-frame visual + slash + recovery forzado",
                () => Premium(HighflyPremiumSkillId.PhantomStep));
            Add("DANZA FANTASMA", "micro-blinks + multicorte",
                () => Premium(HighflyPremiumSkillId.PhantomTwinDance));
            Add("DESGARRO ECLIPSE", "siete cortes + confirmación final",
                () => Premium(HighflyPremiumSkillId.EclipseRend));
            Section("DONOR SOURCE CLEAN • PORT HIGHFLY");
            Add("FOCUS SPECIAL", "ADAPTIVE • fuente MIT + UAL/UAL2 CC0 • port conectado",
                () => Expanded(HighflyDonorExpandedSkillV022.AdaptiveFocusSpecial));
            Add("EXECUTION", "ADAPTIVE • postura rota → ejecución • cuerpo + hit",
                () => Expanded(HighflyDonorExpandedSkillV022.AdaptiveExecution));
            Add("HYPER ARMOR HEAVY", "ADAPTIVE • hyper armor + heavy strike",
                () => Expanded(HighflyDonorExpandedSkillV022.AdaptiveHyperArmorHeavy));

            Section("MECH DONOR + HIGHFLY PRESENTATION");
            Add("MELEE TRACE", "SIGIL MIT • melee trace/poise • presentación HIGHFLY",
                () => Expanded(HighflyDonorExpandedSkillV022.SigilMelee));
            Add("DASH ATTACK", "SIGIL MIT • dash + trace • UAL2 body",
                () => HighflyDonorWeaponRuntimeV021.Instance?.Preview(
                    HighflyDonorWeaponSkillV021.SigilDashAttack));
            Add("RANGED SHOT", "SIGIL MIT • projectile mechanic • body linked",
                () => Expanded(HighflyDonorExpandedSkillV022.SigilRanged));
            Add("FLASH", "SIGIL MIT • blink 5m • body linked + entry/exit VFX",
                () => Expanded(HighflyDonorExpandedSkillV022.SigilFlash));

            Add("SWORD THROW / EMBED / RECALL",
                "DRAGON SOULS • mecánica donor • visual legal reemplazado/aislado",
                () => HighflyDonorWeaponRuntimeV021.Instance?.Preview(
                    HighflyDonorWeaponSkillV021.DragonSwordThrowRecall));

            Add("VEIL STRIKE", "PROJECT-X mechanic • cloak/reposition/strike • HF presentation",
                () => Expanded(HighflyDonorExpandedSkillV022.PxVeilStrike));
            Add("BURST ARROW", "PROJECT-X mechanic • projectile/explosion • HF presentation",
                () => Expanded(HighflyDonorExpandedSkillV022.PxBurstArrow));
            Add("TITAN SWING", "PROJECT-X mechanic • heavy/knockback/stun • HF presentation",
                () => Expanded(HighflyDonorExpandedSkillV022.PxTitanSwing));

            Section("CONTROL / REGRESIÓN");
            Add("DRIFT DE FÓRMULA", "control HIGHFLY",
                () => HighflyAcceptedSkillRuntimeV016.Instance?.Preview(
                    HighflyAcceptedSkillV016.Drift));
            Add("JUMP SMASH", "control HIGHFLY",
                () => HighflyAcceptedSkillRuntimeV016.Instance?.Preview(
                    HighflyAcceptedSkillV016.JumpSmash));
        }

        private static void Premium(HighflyPremiumSkillId skill)
        {
            HighflyPremiumSkillRuntime.Instance?.ForcePreview(skill);
        }

        private static void Expanded(HighflyDonorExpandedSkillV022 skill)
        {
            HighflyDonorExpandedRuntimeV022.Instance?.Preview(skill);
        }

        private void Section(string text)
        {
            var t = Label(_content, text, 13, TextAnchor.MiddleLeft, new Color(0.47f, 0.82f, 1f, 1f));
            t.gameObject.AddComponent<LayoutElement>().preferredHeight = 32f;
            _rows.Add(t.gameObject);
        }

        private void Add(string name, string note, UnityEngine.Events.UnityAction action)
        {
            var b = Button(_content, "●  " + name + "\n    " + note);
            b.gameObject.AddComponent<LayoutElement>().preferredHeight = 62f;

            b.onClick.AddListener(() =>
            {
                if (_info != null)
                    _info.text =
                        name + "\n" + note +
                        "\n\nLUCID = principal. Repetí en KAYKIT sólo para compatibilidad.";
                action();
            });

            _rows.Add(b.gameObject);
        }

        private static Button Button(Transform parent, string value)
        {
            var go = new GameObject(
                "Button",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(Outline));
            go.transform.SetParent(parent, false);

            go.GetComponent<Image>().color = new Color(0.030f, 0.047f, 0.075f, 0.99f);

            var ol = go.GetComponent<Outline>();
            ol.effectColor = new Color(0.33f, 0.23f, 0.82f, 0.95f);
            ol.effectDistance = new Vector2(1f, -1f);

            var t = Label(
                go.transform,
                value,
                13,
                TextAnchor.MiddleLeft,
                new Color(0.93f, 0.97f, 1f, 1f));

            var tr = t.rectTransform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(11f, 4f);
            tr.offsetMax = new Vector2(-9f, -4f);

            return go.GetComponent<Button>();
        }

        private static Text Label(
            Transform parent,
            string value,
            int size,
            TextAnchor align,
            Color color)
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

        private static void SetTop(
            RectTransform r,
            float left,
            float top,
            float right,
            float height)
        {
            r.anchorMin = new Vector2(0f, 1f);
            r.anchorMax = new Vector2(1f, 1f);
            r.pivot = new Vector2(0.5f, 1f);
            r.offsetMin = new Vector2(left, -top - height);
            r.offsetMax = new Vector2(-right, -top);
        }
    }
}
