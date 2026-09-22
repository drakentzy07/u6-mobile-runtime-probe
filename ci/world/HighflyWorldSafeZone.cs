using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Highfly.World
{
    /// <summary>
    /// HIGHFLY WORLD v0.1A
    /// Keeps the starting city of Lucid/Somnia as a SAFE ZONE without
    /// changing Lucid's player, joystick, camera or combat code.
    /// Enemies that spawn or enter the city are moved to validated
    /// exterior combat anchors.
    /// </summary>
    public sealed class HighflyWorldSafeZone : MonoBehaviour
    {
        public const string Version = "HIGHFLY_WORLD_v0.1A";

        // Lucid Somnia player start / initial town reference.
        private static readonly Vector3 CityCenter = new Vector3(-13.8f, 0f, -19.34f);
        private const float SafeRadius = 62f;
        private const float GuardInterval = 0.40f;

        // Existing exterior/NavMesh combat area anchors audited from Somnia.
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (SceneManager.GetActiveScene().name != "Somnia") return;
            if (FindFirstObjectByType<HighflyWorldSafeZone>() != null) return;

            var go = new GameObject("HIGHFLY_WORLD_SAFE_ZONE");
            go.AddComponent<HighflyWorldSafeZone>();
        }

        private void Start()
        {
            _movedOnBoot = SanitizeCity();
            Debug.Log($"[HIGHFLY-WORLD] {Version} SAFE ZONE active. Relocated {_movedOnBoot} enemy spawn(s) outside town.");
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextGuard) return;
            _nextGuard = Time.unscaledTime + GuardInterval;
            SanitizeCity();
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dz = a.z - b.z;
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
            Vector3 desired = ExteriorAnchors[index % ExteriorAnchors.Length];

            if (NavMesh.SamplePosition(desired, out NavMeshHit hit, 14f, NavMesh.AllAreas))
                desired = hit.position;

            var agent = enemy.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.Warp(desired);
            }
            else
            {
                enemy.transform.position = desired;
            }

            try
            {
                enemy.ChangeState(EnemyState.Patrol);
            }
            catch
            {
                // Enemy may still be in its Awake/enable lifecycle; position is already safe.
            }
        }

        private void OnGUI()
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

            GUI.Box(new Rect(12, 12, 285, 34), GUIContent.none);
            GUI.Label(new Rect(16, 14, 278, 30), "HIGHFLY WORLD v0.1A  •  SAFE ZONE", _labelStyle);
        }
    }
}
