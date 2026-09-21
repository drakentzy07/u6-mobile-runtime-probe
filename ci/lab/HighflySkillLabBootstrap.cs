using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Highfly.SkillLab
{
    public static class HighflySkillLabMode
    {
        public static bool IsActive
        {
            get
            {
                string url = Application.absoluteURL ?? string.Empty;
                return url.IndexOf("lab=1", StringComparison.OrdinalIgnoreCase) >= 0 ||
                       url.IndexOf("skilllab", StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }
    }

    public static class HighflySkillLabMetrics
    {
        public static string LastAction = "-";
        public static float LastDamage;
        public static int TotalHits;
        public static float TotalDamage;
        public static int ComboStage;
        public static float LastHitAt;

        public static void RecordAction(string action, int comboStage)
        {
            LastAction = action;
            ComboStage = comboStage;
        }

        public static void RecordHit(float damage)
        {
            LastDamage = damage;
            TotalDamage += damage;
            TotalHits++;
            LastHitAt = Time.unscaledTime;
        }

        public static void Reset()
        {
            LastAction = "-";
            LastDamage = 0f;
            TotalHits = 0;
            TotalDamage = 0f;
            ComboStage = 0;
            LastHitAt = 0f;
        }
    }

    public sealed class HighflyLabDummyStats : CharacterStats
    {
        public override void Start()
        {
            maxEgo = 999999f;
            currentEgo = maxEgo;
        }

        public override void TakeDamage(float damage, float composureDamage = 10f, Transform attacker = null)
        {
            HighflySkillLabMetrics.RecordHit(damage);
            currentEgo = maxEgo;

            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
                renderer.transform.localScale = new Vector3(1.08f, 0.94f, 1.08f);

            StartCoroutine(RestoreScale(renderer));
        }

        private IEnumerator RestoreScale(Renderer renderer)
        {
            yield return new WaitForSecondsRealtime(0.07f);
            if (renderer != null)
                renderer.transform.localScale = Vector3.one;
        }
    }

    [DisallowMultipleComponent]
    public sealed class HighflySkillLabController : MonoBehaviour
    {
        public static HighflySkillLabController Instance { get; private set; }

        private PlayerController _player;
        private CharacterController _cc;
        private int _s1Stage;
        private float _lastS1Time = -99f;
        private const float ComboResetSeconds = 0.85f;

        private readonly Collider[] _hits = new Collider[32];

        private void Awake()
        {
            Instance = this;
            _player = GetComponent<PlayerController>();
            _cc = GetComponent<CharacterController>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void TriggerS1()
        {
            if (_player == null) return;
            if (_player.currentState == PlayerState.Die ||
                _player.currentState == PlayerState.Interact ||
                _player.currentState == PlayerState.UseItem)
                return;

            if (Time.unscaledTime - _lastS1Time > ComboResetSeconds)
                _s1Stage = 0;

            _s1Stage = (_s1Stage % 3) + 1;
            _lastS1Time = Time.unscaledTime;

            HighflySkillLabMetrics.RecordAction("S1 • CHAIN ASSAULT", _s1Stage);

            switch (_s1Stage)
            {
                case 1:
                    StartCoroutine(ExecuteSlash(
                        stage: 1,
                        damage: 24f,
                        reach: 2.2f,
                        radius: 1.15f,
                        lunge: 0.45f,
                        startDelay: 0.05f,
                        accent: new Color(0.10f, 0.82f, 1f, 1f)));
                    break;

                case 2:
                    StartCoroutine(ExecuteSlash(
                        stage: 2,
                        damage: 31f,
                        reach: 2.5f,
                        radius: 1.25f,
                        lunge: 0.60f,
                        startDelay: 0.04f,
                        accent: new Color(0.22f, 0.48f, 1f, 1f)));
                    break;

                default:
                    StartCoroutine(ExecuteSlash(
                        stage: 3,
                        damage: 46f,
                        reach: 2.9f,
                        radius: 1.50f,
                        lunge: 0.90f,
                        startDelay: 0.03f,
                        accent: new Color(0.63f, 0.26f, 1f, 1f)));
                    break;
            }
        }

        private IEnumerator ExecuteSlash(
            int stage,
            float damage,
            float reach,
            float radius,
            float lunge,
            float startDelay,
            Color accent)
        {
            SpawnAnticipation(accent, stage);

            if (startDelay > 0f)
                yield return new WaitForSecondsRealtime(startDelay);

            Vector3 dir = GetFacingDirection();

            if (_cc != null)
                _cc.Move(dir * lunge);

            SpawnSlashArc(dir, accent, stage);
            DamageFront(damage, reach, radius, dir);

            if (stage == 3)
            {
                yield return new WaitForSecondsRealtime(0.035f);
                SpawnImpactRing(accent, 2.8f);
            }
        }

        private Vector3 GetFacingDirection()
        {
            Vector2 move = _player.HighflyMobileMoveInput;
            if (move.sqrMagnitude > 0.03f && _player.cameraTransform != null)
            {
                Vector3 forward = _player.cameraTransform.forward;
                Vector3 right = _player.cameraTransform.right;
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();

                Vector3 desired = forward * move.y + right * move.x;
                if (desired.sqrMagnitude > 0.001f)
                {
                    desired.Normalize();
                    transform.rotation = Quaternion.LookRotation(desired, Vector3.up);
                    return desired;
                }
            }

            return transform.forward;
        }

        private void DamageFront(float damage, float reach, float radius, Vector3 dir)
        {
            Vector3 center = transform.position + Vector3.up * 0.95f + dir * reach;

            int count = Physics.OverlapSphereNonAlloc(
                center,
                radius,
                _hits,
                ~0,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
            {
                Collider hit = _hits[i];
                if (hit == null) continue;

                CharacterStats stats = hit.GetComponentInParent<CharacterStats>();
                if (stats == null || stats.transform == transform) continue;

                bool enemy = false;
                try { enemy = stats.CompareTag("Enemy"); } catch { }

                if (!enemy && !(stats is HighflyLabDummyStats))
                    continue;

                stats.TakeDamage(damage, 20f + damage * 0.25f, transform);
            }
        }

        private void SpawnAnticipation(Color accent, int stage)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "LAB_S1_ANTICIPATION";
            Destroy(go.GetComponent<Collider>());

            go.transform.position = transform.position + Vector3.up * 1.0f;
            go.transform.localScale = Vector3.one * (0.18f + stage * 0.04f);

            var renderer = go.GetComponent<Renderer>();
            renderer.material.color = new Color(accent.r, accent.g, accent.b, 0.55f);

            Destroy(go, 0.12f);
        }

        private void SpawnSlashArc(Vector3 dir, Color accent, int stage)
        {
            var go = new GameObject("LAB_S1_SLASH_ARC");
            go.transform.position = transform.position + Vector3.up * 1.05f + dir * 1.35f;
            go.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.loop = false;
            main.duration = 0.18f;
            main.startLifetime = 0.16f + stage * 0.025f;
            main.startSpeed = 7f + stage * 1.6f;
            main.startSize = 0.12f + stage * 0.035f;
            main.startColor = accent;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, (short)(18 + stage * 10))
            });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 22f + stage * 3f;
            shape.radius = 0.18f;

            ps.Play();
            Destroy(go, 0.8f);
        }

        private void SpawnImpactRing(Color accent, float radius)
        {
            var go = new GameObject("LAB_S1_FINISH_RING");
            go.transform.position = transform.position + Vector3.up * 0.25f + transform.forward * 1.9f;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = false;
            main.duration = 0.24f;
            main.startLifetime = 0.22f;
            main.startSpeed = radius * 5.4f;
            main.startSize = 0.18f;
            main.startColor = accent;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, (short)44)
            });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.30f;

            ps.Play();
            Destroy(go, 1f);
        }
    }

    [DisallowMultipleComponent]
    public sealed class HighflySkillLabBootstrap : MonoBehaviour
    {
        private const float LabY = 45f;
        private bool _setup;
        private Text _metricsText;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (UnityEngine.Object.FindFirstObjectByType<HighflySkillLabBootstrap>() != null) return;

            var root = new GameObject("HIGHFLY_SKILL_LAB_v0.1");
            DontDestroyOnLoad(root);
            root.AddComponent<HighflySkillLabBootstrap>();
        }

        private void Update()
        {
            if (!HighflySkillLabMode.IsActive) return;

            if (!_setup)
            {
                var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
                if (player != null)
                    SetupLab(player);
            }

            if (_metricsText != null)
            {
                float age = HighflySkillLabMetrics.LastHitAt > 0f
                    ? Time.unscaledTime - HighflySkillLabMetrics.LastHitAt
                    : 0f;

                _metricsText.text =
                    "HIGHFLY • SKILL LAB v0.1\n" +
                    "GOLDEN CAMERA + GOLDEN MOBILE CORE\n\n" +
                    "Acción: " + HighflySkillLabMetrics.LastAction + "\n" +
                    "Combo S1: " + HighflySkillLabMetrics.ComboStage + "/3\n" +
                    "Último daño: " + HighflySkillLabMetrics.LastDamage.ToString("0") + "\n" +
                    "Golpes: " + HighflySkillLabMetrics.TotalHits + "\n" +
                    "Daño acumulado: " + HighflySkillLabMetrics.TotalDamage.ToString("0") + "\n" +
                    (age > 0f ? "Último impacto: " + age.ToString("0.00") + "s" : "");
            }
        }

        private void SetupLab(PlayerController player)
        {
            _setup = true;
            HighflySkillLabMetrics.Reset();

            CreateArena();

            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.transform.position = new Vector3(0f, LabY + 1.1f, -6.5f);
            player.transform.rotation = Quaternion.identity;
            if (cc != null) cc.enabled = true;

            if (player.GetComponent<HighflySkillLabController>() == null)
                player.gameObject.AddComponent<HighflySkillLabController>();

            CreateDummy(new Vector3(0f, LabY + 1f, 3.5f));
            CreateMetricsHud();
        }

        private static void CreateArena()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "LAB_ARENA_FLOOR";
            floor.transform.position = new Vector3(0f, LabY, 3f);
            floor.transform.localScale = new Vector3(28f, 0.8f, 28f);

            var renderer = floor.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(0.025f, 0.035f, 0.065f, 1f);

            CreatePillar(new Vector3(-10f, LabY + 2f, 11f));
            CreatePillar(new Vector3(10f, LabY + 2f, 11f));
            CreatePillar(new Vector3(-10f, LabY + 2f, -5f));
            CreatePillar(new Vector3(10f, LabY + 2f, -5f));
        }

        private static void CreatePillar(Vector3 position)
        {
            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = "LAB_PILLAR";
            pillar.transform.position = position;
            pillar.transform.localScale = new Vector3(0.7f, 2.2f, 0.7f);

            var renderer = pillar.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(0.08f, 0.16f, 0.26f, 1f);
        }

        private static void CreateDummy(Vector3 position)
        {
            var dummy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            dummy.name = "LAB_DUMMY_INMORTAL";
            dummy.transform.position = position;
            dummy.transform.localScale = new Vector3(1.1f, 1.3f, 1.1f);

            try { dummy.tag = "Enemy"; } catch { }

            var renderer = dummy.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(0.18f, 0.62f, 0.95f, 1f);

            dummy.AddComponent<HighflyLabDummyStats>();
        }

        private void CreateMetricsHud()
        {
            var canvasGo = new GameObject("LAB_DEV_HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 7000;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var panel = new GameObject("MetricsPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasGo.transform, false);

            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24f, -24f);
            rect.sizeDelta = new Vector2(420f, 270f);

            var image = panel.GetComponent<Image>();
            image.color = new Color(0.015f, 0.025f, 0.05f, 0.78f);

            var textGo = new GameObject("MetricsText", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(panel.transform, false);

            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(18f, 16f);
            textRect.offsetMax = new Vector2(-18f, -16f);

            _metricsText = textGo.GetComponent<Text>();
            _metricsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _metricsText.fontSize = 22;
            _metricsText.alignment = TextAnchor.UpperLeft;
            _metricsText.color = new Color(0.84f, 0.95f, 1f, 1f);
        }
    }
}
