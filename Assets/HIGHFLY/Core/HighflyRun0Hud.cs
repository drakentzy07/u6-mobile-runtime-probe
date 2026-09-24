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
                new Rect(16, 14, 520, 112),
                "HIGHFLY COMBAT REBOOT • RUN 0A CLEAN ROOT\n" +
                "KAYKIT HUNTER • CLEAN MOTOR/CAMERA • UAL2 Sword_Regular_A\n" +
                "PC: WASD • RMB cámara • F ataque • SPACE salto\n" +
                "Clip ataque: " +
                (_player != null && _player.Animation != null
                    ? _player.Animation.AttackClipName
                    : "cargando..."));

            float attackW = Mathf.Max(120f, Screen.width * 0.12f);
            float attackH = Mathf.Max(72f, Screen.height * 0.14f);

            Rect attack =
                new Rect(
                    Screen.width - attackW - 24f,
                    Screen.height - attackH - 28f,
                    attackW,
                    attackH);

            if (GUI.Button(attack, "ATAQUE"))
                HighflyInputRouter.Instance?.QueueAttack();

            Rect jump =
                new Rect(
                    Screen.width - attackW * 2.05f - 34f,
                    Screen.height - attackH * 0.82f - 28f,
                    attackW * 0.88f,
                    attackH * 0.82f);

            if (GUI.Button(jump, "SALTO"))
                HighflyInputRouter.Instance?.QueueJump();

            GUI.Box(
                new Rect(28f, Screen.height - 180f, 180f, 150f),
                "JOYSTICK\nIZQUIERDO");

            GUI.Label(
                new Rect(Screen.width * 0.52f, Screen.height - 60f, 300f, 40f),
                "LADO DERECHO = CÁMARA");
        }
    }
}
