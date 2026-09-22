using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Highfly.World
{
    /// <summary>
    /// HIGHFLY WORLD v0.1B
    /// Persistent world bootstrap. Somnia is treated as the temporary
    /// CITY_01 test scene: no hostile enemies are allowed inside the safe zone.
    /// </summary>
    public sealed class HighflyWorldSafeZone : MonoBehaviour
    {
        public const string Version = "HIGHFLY_WORLD_v0.1B";

        private static HighflyWorldSafeZone _instance;

        private static readonly Vector3 CityCenter = new Vector3(-13.8f, 0f, -19.34f);
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
        private bool _worldSceneActive;
        private GUIStyle _labelStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null) return;

            var go = new GameObject("HIGHFLY_WORLD_BOOTSTRAP");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<HighflyWorldSafeZone>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                _instance = null;
            }
        }

        private void Start()
        {
            ApplyScenePolicy(SceneManager.GetActiveScene());
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyScenePolicy(scene);
        }

        private void ApplyScenePolicy(Scene scene)
        {
            _worldSceneActive = scene.name == "Somnia";
            if (!_worldSceneActive) return;

            int moved = SanitizeCity();
            Debug.Log($"[HIGHFLY-WORLD] {Version} CITY_01 SAFE ZONE active. Relocated {moved} hostile spawn(s).");
        }

        private void Update()
        {
            if (!_worldSceneActive) return;
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
            var enemies = FindObjectsByType<Enemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            int moved = 0;
            int anchorIndex = 0;

            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
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
                agent.Warp(desired);
            else
                enemy.transform.position = desired;

            if (enemy.currentState != EnemyState.Die)
            {
                try { enemy.ChangeState(EnemyState.Patrol); }
                catch { }
            }
        }

        private void OnGUI()
        {
            if (!_worldSceneActive) return;

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

            GUI.Box(new Rect(12, 12, 330, 34), GUIContent.none);
            GUI.Label(new Rect(16, 14, 322, 30), "HIGHFLY WORLD v0.1B  •  CITY SAFE ZONE", _labelStyle);
        }
    }
}
