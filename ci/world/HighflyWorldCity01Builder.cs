#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Highfly.CI
{
    public static class WorldCity01Builder
    {
        private const string SourceScene = "Assets/Stylized Medieval Kingdom URP/Scenes/Demo Stylized Medieval.unity";
        private const string OutputScene = "Assets/HIGHFLY/World/Scenes/HIGHFLY_CITY01.unity";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        private const string PackRoot = "Assets/Stylized Medieval Kingdom URP";
        private static readonly Vector3 Spawn = new Vector3(4.5f, 13.35f, -9.5f);

        public static void Build()
        {
            if (!File.Exists(SourceScene))
                throw new Exception("[HF-WORLD] Medieval source scene missing: " + SourceScene);
            if (!File.Exists(PlayerPrefabPath))
                throw new Exception("[HF-WORLD] Lucid Player prefab missing: " + PlayerPrefabPath);

            OptimizeWorldTextures();
            PrepareCityScene();

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.dataCaching = true;

            const string outputPath = "build/WebGL";
            var options = new BuildPlayerOptions
            {
                scenes = new[] { OutputScene },
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            Debug.Log("[HF-WORLD] Building CITY 01 only (no MainMenu, no Somnia, no prologue).");
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("[HF-WORLD] CITY 01 WebGL build failed: " + report.summary.result);

            Debug.Log("[HF-WORLD] CITY 01 build succeeded. Size: " + report.summary.totalSize);
        }

        private static void OptimizeWorldTextures()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { PackRoot });
            int changed = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                bool dirty = false;
                if (importer.maxTextureSize > 1024)
                {
                    importer.maxTextureSize = 1024;
                    dirty = true;
                }

                if (importer.textureCompression == TextureImporterCompression.Uncompressed)
                {
                    importer.textureCompression = TextureImporterCompression.Compressed;
                    dirty = true;
                }

                if (dirty)
                {
                    importer.SaveAndReimport();
                    changed++;
                }
            }

            Debug.Log($"[HF-WORLD] Mobile texture pass complete. changed={changed}/{guids.Length}");
        }

        private static void PrepareCityScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(OutputScene));

            Scene scene = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Single);
            scene.name = "HIGHFLY_CITY01";

            RemoveSceneCameras(scene);
            RemoveHostiles(scene);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab == null)
                throw new Exception("[HF-WORLD] Could not load Player prefab.");

            var player = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (player == null)
                throw new Exception("[HF-WORLD] Could not instantiate Player prefab.");

            player.name = "HIGHFLY_PLAYER";
            player.transform.SetPositionAndRotation(Spawn, Quaternion.Euler(0f, 155f, 0f));

            var runtime = new GameObject("HIGHFLY_CITY01_RUNTIME");
            SceneManager.MoveGameObjectToScene(runtime, scene);
            runtime.AddComponent<Highfly.World.HighflyWorldCityRuntime>();

            CreateCamera(scene);

            LogLandmark("Inn");
            LogLandmark("Smithy");
            LogLandmark("Market Stall 1");
            LogLandmark("Market Stall 2");
            LogLandmark("Market Stall 3");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, OutputScene, true))
                throw new Exception("[HF-WORLD] Failed to save CITY 01 scene.");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[HF-WORLD] HIGHFLY_CITY01 prepared at " + OutputScene);
            Debug.Log("[HF-WORLD] Player spawn = " + Spawn);
        }

        private static void RemoveSceneCameras(Scene scene)
        {
            var cameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var camera in cameras)
            {
                if (camera == null || camera.gameObject.scene != scene) continue;
                UnityEngine.Object.DestroyImmediate(camera.gameObject);
            }
        }

        private static void RemoveHostiles(Scene scene)
        {
            var enemies = UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var enemy in enemies)
            {
                if (enemy == null || enemy.gameObject.scene != scene) continue;
                UnityEngine.Object.DestroyImmediate(enemy.gameObject);
            }
        }

        private static void CreateCamera(Scene scene)
        {
            var go = new GameObject("HIGHFLY Main Camera", typeof(Camera), typeof(AudioListener));
            SceneManager.MoveGameObjectToScene(go, scene);
            go.tag = "MainCamera";

            Vector3 target = Spawn + Vector3.up * 1.5f;
            go.transform.position = Spawn + new Vector3(0f, 2.5f, -5.6f);
            go.transform.rotation = Quaternion.LookRotation((target - go.transform.position).normalized, Vector3.up);

            var camera = go.GetComponent<Camera>();
            camera.fieldOfView = 62f;
            camera.nearClipPlane = 0.08f;
            camera.farClipPlane = 650f;
        }

        private static void LogLandmark(string name)
        {
            foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go == null || go.name != name) continue;
                Debug.Log($"[HF-WORLD] Landmark {name} @ {go.transform.position}");
                return;
            }
            Debug.LogWarning("[HF-WORLD] Landmark not found: " + name);
        }
    }
}
#endif
