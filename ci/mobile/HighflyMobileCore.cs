using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Highfly.Mobile
{
    public enum HighflyMouseButton
    {
        None,
        Left,
        Right
    }

    public sealed class HighflyVirtualInput : MonoBehaviour
    {
        private Keyboard _keyboard;
        private Mouse _mouse;
        private readonly Dictionary<Key, bool> _keyStates = new Dictionary<Key, bool>();
        private bool _leftMouse;
        private bool _rightMouse;

        public static HighflyVirtualInput Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _keyboard = InputSystem.AddDevice<Keyboard>();
            _mouse = InputSystem.AddDevice<Mouse>();
        }

        public void SetKey(Key key, bool pressed)
        {
            if (_keyboard == null || key == Key.None) return;

            bool current;
            if (_keyStates.TryGetValue(key, out current) && current == pressed) return;

            _keyStates[key] = pressed;
            InputSystem.QueueDeltaStateEvent(_keyboard[key], pressed ? 1f : 0f);
        }

        public void SetMouseButton(HighflyMouseButton button, bool pressed)
        {
            if (_mouse == null || button == HighflyMouseButton.None) return;

            if (button == HighflyMouseButton.Left)
            {
                if (_leftMouse == pressed) return;
                _leftMouse = pressed;
                InputSystem.QueueDeltaStateEvent(_mouse.leftButton, pressed ? 1f : 0f);
            }
            else if (button == HighflyMouseButton.Right)
            {
                if (_rightMouse == pressed) return;
                _rightMouse = pressed;
                InputSystem.QueueDeltaStateEvent(_mouse.rightButton, pressed ? 1f : 0f);
            }
        }

        public void Look(Vector2 screenDelta)
        {
            if (_mouse == null) return;
            InputSystem.QueueDeltaStateEvent(_mouse.delta, screenDelta);
        }

        public void PulseKey(Key key)
        {
            StartCoroutine(PulseKeyRoutine(key));
        }

        private IEnumerator PulseKeyRoutine(Key key)
        {
            SetKey(key, true);
            yield return null;
            SetKey(key, false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            if (_keyboard != null)
            {
                try { InputSystem.RemoveDevice(_keyboard); } catch { }
            }

            if (_mouse != null)
            {
                try { InputSystem.RemoveDevice(_mouse); } catch { }
            }
        }
    }

    public sealed class HighflyActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private Key key = Key.None;
        [SerializeField] private HighflyMouseButton mouseButton = HighflyMouseButton.None;

        public void Configure(Key keyboardKey, HighflyMouseButton virtualMouseButton)
        {
            key = keyboardKey;
            mouseButton = virtualMouseButton;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            var input = HighflyVirtualInput.Instance;
            if (input == null) return;

            if (mouseButton != HighflyMouseButton.None) input.SetMouseButton(mouseButton, true);
            if (key != Key.None) input.SetKey(key, true);
        }

        public void OnPointerUp(PointerEventData eventData) => Release();
        public void OnPointerExit(PointerEventData eventData) => Release();

        private void Release()
        {
            var input = HighflyVirtualInput.Instance;
            if (input == null) return;

            if (mouseButton != HighflyMouseButton.None) input.SetMouseButton(mouseButton, false);
            if (key != Key.None) input.SetKey(key, false);
        }

        private void OnDisable() => Release();
    }

    public sealed class HighflyJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform knob;
        [SerializeField] private float radius = 90f;
        [SerializeField] private float deadZone = 0.18f;

        private RectTransform _rect;
        private int _pointerId = int.MinValue;

        public void Configure(RectTransform knobTransform, float movementRadius)
        {
            knob = knobTransform;
            radius = movementRadius;
            _rect = transform as RectTransform;
        }

        private void Awake()
        {
            _rect = transform as RectTransform;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_pointerId != int.MinValue) return;
            _pointerId = eventData.pointerId;
            UpdateStick(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;
            UpdateStick(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;
            _pointerId = int.MinValue;

            if (knob != null) knob.anchoredPosition = Vector2.zero;
            ApplyMove(Vector2.zero);
        }

        private void UpdateStick(PointerEventData eventData)
        {
            if (_rect == null) return;

            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, eventData.position, eventData.pressEventCamera, out local))
                return;

            Vector2 normalized = Vector2.ClampMagnitude(local / radius, 1f);
            if (knob != null) knob.anchoredPosition = normalized * radius;
            ApplyMove(normalized);
        }

        private void ApplyMove(Vector2 value)
        {
            var input = HighflyVirtualInput.Instance;
            if (input == null) return;

            float x = Mathf.Abs(value.x) >= deadZone ? value.x : 0f;
            float y = Mathf.Abs(value.y) >= deadZone ? value.y : 0f;

            input.SetKey(Key.A, x < 0f);
            input.SetKey(Key.D, x > 0f);
            input.SetKey(Key.S, y < 0f);
            input.SetKey(Key.W, y > 0f);
        }

        private void OnDisable()
        {
            _pointerId = int.MinValue;
            if (knob != null) knob.anchoredPosition = Vector2.zero;
            ApplyMove(Vector2.zero);
        }
    }

    public sealed class HighflyLookZone : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private float sensitivity = 0.42f;
        private int _pointerId = int.MinValue;
        private Vector2 _lastPosition;

        public void Configure(float lookSensitivity)
        {
            sensitivity = lookSensitivity;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_pointerId != int.MinValue) return;
            _pointerId = eventData.pointerId;
            _lastPosition = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;

            Vector2 current = eventData.position;
            Vector2 delta = (current - _lastPosition) * sensitivity;
            _lastPosition = current;

            var input = HighflyVirtualInput.Instance;
            if (input != null) input.Look(delta);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == _pointerId) _pointerId = int.MinValue;
        }

        private void OnDisable() => _pointerId = int.MinValue;
    }

    public sealed class HighflyIntroTap : MonoBehaviour, IPointerDownHandler
    {
        private HighflyMobileBootstrap _owner;

        public void Configure(HighflyMobileBootstrap owner) => _owner = owner;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_owner != null) _owner.DismissIntro();
        }
    }

    public sealed class HighflyMobileBootstrap : MonoBehaviour
    {
        private const int ReferenceWidth = 1920;
        private const int ReferenceHeight = 1080;

        private GameObject _controlsRoot;
        private GameObject _introRoot;
        private Font _font;
        private float _nextPlayerProbe;
        private bool _mobileMode;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void HF_RequestFullscreen();
        [DllImport("__Internal")] private static extern void HF_ConfigureCanvas();
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (UnityEngine.Object.FindFirstObjectByType<HighflyMobileBootstrap>() != null) return;

            var root = new GameObject("HIGHFLY_MOBILE_CORE_v0.1");
            DontDestroyOnLoad(root);

            root.AddComponent<HighflyVirtualInput>();
            root.AddComponent<HighflyMobileBootstrap>();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            _mobileMode =
                Application.isMobilePlatform ||
                Application.platform == RuntimePlatform.WebGLPlayer;

            if (!_mobileMode)
            {
                enabled = false;
                return;
            }

            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Input.multiTouchEnabled = true;

#if UNITY_WEBGL && !UNITY_EDITOR
            try { HF_ConfigureCanvas(); } catch { }
#endif

            EnsureEventSystem();
            BuildUI();
        }

        private void Update()
        {
            if (!_mobileMode) return;

            if (Time.unscaledTime >= _nextPlayerProbe)
            {
                _nextPlayerProbe = Time.unscaledTime + 0.5f;
                RefreshPlayableState();
            }
        }

        private void LateUpdate()
        {
            if (!_mobileMode) return;

            if (Cursor.lockState != CursorLockMode.None)
                Cursor.lockState = CursorLockMode.None;

            if (Cursor.visible)
                Cursor.visible = false;
        }

        public void DismissIntro()
        {
            RequestImmersiveMode();

            var input = HighflyVirtualInput.Instance;
            if (input != null)
            {
                input.PulseKey(Key.Enter);
                input.PulseKey(Key.Space);
            }

            if (_introRoot != null) _introRoot.SetActive(false);
            RefreshPlayableState();
        }

        private void RequestImmersiveMode()
        {
            Screen.fullScreen = true;

            if (Application.isMobilePlatform)
                Screen.orientation = ScreenOrientation.LandscapeLeft;

#if UNITY_WEBGL && !UNITY_EDITOR
            try { HF_ConfigureCanvas(); } catch { }
            try { HF_RequestFullscreen(); } catch { }
#endif
        }

        private void RefreshPlayableState()
        {
            bool hasPlayer = UnityEngine.Object.FindFirstObjectByType<PlayerController>() != null;
            bool introVisible = _introRoot != null && _introRoot.activeSelf;

            if (_controlsRoot != null)
                _controlsRoot.SetActive(hasPlayer && !introVisible);
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;

            var eventSystem = new GameObject("HIGHFLY EventSystem");
            DontDestroyOnLoad(eventSystem);
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        private void BuildUI()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasGo = new GameObject("HIGHFLY Mobile HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.matchWidthOrHeight = 0.5f;

            _controlsRoot = CreateStretchRoot("Controls", canvasGo.transform);
            CreateLookZone(_controlsRoot.transform);
            CreateJoystick(_controlsRoot.transform);
            CreateActionButtons(_controlsRoot.transform);
            CreateStatusTag(_controlsRoot.transform);
            _controlsRoot.SetActive(false);

            _introRoot = CreateStretchRoot("TapToEnter", canvasGo.transform);
            var introImage = _introRoot.AddComponent<Image>();
            introImage.color = new Color(0f, 0f, 0f, 0.08f);
            introImage.raycastTarget = true;

            var introTap = _introRoot.AddComponent<HighflyIntroTap>();
            introTap.Configure(this);

            var introText = CreateText(
                "IntroText",
                _introRoot.transform,
                "TOCAR PARA ENTRAR",
                44,
                TextAnchor.MiddleCenter,
                new Color(1f, 1f, 1f, 0.95f));
            Stretch(introText.rectTransform);
            introText.rectTransform.anchoredPosition = new Vector2(0f, -350f);

            var sub = CreateText(
                "IntroSub",
                _introRoot.transform,
                "HIGHFLY • LUCID MOBILE CORE v0.1",
                22,
                TextAnchor.MiddleCenter,
                new Color(0.65f, 0.9f, 1f, 0.9f));
            Stretch(sub.rectTransform);
            sub.rectTransform.anchoredPosition = new Vector2(0f, -405f);
        }

        private void CreateLookZone(Transform parent)
        {
            var go = new GameObject("CAMERA_DERECHA", typeof(RectTransform), typeof(Image), typeof(HighflyLookZone));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.46f, 0f);
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.001f);
            image.raycastTarget = true;

            go.GetComponent<HighflyLookZone>().Configure(0.42f);
        }

        private void CreateJoystick(Transform parent)
        {
            var baseGo = new GameObject("JOYSTICK_IZQUIERDA", typeof(RectTransform), typeof(Image), typeof(HighflyJoystick));
            baseGo.transform.SetParent(parent, false);

            var baseRect = baseGo.GetComponent<RectTransform>();
            baseRect.anchorMin = baseRect.anchorMax = new Vector2(0f, 0f);
            baseRect.pivot = new Vector2(0.5f, 0.5f);
            baseRect.sizeDelta = new Vector2(230f, 230f);
            baseRect.anchoredPosition = new Vector2(170f, 175f);

            var baseImage = baseGo.GetComponent<Image>();
            baseImage.color = new Color(0.08f, 0.35f, 0.65f, 0.20f);

            var knobGo = new GameObject("Knob", typeof(RectTransform), typeof(Image));
            knobGo.transform.SetParent(baseGo.transform, false);

            var knobRect = knobGo.GetComponent<RectTransform>();
            knobRect.anchorMin = knobRect.anchorMax = new Vector2(0.5f, 0.5f);
            knobRect.pivot = new Vector2(0.5f, 0.5f);
            knobRect.sizeDelta = new Vector2(96f, 96f);
            knobRect.anchoredPosition = Vector2.zero;

            var knobImage = knobGo.GetComponent<Image>();
            knobImage.color = new Color(0.35f, 0.75f, 1f, 0.45f);
            knobImage.raycastTarget = false;

            baseGo.GetComponent<HighflyJoystick>().Configure(knobRect, 86f);

            CreateButton(parent, "CORRER", new Vector2(165f, 330f), new Vector2(130f, 58f), Key.LeftShift, HighflyMouseButton.None, 19);
        }

        private void CreateActionButtons(Transform parent)
        {
            CreateButton(parent, "ATQ",      new Vector2(-125f, 145f), new Vector2(160f, 160f), Key.None, HighflyMouseButton.Left, 28);
            CreateButton(parent, "ESQUIVAR", new Vector2(-305f, 118f), new Vector2(132f, 104f), Key.F, HighflyMouseButton.None, 17);
            CreateButton(parent, "PARRY",    new Vector2(-245f, 255f), new Vector2(126f, 100f), Key.None, HighflyMouseButton.Right, 18);
            CreateButton(parent, "S1",       new Vector2(-110f, 315f), new Vector2(126f, 126f), Key.Q, HighflyMouseButton.None, 22);
            CreateButton(parent, "LOCK",     new Vector2(-395f, 265f), new Vector2(108f, 82f), Key.Tab, HighflyMouseButton.None, 16);
            CreateButton(parent, "USAR",     new Vector2(-420f, 150f), new Vector2(108f, 82f), Key.E, HighflyMouseButton.None, 16);
            CreateButton(parent, "SALTAR",   new Vector2(-305f, 370f), new Vector2(112f, 82f), Key.Space, HighflyMouseButton.None, 15);
            CreateButton(parent, "POCIÓN",   new Vector2(-525f, 230f), new Vector2(108f, 82f), Key.R, HighflyMouseButton.None, 15);
        }

        private void CreateStatusTag(Transform parent)
        {
            var tag = CreateText(
                "CoreTag",
                parent,
                "HIGHFLY CORE • MOBILE v0.1",
                18,
                TextAnchor.UpperLeft,
                new Color(0.75f, 0.92f, 1f, 0.75f));

            tag.rectTransform.anchorMin = tag.rectTransform.anchorMax = new Vector2(0f, 1f);
            tag.rectTransform.pivot = new Vector2(0f, 1f);
            tag.rectTransform.sizeDelta = new Vector2(360f, 40f);
            tag.rectTransform.anchoredPosition = new Vector2(18f, -16f);
        }

        private void CreateButton(
            Transform parent,
            string label,
            Vector2 anchoredPosition,
            Vector2 size,
            Key key,
            HighflyMouseButton mouseButton,
            int fontSize)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(HighflyActionButton));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            var image = go.GetComponent<Image>();
            image.color = new Color(0.12f, 0.32f, 0.58f, 0.26f);
            image.raycastTarget = true;

            go.GetComponent<HighflyActionButton>().Configure(key, mouseButton);

            var text = CreateText(
                label + "_Text",
                go.transform,
                label,
                fontSize,
                TextAnchor.MiddleCenter,
                new Color(1f, 1f, 1f, 0.90f));
            Stretch(text.rectTransform);
        }

        private Text CreateText(
            string name,
            Transform parent,
            string value,
            int fontSize,
            TextAnchor alignment,
            Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            var text = go.GetComponent<Text>();
            text.text = value;
            text.font = _font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.resizeTextForBestFit = false;

            return text;
        }

        private static GameObject CreateStretchRoot(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>());
            return go;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
