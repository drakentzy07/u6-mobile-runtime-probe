using UnityEngine;
using UnityEngine.InputSystem;

namespace Highfly.Clean
{
    public sealed class HighflyInputRouter : MonoBehaviour
    {
        public static HighflyInputRouter Instance { get; private set; }
        public Vector2 Move { get; private set; }
        public Vector2 LookDelta { get; private set; }
        private bool _attackQueued;
        private bool _jumpQueued;

        private void Awake() => Instance = this;

        private void Update()
        {
            Move = Vector2.zero;
            LookDelta = Vector2.zero;
            ReadDesktop();
            ReadTouch();
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
                Move = Vector2.ClampMagnitude(m, 1f);
                if (Keyboard.current.spaceKey.wasPressedThisFrame) _jumpQueued = true;
                if (Keyboard.current.fKey.wasPressedThisFrame) _attackQueued = true;
            }

            if (Mouse.current != null && Mouse.current.rightButton.isPressed)
                LookDelta += Mouse.current.delta.ReadValue() * 0.11f;
        }

        private void ReadTouch()
        {
            Touchscreen screen = Touchscreen.current;
            if (screen == null) return;

            foreach (TouchControl touch in screen.touches)
            {
                if (!touch.press.isPressed) continue;

                Vector2 pos = touch.position.ReadValue();
                Vector2 start = touch.startPosition.ReadValue();
                Vector2 delta = touch.delta.ReadValue();

                bool attackZone =
                    pos.x > Screen.width * 0.80f &&
                    pos.y < Screen.height * 0.34f;

                bool jumpZone =
                    pos.x > Screen.width * 0.64f &&
                    pos.x <= Screen.width * 0.80f &&
                    pos.y < Screen.height * 0.30f;

                if (touch.press.wasPressedThisFrame && attackZone) _attackQueued = true;
                if (touch.press.wasPressedThisFrame && jumpZone) _jumpQueued = true;

                if (start.x < Screen.width * 0.46f)
                {
                    Vector2 virtualStick =
                        (pos - start) /
                        Mathf.Max(90f, Screen.height * 0.16f);

                    Move = Vector2.ClampMagnitude(virtualStick, 1f);
                }
                else if (!attackZone && !jumpZone)
                {
                    LookDelta += delta * 0.115f;
                }
            }
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
    }
}
