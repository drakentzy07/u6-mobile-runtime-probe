using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Highfly.Combat;
using Highfly.SkillLab;

namespace Highfly.Mobile
{
    public enum HighflyMouseButton
    {
        None,
        Left,
        Right
    }

    public enum HighflyMobileAction
    {
        None,
        Attack,
        Skill1,
        Skill2,
        Skill3,
        Skill4,
        Ultimate,
        Dodge,
        Parry,
        Interact,
        Lock,
        JumpClimb,
        Potion,
        Sprint
    }

    public sealed class HighflyVirtualInput : MonoBehaviour
    {
        private Mouse _virtualMouse;
        private readonly List<InputDevice> _disabledPhysicalMice = new List<InputDevice>();
        private bool _exclusiveMobileMouse;
        private float _nextMouseSweep;

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
        }

        public void InitializeForMobile()
        {
            if (_virtualMouse == null)
            {
                _virtualMouse = InputSystem.AddDevice<Mouse>();
                _virtualMouse.MakeCurrent();
            }

            _exclusiveMobileMouse = true;
            DisablePhysicalMice();
        }

        private void Update()
        {
            if (!_exclusiveMobileMouse) return;
            if (Time.unscaledTime < _nextMouseSweep) return;

            _nextMouseSweep = Time.unscaledTime + 0.5f;
            DisablePhysicalMice();
        }

        private void DisablePhysicalMice()
        {
            foreach (var device in InputSystem.devices)
            {
                var mouse = device as Mouse;
                if (mouse == null || mouse == _virtualMouse) continue;
                if (!device.enabled) continue;

                try
                {
                    InputSystem.DisableDevice(device);
                    if (!_disabledPhysicalMice.Contains(device))
                        _disabledPhysicalMice.Add(device);
                }
                catch { }
            }
        }

        public void Look(Vector2 screenDelta)
        {
            if (_virtualMouse == null) return;
            InputSystem.QueueDeltaStateEvent(_virtualMouse.delta, screenDelta);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            for (int i = 0; i < _disabledPhysicalMice.Count; i++)
            {
                try
                {
                    var device = _disabledPhysicalMice[i];
                    if (device != null) InputSystem.EnableDevice(device);
                }
                catch { }
            }
            _disabledPhysicalMice.Clear();

            if (_virtualMouse != null)
            {
                try { InputSystem.RemoveDevice(_virtualMouse); } catch { }
            }
        }
    }

    public enum HighflyTouchOwner
    {
        None,
        Movement,
        Camera,
        Combat
    }

    public static class HighflyTouchOwnership
    {
        private static readonly Dictionary<int, HighflyTouchOwner> Owners =
            new Dictionary<int, HighflyTouchOwner>();

        public static bool TryClaim(int pointerId, HighflyTouchOwner owner)
        {
            HighflyTouchOwner existing;
            if (Owners.TryGetValue(pointerId, out existing))
                return existing == owner;

            Owners[pointerId] = owner;
            return true;
        }

        public static bool IsOwnedBy(int pointerId, HighflyTouchOwner owner)
        {
            HighflyTouchOwner existing;
            return Owners.TryGetValue(pointerId, out existing) && existing == owner;
        }

        public static void Release(int pointerId)
        {
            Owners.Remove(pointerId);
        }

        public static void ReleaseAll()
        {
            Owners.Clear();
        }
    }

    public sealed class HighflyActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private HighflyMobileAction action = HighflyMobileAction.None;

        private RectTransform _rect;
        private Image _image;
        private Vector3 _restScale = Vector3.one;
        private Color _restColor;
        private int _pointerId = int.MinValue;

        public void Configure(HighflyMobileAction mobileAction)
        {
            action = mobileAction;
            _rect = transform as RectTransform;
            _image = GetComponent<Image>();

            if (_rect != null) _restScale = _rect.localScale;
            if (_image != null) _restColor = _image.color;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_pointerId != int.MinValue) return;
            if (!HighflyTouchOwnership.TryClaim(eventData.pointerId, HighflyTouchOwner.Combat)) return;

            _pointerId = eventData.pointerId;

            if (_rect != null) _rect.localScale = _restScale * 0.92f;
            if (_image != null)
            {
                var c = _restColor;
                _image.color = new Color(
                    Mathf.Min(1f, c.r + 0.10f),
                    Mathf.Min(1f, c.g + 0.16f),
                    Mathf.Min(1f, c.b + 0.22f),
                    Mathf.Min(1f, c.a + 0.16f));
            }

            ExecuteAction();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;
            HighflyTouchOwnership.Release(_pointerId);
            _pointerId = int.MinValue;
            RestoreVisual();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Keep ownership until pointer-up, ClaudeCraft-style.
            RestoreVisual();
        }

        private void ExecuteAction()
        {
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            var combat = UnityEngine.Object.FindFirstObjectByType<HighflyLucidCombatBridge>();

            switch (action)
            {
                case HighflyMobileAction.Attack:
                    if (combat != null) combat.Request(HighflyCombatAction.Light);
                    else player?.HighflyMobileAttack();
                    break;
                case HighflyMobileAction.Skill1:
                    if (combat != null) combat.Request(HighflyCombatAction.Skill1);
                    else player?.HighflyMobileSkill1();
                    break;
                case HighflyMobileAction.Skill2:
                    combat?.Request(HighflyCombatAction.Skill2);
                    break;
                case HighflyMobileAction.Skill3:
                    combat?.Request(HighflyCombatAction.Skill3);
                    break;
                case HighflyMobileAction.Skill4:
                    combat?.Request(HighflyCombatAction.Skill4);
                    break;
                case HighflyMobileAction.Ultimate:
                    combat?.Request(HighflyCombatAction.Ultimate);
                    break;
                case HighflyMobileAction.Dodge:
                    if (combat != null) combat.Request(HighflyCombatAction.Dodge);
                    else player?.HighflyMobileRoll();
                    break;
                case HighflyMobileAction.Parry:
                    if (combat != null) combat.Request(HighflyCombatAction.Parry);
                    else player?.HighflyMobileParry();
                    break;
                case HighflyMobileAction.Interact:
                    player?.HighflyMobileInteract();
                    break;
                case HighflyMobileAction.Lock:
                    player?.HighflyMobileLockOn();
                    break;
                case HighflyMobileAction.JumpClimb:
                    player?.HighflyMobileJump();
                    break;
                case HighflyMobileAction.Potion:
                    UnityEngine.Object.FindFirstObjectByType<PlayerPotion>()?.HighflyMobileUsePotion();
                    break;
                case HighflyMobileAction.Sprint:
                case HighflyMobileAction.None:
                default:
                    break;
            }
        }

        private void RestoreVisual()
        {
            if (_rect != null) _rect.localScale = _restScale;
            if (_image != null) _image.color = _restColor;
        }

        private void OnDisable()
        {
            if (_pointerId != int.MinValue)
            {
                HighflyTouchOwnership.Release(_pointerId);
                _pointerId = int.MinValue;
            }
            RestoreVisual();
        }
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
            if (eventData.position.x > Screen.width * 0.50f) return;
            if (!HighflyTouchOwnership.TryClaim(eventData.pointerId, HighflyTouchOwner.Movement)) return;

            _pointerId = eventData.pointerId;
            UpdateStick(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;
            if (!HighflyTouchOwnership.IsOwnedBy(eventData.pointerId, HighflyTouchOwner.Movement)) return;
            UpdateStick(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;
            HighflyTouchOwnership.Release(_pointerId);
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
            float magnitude = value.magnitude;
            Vector2 analog = Vector2.zero;

            if (magnitude >= deadZone)
            {
                float scaled = Mathf.InverseLerp(deadZone, 1f, magnitude);
                analog = value.normalized * scaled;
            }

            var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            player?.SetHighflyMobileMove(analog);
        }

        private void OnDisable()
        {
            if (_pointerId != int.MinValue)
                HighflyTouchOwnership.Release(_pointerId);
            _pointerId = int.MinValue;
            if (knob != null) knob.anchoredPosition = Vector2.zero;
            ApplyMove(Vector2.zero);
        }
    }

    public sealed class HighflyLookZone : MonoBehaviour, IPointerDownHandler, IInitializePotentialDragHandler, IDragHandler, IPointerUpHandler
    {
        private int _pointerId = int.MinValue;
        private Vector2 _lastPosition;

        public void Configure(float unusedSensitivity)
        {
            // Sensitivity is owned by HighflyThirdPersonMobileCamera.
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.useDragThreshold = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_pointerId != int.MinValue) return;

            // Hard screen-space gate: a left-half touch can never become camera.
            if (eventData.position.x < Screen.width * 0.50f) return;
            if (!HighflyTouchOwnership.TryClaim(eventData.pointerId, HighflyTouchOwner.Camera)) return;

            _pointerId = eventData.pointerId;
            _lastPosition = eventData.position;
            eventData.useDragThreshold = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;
            if (!HighflyTouchOwnership.IsOwnedBy(eventData.pointerId, HighflyTouchOwner.Camera)) return;

            Vector2 current = eventData.position;
            Vector2 delta = current - _lastPosition;
            _lastPosition = current;

            HighflyThirdPersonMobileCamera.Instance?.AddLookDelta(delta);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;

            HighflyTouchOwnership.Release(_pointerId);
            _pointerId = int.MinValue;
        }

        private void OnDisable()
        {
            if (_pointerId != int.MinValue)
                HighflyTouchOwnership.Release(_pointerId);
            _pointerId = int.MinValue;
        }
    }

    public sealed class HighflyIntroTap : MonoBehaviour, IPointerDownHandler
    {
        private HighflyMobileBootstrap _owner;

        public void Configure(HighflyMobileBootstrap owner) => _owner = owner;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_owner != null) _owner.RequestImmersiveMode();
        }
    }

    public sealed class HighflyMobileBootstrap : MonoBehaviour
    {
        private const int ReferenceWidth = 1920;
        private const int ReferenceHeight = 1080;

        private GameObject _controlsRoot;
        private Font _font;
        private Sprite _discSprite;
        private Sprite _ringSprite;
        private float _nextPlayerProbe;
        private bool _mobileMode;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern int HF_IsTouchDevice();
        [DllImport("__Internal")] private static extern void HF_RequestFullscreen();
        [DllImport("__Internal")] private static extern void HF_ConfigureCanvas();
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (UnityEngine.Object.FindFirstObjectByType<HighflyMobileBootstrap>() != null) return;

            var root = new GameObject("HIGHFLY_MOBILE_CORE_v0.6");
            DontDestroyOnLoad(root);

            root.AddComponent<HighflyMobileRuntimeLocalizer>();
            root.AddComponent<HighflyThirdPersonMobileCamera>();
            root.AddComponent<HighflyMobileBootstrap>();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            // The Skill Lab intentionally renders the exact mobile HUD/input layout on desktop
            // so PC iteration matches the S23 Ultra control topology.
            _mobileMode = Application.isMobilePlatform || HighflySkillLabMode.IsActive;

#if UNITY_WEBGL && !UNITY_EDITOR
            try { _mobileMode = _mobileMode || HF_IsTouchDevice() != 0; } catch { }
#endif

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

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                HighflyTouchOwnership.ReleaseAll();
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

            // On native mobile there is no hardware mouse cursor to manage.
            // On WebGL, cursor/pointer-lock is handled in JS so desktop browser
            // state is never left hidden after closing or changing tabs.
            if (Application.isMobilePlatform)
            {
                if (Cursor.lockState != CursorLockMode.None)
                    Cursor.lockState = CursorLockMode.None;
                if (Cursor.visible)
                    Cursor.visible = false;
            }
        }

        public void RequestImmersiveMode()
        {
            if (Application.isMobilePlatform)
            {
                Screen.fullScreen = true;
                Screen.orientation = ScreenOrientation.LandscapeLeft;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            try { HF_RequestFullscreen(); } catch { }
#endif
        }

        private void RefreshPlayableState()
        {
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            bool hasPlayer = player != null;
            bool isInteracting = hasPlayer && player.currentState == PlayerState.Interact;

            if (hasPlayer && player.GetComponent<HighflyLucidCombatBridge>() == null)
                player.gameObject.AddComponent<HighflyLucidCombatBridge>();

            if (_controlsRoot != null)
                _controlsRoot.SetActive(hasPlayer && !isInteracting);
        }

        private void EnsureEventSystem()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                var eventSystemGo = new GameObject("HIGHFLY EventSystem");
                DontDestroyOnLoad(eventSystemGo);
                eventSystem = eventSystemGo.AddComponent<EventSystem>();
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        private void BuildUI()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _discSprite = CreateRadialSprite(false);
            _ringSprite = CreateRadialSprite(true);

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
            _controlsRoot.SetActive(false);
        }

        private void CreateLookZone(Transform parent)
        {
            var go = new GameObject("CAMERA_DERECHA", typeof(RectTransform), typeof(Image), typeof(HighflyLookZone));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.50f, 0f);
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.001f);
            image.raycastTarget = true;

            go.GetComponent<HighflyLookZone>().Configure(0.32f);
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
            baseImage.sprite = _discSprite;
            baseImage.color = new Color(0.02f, 0.09f, 0.16f, 0.50f);

            var knobGo = new GameObject("Knob", typeof(RectTransform), typeof(Image));
            knobGo.transform.SetParent(baseGo.transform, false);

            var knobRect = knobGo.GetComponent<RectTransform>();
            knobRect.anchorMin = knobRect.anchorMax = new Vector2(0.5f, 0.5f);
            knobRect.pivot = new Vector2(0.5f, 0.5f);
            knobRect.sizeDelta = new Vector2(96f, 96f);
            knobRect.anchoredPosition = Vector2.zero;

            var knobImage = knobGo.GetComponent<Image>();
            knobImage.sprite = _discSprite;
            knobImage.color = new Color(0.15f, 0.72f, 1f, 0.68f);
            knobImage.raycastTarget = false;

            var ringGo = new GameObject("JoystickRing", typeof(RectTransform), typeof(Image));
            ringGo.transform.SetParent(baseGo.transform, false);
            var ringRect = ringGo.GetComponent<RectTransform>();
            ringRect.anchorMin = ringRect.anchorMax = new Vector2(0.5f, 0.5f);
            ringRect.pivot = new Vector2(0.5f, 0.5f);
            ringRect.sizeDelta = new Vector2(222f, 222f);
            var ringImage = ringGo.GetComponent<Image>();
            ringImage.sprite = _ringSprite;
            ringImage.color = new Color(0.20f, 0.75f, 1f, 0.42f);
            ringImage.raycastTarget = false;

            baseGo.GetComponent<HighflyJoystick>().Configure(knobRect, 86f);

        }

        private void CreateActionButtons(Transform parent)
        {
            // Clean crescent around ATQ: designed for one-thumb reach on landscape phones.
            CreateButton(parent, "ATQ",      new Vector2(-118f, 132f), new Vector2(184f, 184f), HighflyMobileAction.Attack, 31);

            CreateButton(parent, "S1",       new Vector2(-315f, 92f),  new Vector2(110f, 110f), HighflyMobileAction.Skill1, 21);
            CreateButton(parent, "S2",       new Vector2(-345f, 225f), new Vector2(108f, 108f), HighflyMobileAction.Skill2, 20);
            CreateButton(parent, "S3",       new Vector2(-300f, 355f), new Vector2(108f, 108f), HighflyMobileAction.Skill3, 20);
            CreateButton(parent, "S4",       new Vector2(-195f, 445f), new Vector2(108f, 108f), HighflyMobileAction.Skill4, 20);

            // Ultimate gets its own third-row emphasis.
            CreateButton(parent, "ULT",      new Vector2(-365f, 515f), new Vector2(142f, 142f), HighflyMobileAction.Ultimate, 24);

            // Utility lane: intentionally detached from the skill crescent.
            CreateButton(parent, "ESQUIVAR", new Vector2(-535f, 105f), new Vector2(118f, 118f), HighflyMobileAction.Dodge, 15);
            CreateButton(parent, "PARRY",    new Vector2(-655f, 185f), new Vector2(104f, 104f), HighflyMobileAction.Parry, 15);
            CreateButton(parent, "USAR",     new Vector2(-770f, 102f), new Vector2(96f, 96f), HighflyMobileAction.Interact, 13);
            CreateButton(parent, "LOCK",     new Vector2(-780f, 235f), new Vector2(92f, 92f), HighflyMobileAction.Lock, 13);
            CreateButton(parent, "SALTAR\nESCALAR", new Vector2(-690f, 365f), new Vector2(104f, 104f), HighflyMobileAction.JumpClimb, 11);
            CreateButton(parent, "POCIÓN",   new Vector2(-875f, 235f), new Vector2(88f, 88f), HighflyMobileAction.Potion, 11);
        }

        private void CreateButton(
            Transform parent,
            string label,
            Vector2 anchoredPosition,
            Vector2 size,
            HighflyMobileAction action,
            int fontSize)
        {
            string safeName = label.Replace("\n", "_");
            var go = new GameObject(safeName, typeof(RectTransform), typeof(Image), typeof(HighflyActionButton));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            bool leftSide = anchoredPosition.x > 0f;
            rect.anchorMin = rect.anchorMax = leftSide ? new Vector2(0f, 0f) : new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            Color accent = GetButtonAccent(label);

            var glowGo = new GameObject("Glow", typeof(RectTransform), typeof(Image));
            glowGo.transform.SetParent(go.transform, false);
            var glowRect = glowGo.GetComponent<RectTransform>();
            glowRect.anchorMin = glowRect.anchorMax = new Vector2(0.5f, 0.5f);
            glowRect.pivot = new Vector2(0.5f, 0.5f);
            glowRect.sizeDelta = size * 1.16f;
            var glow = glowGo.GetComponent<Image>();
            glow.sprite = _discSprite;
            glow.color = new Color(accent.r, accent.g, accent.b, 0.10f);
            glow.raycastTarget = false;

            var image = go.GetComponent<Image>();
            image.sprite = _discSprite;
            image.color = new Color(0.015f, 0.035f, 0.07f, 0.78f);
            image.raycastTarget = true;

            var ringGo = new GameObject("Ring", typeof(RectTransform), typeof(Image));
            ringGo.transform.SetParent(go.transform, false);
            var ringRect = ringGo.GetComponent<RectTransform>();
            ringRect.anchorMin = Vector2.zero;
            ringRect.anchorMax = Vector2.one;
            ringRect.offsetMin = new Vector2(4f, 4f);
            ringRect.offsetMax = new Vector2(-4f, -4f);
            var ring = ringGo.GetComponent<Image>();
            ring.sprite = _ringSprite;
            ring.color = new Color(accent.r, accent.g, accent.b, 0.82f);
            ring.raycastTarget = false;

            go.GetComponent<HighflyActionButton>().Configure(action);

            var text = CreateText(
                safeName + "_Text",
                go.transform,
                label,
                fontSize,
                TextAnchor.MiddleCenter,
                new Color(0.94f, 0.98f, 1f, 0.96f));
            Stretch(text.rectTransform);
        }

        private static Color GetButtonAccent(string label)
        {
            if (label == "ULT") return new Color(0.62f, 0.32f, 1f, 1f);
            if (label == "ATQ") return new Color(0.10f, 0.85f, 1f, 1f);
            if (label.StartsWith("S")) return new Color(0.18f, 0.58f, 1f, 1f);
            if (label == "PARRY") return new Color(0.35f, 0.82f, 1f, 1f);
            if (label == "POCIÓN") return new Color(0.52f, 0.42f, 1f, 1f);
            return new Color(0.20f, 0.68f, 0.95f, 1f);
        }

        private static Sprite CreateRadialSprite(bool ringOnly)
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = ringOnly ? "HF_Ring" : "HF_Disc";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.hideFlags = HideFlags.DontSave;

            var pixels = new Color32[size * size];
            float center = (size - 1) * 0.5f;
            float outer = center - 1f;
            float inner = outer * 0.82f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    bool visible = ringOnly
                        ? d <= outer && d >= inner
                        : d <= outer;

                    pixels[y * size + x] = visible
                        ? new Color32(255, 255, 255, 255)
                        : new Color32(255, 255, 255, 0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
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
