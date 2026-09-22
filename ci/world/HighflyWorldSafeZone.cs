using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Highfly.World
{
    /// <summary>
    /// HIGHFLY WORLD v0.1B
    /// WORLD-only runtime layer over Lucid/Somnia.
    /// - Boots correctly when Somnia is loaded after MainMenu.
    /// - Keeps the starting town as a SAFE ZONE.
    /// - Relocates common enemies outside the city radius.
    /// - Adds temporary lab shortcuts to jump CITY <-> FIELD for fast validation.
    /// </summary>
    public sealed class HighflyWorldSafeZone : MonoBehaviour
    {
        public const string Version = "HIGHFLY_WORLD_v0.1B";

        private static readonly Vector3 CityCenter = new Vector3(-13.8f, 0.31f, -19.34f);
        private const float SafeRadius = 62f;
        private const float GuardInterval = 0.40f;

        private static readonly Vector3[] ExteriorAnchors =
        {
            new Vector3(-61.2f, 0f, 39.8f),
            new Vector3(-32.5f, 0f, 53.1f),
            new Vector3(15.3f, 0f, 57.9f),
            new Vector3(30.4f, 0f, 59.1f),
            new Vector3(-52.0f, 0f, 54.0f),
            new Vector3(23.0f, 0f, 66.0f),
        };

        private float _nextGuard;
        private int _movedOnBoot;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallSceneHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Somnia") return;
            if (FindFirstObjectByType<HighflyWorldSafeZone>() != null) return;

            var go = new GameObject("HIGHFLY_WORLD_SAFE_ZONE");
            go.AddComponent<HighflyWorldSafeZone>();
        }

        private void Start()
        {
            _movedOnBoot = SanitizeCity();
            Debug.Log($"[HIGHFLY-WORLD] {Version} ACTIVE. Relocated {_movedOnBoot} enemy spawn(s) outside SAFE ZONE.");
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextGuard) return;
            _nextGuard = Time.unscaledTime + GuardInterval;
            SanitizeCity();
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private int SanitizeCity()
        {
            var enemies = FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int moved = 0;
            int anchorIndex = 0;

            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;
                if (FlatDistance(enemy.transform.position, CityCenter) >= SafeRadius) continue;

                MoveEnemyOutside(enemy, anchorIndex++);
                moved++;
            }

            return moved;
        }

        private static void MoveEnemyOutside(Enemy enemy, int index)
        {
            Vector3 desired = GetNavPosition(ExteriorAnchors[index % ExteriorAnchors.Length]);

            var agent = enemy.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled && agent.isOnNavMesh)
                agent.Warp(desired);
            else
                enemy.transform.position = desired;

            try { enemy.ChangeState(EnemyState.Patrol); }
            catch { }
        }

        private static Vector3 GetNavPosition(Vector3 desired)
        {
            if (NavMesh.SamplePosition(desired, out NavMeshHit hit, 14f, NavMesh.AllAreas))
                return hit.position;
            return desired;
        }

        private static void TeleportPlayer(Vector3 desired)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            desired = GetNavPosition(desired) + Vector3.up * 0.35f;

            var cc = player.GetComponent<CharacterController>();
            bool restoreController = cc != null && cc.enabled;
            if (restoreController) cc.enabled = false;

            player.transform.position = desired;

            if (restoreController) cc.enabled = true;
        }

        private void EnsureStyles()
        {
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 15,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(10, 10, 5, 5)
                };
                _labelStyle.normal.textColor = Color.white;
            }

            if (_buttonStyle == null)
            {
                _buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold
                };
            }
        }

        private void OnGUI()
        {
            EnsureStyles();

            GUI.Box(new Rect(12, 12, 300, 34), GUIContent.none);
            GUI.Label(new Rect(16, 14, 294, 30), "HIGHFLY WORLD v0.1B  •  SAFE ZONE", _labelStyle);

            float x = Mathf.Max(12f, Screen.width - 205f);
            if (GUI.Button(new Rect(x, 12, 193, 42), "SALIR AL EXTERIOR", _buttonStyle))
                TeleportPlayer(ExteriorAnchors[0]);

            if (GUI.Button(new Rect(x, 60, 193, 42), "VOLVER A CIUDAD", _buttonStyle))
                TeleportPlayer(CityCenter);
        }
    }
}
