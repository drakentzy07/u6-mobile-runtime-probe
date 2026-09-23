using UnityEngine;

namespace Highfly.SkillLab
{
    public static class HighflyLabTargetsV011
    {
        private static GameObject _small;
        private static GameObject _humanoid;
        private static GameObject _large;
        private static bool _built;

        public static void BuildThreeTargetRange(float labY)
        {
            if (_built) return;
            _built = true;

            _small = BuildSmallMonster(new Vector3(-3.8f, labY + 0.78f, 5.2f));
            _humanoid = BuildHumanoidMonster(new Vector3(0f, labY + 1.05f, 3.9f));
            _large = BuildLargeMonster(new Vector3(4.2f, labY + 1.65f, 5.5f));

            SetThreeTargetMode(true);
        }

        public static void SetThreeTargetMode(bool enabled)
        {
            if (_small != null) _small.SetActive(enabled);
            if (_large != null) _large.SetActive(enabled);
            if (_humanoid != null) _humanoid.SetActive(true);
        }

        private static GameObject BuildSmallMonster(Vector3 position)
        {
            GameObject root = CreateRoot("MONSTER_SMALL_GOBLIN", position, new Color(0.18f, 0.28f, 0.20f, 1f));

            AddPrimitive(root.transform, PrimitiveType.Capsule, "Torso",
                new Vector3(0f, 0f, 0f), new Vector3(0.68f, 0.62f, 0.54f),
                new Color(0.15f, 0.23f, 0.17f, 1f));

            AddPrimitive(root.transform, PrimitiveType.Sphere, "Head",
                new Vector3(0f, 0.72f, 0f), new Vector3(0.54f, 0.48f, 0.50f),
                new Color(0.25f, 0.34f, 0.19f, 1f));

            AddPrimitive(root.transform, PrimitiveType.Cube, "EarL",
                new Vector3(-0.40f, 0.76f, 0f), new Vector3(0.38f, 0.10f, 0.16f),
                new Color(0.25f, 0.34f, 0.19f, 1f));
            AddPrimitive(root.transform, PrimitiveType.Cube, "EarR",
                new Vector3(0.40f, 0.76f, 0f), new Vector3(0.38f, 0.10f, 0.16f),
                new Color(0.25f, 0.34f, 0.19f, 1f));

            AddPrimitive(root.transform, PrimitiveType.Cube, "ArmL",
                new Vector3(-0.47f, 0.02f, 0f), new Vector3(0.18f, 0.66f, 0.18f),
                new Color(0.13f, 0.18f, 0.14f, 1f));
            AddPrimitive(root.transform, PrimitiveType.Cube, "ArmR",
                new Vector3(0.47f, 0.02f, 0f), new Vector3(0.18f, 0.66f, 0.18f),
                new Color(0.13f, 0.18f, 0.14f, 1f));

            AddRing(root.transform, 0.72f, new Color(0.30f, 0.95f, 0.48f, 1f));
            AddLabel(root.transform, "SMALL", new Vector3(0f, 1.55f, 0f));
            return root;
        }

        private static GameObject BuildHumanoidMonster(Vector3 position)
        {
            GameObject root = CreateRoot("MONSTER_HUMANOID_KNIGHT", position, new Color(0.15f, 0.17f, 0.22f, 1f));

            AddPrimitive(root.transform, PrimitiveType.Capsule, "Body",
                Vector3.zero, new Vector3(0.88f, 1.10f, 0.82f),
                new Color(0.12f, 0.15f, 0.20f, 1f));
            AddPrimitive(root.transform, PrimitiveType.Sphere, "Helmet",
                new Vector3(0f, 1.30f, 0f), new Vector3(0.58f, 0.58f, 0.58f),
                new Color(0.20f, 0.24f, 0.31f, 1f));
            AddPrimitive(root.transform, PrimitiveType.Cube, "Shoulders",
                new Vector3(0f, 0.68f, 0f), new Vector3(1.55f, 0.22f, 0.72f),
                new Color(0.18f, 0.22f, 0.29f, 1f));
            AddPrimitive(root.transform, PrimitiveType.Cube, "GuardL",
                new Vector3(-0.58f, 0.03f, 0f), new Vector3(0.20f, 0.84f, 0.22f),
                new Color(0.14f, 0.18f, 0.24f, 1f));
            AddPrimitive(root.transform, PrimitiveType.Cube, "GuardR",
                new Vector3(0.58f, 0.03f, 0f), new Vector3(0.20f, 0.84f, 0.22f),
                new Color(0.14f, 0.18f, 0.24f, 1f));

            AddRing(root.transform, 0.95f, new Color(0.20f, 0.72f, 1f, 1f));
            AddLabel(root.transform, "HUMANOID", new Vector3(0f, 2.35f, 0f));
            return root;
        }

        private static GameObject BuildLargeMonster(Vector3 position)
        {
            GameObject root = CreateRoot("BOSS_POISE_TARGET", position, new Color(0.26f, 0.13f, 0.18f, 1f));

            AddPrimitive(root.transform, PrimitiveType.Capsule, "BruteBody",
                Vector3.zero, new Vector3(1.55f, 1.68f, 1.28f),
                new Color(0.24f, 0.11f, 0.16f, 1f));
            AddPrimitive(root.transform, PrimitiveType.Sphere, "BruteHead",
                new Vector3(0f, 2.05f, 0f), new Vector3(0.82f, 0.76f, 0.78f),
                new Color(0.31f, 0.14f, 0.19f, 1f));
            AddPrimitive(root.transform, PrimitiveType.Cube, "BruteShoulders",
                new Vector3(0f, 1.00f, 0f), new Vector3(2.75f, 0.38f, 1.08f),
                new Color(0.20f, 0.09f, 0.13f, 1f));
            AddPrimitive(root.transform, PrimitiveType.Cube, "BruteArmL",
                new Vector3(-1.04f, 0.15f, 0f), new Vector3(0.42f, 1.48f, 0.44f),
                new Color(0.22f, 0.10f, 0.14f, 1f));
            AddPrimitive(root.transform, PrimitiveType.Cube, "BruteArmR",
                new Vector3(1.04f, 0.15f, 0f), new Vector3(0.42f, 1.48f, 0.44f),
                new Color(0.22f, 0.10f, 0.14f, 1f));

            AddRing(root.transform, 1.45f, new Color(1f, 0.28f, 0.38f, 1f));
            AddLabel(root.transform, "BOSS / POISE", new Vector3(0f, 3.45f, 0f));
            return root;
        }

        private static GameObject CreateRoot(string name, Vector3 position, Color color)
        {
            GameObject root = new GameObject(name);
            root.transform.position = position;
            try { root.tag = "Enemy"; } catch { }

            HighflyLabDummyStats stats = root.AddComponent<HighflyLabDummyStats>();
            return root;
        }

        private static GameObject AddPrimitive(Transform parent, PrimitiveType type, string name, Vector3 localPosition, Vector3 localScale, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = HighflyLabVisuals.CreateMaterial(color, color * 0.12f);

            return go;
        }

        private static void AddRing(Transform parent, float radius, Color color)
        {
            GameObject ring = new GameObject("TargetRing");
            ring.transform.SetParent(parent, false);
            ring.transform.localPosition = new Vector3(0f, -0.80f, 0f);

            LineRenderer lr = ring.AddComponent<LineRenderer>();
            lr.loop = true;
            lr.useWorldSpace = false;
            lr.positionCount = 48;
            lr.widthMultiplier = 0.045f;
            lr.sharedMaterial = HighflyLabVisuals.CreateFxMaterial(color);

            for (int i = 0; i < 48; i++)
            {
                float a = (i / 48f) * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }
        }

        private static void AddLabel(Transform parent, string value, Vector3 localPosition)
        {
            GameObject label = new GameObject("TargetLabel", typeof(TextMesh));
            label.transform.SetParent(parent, false);
            label.transform.localPosition = localPosition;
            label.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            TextMesh text = label.GetComponent<TextMesh>();
            text.text = value;
            text.fontSize = 48;
            text.characterSize = 0.055f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = new Color(0.84f, 0.92f, 1f, 0.92f);
        }
    }
}
