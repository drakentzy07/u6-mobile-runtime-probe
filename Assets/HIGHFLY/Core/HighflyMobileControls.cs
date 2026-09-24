using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Highfly.Clean
{
    public sealed class HighflyMobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private RectTransform _rect;
        private RectTransform _knob;
        private float _radius = 86f;
        private int _pointerId = int.MinValue;

        public void Configure(RectTransform knob, float radius)
        {
            _rect = transform as RectTransform;
            _knob = knob;
            _radius = radius;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (HighflyWebTouchBridge.IsRuntimeWebGL || _pointerId != int.MinValue) return;
            _pointerId = e.pointerId;
            UpdateStick(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (HighflyWebTouchBridge.IsRuntimeWebGL || e.pointerId != _pointerId) return;
            UpdateStick(e);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (HighflyWebTouchBridge.IsRuntimeWebGL || e.pointerId != _pointerId) return;
            _pointerId = int.MinValue;
            if (_knob != null) _knob.anchoredPosition = Vector2.zero;
            HighflyInputRouter.Instance?.SetMobileMove(Vector2.zero);
        }

        private void UpdateStick(PointerEventData e)
        {
            if (_rect == null) return;
            Vector2 local;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rect, e.position, e.pressEventCamera, out local)) return;

            Vector2 stick = Vector2.ClampMagnitude(local / Mathf.Max(1f, _radius), 1f);
            HighflyInputRouter.Instance?.SetMobileMove(stick);
        }
    }

    public sealed class HighflyMobileLookZone : MonoBehaviour, IPointerDownHandler, IInitializePotentialDragHandler, IDragHandler, IPointerUpHandler
    {
        private int _pointerId = int.MinValue;
        private Vector2 _last;

        public void OnInitializePotentialDrag(PointerEventData e) => e.useDragThreshold = false;

        public void OnPointerDown(PointerEventData e)
        {
            if (HighflyWebTouchBridge.IsRuntimeWebGL || _pointerId != int.MinValue) return;
            if (e.position.x < Screen.width * 0.50f) return;
            _pointerId = e.pointerId;
            _last = e.position;
            e.useDragThreshold = false;
        }

        public void OnDrag(PointerEventData e)
        {
            if (HighflyWebTouchBridge.IsRuntimeWebGL || e.pointerId != _pointerId) return;
            Vector2 current = e.position;
            HighflyInputRouter.Instance?.AddMobileLookDelta(current - _last);
            _last = current;
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (HighflyWebTouchBridge.IsRuntimeWebGL) return;
            if (e.pointerId == _pointerId) _pointerId = int.MinValue;
        }
    }

    public enum HighflyMobileAction { Attack, Jump }

    public sealed class HighflyMobileActionButton : MonoBehaviour, IPointerDownHandler
    {
        private HighflyMobileAction _action;

        public void Configure(HighflyMobileAction action) => _action = action;

        public void OnPointerDown(PointerEventData e)
        {
            if (HighflyWebTouchBridge.IsRuntimeWebGL) return;
            if (_action == HighflyMobileAction.Attack) HighflyInputRouter.Instance?.QueueAttack();
            else HighflyInputRouter.Instance?.QueueJump();
        }
    }

    public sealed class HighflyMobileControls : MonoBehaviour
    {
        private Font _font;
        private Sprite _disc;
        private Sprite _ring;
        private RectTransform _knob;

        private void Awake()
        {
            EnsureEventSystem();
            BuildUI();
            Debug.Log("[RUN0C] Mobile HUD • direct WebGL touch + native fallback.");
        }

        private void Update()
        {
            if (_knob == null || HighflyInputRouter.Instance == null) return;
            _knob.anchoredPosition = HighflyInputRouter.Instance.MobileStickVisual * 86f;
        }

        private static void EnsureEventSystem()
        {
            EventSystem es = EventSystem.current;
            if (es == null)
            {
                GameObject go = new GameObject("HIGHFLY_EVENT_SYSTEM");
                es = go.AddComponent<EventSystem>();
            }
            if (es.GetComponent<InputSystemUIInputModule>() == null)
                es.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        private void BuildUI()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _disc = CreateRadialSprite(false);
            _ring = CreateRadialSprite(true);

            GameObject canvasGo = new GameObject(
                "HIGHFLY_MOBILE_CONTROLS",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject root = new GameObject("Controls", typeof(RectTransform));
            root.transform.SetParent(canvasGo.transform, false);
            Stretch(root.GetComponent<RectTransform>());

            CreateLookZone(root.transform);
            CreateJoystick(root.transform);
            CreateButton(root.transform, "SALTO", new Vector2(-335f,135f), new Vector2(130f,130f), HighflyMobileAction.Jump, 22);
            CreateButton(root.transform, "ATQ", new Vector2(-145f,145f), new Vector2(176f,176f), HighflyMobileAction.Attack, 30);
        }

        private void CreateLookZone(Transform parent)
        {
            GameObject go = new GameObject("CAMERA_DERECHA", typeof(RectTransform), typeof(Image), typeof(HighflyMobileLookZone));
            go.transform.SetParent(parent, false);
            RectTransform r = go.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0.5f,0f);
            r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
            Image image = go.GetComponent<Image>();
            image.color = new Color(0f,0f,0f,0.001f);
            image.raycastTarget = true;
        }

        private void CreateJoystick(Transform parent)
        {
            GameObject baseGo = new GameObject("JOYSTICK_IZQUIERDA", typeof(RectTransform), typeof(Image), typeof(HighflyMobileJoystick));
            baseGo.transform.SetParent(parent, false);
            RectTransform r = baseGo.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = Vector2.zero;
            r.pivot = new Vector2(0.5f,0.5f);
            r.sizeDelta = new Vector2(230f,230f);
            r.anchoredPosition = new Vector2(170f,175f);
            Image image = baseGo.GetComponent<Image>();
            image.sprite = _disc;
            image.color = new Color(0.02f,0.09f,0.16f,0.50f);

            GameObject knobGo = new GameObject("Knob", typeof(RectTransform), typeof(Image));
            knobGo.transform.SetParent(baseGo.transform, false);
            _knob = knobGo.GetComponent<RectTransform>();
            _knob.anchorMin = _knob.anchorMax = new Vector2(0.5f,0.5f);
            _knob.pivot = new Vector2(0.5f,0.5f);
            _knob.sizeDelta = new Vector2(96f,96f);
            Image knobImage = knobGo.GetComponent<Image>();
            knobImage.sprite = _disc;
            knobImage.color = new Color(0.15f,0.72f,1f,0.72f);
            knobImage.raycastTarget = false;

            GameObject ringGo = new GameObject("Ring", typeof(RectTransform), typeof(Image));
            ringGo.transform.SetParent(baseGo.transform, false);
            RectTransform rr = ringGo.GetComponent<RectTransform>();
            rr.anchorMin = rr.anchorMax = new Vector2(0.5f,0.5f);
            rr.pivot = new Vector2(0.5f,0.5f);
            rr.sizeDelta = new Vector2(222f,222f);
            Image ri = ringGo.GetComponent<Image>();
            ri.sprite = _ring;
            ri.color = new Color(0.20f,0.75f,1f,0.46f);
            ri.raycastTarget = false;

            baseGo.GetComponent<HighflyMobileJoystick>().Configure(_knob, 86f);
        }

        private void CreateButton(Transform parent, string label, Vector2 position, Vector2 size, HighflyMobileAction action, int fontSize)
        {
            GameObject go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(HighflyMobileActionButton));
            go.transform.SetParent(parent, false);
            RectTransform r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(1f,0f);
            r.pivot = new Vector2(0.5f,0.5f);
            r.sizeDelta = size;
            r.anchoredPosition = position;
            Image image = go.GetComponent<Image>();
            image.sprite = _disc;
            image.color = new Color(0.015f,0.035f,0.07f,0.82f);

            GameObject ringGo = new GameObject("Ring", typeof(RectTransform), typeof(Image));
            ringGo.transform.SetParent(go.transform, false);
            RectTransform rr = ringGo.GetComponent<RectTransform>();
            Stretch(rr);
            rr.offsetMin = new Vector2(4f,4f);
            rr.offsetMax = new Vector2(-4f,-4f);
            Image ri = ringGo.GetComponent<Image>();
            ri.sprite = _ring;
            ri.color = action == HighflyMobileAction.Attack
                ? new Color(0.10f,0.85f,1f,0.90f)
                : new Color(0.35f,0.70f,1f,0.82f);
            ri.raycastTarget = false;

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            Stretch(textGo.GetComponent<RectTransform>());
            Text t = textGo.GetComponent<Text>();
            t.font = _font;
            t.text = label;
            t.fontSize = fontSize;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.raycastTarget = false;

            go.GetComponent<HighflyMobileActionButton>().Configure(action);
        }

        private static Sprite CreateRadialSprite(bool ringOnly)
        {
            const int size = 128;
            Texture2D tex = new Texture2D(size,size,TextureFormat.RGBA32,false);
            Color32[] pixels = new Color32[size*size];
            float center = (size-1)*0.5f;
            float outer = center-1f;
            float inner = outer*0.84f;

            for(int y=0;y<size;y++)
            for(int x=0;x<size;x++)
            {
                float dx=x-center, dy=y-center;
                float d=Mathf.Sqrt(dx*dx+dy*dy);
                float a = ringOnly
                    ? Mathf.Clamp01(Mathf.Min((outer-d)*1.5f,(d-inner)*1.5f))
                    : Mathf.Clamp01((outer-d)*1.5f);
                pixels[y*size+x]=new Color32(255,255,255,(byte)(255f*a));
            }

            tex.SetPixels32(pixels);
            tex.Apply(false,true);
            return Sprite.Create(tex,new Rect(0,0,size,size),new Vector2(0.5f,0.5f),100f);
        }

        private static void Stretch(RectTransform r)
        {
            r.anchorMin=Vector2.zero;
            r.anchorMax=Vector2.one;
            r.offsetMin=Vector2.zero;
            r.offsetMax=Vector2.zero;
        }
    }
}
