using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Highfly.World
{
    public enum HighflyResourceType
    {
        Wood,
        Ore,
        Herb,
        Ingot,
        Gold
    }

    public enum HighflyLandmarkType
    {
        Inn,
        Smithy,
        Market,
        Guild,
        Generic
    }

    [DisallowMultipleComponent]
    public sealed class HighflyWorldState : MonoBehaviour
    {
        public static HighflyWorldState Instance { get; private set; }

        private const string Prefix = "HF_WORLD_V1_";
        private float _noticeUntil;
        private string _notice = "";
        private GUIStyle _noticeStyle;
        private GUIStyle _inventoryStyle;

        public int Wood => PlayerPrefs.GetInt(Prefix + "wood", 0);
        public int Ore => PlayerPrefs.GetInt(Prefix + "ore", 0);
        public int Herb => PlayerPrefs.GetInt(Prefix + "herb", 0);
        public int Ingot => PlayerPrefs.GetInt(Prefix + "ingot", 0);
        public int Gold => PlayerPrefs.GetInt(Prefix + "gold", 0);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (Instance != null) return;
            var go = new GameObject("HIGHFLY_WORLD_STATE");
            DontDestroyOnLoad(go);
            go.AddComponent<HighflyWorldState>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public int Get(HighflyResourceType type)
        {
            return PlayerPrefs.GetInt(Prefix + Key(type), 0);
        }

        public void Add(HighflyResourceType type, int amount)
        {
            if (amount == 0) return;
            string key = Prefix + Key(type);
            int next = Mathf.Max(0, PlayerPrefs.GetInt(key, 0) + amount);
            PlayerPrefs.SetInt(key, next);
            PlayerPrefs.Save();
        }

        public bool Spend(HighflyResourceType type, int amount)
        {
            if (amount <= 0) return true;
            int current = Get(type);
            if (current < amount) return false;
            Add(type, -amount);
            return true;
        }

        public void MarkDiscovered(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            PlayerPrefs.SetInt(Prefix + "discovered_" + Sanitize(id), 1);
            PlayerPrefs.Save();
        }

        public bool IsDiscovered(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            return PlayerPrefs.GetInt(Prefix + "discovered_" + Sanitize(id), 0) != 0;
        }

        public long GetTimestamp(string id)
        {
            string raw = PlayerPrefs.GetString(Prefix + "ts_" + Sanitize(id), "0");
            long value;
            return long.TryParse(raw, out value) ? value : 0L;
        }

        public void SetTimestamp(string id, long unixSeconds)
        {
            PlayerPrefs.SetString(Prefix + "ts_" + Sanitize(id), unixSeconds.ToString());
            PlayerPrefs.Save();
        }

        public void Notice(string message, float seconds = 2.4f)
        {
            _notice = message ?? "";
            _noticeUntil = Time.unscaledTime + Mathf.Max(0.3f, seconds);
        }

        private static string Key(HighflyResourceType type)
        {
            switch (type)
            {
                case HighflyResourceType.Wood: return "wood";
                case HighflyResourceType.Ore: return "ore";
                case HighflyResourceType.Herb: return "herb";
                case HighflyResourceType.Ingot: return "ingot";
                case HighflyResourceType.Gold: return "gold";
                default: return type.ToString().ToLowerInvariant();
            }
        }

        private static string Sanitize(string value)
        {
            return value.Trim().ToLowerInvariant().Replace(" ", "_").Replace("/", "_");
        }

        private void OnGUI()
        {
            _inventoryStyle ??= new GUIStyle(GUI.skin.box)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleLeft
            };

            GUI.Box(
                new Rect(12f, 76f, 285f, 30f),
                $"Madera {Wood}   Mineral {Ore}   Lingote {Ingot}   Oro {Gold}",
                _inventoryStyle);

            if (Time.unscaledTime >= _noticeUntil || string.IsNullOrEmpty(_notice)) return;

            _noticeStyle ??= new GUIStyle(GUI.skin.box)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };

            float width = Mathf.Min(Screen.width * 0.62f, 760f);
            GUI.Box(
                new Rect((Screen.width - width) * 0.5f, Screen.height * 0.18f, width, 64f),
                _notice,
                _noticeStyle);
        }
    }

    [DisallowMultipleComponent]
    public sealed class HighflyLandmarkInteractable : MonoBehaviour, IInteractable
    {
        public string landmarkId = "landmark";
        public string displayName = "Lugar";
        public HighflyLandmarkType type = HighflyLandmarkType.Generic;

        public string GetInteractPrompt()
        {
            switch (type)
            {
                case HighflyLandmarkType.Inn: return "USAR • Descansar en " + displayName;
                case HighflyLandmarkType.Smithy: return "USAR • Trabajar en " + displayName;
                case HighflyLandmarkType.Market: return "USAR • Comerciar en " + displayName;
                case HighflyLandmarkType.Guild: return "USAR • Entrar al " + displayName;
                default: return "USAR • " + displayName;
            }
        }

        public void Interact(GameObject player)
        {
            var world = HighflyWorldState.Instance;
            if (world == null) return;

            bool first = !world.IsDiscovered(landmarkId);
            world.MarkDiscovered(landmarkId);

            switch (type)
            {
                case HighflyLandmarkType.Inn:
                {
                    var stats = player != null ? player.GetComponent<PlayerStats>() : null;
                    var potion = player != null ? player.GetComponent<PlayerPotion>() : null;
                    if (stats != null) stats.RestoreEgo(99999f);
                    if (potion != null) potion.RefillPotions();
                    world.Notice(first
                        ? $"Descubriste {displayName}. Descansaste y recuperaste tus suministros."
                        : $"Descansaste en {displayName}. Ego y pociones restaurados.");
                    break;
                }

                case HighflyLandmarkType.Smithy:
                {
                    if (world.Spend(HighflyResourceType.Ore, 1))
                    {
                        world.Add(HighflyResourceType.Ingot, 1);
                        world.Notice($"{displayName}: 1 Mineral → 1 Lingote.");
                    }
                    else
                    {
                        world.Notice($"{displayName}: necesitás Mineral. Buscalo fuera de la ciudad.");
                    }
                    break;
                }

                case HighflyLandmarkType.Market:
                {
                    int sold = 0;
                    if (world.Spend(HighflyResourceType.Wood, 2)) sold += 2;
                    if (world.Spend(HighflyResourceType.Herb, 2)) sold += 2;
                    if (sold > 0)
                    {
                        int gold = sold * 3;
                        world.Add(HighflyResourceType.Gold, gold);
                        world.Notice($"{displayName}: vendiste recursos por {gold} de Oro.");
                    }
                    else
                    {
                        world.Notice($"{displayName}: traé madera o hierbas de las afueras para comerciar.");
                    }
                    break;
                }

                default:
                    world.Notice(first ? $"Descubriste {displayName}." : displayName);
                    break;
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class HighflyResourceNode : MonoBehaviour, IInteractable
    {
        public string nodeId = "resource_node";
        public HighflyResourceType resourceType = HighflyResourceType.Wood;
        public int yieldAmount = 2;
        public float respawnSeconds = 75f;
        public Transform visualRoot;

        private Collider _interactionCollider;
        private Renderer[] _renderers;
        private bool _busy;
        private bool _depleted;
        private Vector3 _originalScale;

        private void Awake()
        {
            _interactionCollider = GetComponent<Collider>();
            if (visualRoot == null && transform.parent != null) visualRoot = transform.parent;
            if (visualRoot == null) visualRoot = transform;

            _renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            _originalScale = visualRoot.localScale;
            RefreshState();
        }

        private void Update()
        {
            if (_depleted && DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= NextRespawn())
                SetDepleted(false);
        }

        public string GetInteractPrompt()
        {
            if (_depleted) return "Recurso agotado";
            switch (resourceType)
            {
                case HighflyResourceType.Wood: return "USAR • Talar";
                case HighflyResourceType.Ore: return "USAR • Minar";
                case HighflyResourceType.Herb: return "USAR • Recolectar";
                default: return "USAR • Recolectar";
            }
        }

        public void Interact(GameObject player)
        {
            if (_busy || _depleted || player == null) return;
            StartCoroutine(Harvest(player));
        }

        private IEnumerator Harvest(GameObject player)
        {
            _busy = true;

            Vector3 direction = transform.position - player.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
                player.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

            var controller = player.GetComponent<PlayerController>();
            if (controller != null && controller.animator != null)
                controller.animator.SetTrigger(Animator.StringToHash("doAttack"));

            var world = HighflyWorldState.Instance;
            if (world != null)
            {
                string verb = resourceType == HighflyResourceType.Wood ? "Talando..." :
                              resourceType == HighflyResourceType.Ore ? "Minando..." :
                              "Recolectando...";
                world.Notice(verb, 0.7f);
            }

            Pulse();
            yield return new WaitForSeconds(0.48f);

            world = HighflyWorldState.Instance;
            if (world != null)
            {
                world.Add(resourceType, Mathf.Max(1, yieldAmount));
                world.Notice($"+{Mathf.Max(1, yieldAmount)} {Display(resourceType)}");
                long next = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + Mathf.RoundToInt(respawnSeconds);
                world.SetTimestamp(nodeId, next);
            }

            SetDepleted(true);
            _busy = false;
        }

        private void Pulse()
        {
            var psGo = new GameObject("HIGHFLY_RESOURCE_FX");
            psGo.transform.position = transform.position + Vector3.up * 0.8f;
            var ps = psGo.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.loop = false;
            main.duration = 0.2f;
            main.startLifetime = 0.35f;
            main.startSpeed = 2.2f;
            main.startSize = 0.09f;
            main.startColor = resourceType == HighflyResourceType.Ore
                ? new Color(0.35f, 0.75f, 1f, 1f)
                : resourceType == HighflyResourceType.Herb
                    ? new Color(0.35f, 1f, 0.4f, 1f)
                    : new Color(0.65f, 0.38f, 0.16f, 1f);

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.22f;

            ps.Play();
            Destroy(psGo, 1.2f);
        }

        private void RefreshState()
        {
            SetDepleted(DateTimeOffset.UtcNow.ToUnixTimeSeconds() < NextRespawn());
        }

        private long NextRespawn()
        {
            var world = HighflyWorldState.Instance;
            return world != null ? world.GetTimestamp(nodeId) : 0L;
        }

        private void SetDepleted(bool depleted)
        {
            _depleted = depleted;
            if (_interactionCollider != null) _interactionCollider.enabled = !depleted;

            if (_renderers != null)
            {
                foreach (var renderer in _renderers)
                    if (renderer != null) renderer.enabled = !depleted;
            }

            if (!depleted && visualRoot != null)
                visualRoot.localScale = _originalScale;
        }

        private static string Display(HighflyResourceType type)
        {
            switch (type)
            {
                case HighflyResourceType.Wood: return "Madera";
                case HighflyResourceType.Ore: return "Mineral";
                case HighflyResourceType.Herb: return "Hierba";
                case HighflyResourceType.Ingot: return "Lingote";
                case HighflyResourceType.Gold: return "Oro";
                default: return type.ToString();
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class HighflyAmbientWander : MonoBehaviour
    {
        public float radius = 4f;
        public float speed = 0.75f;
        public float turnSpeed = 3f;

        private Vector3 _origin;
        private Vector3 _target;
        private float _nextDecision;
        private Animator _animator;
        private int _speedParam = -1;

        private void Awake()
        {
            _origin = transform.position;
            _target = _origin;
            _animator = GetComponentInChildren<Animator>(true);

            if (_animator != null)
            {
                foreach (var p in _animator.parameters)
                {
                    if (string.Equals(p.name, "speed", StringComparison.OrdinalIgnoreCase) &&
                        p.type == AnimatorControllerParameterType.Float)
                    {
                        _speedParam = Animator.StringToHash(p.name);
                        break;
                    }
                }
            }
        }

        private void Update()
        {
            if (_animator == null) return; // Never slide static decorative animals.

            if (Time.time >= _nextDecision || Vector3.Distance(transform.position, _target) < 0.35f)
            {
                _nextDecision = Time.time + UnityEngine.Random.Range(3f, 7f);
                Vector2 circle = UnityEngine.Random.insideUnitCircle * radius;
                _target = _origin + new Vector3(circle.x, 0f, circle.y);
            }

            Vector3 dir = _target - transform.position;
            dir.y = 0f;
            bool moving = dir.sqrMagnitude > 0.18f;

            if (moving)
            {
                Quaternion look = Quaternion.LookRotation(dir.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * turnSpeed);

                Vector3 next = transform.position + transform.forward * speed * Time.deltaTime;
                RaycastHit hit;
                if (Physics.Raycast(next + Vector3.up * 2f, Vector3.down, out hit, 5f, ~0, QueryTriggerInteraction.Ignore))
                    next.y = hit.point.y;

                transform.position = next;
            }

            if (_speedParam != -1)
                _animator.SetFloat(_speedParam, moving ? 1f : 0f);
        }
    }
}
