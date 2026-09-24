using UnityEngine;

namespace Highfly.Clean
{
    public sealed class HighflyRun0Hud : MonoBehaviour
    {
        private HighflyRun0Player _player;

        public void Bind(HighflyRun0Player player) => _player = player;

        private void OnGUI()
        {
            GUI.depth = -1000;
            GUI.Box(
                new Rect(16,14,620,132),
                "HIGHFLY SKILLS LAB SUPREMO • RUN 0C • UNITY 6000.6.2f1\n" +
                "DIRECT WEBGL TOUCH BRIDGE • joystick izquierda • cámara SOLO derecha\n" +
                "KAYKIT HUNTER • SINGLE SWORD • UAL2 Sword_Regular_A\n" +
                "PC: WASD • RMB cámara • F ataque • SPACE salto\n" +
                "Clip ataque: " +
                (_player != null && _player.Animation != null
                    ? _player.Animation.AttackClipName
                    : "cargando..."));
        }
    }
}
