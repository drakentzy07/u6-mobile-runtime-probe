using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Highfly.World
{
    [DisallowMultipleComponent]
    public sealed class HighflyWorldCityRuntime : MonoBehaviour
    {
        public const string Version = "HIGHFLY_WORLD_CITY01_v0.3";
        private const string CitySceneName = "HIGHFLY_CITY01";
        private float _nextSweep;
        private GUIStyle _title;
        private GUIStyle _sub;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            ApplyPolicy(SceneManager.GetActiveScene());
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyPolicy(scene);
        }

        private void Update()
        {
            if (SceneManager.GetActiveScene().name != CitySceneName) return;
            if (Time.unscaledTime < _nextSweep) return;
            _nextSweep = Time.unscaledTime + 1.0f;
            RemoveHostiles();
        }

        private static void ApplyPolicy(Scene scene)
        {
            if (scene.name != CitySceneName) return;
            int removed = RemoveHostiles();
            Debug.Log($"[HIGHFLY-WORLD] {Version} CITY 01 loaded. SAFE ZONE. Removed {removed} hostile object(s).");
        }

        private static int RemoveHostiles()
        {
            int removed = 0;

            var enemies = FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                Destroy(enemy.gameObject);
                removed++;
            }

            var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var behaviour in behaviours)
            {
                if (behaviour == null) continue;
                Type type = behaviour.GetType();
                string name = type.Name ?? string.Empty;
                if (name.IndexOf("Enemy", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    name.IndexOf("Spawn", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    behaviour.enabled = false;
                }
            }

            return removed;
        }

        private void OnGUI()
        {
            if (SceneManager.GetActiveScene().name != CitySceneName) return;

            _title ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            _title.normal.textColor = Color.white;

            _sub ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft
            };
            _sub.normal.textColor = new Color(0.65f, 0.92f, 1f, 1f);

            GUI.Box(new Rect(12, 12, 360, 58), GUIContent.none);
            GUI.Label(new Rect(22, 16, 340, 24), "HIGHFLY WORLD v0.3 • CITY 01 • SAFE", _title);
            GUI.Label(new Rect(22, 40, 340, 20), "Mercado • Posada • Herrería • 0 monstruos", _sub);
        }
    }
}
