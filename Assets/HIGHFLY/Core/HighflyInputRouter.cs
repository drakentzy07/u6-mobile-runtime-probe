using UnityEngine;
using UnityEngine.InputSystem;

namespace Highfly.Clean
{
    public sealed class HighflyInputRouter : MonoBehaviour
    {
        public static HighflyInputRouter Instance { get; private set; }

        private Vector2 _mobileMove;
        private Vector2 _mobileLookDelta;
        private Vector2 _desktopLookDelta;
        private bool _attackQueued;
        private bool _jumpQueued;

        public Vector2 Move { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            Move = _mobileMove;
            _desktopLookDelta = Vector2.zero;
            ReadDesktop();
        }

        private void ReadDesktop()
        {
            if (Keyboard.current != null)
            {
                Vector2 keyboardMove = Vector2.zero;

                if (Keyboard.current.wKey.isPressed)
                    keyboardMove.y += 1f;

                if (Keyboard.current.sKey.isPressed)
                    keyboardMove.y -= 1f;

                if (Keyboard.current.dKey.isPressed)
                    keyboardMove.x += 1f;

                if (Keyboard.current.aKey.isPressed)
                    keyboardMove.x -= 1f;

                if (keyboardMove.sqrMagnitude > 0.001f)
                    Move = Vector2.ClampMagnitude(
                        keyboardMove,
                        1f);

                if (Keyboard.current.spaceKey.wasPressedThisFrame)
                    _jumpQueued = true;

                if (Keyboard.current.fKey.wasPressedThisFrame)
                    _attackQueued = true;
            }

            if (Mouse.current != null &&
                Mouse.current.rightButton.isPressed)
            {
                _desktopLookDelta +=
                    Mouse.current.delta.ReadValue();
            }
        }

        public void SetMobileMove(Vector2 value)
        {
            _mobileMove =
                Vector2.ClampMagnitude(value, 1f);
        }

        public void AddMobileLookDelta(Vector2 screenDelta)
        {
            _mobileLookDelta += screenDelta;
        }

        public Vector2 ConsumeLookDelta()
        {
            Vector2 value =
                _desktopLookDelta +
                _mobileLookDelta;

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

        public void QueueAttack()
        {
            _attackQueued = true;
        }

        public void QueueJump()
        {
            _jumpQueued = true;
        }

        private void OnDisable()
        {
            _mobileMove = Vector2.zero;
            _mobileLookDelta = Vector2.zero;
            _desktopLookDelta = Vector2.zero;
        }
    }
}
