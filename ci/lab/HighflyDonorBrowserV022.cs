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
        private GameObject _panel;
        private Text _toggleText;
        private readonly List<GameObject> _rows = new List<GameObject>();

        public static HighflyDonorBrowserV022 Install(Transform parent)
        {
            var existing = Object.FindFirstObjectByType<HighflyDonorBrowserV022>();
            if (existing != null) return existing;

            var go = new GameObject("HIGHFLY_DONOR_BROWSER_v023_SKILL_VAULT");
            go.transform.SetParent(parent, false);
            return go.AddComponent<HighflyDonorBrowserV022>();
        }

        private void Awake() => Build();

        private void Build()
        {
            var cg = new GameObject("SkillVaultCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cg.transform.SetParent(transform, false);

            var canvas = cg.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9950;

            var scaler = cg.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.35f;

            _panel = new GameObject("SKILL_VAULT_PANEL", typeof(RectTransform), typeof(Image), typeof(Outline));
            _panel.transform.SetParent(cg.transform, false);

            var pr = _panel.GetComponent<RectTransform>();
            pr.anchorMin = new Vector2(0f, 0f);
            pr.anchorMax = new Vector2(0.38f, 1f);
            pr.offsetMin = new Vector2(10f, 12f);
            pr.offsetMax = new Vector2(-8f, -12f);

            _panel.GetComponent<Image>().color = new Color(0.008f, 0.014f, 0.026f, 0.965f);
            _panel.GetComponent<Outline>().effectColor = new Color(0.40f, 0.25f, 0.95f, 0.95f);

            BuildPersistentToggle(cg.transform);

            var title = Label(_panel.transform,
                "HIGHFLY • DONOR LAB 2.3 • SKILL VAULT",
                24,
                TextAnchor.UpperLeft,
                Color.white);
            SetTop(title.rectTransform, 18f, 14f, 18f, 44f);

            _status = Label(_panel.transform,
                "33 ENTRADAS TESTEABLES • SCROLL • TOCÁ = PREVIEW",
                14,
                TextAnchor.MiddleLeft,
                new Color(0.35f, 0.84f, 1f, 1f));
            SetTop(_status.rectTransform, 18f, 57f, 18f, 32f);

            BuildCharacterSwitch(_panel.transform);
            BuildScroll(_panel.transform);

            _info = Label(
                _panel.transform,
                "VAULT 2.3: mecánicas únicas, sin inflar variantes.\n" +
                "A/B comparte PlayerController + AnimatorController.\n" +
                "Project-X = referencia mecánica, implementación HIGHFLY independiente.",
                13,
                TextAnchor.UpperLeft,
                new Color(0.80f, 0.87f, 0.94f, 1f));

            var ir = _info.rectTransform;
            ir.anchorMin = new Vector2(0f, 0f);
            ir.anchorMax = new Vector2(1f, 0f);
            ir.pivot = new Vector2(0.5f, 0f);
            ir.offsetMin = new Vector2(18f, 14f);
            ir.offsetMax = new Vector2(-18f, 132f);

            Populate();
        }

        private void BuildPersistentToggle(Transform canvasParent)
        {
            var b = Button(canvasParent, "SKILLS -");
            b.gameObject.name = "SkillVault_Minimize";
            var r = b.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.38f, 1f);
            r.pivot = new Vector2(1f, 1f);
            r.anchoredPosition = new Vector2(-18f, -18f);
            r.sizeDelta = new Vector2(132f, 46f);

            _toggleText = b.GetComponentInChildren<Text>();
            if (_toggleText != null)
            {
                _toggleText.fontSize = 14;
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

        private void BuildCharacterSwitch(Transform parent)
        {
            var bar = new GameObject("CharacterSwitch", typeof(RectTransform), typeof(Image), typeof(Outline));
            bar.transform.SetParent(parent, false);

            var br = bar.GetComponent<RectTransform>();
            br.anchorMin = new Vector2(0f, 1f);
            br.anchorMax = new Vector2(1f, 1f);
            br.pivot = new Vector2(0.5f, 1f);
            br.offsetMin = new Vector2(18f, -152f);
            br.offsetMax = new Vector2(-18f, -96f);

            bar.GetComponent<Image>().color = new Color(0.018f, 0.035f, 0.060f, 0.98f);
            bar.GetComponent<Outline>().effectColor = new Color(0.16f, 0.70f, 1f, 0.95f);

            var a = Button(bar.transform, "A • LUCID");
            var ar = a.GetComponent<RectTransform>();
            ar.anchorMin = new Vector2(0f, 0f);
            ar.anchorMax = new Vector2(0.49f, 1f);
            ar.offsetMin = new Vector2(4f, 4f);
            ar.offsetMax = new Vector2(-3f, -4f);
            a.onClick.AddListener(() => HighflyCharacterCompareV022.Instance?.UseLucid());

            var b = Button(bar.transform, "B • KAYKIT");
            var rr = b.GetComponent<RectTransform>();
            rr.anchorMin = new Vector2(0.51f, 0f);
            rr.anchorMax = new Vector2(1f, 1f);
            rr.offsetMin = new Vector2(3f, 4f);
            rr.offsetMax = new Vector2(-4f, -4f);
            b.onClick.AddListener(() => HighflyCharacterCompareV022.Instance?.UseKayKit());
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
            vr.offsetMin = new Vector2(16f, 142f);
            vr.offsetMax = new Vector2(-16f, -164f);

            viewport.GetComponent<Image>().color = new Color(0.004f, 0.010f, 0.020f, 0.82f);

            var content = new GameObject(
                "Content",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);

            _content = content.GetComponent<RectTransform>();
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            _content.anchoredPosition = Vector2.zero;

            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(7, 7, 8, 8);
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
            scroll.decelerationRate = 0.12f;
            scroll.scrollSensitivity = 62f;
            scroll.movementType = ScrollRect.MovementType.Elastic;
        }

        private void Populate()
        {
            Section("KEEP / CONTROL");
            Add("DRIFT DE FÓRMULA", "KEEP • HIGHFLY",
                () => HighflyAcceptedSkillRuntimeV016.Instance?.Preview(HighflyAcceptedSkillV016.Drift));
            Add("JUMP SMASH", "KEEP • HIGHFLY",
                () => HighflyAcceptedSkillRuntimeV016.Instance?.Preview(HighflyAcceptedSkillV016.JumpSmash));

            Section("SIGIL COMBAT • MIT");
            Add("MELEE TRACE", "VERIFIED • melee trace + poise",
                () => Expanded(HighflyDonorExpandedSkillV022.SigilMelee));
            Add("DASH ATTACK", "RAW VERIFIED • lunge + trace",
                () => HighflyDonorWeaponRuntimeV021.Instance?.Preview(HighflyDonorWeaponSkillV021.SigilDashAttack));
            Add("RANGED SHOT", "VERIFIED • projectile",
                () => Expanded(HighflyDonorExpandedSkillV022.SigilRanged));
            Add("FLASH", "VERIFIED • blink 5m + obstacle check",
                () => Expanded(HighflyDonorExpandedSkillV022.SigilFlash));
            Add("CHARGED FIREBALL", "VERIFIED • charge + damage + stun",
                () => Expanded(HighflyDonorExpandedSkillV022.SigilFireball));

            Section("DRAGON SOULS • MIT");
            Add("SWORD THROW / EMBED / RECALL",
                "VERIFIED • 3-state weapon mechanic",
                () => HighflyDonorWeaponRuntimeV021.Instance?.Preview(
                    HighflyDonorWeaponSkillV021.DragonSwordThrowRecall));

            Section("PROJECT-X • MECHANIC STUDY");
            Add("VORTEX EDGE", "BladeStorm ref • spin AOE / repeated ticks",
                () => Expanded(HighflyDonorExpandedSkillV022.PxVortexEdge));
            Add("VEIL STRIKE", "CloakStrike ref • vanish + reposition + strike",
                () => Expanded(HighflyDonorExpandedSkillV022.PxVeilStrike));
            Add("BURST ARROW", "ExplosiveShot ref • projectile + explosion AOE",
                () => Expanded(HighflyDonorExpandedSkillV022.PxBurstArrow));
            Add("HUNTER CLAW", "GrapplingClaw ref • grapple + impact + stun",
                () => Expanded(HighflyDonorExpandedSkillV022.PxHunterClaw));
            Add("TITAN SWING", "HomerunSwing ref • charge + knockback + stun",
                () => Expanded(HighflyDonorExpandedSkillV022.PxTitanSwing));
            Add("VITAL DRAIN", "LifeDrain ref • DOT pulses + self heal",
                () => Expanded(HighflyDonorExpandedSkillV022.PxVitalDrain));
            Add("OVERCHARGE DOMAIN", "OverchargeField ref • temporary movement buff",
                () => Expanded(HighflyDonorExpandedSkillV022.PxOverchargeDomain));
            Add("PLASMA GUARD", "PlasmaShield ref • real damage shield",
                () => Expanded(HighflyDonorExpandedSkillV022.PxPlasmaGuard));
            Add("BARRAGE", "RapidFire ref • 5-shot burst",
                () => Expanded(HighflyDonorExpandedSkillV022.PxBarrage));
            Add("HUNT DRONE", "SentryDrone ref • seek + AOE explosion",
                () => Expanded(HighflyDonorExpandedSkillV022.PxHuntDrone));
            Add("RAIL SHOT", "SniperShot ref • charge + high-speed hit",
                () => Expanded(HighflyDonorExpandedSkillV022.PxRailShot));
            Add("AEGIS FORM", "AegisProtocol ref • shield + hyper armor + speed tradeoff",
                () => Expanded(HighflyDonorExpandedSkillV022.PxAegisForm));

            Section("ASHWALKER • MIT");
            Add("FORCE PUSH", "physics/control donor • push target",
                () => Expanded(HighflyDonorExpandedSkillV022.AshForcePush));
            Add("FORCE PULL", "physics/control donor • pull target",
                () => Expanded(HighflyDonorExpandedSkillV022.AshForcePull));
            Add("HASTE DOMAIN", "time-field donor • movement x1.5",
                () => Expanded(HighflyDonorExpandedSkillV022.AshHasteDomain));
            Add("SLOW DOMAIN", "time-field donor • enemy animation x0.35",
                () => Expanded(HighflyDonorExpandedSkillV022.AshSlowDomain));

            Section("SUBSPACEHUNTER • MIT CODE / RAW ART EXCLUDED");
            Add("EMBER BOLT", "fire projectile mechanic",
                () => Expanded(HighflyDonorExpandedSkillV022.SubEmberBolt));
            Add("THUNDER MARK", "electric instant strike + short stun",
                () => Expanded(HighflyDonorExpandedSkillV022.SubThunderMark));
            Add("FROST LANCE", "ice projectile + stun",
                () => Expanded(HighflyDonorExpandedSkillV022.SubFrostLance));
            Add("METEOR BREAK", "falling meteor + AOE impact",
                () => Expanded(HighflyDonorExpandedSkillV022.SubMeteorBreak));
            Add("AEGIS", "shield mechanic • 85 HP / 5s",
                () => Expanded(HighflyDonorExpandedSkillV022.SubAegis));
            Add("RESTORE", "heal mechanic • +35 EGO",
                () => Expanded(HighflyDonorExpandedSkillV022.SubHeal));

            Section("ADAPTIVE BOSS ARENA • MIT SYSTEM STUDY");
            Add("FOCUS SPECIAL", "full-meter empowered special preview",
                () => Expanded(HighflyDonorExpandedSkillV022.AdaptiveFocusSpecial));
            Add("EXECUTION", "posture-break execution preview",
                () => Expanded(HighflyDonorExpandedSkillV022.AdaptiveExecution));
            Add("HYPER ARMOR HEAVY", "real hyper-armor window + heavy strike",
                () => Expanded(HighflyDonorExpandedSkillV022.AdaptiveHyperArmorHeavy));
        }

        private static void Expanded(HighflyDonorExpandedSkillV022 skill)
        {
            HighflyDonorExpandedRuntimeV022.Instance?.Preview(skill);
        }

        private void Section(string text)
        {
            var t = Label(_content, text, 14, TextAnchor.MiddleLeft, new Color(0.47f, 0.82f, 1f, 1f));
            var e = t.gameObject.AddComponent<LayoutElement>();
            e.preferredHeight = 36f;
            _rows.Add(t.gameObject);
        }

        private void Add(string name, string note, UnityEngine.Events.UnityAction action)
        {
            var b = Button(_content, "●  " + name + "\n    " + note);
            var e = b.gameObject.AddComponent<LayoutElement>();
            e.preferredHeight = 72f;

            b.onClick.AddListener(() =>
            {
                if (_info != null)
                    _info.text =
                        name + "\n" + note +
                        "\n\nA/B: cambiá LUCID / KAYKIT y repetí la misma prueba.";
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
                14,
                TextAnchor.MiddleLeft,
                new Color(0.93f, 0.97f, 1f, 1f));

            var tr = t.rectTransform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(13f, 5f);
            tr.offsetMax = new Vector2(-10f, -5f);

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
