using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Highfly.SkillLab
{
    [DisallowMultipleComponent]
    public sealed class HighflyLabTelemetryV026 : MonoBehaviour
    {
        public static HighflyLabTelemetryV026 Instance { get; private set; }

        private const int MaxEntries = 96;
        private static readonly Queue<string> Entries = new Queue<string>(MaxEntries);

        private PlayerController _player;
        private GameObject _panel;
        private Text _body;
        private Text _buttonText;
        private bool _expanded;
        private int _warnings;
        private int _errors;
        private float _nextStateSample;
        private PlayerState _lastPlayerState;
        private string _lastAnimatorState = "-";

        public static HighflyLabTelemetryV026 Install(PlayerController player, Transform uiParent)
        {
            if (player == null || uiParent == null) return null;

            var existing = player.GetComponent<HighflyLabTelemetryV026>();
            if (existing != null) return existing;

            var telemetry = player.gameObject.AddComponent<HighflyLabTelemetryV026>();
            telemetry.Initialize(player, uiParent);
            return telemetry;
        }

        private void Awake()
        {
            Instance = this;
            Application.logMessageReceived += OnUnityLog;
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnUnityLog;
            if (Instance == this) Instance = null;
        }

        private void Initialize(PlayerController player, Transform uiParent)
        {
            _player = player;
            _lastPlayerState = player.currentState;
            BuildUi(uiParent);
            Record("BOOT", "telemetría v2.6 activa");
        }

        public static void Record(string category, string message)
        {
            string stamp = Time.unscaledTime.ToString("0000.00");
            string entry = stamp + " [" + category + "] " + message;

            while (Entries.Count >= MaxEntries)
                Entries.Dequeue();

            Entries.Enqueue(entry);

            if (Instance != null)
                Instance.Refresh();
        }

        public static string BuildReport()
        {
            var sb = new StringBuilder(8192);
            sb.AppendLine("HIGHFLY DONOR LAB v2.6 • TEST HISTORY");
            sb.AppendLine("Generated at runtime");
            sb.AppendLine();

            foreach (string entry in Entries)
                sb.AppendLine(entry);

            return sb.ToString();
        }

        private void Update()
        {
            if (_player == null) return;

            if (_player.currentState != _lastPlayerState)
            {
                Record("STATE", _lastPlayerState + " -> " + _player.currentState);
                _lastPlayerState = _player.currentState;
            }

            if (Time.unscaledTime < _nextStateSample) return;
            _nextStateSample = Time.unscaledTime + 0.25f;

            Animator a = _player.animator;
            string animatorState = "-";
            if (a != null && a.isActiveAndEnabled)
            {
                AnimatorStateInfo info = a.GetCurrentAnimatorStateInfo(0);
                animatorState = info.shortNameHash + " @" + (info.normalizedTime % 1f).ToString("0.00");
            }

            if (animatorState != _lastAnimatorState &&
                HighflyLabActionGuardV026.Instance != null &&
                HighflyLabActionGuardV026.Instance.IsLocked)
            {
                _lastAnimatorState = animatorState;
                Record("ANIM", animatorState + " • owner=" + HighflyLabActionGuardV026.Instance.Owner);
            }
        }

        private void OnUnityLog(string condition, string stackTrace, LogType type)
        {
            if (string.IsNullOrEmpty(condition)) return;
            if (condition.StartsWith("[HF026]")) return;

            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                _errors++;
                Record("ERROR", Trim(condition, 180));
            }
            else if (type == LogType.Warning)
            {
                _warnings++;
                Record("WARN", Trim(condition, 180));
            }
        }

        private void BuildUi(Transform parent)
        {
            var canvasGo = new GameObject(
                "LAB_TEST_HISTORY_V026",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(parent, false);

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9995;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Button toggle = MakeButton(canvasGo.transform, "TEST LOG ▸");
            RectTransform tr = toggle.GetComponent<RectTransform>();
            tr.anchorMin = tr.anchorMax = new Vector2(0f, 0f);
            tr.pivot = new Vector2(0f, 0f);
            tr.anchoredPosition = new Vector2(18f, 18f);
            tr.sizeDelta = new Vector2(190f, 48f);
            _buttonText = toggle.GetComponentInChildren<Text>();
            toggle.onClick.AddListener(Toggle);

            _panel = new GameObject("HistoryPanel", typeof(RectTransform), typeof(Image), typeof(Outline));
            _panel.transform.SetParent(canvasGo.transform, false);

            RectTransform pr = _panel.GetComponent<RectTransform>();
            pr.anchorMin = pr.anchorMax = new Vector2(0f, 0f);
            pr.pivot = new Vector2(0f, 0f);
            pr.anchoredPosition = new Vector2(18f, 76f);
            pr.sizeDelta = new Vector2(760f, 420f);

            _panel.GetComponent<Image>().color = new Color(0.01f, 0.015f, 0.025f, 0.94f);
            Outline outline = _panel.GetComponent<Outline>();
            outline.effectColor = new Color(0.20f, 0.70f, 1f, 0.92f);
            outline.effectDistance = new Vector2(1f, -1f);

            _body = MakeLabel(_panel.transform, "", 14, TextAnchor.UpperLeft);
            RectTransform br = _body.rectTransform;
            br.anchorMin = Vector2.zero;
            br.anchorMax = Vector2.one;
            br.offsetMin = new Vector2(16f, 62f);
            br.offsetMax = new Vector2(-16f, -16f);

            Button copy = MakeButton(_panel.transform, "COPIAR LOG");
            RectTransform cr = copy.GetComponent<RectTransform>();
            cr.anchorMin = cr.anchorMax = new Vector2(0f, 0f);
            cr.pivot = new Vector2(0f, 0f);
            cr.anchoredPosition = new Vector2(16f, 14f);
            cr.sizeDelta = new Vector2(180f, 38f);
            copy.onClick.AddListener(CopyReport);

            Button recover = MakeButton(_panel.transform, "RECUPERAR MOV.");
            RectTransform rr = recover.GetComponent<RectTransform>();
            rr.anchorMin = rr.anchorMax = new Vector2(0f, 0f);
            rr.pivot = new Vector2(0f, 0f);
            rr.anchoredPosition = new Vector2(208f, 14f);
            rr.sizeDelta = new Vector2(190f, 38f);
            recover.onClick.AddListener(() =>
            {
                HighflyLabActionGuardV026.Instance?.ForceRecover("manual telemetry button");
            });

            _panel.SetActive(false);
            Refresh();
        }

        private void Toggle()
        {
            _expanded = !_expanded;
            if (_panel != null) _panel.SetActive(_expanded);
            Refresh();
        }

        private void CopyReport()
        {
            GUIUtility.systemCopyBuffer = BuildReport();
            Record("EXPORT", "historial copiado");
        }

        private void Refresh()
        {
            if (_buttonText != null)
            {
                _buttonText.text =
                    "TEST LOG " + (_expanded ? "▾" : "▸") +
                    " • W" + _warnings + " E" + _errors;
            }

            if (_body == null || !_expanded) return;

            string[] items = Entries.ToArray();
            int start = Mathf.Max(0, items.Length - 18);

            var sb = new StringBuilder(4096);
            sb.AppendLine("HIGHFLY v2.6 • historial oculto de prueba");
            sb.AppendLine("Owner: " +
                (HighflyLabActionGuardV026.Instance != null
                    ? HighflyLabActionGuardV026.Instance.Owner
                    : "-"));
            sb.AppendLine();

            for (int i = start; i < items.Length; i++)
                sb.AppendLine(items[i]);

            _body.text = sb.ToString();
        }

        private static Button MakeButton(Transform parent, string value)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.025f, 0.055f, 0.085f, 0.98f);

            Outline outline = go.GetComponent<Outline>();
            outline.effectColor = new Color(0.18f, 0.68f, 1f, 0.9f);
            outline.effectDistance = new Vector2(1f, -1f);

            Text t = MakeLabel(go.transform, value, 14, TextAnchor.MiddleCenter);
            RectTransform rt = t.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(6f, 3f);
            rt.offsetMax = new Vector2(-6f, -3f);

            return go.GetComponent<Button>();
        }

        private static Text MakeLabel(Transform parent, string value, int size, TextAnchor anchor)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
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

        private static string Trim(string value, int max)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= max) return value;
            return value.Substring(0, max) + "…";
        }
    }
}
