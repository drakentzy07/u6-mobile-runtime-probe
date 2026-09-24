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
                new Rect(16,14,610,138),
                "HIGHFLY SKILLS LAB SUPREMO • RUN 0D • UNITY 6000.6.2\n" +
                "DIRECT WEBGL TOUCH BRIDGE • joystick izquierda • cámara SOLO derecha\n" +
                "KAYKIT HUNTER • UAL2 HUMANOID ROOT BAKED • SINGLE SWORD\n" +
                "MÓVIL: joystick + cámara + multitouch • ATQ / SALTO\n" +
                "Clip ataque: " +
                (_player != null && _player.Animation != null
                    ? _player.Animation.AttackClipName
                    : "cargando..."));
        }
    }
}
