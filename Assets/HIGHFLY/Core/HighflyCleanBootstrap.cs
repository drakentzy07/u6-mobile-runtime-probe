using UnityEngine;

namespace Highfly.Clean
{
    public sealed class HighflyCleanBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            BuildArena();

            GameObject inputGo =
                new GameObject("HIGHFLY_INPUT");

            inputGo.AddComponent<HighflyInputRouter>();

            GameObject cameraGo =
                new GameObject("HIGHFLY_CAMERA");

            Camera camera =
                cameraGo.AddComponent<Camera>();

            camera.clearFlags =
                CameraClearFlags.SolidColor;

            camera.backgroundColor =
                new Color(
                    0.62f,
                    0.68f,
                    0.76f,
                    1f);

            camera.nearClipPlane = 0.08f;
            cameraGo.tag = "MainCamera";

            HighflyCameraRig cameraRig =
                cameraGo.AddComponent<HighflyCameraRig>();

            GameObject playerGo =
                new GameObject("HIGHFLY_PLAYER");

            playerGo.transform.position =
                new Vector3(
                    0f,
                    0.05f,
                    -3.8f);

            playerGo.AddComponent<CharacterController>();

            HighflyRun0Player player =
                playerGo.AddComponent<HighflyRun0Player>();

            player.Initialize(cameraRig);
            cameraRig.Bind(playerGo.transform);

            HighflyRun0Hud hud =
                gameObject.AddComponent<HighflyRun0Hud>();

            hud.Bind(player);

            gameObject.AddComponent<HighflyMobileControls>();

            Debug.Log(
                "[CLEAN-RUN0B] Bootstrap complete • " +
                "golden mobile controls restored.");
        }

        private static void BuildArena()
        {
            Material floorMat =
                CreateMaterial(
                    new Color(
                        0.56f,
                        0.59f,
                        0.63f));

            Material wallMat =
                CreateMaterial(
                    new Color(
                        0.78f,
                        0.80f,
                        0.84f));

            Material targetMat =
                CreateMaterial(
                    new Color(
                        0.40f,
                        0.08f,
                        0.08f));

            Block(
                "FLOOR",
                new Vector3(0f, -0.4f, 1f),
                new Vector3(22f, 0.8f, 22f),
                floorMat);

            Block(
                "BACK_WALL",
                new Vector3(0f, 3.2f, 11.5f),
                new Vector3(22f, 6.4f, 0.5f),
                wallMat);

            Block(
                "LEFT_WALL",
                new Vector3(-10.8f, 3.2f, 1f),
                new Vector3(0.5f, 6.4f, 22f),
                wallMat);

            Block(
                "RIGHT_WALL",
                new Vector3(10.8f, 3.2f, 1f),
                new Vector3(0.5f, 6.4f, 22f),
                wallMat);

            GameObject dummy =
                GameObject.CreatePrimitive(
                    PrimitiveType.Capsule);

            dummy.name = "RUN0_DUMMY";

            dummy.transform.position =
                new Vector3(
                    0f,
                    1f,
                    2.6f);

            dummy.transform.localScale =
                new Vector3(
                    0.75f,
                    1f,
                    0.75f);

            dummy
                .GetComponent<Renderer>()
                .material = targetMat;

            for (int z = -2; z <= 8; z += 2)
            {
                Block(
                    "GRID_" + z,
                    new Vector3(
                        0f,
                        0.015f,
                        z),
                    new Vector3(
                        18f,
                        0.02f,
                        0.025f),
                    wallMat);
            }

            GameObject sun =
                new GameObject("SUN");

            sun.transform.rotation =
                Quaternion.Euler(
                    48f,
                    -28f,
                    0f);

            Light light =
                sun.AddComponent<Light>();

            light.type =
                LightType.Directional;

            light.intensity = 1.25f;

            RenderSettings.ambientLight =
                new Color(
                    0.42f,
                    0.44f,
                    0.50f);
        }

        private static Material CreateMaterial(
            Color color)
        {
            Shader shader =
                Shader.Find("Standard");

            Material material =
                new Material(shader);

            material.color = color;
            return material;
        }

        private static GameObject Block(
            string name,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            GameObject go =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube);

            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;

            go
                .GetComponent<Renderer>()
                .material = material;

            return go;
        }
    }
}
