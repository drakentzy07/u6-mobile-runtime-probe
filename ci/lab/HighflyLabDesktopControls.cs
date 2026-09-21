using UnityEngine;
using UnityEngine.InputSystem;
using Highfly.Mobile;
using Highfly.Combat;

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

                // Fast LAB keyboard: 1..5 skills, 6 dodge, 7 parry,
                // 8 lock, 9 potion, 0 basic attack.
                HighflyLucidCombatBridge combat =
                    UnityEngine.Object.FindFirstObjectByType<HighflyLucidCombatBridge>();

                if (keyboard.digit1Key.wasPressedThisFrame)
                    combat?.Request(HighflyCombatAction.Skill1);
                if (keyboard.digit2Key.wasPressedThisFrame)
                    combat?.Request(HighflyCombatAction.Skill2);
                if (keyboard.digit3Key.wasPressedThisFrame)
                    combat?.Request(HighflyCombatAction.Skill3);
                if (keyboard.digit4Key.wasPressedThisFrame)
                    combat?.Request(HighflyCombatAction.Skill4);
                if (keyboard.digit5Key.wasPressedThisFrame)
                    combat?.Request(HighflyCombatAction.Ultimate);

                if (keyboard.digit6Key.wasPressedThisFrame)
                    combat?.Request(HighflyCombatAction.Dodge);
                if (keyboard.digit7Key.wasPressedThisFrame)
                    combat?.Request(HighflyCombatAction.Parry);
                if (keyboard.digit8Key.wasPressedThisFrame)
                    _player.HighflyMobileLockOn();
                if (keyboard.digit9Key.wasPressedThisFrame)
                    UnityEngine.Object.FindFirstObjectByType<PlayerPotion>()?.HighflyMobileUsePotion();
                if (keyboard.digit0Key.wasPressedThisFrame)
                    combat?.Request(HighflyCombatAction.Light);

                // R = instant recenter to the approved Golden third-person framing.
                if (keyboard.rKey.wasPressedThisFrame)
                    HighflyThirdPersonMobileCamera.Instance?.SnapBehindPlayer(13f);
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
