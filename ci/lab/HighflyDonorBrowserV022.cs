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
                "HIGHFLY • LAB 2.4 • FINAL VISUAL PASS",
                22,
                TextAnchor.UpperLeft,
                Color.white);
            SetTop(title.rectTransform, 16f, 12f, 14f, 40f);

            var status = Label(
                _panel.transform,
                "FINAL / NEAR-FINAL • NO PROXY • SCROLL • TOCÁ = PREVIEW",
                13,
                TextAnchor.MiddleLeft,
                new Color(0.35f, 0.84f, 1f, 1f));
            SetTop(status.rectTransform, 16f, 52f, 14f, 30f);

            BuildScroll(_panel.transform);

            _info = Label(
                _panel.transform,
                "LUCID = personaje principal. KAYKIT = compatibilidad A/B.\n" +
                "Catálogo principal: sólo skills con animación + VFX asset-backed + hit real.\n" +
                "WIP ocultas: Hunter Claw, Hunt Drone y Aegis Form.",
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
            Section("HIGHFLY CORE • FINAL / NEAR-FINAL");
            Add("DANZA GEMELA", "S1 • combo 3 etapas • cortes asset-backed",
                () => Premium(HighflyPremiumSkillId.TwinDance));
            Add("PASO FANTASMA", "blink / reposición • core HIGHFLY",
                () => Premium(HighflyPremiumSkillId.PhantomStep));
            Add("DANZA FANTASMA", "micro-blinks + multicorte",
                () => Premium(HighflyPremiumSkillId.PhantomTwinDance));
            Add("DESGARRO ECLIPSE", "siete cortes + confirmación final",
                () => Premium(HighflyPremiumSkillId.EclipseRend));
            Add("GRILLETE ABISAL", "control / convergencia",
                () => Premium(HighflyPremiumSkillId.AbyssalShackle));

            Section("CONTROL");
            Add("DRIFT DE FÓRMULA", "KEEP • HIGHFLY",
                () => HighflyAcceptedSkillRuntimeV016.Instance?.Preview(HighflyAcceptedSkillV016.Drift));
            Add("JUMP SMASH", "KEEP • HIGHFLY",
                () => HighflyAcceptedSkillRuntimeV016.Instance?.Preview(HighflyAcceptedSkillV016.JumpSmash));

            Section("SIGIL COMBAT • MIT");
            Add("MELEE TRACE", "melee trace + poise",
                () => Expanded(HighflyDonorExpandedSkillV022.SigilMelee));
            Add("DASH ATTACK", "lunge + trace",
                () => HighflyDonorWeaponRuntimeV021.Instance?.Preview(HighflyDonorWeaponSkillV021.SigilDashAttack));
            Add("RANGED SHOT", "projectile VFX asset-backed",
                () => Expanded(HighflyDonorExpandedSkillV022.SigilRanged));
            Add("FLASH", "blink + salida/entrada con partículas",
                () => Expanded(HighflyDonorExpandedSkillV022.SigilFlash));
            Add("CHARGED FIREBALL", "charge + projectile + stun",
                () => Expanded(HighflyDonorExpandedSkillV022.SigilFireball));

            Section("DRAGON SOULS • MIT");
            Add("SWORD THROW / EMBED / RECALL", "espada CC0 real + trail",
                () => HighflyDonorWeaponRuntimeV021.Instance?.Preview(
                    HighflyDonorWeaponSkillV021.DragonSwordThrowRecall));

            Section("PROJECT-X • MECHANIC STUDY");
            Add("VORTEX EDGE", "spin AOE + slash/field VFX",
                () => Expanded(HighflyDonorExpandedSkillV022.PxVortexEdge));
            Add("VEIL STRIKE", "cloak + reposition + strike",
                () => Expanded(HighflyDonorExpandedSkillV022.PxVeilStrike));
            Add("BURST ARROW", "flecha CC0 + explosión AOE",
                () => Expanded(HighflyDonorExpandedSkillV022.PxBurstArrow));
            Add("TITAN SWING", "heavy + knockback + stun",
                () => Expanded(HighflyDonorExpandedSkillV022.PxTitanSwing));
            Add("VITAL DRAIN", "DOT pulses + self heal",
                () => Expanded(HighflyDonorExpandedSkillV022.PxVitalDrain));
            Add("OVERCHARGE DOMAIN", "domain + movement buff",
                () => Expanded(HighflyDonorExpandedSkillV022.PxOverchargeDomain));
            Add("PLASMA GUARD", "shield real + aura particle",
                () => Expanded(HighflyDonorExpandedSkillV022.PxPlasmaGuard));
            Add("BARRAGE", "5-shot burst VFX",
                () => Expanded(HighflyDonorExpandedSkillV022.PxBarrage));
            Add("RAIL SHOT", "charge + high-speed hit",
                () => Expanded(HighflyDonorExpandedSkillV022.PxRailShot));

            Section("ASHWALKER • MIT");
            Add("FORCE PUSH", "push físico + impact VFX",
                () => Expanded(HighflyDonorExpandedSkillV022.AshForcePush));
            Add("FORCE PULL", "pull físico + impact VFX",
                () => Expanded(HighflyDonorExpandedSkillV022.AshForcePull));
            Add("HASTE DOMAIN", "campo temporal + movement x1.5",
                () => Expanded(HighflyDonorExpandedSkillV022.AshHasteDomain));
            Add("SLOW DOMAIN", "campo temporal + enemy x0.35",
                () => Expanded(HighflyDonorExpandedSkillV022.AshSlowDomain));

            Section("SUBSPACEHUNTER • MIT CODE / ART REEMPLAZADO");
            Add("EMBER BOLT", "fire projectile + partículas",
                () => Expanded(HighflyDonorExpandedSkillV022.SubEmberBolt));
            Add("THUNDER MARK", "electric strike + stun",
                () => Expanded(HighflyDonorExpandedSkillV022.SubThunderMark));
            Add("FROST LANCE", "ice projectile + stun",
                () => Expanded(HighflyDonorExpandedSkillV022.SubFrostLance));
            Add("METEOR BREAK", "meteor + AOE impact",
                () => Expanded(HighflyDonorExpandedSkillV022.SubMeteorBreak));
            Add("AEGIS", "shield + aura particle",
                () => Expanded(HighflyDonorExpandedSkillV022.SubAegis));
            Add("RESTORE", "heal + particle burst",
                () => Expanded(HighflyDonorExpandedSkillV022.SubHeal));

            Section("ADAPTIVE BOSS ARENA • MIT SYSTEM STUDY");
            Add("FOCUS SPECIAL", "empowered special",
                () => Expanded(HighflyDonorExpandedSkillV022.AdaptiveFocusSpecial));
            Add("EXECUTION", "posture-break execution",
                () => Expanded(HighflyDonorExpandedSkillV022.AdaptiveExecution));
            Add("HYPER ARMOR HEAVY", "hyper-armor + heavy strike",
                () => Expanded(HighflyDonorExpandedSkillV022.AdaptiveHyperArmorHeavy));
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
