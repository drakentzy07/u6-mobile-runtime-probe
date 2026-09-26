using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Highfly.Mobile;

namespace Highfly.MonsterLab
{
    [DisallowMultipleComponent]
    public sealed class MonsterLabGoblinStats : CharacterStats
    {
        private GoblinAgent _agent;
        private Renderer _renderer;
        private Collider[] _colliders;

        public void Bind(GoblinAgent agent, Renderer renderer)
        {
            _agent = agent;
            _renderer = renderer;
            _colliders = GetComponentsInChildren<Collider>(true);

            if (_agent != null)
            {
                _agent.Defeated += OnAgentDefeated;
                maxEgo = _agent.State.definition.maxHealth;
                currentEgo = maxEgo;
            }
        }

        public override void Start()
        {
            if (_agent != null)
            {
                maxEgo = _agent.State.definition.maxHealth;
                currentEgo = maxEgo;
            }
        }

        public override void TakeDamage(float damage, float composureDamage = 10f, Transform attacker = null)
        {
            if (_agent == null || _agent.State == null) return;
            if (_agent.State.lifeState == MonsterLifeState.DEFEATED ||
                _agent.State.lifeState == MonsterLifeState.ESCAPED) return;

            _agent.ApplyDamage(Mathf.Max(0f, damage));
            currentEgo = _agent.State.health;

            StopAllCoroutines();
            if (_agent.State.lifeState != MonsterLifeState.DEFEATED)
                StartCoroutine(HitPulse());
        }

        private IEnumerator HitPulse()
        {
            if (_renderer == null) yield break;
            Vector3 baseScale = transform.localScale;
            transform.localScale = new Vector3(baseScale.x * 1.07f, baseScale.y * 0.94f, baseScale.z * 1.07f);
            yield return new WaitForSecondsRealtime(0.055f);
            transform.localScale = baseScale;
        }

        private void OnAgentDefeated(GoblinAgent agent)
        {
            if (_renderer != null)
            {
                Material mat = _renderer.material;
                Color defeated = new Color(0.10f, 0.10f, 0.11f, 1f);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", defeated);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", defeated);
            }

            if (_colliders != null)
                for (int i = 0; i < _colliders.Length; i++)
                    if (_colliders[i] != null) _colliders[i].enabled = false;

            transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y * 0.28f, transform.localScale.z);
        }

        private void OnDestroy()
        {
            if (_agent != null)
                _agent.Defeated -= OnAgentDefeated;
        }
    }

    [DisallowMultipleComponent]
    public sealed class HighflyMonsterRun0Bootstrap : MonoBehaviour
    {
        private const float LabY = 120f;
        private static readonly Vector3 LabSpawn = new Vector3(0f, LabY + 0.9f, -9.0f);

        private PlayerController _player;
        private GoblinEncounterRuntime _encounter;
        private bool _setup;
        private float _nextHudRefresh;
        private Text _hud;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (FindAnyObjectByType<HighflyMonsterRun0Bootstrap>() != null) return;
            var root = new GameObject("HIGHFLY_MONSTER_RUN0_BOOTSTRAP");
            DontDestroyOnLoad(root);
            root.AddComponent<HighflyMonsterRun0Bootstrap>();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;
        }

        private void Update()
        {
            if (!_setup)
            {
                PlayerController player = FindAnyObjectByType<PlayerController>();
                if (player != null)
                    Setup(player);
                return;
            }

            if (_player == null) return;

            Vector3 p = _player.transform.position;
            if (p.y < LabY - 3f || p.y > LabY + 14f || Mathf.Abs(p.x) > 15f || p.z < -12f || p.z > 18f)
            {
                MovePlayerToLab();
                HighflyThirdPersonMobileCamera.Instance?.SnapBehindPlayer(13f);
            }

            if (Time.unscaledTime >= _nextHudRefresh)
            {
                _nextHudRefresh = Time.unscaledTime + 0.12f;
                RefreshHud();
            }
        }

        private void Setup(PlayerController player)
        {
            _setup = true;
            _player = player;

            BuildRoom();
            MovePlayerToLab();
            HideHostUi();
            BuildEncounter();
            BuildHud();

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.045f, 0.055f, 0.075f, 1f);
            }

            StartCoroutine(SnapCamera());

            Debug.Log("[MONSTER RUN 0] READY • GOLDEN SKILL LAB CAMERA • GOBLIN ENCOUNTER.");
        }

        private void MovePlayerToLab()
        {
            CharacterController cc = _player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            _player.transform.position = LabSpawn;
            _player.transform.rotation = Quaternion.identity;
            _player.SetHighflyMobileMove(Vector2.zero);

            if (cc != null) cc.enabled = true;
        }

        private IEnumerator SnapCamera()
        {
            yield return null;
            yield return null;
            HighflyThirdPersonMobileCamera.Instance?.SnapBehindPlayer(13f);
        }

        private void HideHostUi()
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null) continue;
                string n = canvas.gameObject.name;
                bool keep = n.Contains("HIGHFLY Mobile HUD") || n.Contains("MONSTER_RUN0");
                if (!keep) canvas.gameObject.SetActive(false);
            }
        }

        private void BuildEncounter()
        {
            _encounter = new GoblinEncounterRuntime();

            SpawnGoblin(GoblinDefinitions.Scavenger(), new Vector3(-4.6f, LabY + 1.0f, 2.2f));
            SpawnGoblin(GoblinDefinitions.Scavenger(), new Vector3( 4.6f, LabY + 1.0f, 2.2f));
            SpawnGoblin(GoblinDefinitions.Scavenger(), new Vector3( 0.0f, LabY + 1.0f, 4.0f));

            SpawnGoblin(GoblinDefinitions.Marauder(), new Vector3(-3.0f, LabY + 1.0f, 6.2f));
            SpawnGoblin(GoblinDefinitions.Marauder(), new Vector3( 3.0f, LabY + 1.0f, 6.2f));

            SpawnGoblin(GoblinDefinitions.Warleader(), new Vector3(0.0f, LabY + 1.2f, 9.2f));
        }

        private void SpawnGoblin(MonsterDefinition definition, Vector3 position)
        {
            GameObject root = new GameObject(definition.monsterDefinitionId + "_" + definition.role);
            root.transform.position = position;
            try { root.tag = "Enemy"; } catch { }

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);

            float scale = definition.role == MonsterRole.WARLEADER ? 1.28f :
                          definition.role == MonsterRole.MARAUDER ? 1.08f : 0.90f;
            body.transform.localScale = new Vector3(scale, scale, scale);

            Renderer renderer = body.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = CreateMaterial(RoleColor(definition.role));

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "RoleMarker";
            marker.transform.SetParent(root.transform, false);
            marker.transform.localPosition = new Vector3(0f, 1.55f * scale, 0f);
            marker.transform.localScale = definition.role == MonsterRole.WARLEADER
                ? new Vector3(0.72f, 0.18f, 0.72f)
                : new Vector3(0.42f, 0.12f, 0.42f);

            Collider markerCollider = marker.GetComponent<Collider>();
            if (markerCollider != null) Destroy(markerCollider);

            Renderer markerRenderer = marker.GetComponent<Renderer>();
            if (markerRenderer != null)
                markerRenderer.sharedMaterial = CreateMaterial(
                    definition.role == MonsterRole.WARLEADER
                        ? new Color(1f, 0.12f, 0.08f, 1f)
                        : new Color(0.20f, 0.75f, 1f, 1f));

            GoblinAgent agent = root.AddComponent<GoblinAgent>();
            agent.Bind(definition, _encounter.Blackboard, _player.transform, position);
            _encounter.Register(agent);

            MonsterLabGoblinStats stats = root.AddComponent<MonsterLabGoblinStats>();
            stats.Bind(agent, renderer);
        }

        private static Color RoleColor(MonsterRole role)
        {
            switch (role)
            {
                case MonsterRole.WARLEADER: return new Color(0.45f, 0.08f, 0.07f, 1f);
                case MonsterRole.MARAUDER: return new Color(0.38f, 0.22f, 0.06f, 1f);
                default: return new Color(0.12f, 0.34f, 0.12f, 1f);
            }
        }

        private static Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            return mat;
        }

        private static void BuildRoom()
        {
            Material floor = CreateMaterial(new Color(0.16f, 0.18f, 0.22f, 1f));
            Material wall = CreateMaterial(new Color(0.08f, 0.10f, 0.14f, 1f));
            Material line = CreateMaterial(new Color(0.07f, 0.38f, 0.48f, 1f));

            CreateBlock("MONSTER_FLOOR", new Vector3(0f, LabY - 0.35f, 3f), new Vector3(32f, 0.7f, 30f), floor);
            CreateBlock("MONSTER_BACK", new Vector3(0f, LabY + 5f, 17.5f), new Vector3(32f, 10f, 0.5f), wall);
            CreateBlock("MONSTER_LEFT", new Vector3(-15.7f, LabY + 5f, 3f), new Vector3(0.5f, 10f, 30f), wall);
            CreateBlock("MONSTER_RIGHT", new Vector3(15.7f, LabY + 5f, 3f), new Vector3(0.5f, 10f, 30f), wall);
            CreateBlock("MONSTER_FRONT", new Vector3(0f, LabY + 5f, -11.5f), new Vector3(32f, 10f, 0.5f), wall);

            for (int z = -8; z <= 14; z += 2)
                CreateBlock("MONSTER_GRID_Z_" + z, new Vector3(0f, LabY + 0.025f, z), new Vector3(28f, 0.03f, 0.025f), line);

            GameObject key = new GameObject("MONSTER_KEY_LIGHT");
            key.transform.rotation = Quaternion.Euler(52f, 32f, 0f);
            Light light = key.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.color = new Color(0.94f, 0.97f, 1f, 1f);
        }

        private static GameObject CreateBlock(string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;
            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = material;
            return go;
        }

        private void BuildHud()
        {
            GameObject canvasGo = new GameObject("MONSTER_RUN0_UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 6500;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject panel = new GameObject("MONSTER_STATUS", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasGo.transform, false);
            RectTransform pr = panel.GetComponent<RectTransform>();
            pr.anchorMin = pr.anchorMax = new Vector2(0f, 1f);
            pr.pivot = new Vector2(0f, 1f);
            pr.anchoredPosition = new Vector2(24f, -24f);
            pr.sizeDelta = new Vector2(590f, 245f);

            Image image = panel.GetComponent<Image>();
            image.color = new Color(0.015f, 0.025f, 0.045f, 0.88f);
            image.raycastTarget = false;

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(panel.transform, false);
            _hud = textGo.GetComponent<Text>();
            _hud.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _hud.fontSize = 20;
            _hud.alignment = TextAnchor.UpperLeft;
            _hud.color = new Color(0.90f, 0.96f, 1f, 1f);
            _hud.raycastTarget = false;

            RectTransform tr = _hud.rectTransform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(18f, 14f);
            tr.offsetMax = new Vector2(-18f, -14f);

            RefreshHud();
        }

        private void RefreshHud()
        {
            if (_hud == null || _encounter == null) return;
            GroupBlackboard b = _encounter.Blackboard;
            EncounterOutcome outcome = EncounterResolver.Resolve(b, false, false);

            _hud.text =
                "HIGHFLY • MONSTER RUN 0\n" +
                "GOLDEN CAMERA: SKILL LAB • IZQ MOVIMIENTO / DER CÁMARA\n" +
                "Encounter: " + b.state + "   Outcome: " + outcome + "\n" +
                "Vivos: " + b.alive + "   Derrotados: " + b.defeated + "   Escapados: " + b.escaped + "\n" +
                "Objetivo: acercate → aggro → derrotá al WARLEADER → observá retirada Scavenger\n" +
                "Verde=Scavenger  Ocre=Marauder  Rojo grande=Warleader";
        }
    }
}