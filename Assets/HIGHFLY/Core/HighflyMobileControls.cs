using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Highfly.Clean
{
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

    public sealed class HighflyMobileJoystick :
        MonoBehaviour,
        IPointerDownHandler,
        IDragHandler,
        IPointerUpHandler
    {
        private RectTransform _rect;
        private RectTransform _knob;
        private float _radius = 86f;
        private float _deadZone = 0.14f;
        private int _pointerId = int.MinValue;

        public void Configure(RectTransform knob, float radius)
        {
            _rect = transform as RectTransform;
            _knob = knob;
            _radius = radius;
        }

        private void Awake()
        {
            _rect = transform as RectTransform;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_pointerId != int.MinValue) return;
            if (eventData.position.x > Screen.width * 0.50f) return;
            if (!HighflyTouchOwnership.TryClaim(
                    eventData.pointerId,
                    HighflyTouchOwner.Movement))
                return;

            _pointerId = eventData.pointerId;
            UpdateStick(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;
            if (!HighflyTouchOwnership.IsOwnedBy(
                    eventData.pointerId,
                    HighflyTouchOwner.Movement))
                return;

            UpdateStick(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;

            HighflyTouchOwnership.Release(_pointerId);
            _pointerId = int.MinValue;

            if (_knob != null)
                _knob.anchoredPosition = Vector2.zero;

            HighflyInputRouter.Instance?.SetMobileMove(Vector2.zero);
        }

        private void UpdateStick(PointerEventData eventData)
        {
            if (_rect == null) return;

            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out local))
                return;

            Vector2 normalized =
                Vector2.ClampMagnitude(local / Mathf.Max(1f, _radius), 1f);

            if (_knob != null)
                _knob.anchoredPosition = normalized * _radius;

            float magnitude = normalized.magnitude;
            Vector2 analog = Vector2.zero;

            if (magnitude >= _deadZone)
            {
                float scaled =
                    Mathf.InverseLerp(_deadZone, 1f, magnitude);

                analog = normalized.normalized * scaled;
            }

            HighflyInputRouter.Instance?.SetMobileMove(analog);
        }

        private void OnDisable()
        {
            if (_pointerId != int.MinValue)
                HighflyTouchOwnership.Release(_pointerId);

            _pointerId = int.MinValue;

            if (_knob != null)
                _knob.anchoredPosition = Vector2.zero;

            HighflyInputRouter.Instance?.SetMobileMove(Vector2.zero);
        }
    }

    public sealed class HighflyMobileLookZone :
        MonoBehaviour,
        IPointerDownHandler,
        IInitializePotentialDragHandler,
        IDragHandler,
        IPointerUpHandler
    {
        private int _pointerId = int.MinValue;
        private Vector2 _lastPosition;

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.useDragThreshold = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_pointerId != int.MinValue) return;

            // The left half can never become camera input.
            if (eventData.position.x < Screen.width * 0.50f) return;

            if (!HighflyTouchOwnership.TryClaim(
                    eventData.pointerId,
                    HighflyTouchOwner.Camera))
                return;

            _pointerId = eventData.pointerId;
            _lastPosition = eventData.position;
            eventData.useDragThreshold = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;
            if (!HighflyTouchOwnership.IsOwnedBy(
                    eventData.pointerId,
                    HighflyTouchOwner.Camera))
                return;

            Vector2 current = eventData.position;
            Vector2 delta = current - _lastPosition;
            _lastPosition = current;

            HighflyInputRouter.Instance?.AddMobileLookDelta(delta);
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

    public enum HighflyMobileAction
    {
        Attack,
        Jump
    }

    public sealed class HighflyMobileActionButton :
        MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerExitHandler
    {
        private HighflyMobileAction _action;
        private RectTransform _rect;
        private Image _image;
        private Vector3 _restScale = Vector3.one;
        private Color _restColor;
        private int _pointerId = int.MinValue;

        public void Configure(HighflyMobileAction action)
        {
            _action = action;
            _rect = transform as RectTransform;
            _image = GetComponent<Image>();

            if (_rect != null)
                _restScale = _rect.localScale;

            if (_image != null)
                _restColor = _image.color;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_pointerId != int.MinValue) return;

            if (!HighflyTouchOwnership.TryClaim(
                    eventData.pointerId,
                    HighflyTouchOwner.Combat))
                return;

            _pointerId = eventData.pointerId;

            if (_rect != null)
                _rect.localScale = _restScale * 0.92f;

            if (_image != null)
            {
                Color c = _restColor;
                _image.color = new Color(
                    Mathf.Min(1f, c.r + 0.10f),
                    Mathf.Min(1f, c.g + 0.14f),
                    Mathf.Min(1f, c.b + 0.18f),
                    Mathf.Min(1f, c.a + 0.12f));
            }

            if (_action == HighflyMobileAction.Attack)
                HighflyInputRouter.Instance?.QueueAttack();
            else if (_action == HighflyMobileAction.Jump)
                HighflyInputRouter.Instance?.QueueJump();
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
            // Keep ownership until pointer-up so a sliding combat thumb
            // cannot accidentally turn into camera input.
            RestoreVisual();
        }

        private void RestoreVisual()
        {
            if (_rect != null)
                _rect.localScale = _restScale;

            if (_image != null)
                _image.color = _restColor;
        }

        private void OnDisable()
        {
            if (_pointerId != int.MinValue)
                HighflyTouchOwnership.Release(_pointerId);

            _pointerId = int.MinValue;
            RestoreVisual();
        }
    }

    public sealed class HighflyMobileControls : MonoBehaviour
    {
        private const int ReferenceWidth = 1920;
        private const int ReferenceHeight = 1080;

        private Font _font;
        private Sprite _discSprite;
        private Sprite _ringSprite;

        private void Awake()
        {
            EnsureEventSystem();
            BuildUI();

            Debug.Log(
                "[CLEAN-RUN0B] Golden mobile input installed • " +
                "left=movement • right=camera • multitouch ownership=ON");
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus) return;

            HighflyTouchOwnership.ReleaseAll();
            HighflyInputRouter.Instance?.SetMobileMove(Vector2.zero);
        }

        private void EnsureEventSystem()
        {
            EventSystem eventSystem = EventSystem.current;

            if (eventSystem == null)
            {
                GameObject eventSystemGo =
                    new GameObject("HIGHFLY_EVENT_SYSTEM");

                eventSystem =
                    eventSystemGo.AddComponent<EventSystem>();
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        private void BuildUI()
        {
            _font =
                Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _discSprite = CreateRadialSprite(false);
            _ringSprite = CreateRadialSprite(true);

            GameObject canvasGo = new GameObject(
                "HIGHFLY_MOBILE_CONTROLS",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;

            CanvasScaler scaler =
                canvasGo.GetComponent<CanvasScaler>();

            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;

            scaler.referenceResolution =
                new Vector2(ReferenceWidth, ReferenceHeight);

            scaler.matchWidthOrHeight = 0.5f;

            GameObject root = new GameObject(
                "Controls",
                typeof(RectTransform));

            root.transform.SetParent(canvasGo.transform, false);
            Stretch(root.GetComponent<RectTransform>());

            // Look zone must be created first so joystick/buttons render
            // above it and receive their own pointer events.
            CreateLookZone(root.transform);
            CreateJoystick(root.transform);

            CreateActionButton(
                root.transform,
                "SALTO",
                new Vector2(-335f, 135f),
                new Vector2(130f, 130f),
                HighflyMobileAction.Jump,
                22);

            CreateActionButton(
                root.transform,
                "ATQ",
                new Vector2(-145f, 145f),
                new Vector2(176f, 176f),
                HighflyMobileAction.Attack,
                30);
        }

        private void CreateLookZone(Transform parent)
        {
            GameObject go = new GameObject(
                "CAMERA_DERECHA",
                typeof(RectTransform),
                typeof(Image),
                typeof(HighflyMobileLookZone));

            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.50f, 0f);
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = go.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.001f);
            image.raycastTarget = true;
        }

        private void CreateJoystick(Transform parent)
        {
            GameObject baseGo = new GameObject(
                "JOYSTICK_IZQUIERDA",
                typeof(RectTransform),
                typeof(Image),
                typeof(HighflyMobileJoystick));

            baseGo.transform.SetParent(parent, false);

            RectTransform baseRect =
                baseGo.GetComponent<RectTransform>();

            baseRect.anchorMin =
                baseRect.anchorMax =
                    new Vector2(0f, 0f);

            baseRect.pivot =
                new Vector2(0.5f, 0.5f);

            baseRect.sizeDelta =
                new Vector2(230f, 230f);

            baseRect.anchoredPosition =
                new Vector2(170f, 175f);

            Image baseImage = baseGo.GetComponent<Image>();
            baseImage.sprite = _discSprite;
            baseImage.color =
                new Color(0.02f, 0.09f, 0.16f, 0.50f);
            baseImage.raycastTarget = true;

            GameObject knobGo = new GameObject(
                "Knob",
                typeof(RectTransform),
                typeof(Image));

            knobGo.transform.SetParent(baseGo.transform, false);

            RectTransform knobRect =
                knobGo.GetComponent<RectTransform>();

            knobRect.anchorMin =
                knobRect.anchorMax =
                    new Vector2(0.5f, 0.5f);

            knobRect.pivot =
                new Vector2(0.5f, 0.5f);

            knobRect.sizeDelta =
                new Vector2(96f, 96f);

            knobRect.anchoredPosition = Vector2.zero;

            Image knobImage = knobGo.GetComponent<Image>();
            knobImage.sprite = _discSprite;
            knobImage.color =
                new Color(0.15f, 0.72f, 1f, 0.72f);
            knobImage.raycastTarget = false;

            GameObject ringGo = new GameObject(
                "JoystickRing",
                typeof(RectTransform),
                typeof(Image));

            ringGo.transform.SetParent(baseGo.transform, false);

            RectTransform ringRect =
                ringGo.GetComponent<RectTransform>();

            ringRect.anchorMin =
                ringRect.anchorMax =
                    new Vector2(0.5f, 0.5f);

            ringRect.pivot =
                new Vector2(0.5f, 0.5f);

            ringRect.sizeDelta =
                new Vector2(222f, 222f);

            Image ringImage = ringGo.GetComponent<Image>();
            ringImage.sprite = _ringSprite;
            ringImage.color =
                new Color(0.20f, 0.75f, 1f, 0.46f);
            ringImage.raycastTarget = false;

            baseGo
                .GetComponent<HighflyMobileJoystick>()
                .Configure(knobRect, 86f);
        }

        private void CreateActionButton(
            Transform parent,
            string label,
            Vector2 anchoredPosition,
            Vector2 size,
            HighflyMobileAction action,
            int fontSize)
        {
            GameObject go = new GameObject(
                label,
                typeof(RectTransform),
                typeof(Image),
                typeof(HighflyMobileActionButton));

            go.transform.SetParent(parent, false);

            RectTransform rect =
                go.GetComponent<RectTransform>();

            rect.anchorMin =
                rect.anchorMax =
                    new Vector2(1f, 0f);

            rect.pivot =
                new Vector2(0.5f, 0.5f);

            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            Image image = go.GetComponent<Image>();
            image.sprite = _discSprite;
            image.color =
                new Color(0.015f, 0.035f, 0.07f, 0.82f);
            image.raycastTarget = true;

            GameObject ringGo = new GameObject(
                "Ring",
                typeof(RectTransform),
                typeof(Image));

            ringGo.transform.SetParent(go.transform, false);

            RectTransform ringRect =
                ringGo.GetComponent<RectTransform>();

            ringRect.anchorMin = Vector2.zero;
            ringRect.anchorMax = Vector2.one;
            ringRect.offsetMin = new Vector2(4f, 4f);
            ringRect.offsetMax = new Vector2(-4f, -4f);

            Image ring = ringGo.GetComponent<Image>();
            ring.sprite = _ringSprite;
            ring.color =
                action == HighflyMobileAction.Attack
                    ? new Color(0.10f, 0.85f, 1f, 0.90f)
                    : new Color(0.35f, 0.70f, 1f, 0.82f);
            ring.raycastTarget = false;

            GameObject textGo = new GameObject(
                "Text",
                typeof(RectTransform),
                typeof(Text));

            textGo.transform.SetParent(go.transform, false);

            RectTransform textRect =
                textGo.GetComponent<RectTransform>();

            Stretch(textRect);

            Text text = textGo.GetComponent<Text>();
            text.font = _font;
            text.text = label;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.94f, 0.98f, 1f, 0.97f);
            text.raycastTarget = false;

            go.GetComponent<HighflyMobileActionButton>()
                .Configure(action);
        }

        private static Sprite CreateRadialSprite(bool ringOnly)
        {
            const int size = 128;

            Texture2D texture =
                new Texture2D(
                    size,
                    size,
                    TextureFormat.RGBA32,
                    false);

            texture.name =
                ringOnly
                    ? "HF_CLEAN_RING"
                    : "HF_CLEAN_DISC";

            Color32[] pixels =
                new Color32[size * size];

            float center = (size - 1) * 0.5f;
            float outer = center - 1f;
            float inner = outer * 0.84f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);

                    byte a;

                    if (ringOnly)
                    {
                        float edge =
                            Mathf.Clamp01((outer - d) * 1.5f);

                        float hole =
                            Mathf.Clamp01((d - inner) * 1.5f);

                        a = (byte)(
                            255f *
                            Mathf.Clamp01(
                                Mathf.Min(edge, hole)));
                    }
                    else
                    {
                        a = (byte)(
                            255f *
                            Mathf.Clamp01(
                                (outer - d) * 1.5f));
                    }

                    pixels[y * size + x] =
                        new Color32(255, 255, 255, a);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
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
