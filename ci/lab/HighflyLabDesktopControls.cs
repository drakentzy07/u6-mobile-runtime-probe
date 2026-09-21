using UnityEngine;
using UnityEngine.InputSystem;
using Highfly.Mobile;

namespace Highfly.SkillLab
{
    /// <summary>
    /// Desktop convenience layer for the LAB only.
    /// It feeds the exact same Golden mobile camera and mobile movement bridge:
    /// WASD -> SetHighflyMobileMove, RMB drag -> HighflyThirdPersonMobileCamera.AddLookDelta.
    /// No second camera/controller is created.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HighflyLabDesktopControls : MonoBehaviour
    {
        private PlayerController _player;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
        }

        private void Update()
        {
            if (!HighflySkillLabMode.IsActive || _player == null) return;
            if (Application.isMobilePlatform) return;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                Vector2 move = Vector2.zero;
                if (keyboard.aKey.isPressed) move.x -= 1f;
                if (keyboard.dKey.isPressed) move.x += 1f;
                if (keyboard.sKey.isPressed) move.y -= 1f;
                if (keyboard.wKey.isPressed) move.y += 1f;
                move = Vector2.ClampMagnitude(move, 1f);
                _player.SetHighflyMobileMove(move);
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                HighflyThirdPersonMobileCamera.Instance?.AddLookDelta(delta);
            }
        }

        private void OnDisable()
        {
            if (_player != null && !Application.isMobilePlatform)
                _player.SetHighflyMobileMove(Vector2.zero);
        }
    }
}
