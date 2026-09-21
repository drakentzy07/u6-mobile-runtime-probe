using System;
using UnityEngine;
using UnityEngine.UI;

namespace Highfly.SkillLab
{
    public sealed class HighflySkillDefinitionLite
    {
        public HighflyPremiumSkillId Id;
        public string Name;
        public string Family;
        public string Role;
        public string Evolution;
        public bool Implemented;

        public HighflySkillDefinitionLite(
            HighflyPremiumSkillId id,
            string name,
            string family,
            string role,
            string evolution,
            bool implemented)
        {
            Id = id;
            Name = name;
            Family = family;
            Role = role;
            Evolution = evolution;
            Implemented = implemented;
        }
    }

    public static class HighflySkillCatalog
    {
        public static readonly HighflySkillDefinitionLite[] All =
        {
            new HighflySkillDefinitionLite(
                HighflyPremiumSkillId.TwinDance,
                "DANZA GEMELA",
                "ARMA / COMBO / MULTI-HIT",
                "Presión melee y finisher encadenado.",
                "Base de DANZA FANTASMA.",
                true),

            new HighflySkillDefinitionLite(
                HighflyPremiumSkillId.PhantomStep,
                "PASO FANTASMA",
                "MOVILIDAD / RUSH / I-FRAME",
                "Reposicionamiento ofensivo y evasión perfecta.",
                "Base de DANZA FANTASMA.",
                true),

            new HighflySkillDefinitionLite(
                HighflyPremiumSkillId.ShadowShackle,
                "GRILLETE UMBRÍO",
                "SOMBRA / CONTROL / MARK",
                "Inmoviliza, marca y atrae al objetivo.",
                "Puede mutar hacia control múltiple.",
                true),

            new HighflySkillDefinitionLite(
                HighflyPremiumSkillId.VitalPact,
                "PACTO VITAL",
                "VITAL / BUFF / SUSTAIN",
                "Convierte agresión en supervivencia.",
                "Base de DOMINIO VITAL.",
                true),

            new HighflySkillDefinitionLite(
                HighflyPremiumSkillId.ShadowCall,
                "LLAMADO DE LA SOMBRA",
                "SOMBRA / SUMMON / ECHO",
                "Sombras ofensivas sincronizadas con el cazador.",
                "Base de ECO UMBRÍO.",
                true),

            new HighflySkillDefinitionLite(
                HighflyPremiumSkillId.PhantomTwinDance,
                "DANZA FANTASMA",
                "FUSIÓN / ARMA + MOVILIDAD",
                "Tres cortes espaciales con micro-blink, afterimages y remate.",
                "Danza Gemela + Paso Fantasma • condición de fusión.",
                true),

            new HighflySkillDefinitionLite(
                HighflyPremiumSkillId.ShadowLink,
                "VÍNCULO UMBRÍO",
                "FUSIÓN / SOMBRA + VITAL",
                "Activa formación sombra, drenaje de vida y ataque coordinado.",
                "Llamado de la Sombra + Pacto Vital.",
                true),

            new HighflySkillDefinitionLite(
                HighflyPremiumSkillId.AbyssalShackle,
                "GRILLETE ABISAL",
                "EVOLUCIÓN / CONTROL + SOMBRA",
                "Manos y grilletes atrapan hasta tres objetivos y los convergen.",
                "Evolución de Grillete Umbrío por maestría/condición.",
                true),

            new HighflySkillDefinitionLite(
                HighflyPremiumSkillId.VitalDomain,
                "DOMINIO VITAL",
                "EVOLUCIÓN / DOMINIO + SUSTAIN",
                "Ritual persistente: drena enemigos y restaura al cazador.",
                "Evolución de Pacto Vital.",
                true),

            new HighflySkillDefinitionLite(
                HighflyPremiumSkillId.ShadowJudgment,
                "JUICIO DE LA SOMBRA",
                "FINISHER / EJECUCIÓN / SOMBRA",
                "Mano umbría + formación en V + ejecución triple sincronizada.",
                "Finisher avanzado de la rama Umbría.",
                true)
        };

        public static HighflySkillDefinitionLite Get(HighflyPremiumSkillId id)
        {
            for (int i = 0; i < All.Length; i++)
                if (All[i].Id == id)
                    return All[i];

            return null;
        }
    }

    public static class HighflySkillLoadout
    {
        private static readonly HighflyPremiumSkillId[] Slots =
        {
            HighflyPremiumSkillId.TwinDance,
            HighflyPremiumSkillId.PhantomStep,
            HighflyPremiumSkillId.ShadowShackle,
            HighflyPremiumSkillId.VitalPact,
            HighflyPremiumSkillId.ShadowCall
        };

        public static event Action Changed;

        public static HighflyPremiumSkillId Get(int slot)
        {
            slot = Mathf.Clamp(slot, 0, Slots.Length - 1);
            return Slots[slot];
        }

        public static void Assign(int slot, HighflyPremiumSkillId skill)
        {
            slot = Mathf.Clamp(slot, 0, Slots.Length - 1);

            HighflySkillDefinitionLite definition = HighflySkillCatalog.Get(skill);
            if (definition == null || !definition.Implemented)
                return;

            if (Slots[slot] == skill)
                return;

            int existing = -1;
            for (int i = 0; i < Slots.Length; i++)
            {
                if (Slots[i] == skill)
                {
                    existing = i;
                    break;
                }
            }

            if (existing >= 0)
            {
                HighflyPremiumSkillId previous = Slots[slot];
                Slots[slot] = skill;
                Slots[existing] = previous;
            }
            else
            {
                Slots[slot] = skill;
            }

            Changed?.Invoke();
        }
    }

    [DisallowMultipleComponent]
    public sealed class HighflySkillLoadoutMenu : MonoBehaviour
    {
        private Canvas _canvas;
        private GameObject _panel;
        private Button _openButton;
        private Text _selectedInfo;
        private Text[] _slotLabels = new Text[5];
        private Button[] _slotButtons = new Button[5];

        private int _selectedSlot;

        public static HighflySkillLoadoutMenu Install(Transform parent)
        {
            HighflySkillLoadoutMenu existing =
                UnityEngine.Object.FindFirstObjectByType<HighflySkillLoadoutMenu>();

            if (existing != null)
                return existing;

            var go = new GameObject("HIGHFLY_SKILL_LOADOUT_MENU");
            go.transform.SetParent(parent, false);

            var menu = go.AddComponent<HighflySkillLoadoutMenu>();
            menu.Build();
            return menu;
        }

        private void OnDestroy()
        {
            HighflySkillLoadout.Changed -= Refresh;
        }

        private void Build()
        {
            HighflySkillLoadout.Changed += Refresh;

            var canvasGo = new GameObject(
                "SkillLoadoutCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            canvasGo.transform.SetParent(transform, false);

            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 8500;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _openButton = CreateButton(
                canvasGo.transform,
                "SKILLS",
                new Vector2(1f, 1f),
                new Vector2(-92f, -58f),
                new Vector2(154f, 64f),
                new Color(0.07f, 0.09f, 0.13f, 0.92f),
                new Color(0.13f, 0.80f, 1f, 1f));

            _openButton.onClick.AddListener(TogglePanel);

            _panel = new GameObject(
                "SkillLoadoutPanel",
                typeof(RectTransform),
                typeof(Image));

            _panel.transform.SetParent(canvasGo.transform, false);

            RectTransform panelRect = _panel.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = new Vector2(145f, 0f);
            panelRect.sizeDelta = new Vector2(1180f, 820f);

            _panel.GetComponent<Image>().color =
                new Color(0.025f, 0.035f, 0.055f, 0.97f);

            CreateLabel(
                _panel.transform,
                "HIGHFLY SYSTEM • SKILL CODEX / LOADOUT",
                new Vector2(-545f, 355f),
                new Vector2(790f, 60f),
                30,
                TextAnchor.MiddleLeft,
                new Color(0.88f, 0.96f, 1f, 1f));

            CreateLabel(
                _panel.transform,
                "10 skills activas • seleccioná S1–S5 y equipá. Base, evolución y fusión comparten el mismo CORE.",
                new Vector2(-545f, 305f),
                new Vector2(990f, 45f),
                18,
                TextAnchor.MiddleLeft,
                new Color(0.68f, 0.74f, 0.82f, 1f));

            Button close = CreateButton(
                _panel.transform,
                "CERRAR",
                new Vector2(0.5f, 0.5f),
                new Vector2(490f, 350f),
                new Vector2(150f, 54f),
                new Color(0.12f, 0.13f, 0.16f, 1f),
                new Color(0.70f, 0.74f, 0.80f, 1f));

            close.onClick.AddListener(() => _panel.SetActive(false));

            for (int i = 0; i < 5; i++)
            {
                int slot = i;
                float x = -460f + i * 205f;

                Button button = CreateButton(
                    _panel.transform,
                    "S" + (i + 1),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(x, 230f),
                    new Vector2(185f, 105f),
                    new Color(0.055f, 0.075f, 0.11f, 1f),
                    new Color(0.06f, 0.77f, 1f, 1f));

                _slotButtons[i] = button;
                _slotLabels[i] = button.GetComponentInChildren<Text>();

                button.onClick.AddListener(() =>
                {
                    _selectedSlot = slot;
                    Refresh();
                });
            }

            CreateLabel(
                _panel.transform,
                "CATÁLOGO DE HABILIDADES",
                new Vector2(-545f, 135f),
                new Vector2(470f, 45f),
                22,
                TextAnchor.MiddleLeft,
                new Color(0.86f, 0.90f, 0.96f, 1f));

            for (int i = 0; i < HighflySkillCatalog.All.Length; i++)
            {
                HighflySkillDefinitionLite def = HighflySkillCatalog.All[i];

                int col = i < 5 ? 0 : 1;
                int row = i % 5;

                float x = col == 0 ? -300f : 300f;
                float y = 70f - row * 105f;

                string title =
                    (def.Implemented ? "" : "🔒 ") +
                    def.Name +
                    "\n" +
                    def.Family;

                Button card = CreateButton(
                    _panel.transform,
                    title,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(x, y),
                    new Vector2(540f, 88f),
                    def.Implemented
                        ? new Color(0.055f, 0.075f, 0.105f, 1f)
                        : new Color(0.065f, 0.065f, 0.075f, 0.88f),
                    def.Implemented
                        ? new Color(0.13f, 0.70f, 0.92f, 1f)
                        : new Color(0.32f, 0.32f, 0.35f, 1f));

                Text cardText = card.GetComponentInChildren<Text>();
                cardText.fontSize = 17;
                cardText.alignment = TextAnchor.MiddleLeft;

                if (def.Implemented)
                {
                    HighflyPremiumSkillId id = def.Id;
                    card.onClick.AddListener(() =>
                    {
                        HighflySkillLoadout.Assign(_selectedSlot, id);
                        UpdateSelectedInfo();
                    });
                }
            }

            _selectedInfo = CreateLabel(
                _panel.transform,
                "",
                new Vector2(-545f, -365f),
                new Vector2(1080f, 62f),
                17,
                TextAnchor.MiddleLeft,
                new Color(0.72f, 0.80f, 0.88f, 1f));

            _selectedSlot = 0;
            Refresh();
            _panel.SetActive(false);
        }

        private void TogglePanel()
        {
            if (_panel == null) return;
            _panel.SetActive(!_panel.activeSelf);

            if (_panel.activeSelf)
                Refresh();
        }

        private void Refresh()
        {
            for (int i = 0; i < 5; i++)
            {
                HighflySkillDefinitionLite def =
                    HighflySkillCatalog.Get(HighflySkillLoadout.Get(i));

                if (_slotLabels[i] != null)
                    _slotLabels[i].text =
                        "S" + (i + 1) + "\n" +
                        (def != null ? def.Name : "-");

                Image image = _slotButtons[i] != null
                    ? _slotButtons[i].GetComponent<Image>()
                    : null;

                if (image != null)
                {
                    image.color = i == _selectedSlot
                        ? new Color(0.08f, 0.38f, 0.50f, 1f)
                        : new Color(0.055f, 0.075f, 0.11f, 1f);
                }
            }

            UpdateSelectedInfo();
        }

        private void UpdateSelectedInfo()
        {
            if (_selectedInfo == null) return;

            HighflySkillDefinitionLite def =
                HighflySkillCatalog.Get(HighflySkillLoadout.Get(_selectedSlot));

            if (def == null)
            {
                _selectedInfo.text = "";
                return;
            }

            _selectedInfo.text =
                "S" + (_selectedSlot + 1) + " • " +
                def.Name +
                "  |  " +
                def.Role +
                "  |  " +
                def.Evolution;
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
            outline.effectDistance = new Vector2(2f, -2f);

            Button button = go.GetComponent<Button>();

            Text text = CreateLabel(
                go.transform,
                label,
                Vector2.zero,
                size - new Vector2(14f, 8f),
                20,
                TextAnchor.MiddleCenter,
                Color.white);

            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;

            return button;
        }

        private static Text CreateLabel(
            Transform parent,
            string label,
            Vector2 position,
            Vector2 size,
            int fontSize,
            TextAnchor alignment,
            Color color)
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
