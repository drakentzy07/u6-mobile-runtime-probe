using System;
using System.Collections;
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
        public float CooldownSeconds;

        public HighflySkillDefinitionLite(
            HighflyPremiumSkillId id,
            string name,
            string family,
            string role,
            string evolution,
            bool implemented,
            float cooldownSeconds)
        {
            Id = id;
            Name = name;
            Family = family;
            Role = role;
            Evolution = evolution;
            Implemented = implemented;
            CooldownSeconds = cooldownSeconds;
        }
    }

    public static class HighflySkillCatalog
    {
        public static readonly HighflySkillDefinitionLite[] All =
        {
            new HighflySkillDefinitionLite(
                HighflyPremiumSkillId.TwinDance,
                "DANZA GEMELA",
                "ARMA / COMBO",
                "Doble corte por activación; CD real sólo después del finisher.",
                "Base de DANZA FANTASMA.",
                true,
                1.35f),

            new HighflySkillDefinitionLite(
                HighflyPremiumSkillId.PhantomStep,
                "PASO FANTASMA",
                "MOVILIDAD / I-FRAME",
                "Rush eléctrico, afterimages y reposicionamiento.",
                "Base de DANZA FANTASMA.",
                true,
                1.15f),

            new HighflySkillDefinitionLite(
                HighflyPremiumSkillId.ShadowShackle,
                "GRILLETE UMBRÍO",
                "SOMBRA / CONTROL",
                "Mano 3D articulada, cadena segmentada, root y pull.",
                "Evoluciona a GRILLETE ABISAL.",
                true,
                4.5f),

            new HighflySkillDefinitionLite(
                HighflyPremiumSkillId.VitalPact,
                "PACTO VITAL",
                "VITAL / BUFF",
                "Ritual de sustain y conversión de daño en vida.",
                "Evoluciona a DOMINIO VITAL.",
                true,
                10f),

            new HighflySkillDefinitionLite(
                HighflyPremiumSkillId.ShadowCall,
                "LLAMADO DE LA SOMBRA",
                "SOMBRA / SUMMON",
                "Dos sombras en V copian y encadenan ataques.",
                "Base de VÍNCULO UMBRÍO.",
                true,
                12f),

            new HighflySkillDefinitionLite(
                HighflyPremiumSkillId.PhantomTwinDance,
                "DANZA FANTASMA",
                "FUSIÓN / ARMA + MOVILIDAD",
                "Tres micro-blinks con cortes espaciales y finisher.",
                "Danza Gemela + Paso Fantasma.",
                true,
                5f),

            new HighflySkillDefinitionLite(
                HighflyPremiumSkillId.ShadowLink,
                "VÍNCULO UMBRÍO",
                "FUSIÓN / SOMBRA + VITAL",
                "Formación sombra, drenaje y ataque coordinado.",
                "Llamado de la Sombra + Pacto Vital.",
                true,
                11f),

            new HighflySkillDefinitionLite(
                HighflyPremiumSkillId.AbyssalShackle,
                "GRILLETE ABISAL",
                "EVOLUCIÓN / CONTROL",
                "Hasta tres objetivos: manos, grilletes y convergencia.",
                "Evolución de Grillete Umbrío.",
                true,
                7f),

            new HighflySkillDefinitionLite(
                HighflyPremiumSkillId.VitalDomain,
                "DOMINIO VITAL",
                "EVOLUCIÓN / DOMINIO",
                "Campo ritual persistente de drain y sustain.",
                "Evolución de Pacto Vital.",
                true,
                15f),

            new HighflySkillDefinitionLite(
                HighflyPremiumSkillId.ShadowJudgment,
                "JUICIO DE LA SOMBRA",
                "FINISHER / EJECUCIÓN",
                "Fijación + formación triple + ejecución coordinada.",
                "Finisher avanzado de la rama Umbría.",
                true,
                14f),

            new HighflySkillDefinitionLite(
                HighflyPremiumSkillId.EclipseRend,
                "DESGARRO ECLIPSE",
                "MULTI-CUT / EXECUTION",
                "Siete cortes visibles y confirmación X retardada.",
                "Familia de multicorte de alta velocidad.",
                true,
                7f),

            new HighflySkillDefinitionLite(
                HighflyPremiumSkillId.ReturnWall,
                "MURALLA DE RETORNO",
                "DEFENSA / REFLECT",
                "Ventana frontal que anula y devuelve daño real.",
                "Familia reactiva / perfect guard.",
                true,
                9f),

            new HighflySkillDefinitionLite(
                HighflyPremiumSkillId.VoraciousEcho,
                "ECO VORAZ",
                "DECOY / BLINK / COUNTER",
                "Deja un clon señuelo, reposiciona y contraataca.",
                "Familia engaño / clon / evasión.",
                true,
                8f),

            new HighflySkillDefinitionLite(HighflyPremiumSkillId.GravityZero,"GRAVEDAD CERO","MOVILIDAD / GRAVEDAD","Convierte pared/superficie en trayectoria y altera el eje del cuerpo.","Base APEX de movilidad tridimensional.",true,6f),
            new HighflySkillDefinitionLite(HighflyPremiumSkillId.MomentSight,"VISTA DEL INSTANTE","PERCEPCIÓN / PRECISIÓN","Ventana de percepción acelerada con jugador compensado.","Base APEX de precisión y reacción.",true,9f),
            new HighflySkillDefinitionLite(HighflyPremiumSkillId.FormulaDrift,"DRIFT DE FÓRMULA","MOVILIDAD / BLINDSIDE","Orbita al objetivo y termina en su punto ciego.","Evolución de Deriva.",true,3.8f),
            new HighflySkillDefinitionLite(HighflyPremiumSkillId.SevenSinker,"SEVEN SINKER","ESPADAS REMOTAS / CONTROL","Siete espadas forman prisión gravitatoria rompible.","APEX de espadas sirvientes.",true,12f),
            new HighflySkillDefinitionLite(HighflyPremiumSkillId.ScrapBuild,"SCRAP & BUILD","SACRIFICIO / BUFF","Simula sacrificar arma para convertirla en poder temporal.","APEX de riesgo con arma.",true,14f),
            new HighflySkillDefinitionLite(HighflyPremiumSkillId.VictimArts,"ARTE DEL SACRIFICIO","DAGA / EXPLOSIÓN","Convierte el arma sacrificada en detonación de rareza.","APEX de daga sacrificial.",true,11f),
            new HighflySkillDefinitionLite(HighflyPremiumSkillId.BeastPossession,"POSESIÓN BESTIAL","BOND / TRANSFORMACIÓN","Fusión temporal: velocidad, daño, sentidos y garras.","Rama Bond de Ragnarok.",true,16f),
            new HighflySkillDefinitionLite(HighflyPremiumSkillId.DemonStrike,"GOLPE DEMONÍACO","BOND / CHARGE","Carga energía en un miembro y descarga un impacto concentrado.","Rama Bond de Ragnarok.",true,7f),
            new HighflySkillDefinitionLite(HighflyPremiumSkillId.SpiritArmament,"ARMAMENTO ESPIRITUAL","SOMBRA / BOND","Los summons absorben espíritus y reciben overdrive temporal.","Fusión sombra + Bond.",true,15f),
            new HighflySkillDefinitionLite(HighflyPremiumSkillId.ShadowCreation,"CREACIÓN DE SOMBRA","SOMBRA / ARMA","Materializa un arma desde una sombra y la usa como proyectil/filo.","Evolución utilitaria de Extracción.",true,6f),
            new HighflySkillDefinitionLite(HighflyPremiumSkillId.TemporalCut,"CORTE FUTURO","TEMPORAL / TRAMPA","Marca una posición; el corte ocurre después aunque el blanco se mueva.","APEX temporal.",true,10f),
            new HighflySkillDefinitionLite(HighflyPremiumSkillId.MemoryRelease,"LIBERACIÓN DE MEMORIA","ARMA / RELEASE","El arma libera su identidad en una fase ofensiva especial.","APEX de arma vinculada.",true,18f),
            new HighflySkillDefinitionLite(HighflyPremiumSkillId.BoundlessMassacre,"MASACRE SIN LÍMITE","MULTI-HIT / RESISTENCIA","Ráfaga continua cuya identidad es sostener presión, no un número fijo de tajos.","APEX de endurance.",true,10f),
            new HighflySkillDefinitionLite(HighflyPremiumSkillId.TheFool,"EL LOCO","REGLA / RIESGO","Reduce recast de la tanda v0.9 a cambio de una ventana de vulnerabilidad.","APEX de reglas/riesgo.",true,20f),
            new HighflySkillDefinitionLite(HighflyPremiumSkillId.ReserveSpell,"RESERVA ARCANA","MAGIA / PRECAST","Primer toque almacena; segundo toque libera el hechizo ya preparado.","Base de casting paralelo.",true,5f)
        };

        public static HighflySkillDefinitionLite Get(HighflyPremiumSkillId id)
        {
            for (int i = 0; i < All.Length; i++)
                if (All[i].Id == id)
                    return All[i];

            return null;
        }

        public static float GetCooldownDuration(HighflyPremiumSkillId id)
        {
            HighflySkillDefinitionLite def = Get(id);
            return def != null ? Mathf.Max(0.01f, def.CooldownSeconds) : 1f;
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
        public static HighflySkillLoadoutMenu Instance { get; private set; }

        private Canvas _canvas;
        private GameObject _panel;
        private GameObject _quickPanel;

        private Button _openButton;
        private Button _quickButton;

        private Text _selectedInfo;
        private Text _quickTitle;
        private Text _equipButtonText;
        private HighflyPremiumSkillId _previewSkill = HighflyPremiumSkillId.TwinDance;
        private bool _previewing;

        private readonly Text[] _slotLabels = new Text[5];
        private readonly Button[] _slotButtons = new Button[5];

        private readonly Text[] _quickSlotLabels = new Text[5];
        private readonly Button[] _quickSlotButtons = new Button[5];

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

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            HighflySkillLoadout.Changed -= Refresh;

            if (Instance == this)
                Instance = null;
        }

        public void OpenQuickForSlot(int slot)
        {
            _selectedSlot = Mathf.Clamp(slot, 0, 4);

            if (_panel != null)
                _panel.SetActive(false);

            if (_quickPanel != null)
            {
                _quickPanel.SetActive(true);
                Refresh();
            }
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

            _quickButton = CreateButton(
                canvasGo.transform,
                "RÁPIDO",
                new Vector2(1f, 1f),
                new Vector2(-92f, -130f),
                new Vector2(154f, 58f),
                new Color(0.055f, 0.065f, 0.09f, 0.92f),
                new Color(0.58f, 0.34f, 1f, 1f));

            _quickButton.onClick.AddListener(ToggleQuickPanel);

            BuildFullPanel(canvasGo.transform);
            BuildQuickPanel(canvasGo.transform);

            _selectedSlot = 0;
            Refresh();

            _panel.SetActive(false);
            _quickPanel.SetActive(false);
        }

        private void BuildFullPanel(Transform parent)
        {
            _panel = new GameObject(
                "SkillLoadoutPanel",
                typeof(RectTransform),
                typeof(Image));

            _panel.transform.SetParent(parent, false);

            RectTransform panelRect = _panel.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = new Vector2(110f, 0f);
            panelRect.sizeDelta = new Vector2(1360f, 900f);

            _panel.GetComponent<Image>().color =
                new Color(0.025f, 0.035f, 0.055f, 0.97f);

            CreateLabel(
                _panel.transform,
                "HIGHFLY SYSTEM • SKILL CODEX / LOADOUT",
                new Vector2(-625f, 395f),
                new Vector2(900f, 60f),
                30,
                TextAnchor.MiddleLeft,
                new Color(0.88f, 0.96f, 1f, 1f));

            CreateLabel(
                _panel.transform,
                "TOCÁ UNA SKILL = PREVIEW EN VIVO • después EQUIPAR EN S1–S5 • mismo HIGHFLY CORE",
                new Vector2(-625f, 346f),
                new Vector2(1120f, 42f),
                18,
                TextAnchor.MiddleLeft,
                new Color(0.68f, 0.74f, 0.82f, 1f));

            Button close = CreateButton(
                _panel.transform,
                "CERRAR",
                new Vector2(0.5f, 0.5f),
                new Vector2(575f, 395f),
                new Vector2(150f, 54f),
                new Color(0.12f, 0.13f, 0.16f, 1f),
                new Color(0.70f, 0.74f, 0.80f, 1f));

            close.onClick.AddListener(() => _panel.SetActive(false));

            for (int i = 0; i < 5; i++)
            {
                int slot = i;
                float x = -510f + i * 250f;

                Button button = CreateButton(
                    _panel.transform,
                    "S" + (i + 1),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(x, 270f),
                    new Vector2(225f, 105f),
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
                "CATÁLOGO",
                new Vector2(-625f, 190f),
                new Vector2(260f, 45f),
                22,
                TextAnchor.MiddleLeft,
                new Color(0.86f, 0.90f, 0.96f, 1f));

            const int columns = 3;
            const int rows = 5;

            for (int i = 0; i < HighflySkillCatalog.All.Length; i++)
            {
                HighflySkillDefinitionLite def = HighflySkillCatalog.All[i];

                int col = i / rows;
                int row = i % rows;

                float x = -420f + col * 420f;
                float y = 130f - row * 102f;

                string title =
                    def.Name +
                    "\n" +
                    def.Family +
                    "   CD " +
                    def.CooldownSeconds.ToString("0.#") +
                    "s";

                Button card = CreateButton(
                    _panel.transform,
                    title,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(x, y),
                    new Vector2(390f, 86f),
                    new Color(0.055f, 0.075f, 0.105f, 1f),
                    new Color(0.13f, 0.70f, 0.92f, 1f));

                Text cardText = card.GetComponentInChildren<Text>();
                cardText.fontSize = 15;
                cardText.alignment = TextAnchor.MiddleLeft;

                HighflyPremiumSkillId id = def.Id;
                card.onClick.AddListener(() =>
                {
                    _previewSkill = id;
                    UpdateSelectedInfo();

                    if (!_previewing)
                        StartCoroutine(PreviewRoutine(id));
                });
            }

            Button equip = CreateButton(
                _panel.transform,
                "EQUIPAR",
                new Vector2(0.5f, 0.5f),
                new Vector2(515f, -400f),
                new Vector2(230f, 62f),
                new Color(0.055f, 0.18f, 0.26f, 1f),
                new Color(0.10f, 0.82f, 1f, 1f));

            _equipButtonText = equip.GetComponentInChildren<Text>();
            equip.onClick.AddListener(() =>
            {
                HighflySkillLoadout.Assign(_selectedSlot, _previewSkill);
                Refresh();
            });

            _selectedInfo = CreateLabel(
                _panel.transform,
                "",
                new Vector2(-625f, -400f),
                new Vector2(990f, 66f),
                17,
                TextAnchor.MiddleLeft,
                new Color(0.72f, 0.80f, 0.88f, 1f));
        }

        private void BuildQuickPanel(Transform parent)
        {
            _quickPanel = new GameObject(
                "QuickSkillPanel",
                typeof(RectTransform),
                typeof(Image));

            _quickPanel.transform.SetParent(parent, false);

            RectTransform rect = _quickPanel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-18f, -200f);
            rect.sizeDelta = new Vector2(720f, 690f);

            _quickPanel.GetComponent<Image>().color =
                new Color(0.025f, 0.035f, 0.055f, 0.96f);

            _quickTitle = CreateLabel(
                _quickPanel.transform,
                "",
                new Vector2(-205f, 305f),
                new Vector2(520f, 42f),
                22,
                TextAnchor.MiddleLeft,
                new Color(0.90f, 0.96f, 1f, 1f));

            Button close = CreateButton(
                _quickPanel.transform,
                "X",
                new Vector2(0.5f, 0.5f),
                new Vector2(315f, 305f),
                new Vector2(54f, 46f),
                new Color(0.11f, 0.12f, 0.15f, 1f),
                new Color(0.55f, 0.60f, 0.68f, 1f));

            close.onClick.AddListener(() => _quickPanel.SetActive(false));

            for (int i = 0; i < 5; i++)
            {
                int slot = i;

                Button slotButton = CreateButton(
                    _quickPanel.transform,
                    "S" + (i + 1),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(-260f + i * 130f, 245f),
                    new Vector2(116f, 62f),
                    new Color(0.055f, 0.075f, 0.105f, 1f),
                    new Color(0.16f, 0.68f, 0.96f, 1f));

                _quickSlotButtons[i] = slotButton;
                _quickSlotLabels[i] = slotButton.GetComponentInChildren<Text>();

                slotButton.onClick.AddListener(() =>
                {
                    _selectedSlot = slot;
                    Refresh();
                });
            }

            for (int i = 0; i < HighflySkillCatalog.All.Length; i++)
            {
                HighflySkillDefinitionLite def = HighflySkillCatalog.All[i];

                int col = i % 2;
                int row = i / 2;

                float x = col == 0 ? -178f : 178f;
                float y = 165f - row * 72f;

                Button card = CreateButton(
                    _quickPanel.transform,
                    def.Name,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(x, y),
                    new Vector2(330f, 58f),
                    new Color(0.05f, 0.065f, 0.095f, 1f),
                    new Color(0.34f, 0.26f, 0.62f, 1f));

                Text t = card.GetComponentInChildren<Text>();
                t.fontSize = 14;

                HighflyPremiumSkillId id = def.Id;
                card.onClick.AddListener(() =>
                {
                    HighflySkillLoadout.Assign(_selectedSlot, id);
                    Refresh();
                });
            }
        }

        private IEnumerator PreviewRoutine(HighflyPremiumSkillId id)
        {
            if (_previewing)
                yield break;

            _previewing = true;

            if (_panel != null)
                _panel.SetActive(false);

            if (_quickPanel != null)
                _quickPanel.SetActive(false);

            yield return new WaitForSecondsRealtime(0.08f);

            HighflyPremiumSkillRuntime runtime =
                HighflyPremiumSkillRuntime.Instance;

            if (runtime != null && NeedsTarget(id))
            {
                CharacterStats target =
                    runtime.FindBestTarget(12.5f, 360f);

                if (target != null)
                {
                    Vector3 dir =
                        target.transform.position -
                        runtime.transform.position;
                    dir.y = 0f;

                    if (dir.sqrMagnitude > 0.001f)
                        runtime.transform.rotation =
                            Quaternion.LookRotation(dir.normalized, Vector3.up);
                }
            }

            runtime?.ForcePreview(id);

            yield return new WaitForSecondsRealtime(PreviewDuration(id));

            runtime?.ClearPreviewCooldown(id);

            _previewing = false;

            if (_panel != null)
            {
                _panel.SetActive(true);
                Refresh();
            }
        }

        private static bool NeedsTarget(HighflyPremiumSkillId id)
        {
            return
                id == HighflyPremiumSkillId.ShadowShackle ||
                id == HighflyPremiumSkillId.PhantomTwinDance ||
                id == HighflyPremiumSkillId.AbyssalShackle ||
                id == HighflyPremiumSkillId.ShadowJudgment ||
                id == HighflyPremiumSkillId.EclipseRend;
        }

        private static float PreviewDuration(HighflyPremiumSkillId id)
        {
            switch (id)
            {
                case HighflyPremiumSkillId.ShadowCall:
                case HighflyPremiumSkillId.ShadowLink:
                    return 2.15f;

                case HighflyPremiumSkillId.ShadowJudgment:
                case HighflyPremiumSkillId.EclipseRend:
                    return 1.95f;

                case HighflyPremiumSkillId.VitalDomain:
                    return 1.70f;

                default:
                    return 1.55f;
            }
        }

        private void TogglePanel()
        {
            if (_panel == null) return;

            if (_quickPanel != null)
                _quickPanel.SetActive(false);

            _panel.SetActive(!_panel.activeSelf);

            if (_panel.activeSelf)
                Refresh();
        }

        private void ToggleQuickPanel()
        {
            if (_quickPanel == null) return;

            if (_panel != null)
                _panel.SetActive(false);

            _quickPanel.SetActive(!_quickPanel.activeSelf);

            if (_quickPanel.activeSelf)
                Refresh();
        }

        private void Refresh()
        {
            for (int i = 0; i < 5; i++)
            {
                HighflySkillDefinitionLite def =
                    HighflySkillCatalog.Get(HighflySkillLoadout.Get(i));

                string shortName = def != null ? def.Name : "-";

                if (_slotLabels[i] != null)
                    _slotLabels[i].text =
                        "S" + (i + 1) + "\n" +
                        shortName;

                if (_quickSlotLabels[i] != null)
                    _quickSlotLabels[i].text =
                        "S" + (i + 1) + "\n" +
                        CompactName(shortName);

                Image image = _slotButtons[i] != null
                    ? _slotButtons[i].GetComponent<Image>()
                    : null;

                if (image != null)
                {
                    image.color = i == _selectedSlot
                        ? new Color(0.08f, 0.38f, 0.50f, 1f)
                        : new Color(0.055f, 0.075f, 0.11f, 1f);
                }

                Image quickImage = _quickSlotButtons[i] != null
                    ? _quickSlotButtons[i].GetComponent<Image>()
                    : null;

                if (quickImage != null)
                {
                    quickImage.color = i == _selectedSlot
                        ? new Color(0.11f, 0.28f, 0.52f, 1f)
                        : new Color(0.055f, 0.075f, 0.105f, 1f);
                }
            }

            if (_quickTitle != null)
            {
                HighflySkillDefinitionLite def =
                    HighflySkillCatalog.Get(HighflySkillLoadout.Get(_selectedSlot));

                _quickTitle.text =
                    "SELECCIÓN RÁPIDA • S" +
                    (_selectedSlot + 1) +
                    " • " +
                    (def != null ? def.Name : "-");
            }

            UpdateSelectedInfo();
        }

        private void UpdateSelectedInfo()
        {
            if (_selectedInfo == null) return;

            HighflySkillDefinitionLite def =
                HighflySkillCatalog.Get(_previewSkill);

            if (def == null)
            {
                _selectedInfo.text = "";
                return;
            }

            _selectedInfo.text =
                "PREVIEW • " +
                def.Name +
                "  |  " +
                def.Role +
                "  |  " +
                def.Evolution +
                "  |  CD " +
                def.CooldownSeconds.ToString("0.#") +
                "s";

            if (_equipButtonText != null)
                _equipButtonText.text =
                    "EQUIPAR EN S" +
                    (_selectedSlot + 1);
        }

        private static string CompactName(string value)
        {
            if (string.IsNullOrEmpty(value)) return "-";

            string[] parts = value.Split(' ');
            if (parts.Length <= 2) return value;

            return parts[0] + " " + parts[1];
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
