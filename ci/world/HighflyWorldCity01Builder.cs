#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
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
        private const string PlayerBaseStatsGuid = "92cc0753dc9c64ecd8aa1b624171ad8c";
        private const string PlayerWeaponDataGuid = "16ccb54be70294c84993bfaeaf8f5e5e";
        private const string PlayerSkillGuid = "79402f9292d034e19901e497040863ba";
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
            PlayerSettings.WebGL.dataCaching = false;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.FullWithStacktrace;
            PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Embedded;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.Minimal);

            const string outputPath = "build/WebGL";
            var options = new BuildPlayerOptions
            {
                scenes = new[] { OutputScene },
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.Development
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

            RemoveSceneCameras(scene);
            RemoveLegacyCinemachineAndDemoScripts(scene);
            RemoveHostiles(scene);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab == null)
                throw new Exception("[HF-WORLD] Could not load Player prefab.");

            var player = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (player == null)
                throw new Exception("[HF-WORLD] Could not instantiate Player prefab.");

            player.name = "HIGHFLY_PLAYER";
            player.transform.SetPositionAndRotation(Spawn, Quaternion.Euler(0f, 155f, 0f));
            RestoreLucidPlayerSceneOverrides(player);

            var runtime = new GameObject("HIGHFLY_CITY01_RUNTIME");
            SceneManager.MoveGameObjectToScene(runtime, scene);
            runtime.AddComponent<Highfly.World.HighflyWorldCityRuntime>();

            CreateCamera(scene);
            BindPlayerCamera(player);
            ReplaceWaterForWebGL(scene);
            CreateWorldBoundaryFallback(scene);

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

        private static void RestoreLucidPlayerSceneOverrides(GameObject player)
        {
            var controller = player.GetComponent<PlayerController>();
            var stats = player.GetComponent<PlayerStats>();
            if (controller == null || stats == null)
                throw new Exception("[HF-WORLD] Lucid PlayerController/PlayerStats missing.");

            string statsPath = AssetDatabase.GUIDToAssetPath(PlayerBaseStatsGuid);
            string weaponPath = AssetDatabase.GUIDToAssetPath(PlayerWeaponDataGuid);
            string skillPath = AssetDatabase.GUIDToAssetPath(PlayerSkillGuid);

            var baseStats = AssetDatabase.LoadAssetAtPath<PlayerBaseStatsSO>(statsPath);
            var weaponData = AssetDatabase.LoadAssetAtPath<WeaponData>(weaponPath);
            var activeSkill = AssetDatabase.LoadAssetAtPath<SkillData>(skillPath);

            if (baseStats == null) throw new Exception("[HF-WORLD] PlayerBaseStatsSO missing: " + statsPath);
            if (weaponData == null) throw new Exception("[HF-WORLD] WeaponData missing: " + weaponPath);
            if (activeSkill == null) throw new Exception("[HF-WORLD] SkillData missing: " + skillPath);

            stats.baseStats = baseStats;
            stats._playerController = controller;

            controller.rollVolitionCost = 15f;
            controller.attackVolitionCost = 10f;
            controller.activeSkill = activeSkill;

            var oldWeapons = player.GetComponentsInChildren<Weapon>(true);
            GameObject weaponObject = null;
            BoxCollider weaponCollider = null;
            foreach (var old in oldWeapons)
            {
                if (old == null) continue;
                if (old is PlayerWeapon)
                {
                    controller.myWeapon = (PlayerWeapon)old;
                    continue;
                }

                if (weaponObject == null)
                {
                    weaponObject = old.gameObject;
                    weaponCollider = old.GetComponent<BoxCollider>();
                    if (weaponCollider == null)
                        weaponCollider = old.GetComponentInChildren<BoxCollider>(true);
                }

                UnityEngine.Object.DestroyImmediate(old);
            }

            if (controller.myWeapon == null)
            {
                if (weaponObject == null)
                    throw new Exception("[HF-WORLD] Weapon Hitbox object missing on Lucid prefab.");

                var playerWeapon = weaponObject.AddComponent<PlayerWeapon>();
                playerWeapon.weaponData = weaponData;
                playerWeapon.ownerTag = "Player";
                playerWeapon._collider = weaponCollider;
                playerWeapon.damageMultiplier = 1f;
                controller.myWeapon = playerWeapon;
            }

            var cameraRootGo = new GameObject("HIGHFLY Lock Camera Root");
            cameraRootGo.transform.SetParent(player.transform, false);
            cameraRootGo.transform.localPosition = new Vector3(0f, 1.7f, 0f);
            controller.cameraRoot = cameraRootGo.transform;

            if (player.GetComponent<PlayerLockOn>() == null)
            {
                var lockOn = player.AddComponent<PlayerLockOn>();
                lockOn.detectionRadius = 10f;
                lockOn.maxLockOnDistance = 15f;
                lockOn.enemyLayer = 1 << 3;
            }

            if (player.GetComponent<PlayerWallet>() == null)
                player.AddComponent<PlayerWallet>();

            Debug.Log("[HF-WORLD] Restored Lucid Somnia player overrides: stats + PlayerWeapon + active skill + lock-on.");
        }

        private static void BindPlayerCamera(GameObject player)
        {
            var controller = player.GetComponent<PlayerController>();
            var camera = Camera.main;
            if (controller == null || camera == null) return;
            controller.cameraTransform = camera.transform;
        }

        private static void ReplaceWaterForWebGL(Scene scene)
        {
            const string materialPath = "Assets/HIGHFLY/World/HIGHFLY_Water_WebGL.mat";
            Material water = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            if (water == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                    shader = Shader.Find("Universal Render Pipeline/Simple Lit");
                if (shader == null)
                    throw new Exception("[HF-WORLD] Could not find a URP fallback shader for water.");

                water = new Material(shader) { name = "HIGHFLY_Water_WebGL" };
                if (water.HasProperty("_BaseColor"))
                    water.SetColor("_BaseColor", new Color(0.04f, 0.32f, 0.58f, 1f));
                if (water.HasProperty("_Color"))
                    water.SetColor("_Color", new Color(0.04f, 0.32f, 0.58f, 1f));
                if (water.HasProperty("_Smoothness"))
                    water.SetFloat("_Smoothness", 0.88f);
                if (water.HasProperty("_Metallic"))
                    water.SetFloat("_Metallic", 0.05f);

                Directory.CreateDirectory("Assets/HIGHFLY/World");
                AssetDatabase.CreateAsset(water, materialPath);
                AssetDatabase.SaveAssets();
            }

            int replacements = 0;
            var renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var renderer in renderers)
            {
                if (renderer == null || renderer.gameObject.scene != scene) continue;

                var mats = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material mat = mats[i];
                    if (mat == null) continue;
                    if (mat.name.IndexOf("Water IS", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    mats[i] = water;
                    changed = true;
                    replacements++;
                }
                if (changed) renderer.sharedMaterials = mats;
            }

            Debug.Log($"[HF-WORLD] WebGL water fallback applied to {replacements} material slot(s).");
        }

        private static void CreateWorldBoundaryFallback(Scene scene)
        {
            var terrain = UnityEngine.Object.FindFirstObjectByType<Terrain>(FindObjectsInactive.Include);
            if (terrain == null || terrain.gameObject.scene != scene)
            {
                Debug.LogWarning("[HF-WORLD] Terrain not found; boundary fallback skipped.");
                return;
            }

            Vector3 size = terrain.terrainData.size;
            Vector3 origin = terrain.transform.position;

            var baseGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseGo.name = "HIGHFLY_WORLD_EDGE_BASE";
            SceneManager.MoveGameObjectToScene(baseGo, scene);
            baseGo.transform.position = origin + new Vector3(size.x * 0.5f, -0.75f, size.z * 0.5f);
            baseGo.transform.localScale = new Vector3(size.x * 3.0f, 1.0f, size.z * 3.0f);

            var col = baseGo.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.DestroyImmediate(col);

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
                const string edgeMatPath = "Assets/HIGHFLY/World/HIGHFLY_EdgeGround.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(edgeMatPath);
                if (mat == null)
                {
                    mat = new Material(shader) { name = "HIGHFLY_EdgeGround" };
                    if (mat.HasProperty("_BaseColor"))
                        mat.SetColor("_BaseColor", new Color(0.08f, 0.18f, 0.08f, 1f));
                    AssetDatabase.CreateAsset(mat, edgeMatPath);
                    AssetDatabase.SaveAssets();
                }
                baseGo.GetComponent<Renderer>().sharedMaterial = mat;
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.55f, 0.68f, 0.78f, 1f);
            RenderSettings.fogStartDistance = 105f;
            RenderSettings.fogEndDistance = 210f;

            Debug.Log($"[HF-WORLD] Boundary fallback + fog enabled. Terrain size={size}");
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

        private static void RemoveLegacyCinemachineAndDemoScripts(Scene scene)
        {
            var behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var cameraRigObjects = new HashSet<GameObject>();
            int rotatorsRemoved = 0;

            foreach (var behaviour in behaviours)
            {
                if (behaviour == null || behaviour.gameObject.scene != scene) continue;

                Type type = behaviour.GetType();
                string fullName = type.FullName ?? type.Name;

                if (fullName.IndexOf("Cinemachine", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    cameraRigObjects.Add(behaviour.gameObject);
                    continue;
                }

                if (string.Equals(type.Name, "Rotator", StringComparison.Ordinal))
                {
                    UnityEngine.Object.DestroyImmediate(behaviour);
                    rotatorsRemoved++;
                }
            }

            int rigsRemoved = 0;
            foreach (var go in cameraRigObjects)
            {
                if (go == null || go.scene != scene) continue;
                UnityEngine.Object.DestroyImmediate(go);
                rigsRemoved++;
            }

            Debug.Log($"[HF-WORLD] Removed legacy demo runtime: Cinemachine rigs={rigsRemoved}, Rotator scripts={rotatorsRemoved}");
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
