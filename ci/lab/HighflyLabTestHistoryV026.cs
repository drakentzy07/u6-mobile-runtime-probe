using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Highfly.SkillLab
{
    [DisallowMultipleComponent]
    public sealed class HighflyLabTestHistoryV026 : MonoBehaviour
    {
        private static readonly Queue<string> Entries = new Queue<string>();
        private static int _warnings;
        private static HighflyLabTestHistoryV026 _instance;

        private PlayerController _player;
        private bool _expanded;
        private float _nextSample;
        private GUIStyle _button;
        private GUIStyle _box;

        public static int WarningCount => _warnings;

        public static HighflyLabTestHistoryV026 Install(PlayerController player)
        {
            if (player == null) return null;
            var h = player.GetComponent<HighflyLabTestHistoryV026>();
            if (h == null) h = player.gameObject.AddComponent<HighflyLabTestHistoryV026>();
            h._player = player;
            _instance = h;
            Log("TEST HISTORY ONLINE • v2.7 KAYKIT HUNTER");
            return h;
        }

        public static void Log(string message)
        {
            string line = Time.unscaledTime.ToString("0000.00") + "  " + message;
            Entries.Enqueue(line);
            while (Entries.Count > 64) Entries.Dequeue();
        }

        public static void Flag(string message)
        {
            _warnings++;
            Log("WARN " + message);
            Debug.LogWarning("[HIGHFLY TEST] " + message);
        }

        public static string Snapshot()
        {
            var sb = new StringBuilder();
            sb.AppendLine("HIGHFLY DONOR LAB v2.7 TEST HISTORY");
            sb.AppendLine("warnings=" + _warnings);
            foreach (string e in Entries) sb.AppendLine(e);
            return sb.ToString();
        }

        private void Update()
        {
            if (_player == null || Time.unscaledTime < _nextSample) return;
            _nextSample = Time.unscaledTime + 0.50f;

            Animator a = _player.animator;
            string anim = "-";
            if (a != null && a.isActiveAndEnabled)
            {
                AnimatorStateInfo st = a.GetCurrentAnimatorStateInfo(0);
                anim = st.shortNameHash.ToString();
            }

            string owner = HighflyLabMotionGuardV026.Instance != null
                ? HighflyLabMotionGuardV026.Instance.Owner
                : "NO-GUARD";

            Vector3 rootForward = _player.transform.forward;
            Vector3 cameraForward = _player.cameraTransform != null
                ? _player.cameraTransform.forward
                : rootForward;
            Vector3 combatForward = HighflyCombatFacingV027.Forward(_player.transform);
            rootForward.y = cameraForward.y = combatForward.y = 0f;
            rootForward.Normalize();
            cameraForward.Normalize();
            combatForward.Normalize();

            Log(
                "STATE " + _player.currentState +
                " • owner=" + owner +
                " • grounded=" + _player.HighflyIsGrounded +
                " • anim=" + anim +
                " • rootCam=" + Vector3.Dot(rootForward, cameraForward).ToString("0.00") +
                " • combatRoot=" + Vector3.Dot(combatForward, rootForward).ToString("0.00") +
                " • pos=" + _player.transform.position.ToString("F1"));
        }

        private void OnGUI()
        {
            if (_button == null)
            {
                _button = new GUIStyle(GUI.skin.button) { fontSize = 18 };
                _box = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 15,
                    alignment = TextAnchor.UpperLeft,
                    wordWrap = false
                };
            }

            string label = _warnings > 0 ? "TEST LOG ⚠ " + _warnings : "TEST LOG ✓";
            if (GUI.Button(new Rect(12, 12, 170, 44), label, _button))
                _expanded = !_expanded;

            if (!_expanded) return;

            GUI.Box(new Rect(12, 62, Mathf.Min(Screen.width - 24, 760), Mathf.Min(Screen.height - 80, 430)),
                Tail(14), _box);

            if (GUI.Button(new Rect(190, 12, 150, 44), "RESET LOG", _button))
            {
                Entries.Clear();
                _warnings = 0;
                Log("LOG RESET");
            }
        }

        private static string Tail(int maxLines)
        {
            string[] arr = Entries.ToArray();
            int start = Mathf.Max(0, arr.Length - maxLines);
            var sb = new StringBuilder();
            sb.AppendLine("HISTORIAL TÉCNICO • minimizá con TEST LOG");
            sb.AppendLine("warnings=" + _warnings);
            for (int i = start; i < arr.Length; i++)
                sb.AppendLine(arr[i]);
            return sb.ToString();
        }
    }
}
