using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Highfly.Clean
{
    public enum HighflyTouchOwner
    {
        None,
        Movement,
        Camera,
        Combat
    }

    public sealed class HighflyInputRouter : MonoBehaviour
    {
        private sealed class WebTouchTrack
        {
            public HighflyTouchOwner Owner;
            public Vector2 StartPixels;
            public Vector2 PreviousPixels;
        }

        public static HighflyInputRouter Instance { get; private set; }

        private readonly Dictionary<int, WebTouchTrack> _webTouches =
            new Dictionary<int, WebTouchTrack>();
        private readonly HashSet<int> _seenTouches = new HashSet<int>();
        private readonly List<int> _endedTouches = new List<int>();

        private Vector2 _mobileMove;
        private Vector2 _mobileLookDelta;
        private Vector2 _desktopLookDelta;
        private int _movementTouchId = int.MinValue;
        private int _cameraTouchId = int.MinValue;
        private bool _attackQueued;
        private bool _jumpQueued;

        public Vector2 Move { get; private set; }
        public Vector2 MobileStickVisual { get; private set; }

        private void Awake()
        {
            Instance = this;
            HighflyWebTouchBridge.Initialize();
        }

        private void Update()
        {
            Move = _mobileMove;
            _desktopLookDelta = Vector2.zero;
            ReadDesktop();

            if (HighflyWebTouchBridge.IsRuntimeWebGL)
                ReadWebTouches();
        }

        private void ReadDesktop()
        {
            if (Keyboard.current != null)
            {
                Vector2 m = Vector2.zero;
                if (Keyboard.current.wKey.isPressed) m.y += 1f;
                if (Keyboard.current.sKey.isPressed) m.y -= 1f;
                if (Keyboard.current.dKey.isPressed) m.x += 1f;
                if (Keyboard.current.aKey.isPressed) m.x -= 1f;

                if (m.sqrMagnitude > 0.001f)
                    Move = Vector2.ClampMagnitude(m, 1f);

                if (Keyboard.current.spaceKey.wasPressedThisFrame) _jumpQueued = true;
                if (Keyboard.current.fKey.wasPressedThisFrame) _attackQueued = true;
            }

            if (Mouse.current != null && Mouse.current.rightButton.isPressed)
                _desktopLookDelta += Mouse.current.delta.ReadValue();
        }

        private void ReadWebTouches()
        {
            _seenTouches.Clear();
            int count = HighflyWebTouchBridge.Count;

            for (int i = 0; i < count; i++)
            {
                int id;
                Vector2 normalized;
                if (!HighflyWebTouchBridge.TryGet(i, out id, out normalized)) continue;

                _seenTouches.Add(id);

                Vector2 pixels = new Vector2(
                    normalized.x * Screen.width,
                    normalized.y * Screen.height);

                WebTouchTrack track;
                if (!_webTouches.TryGetValue(id, out track))
                {
                    track = BeginWebTouch(id, normalized, pixels);
                    _webTouches[id] = track;
                }

                UpdateWebTouch(id, track, pixels);
            }

            _endedTouches.Clear();

            foreach (KeyValuePair<int, WebTouchTrack> pair in _webTouches)
                if (!_seenTouches.Contains(pair.Key))
                    _endedTouches.Add(pair.Key);

            for (int i = 0; i < _endedTouches.Count; i++)
            {
                int id = _endedTouches[i];
                EndWebTouch(id, _webTouches[id]);
                _webTouches.Remove(id);
            }
        }

        private WebTouchTrack BeginWebTouch(int id, Vector2 n, Vector2 pixels)
        {
            HighflyTouchOwner owner = HighflyTouchOwner.None;

            bool attackZone = n.x >= 0.87f && n.y <= 0.34f;
            bool jumpZone = n.x >= 0.74f && n.x < 0.87f && n.y <= 0.32f;

            if (attackZone)
            {
                owner = HighflyTouchOwner.Combat;
                _attackQueued = true;
            }
            else if (jumpZone)
            {
                owner = HighflyTouchOwner.Combat;
                _jumpQueued = true;
            }
            else if (n.x < 0.50f && _movementTouchId == int.MinValue)
            {
                owner = HighflyTouchOwner.Movement;
                _movementTouchId = id;
            }
            else if (n.x >= 0.50f && _cameraTouchId == int.MinValue)
            {
                owner = HighflyTouchOwner.Camera;
                _cameraTouchId = id;
            }

            return new WebTouchTrack
            {
                Owner = owner,
                StartPixels = pixels,
                PreviousPixels = pixels
            };
        }

        private void UpdateWebTouch(int id, WebTouchTrack track, Vector2 pixels)
        {
            if (track.Owner == HighflyTouchOwner.Movement && id == _movementTouchId)
            {
                float radius = Mathf.Max(80f, Screen.height * 0.16f);
                Vector2 raw = (pixels - track.StartPixels) / radius;
                Vector2 stick = Vector2.ClampMagnitude(raw, 1f);
                const float deadZone = 0.14f;

                if (stick.magnitude >= deadZone)
                {
                    float scaled = Mathf.InverseLerp(deadZone, 1f, stick.magnitude);
                    _mobileMove = stick.normalized * scaled;
                }
                else
                {
                    _mobileMove = Vector2.zero;
                }

                MobileStickVisual = stick;
            }
            else if (track.Owner == HighflyTouchOwner.Camera && id == _cameraTouchId)
            {
                Vector2 delta = pixels - track.PreviousPixels;
                float maxDelta = Mathf.Max(24f, Screen.height * 0.14f);
                delta.x = Mathf.Clamp(delta.x, -maxDelta, maxDelta);
                delta.y = Mathf.Clamp(delta.y, -maxDelta, maxDelta);
                _mobileLookDelta += delta;
            }

            track.PreviousPixels = pixels;
        }

        private void EndWebTouch(int id, WebTouchTrack track)
        {
            if (track.Owner == HighflyTouchOwner.Movement && id == _movementTouchId)
            {
                _movementTouchId = int.MinValue;
                _mobileMove = Vector2.zero;
                MobileStickVisual = Vector2.zero;
            }

            if (track.Owner == HighflyTouchOwner.Camera && id == _cameraTouchId)
                _cameraTouchId = int.MinValue;
        }

        public void SetMobileMove(Vector2 value)
        {
            if (HighflyWebTouchBridge.IsRuntimeWebGL) return;
            _mobileMove = Vector2.ClampMagnitude(value, 1f);
            MobileStickVisual = _mobileMove;
        }

        public void AddMobileLookDelta(Vector2 delta)
        {
            if (HighflyWebTouchBridge.IsRuntimeWebGL) return;
            _mobileLookDelta += delta;
        }

        public Vector2 ConsumeLookDelta()
        {
            Vector2 value = _desktopLookDelta + _mobileLookDelta;
            _desktopLookDelta = Vector2.zero;
            _mobileLookDelta = Vector2.zero;
            return value;
        }

        public bool ConsumeAttack()
        {
            bool value = _attackQueued;
            _attackQueued = false;
            return value;
        }

        public bool ConsumeJump()
        {
            bool value = _jumpQueued;
            _jumpQueued = false;
            return value;
        }

        public void QueueAttack() => _attackQueued = true;
        public void QueueJump() => _jumpQueued = true;

        private void OnDisable()
        {
            _mobileMove = Vector2.zero;
            MobileStickVisual = Vector2.zero;
            _mobileLookDelta = Vector2.zero;
            _desktopLookDelta = Vector2.zero;
            _webTouches.Clear();
            _movementTouchId = int.MinValue;
            _cameraTouchId = int.MinValue;
        }
    }
}
