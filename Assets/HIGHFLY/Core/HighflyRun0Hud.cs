using UnityEngine;

namespace Highfly.Clean
{
    public sealed class HighflyRun0Hud : MonoBehaviour
    {
        private HighflyRun0Player _player;

        public void Bind(HighflyRun0Player player)
        {
            _player = player;
        }

        private void OnGUI()
        {
            GUI.depth = -1000;

            GUI.Box(
                new Rect(
                    16,
                    14,
                    570,
                    126),
                "HIGHFLY SKILLS LAB SUPREMO • RUN 0B MOBILE INPUT\n" +
                "KAYKIT HUNTER • GOLDEN JOYSTICK/CAMERA • UAL2 Sword_Regular_A\n" +
                "MÓVIL: joystick izquierda • cámara SOLO derecha • multitouch\n" +
                "PC: WASD • RMB cámara • F ataque • SPACE salto\n" +
                "Clip ataque: " +
                (_player != null &&
                 _player.Animation != null
                    ? _player.Animation.AttackClipName
                    : "cargando..."));
        }
    }
}
