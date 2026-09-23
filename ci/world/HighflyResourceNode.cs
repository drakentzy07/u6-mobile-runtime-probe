using System;
using System.Collections;
using UnityEngine;

namespace Highfly.World
{
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
        private GameObject _marker;

        private void Awake()
        {
            _interactionCollider = GetComponent<Collider>();
            if (visualRoot == null && transform.parent != null) visualRoot = transform.parent;
            if (visualRoot == null) visualRoot = transform;
            _renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            CreateMarker();
            RefreshState();
        }

        private void Update()
        {
            if (_depleted && DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= NextRespawn())
                SetDepleted(false);

            if (_marker != null && Camera.main != null)
            {
                float d = Vector3.Distance(Camera.main.transform.position, transform.position);
                _marker.SetActive(!_depleted && d < 18f);
                if (_marker.activeSelf)
                    _marker.transform.Rotate(Vector3.up, 55f * Time.deltaTime, Space.World);
            }
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
                              resourceType == HighflyResourceType.Ore ? "Minando..." : "Recolectando...";
                world.Notice(verb, 0.7f);
            }

            Pulse();
            yield return new WaitForSeconds(0.48f);

            world = HighflyWorldState.Instance;
            if (world != null)
            {
                int amount = Mathf.Max(1, yieldAmount);
                world.Add(resourceType, amount);
                world.Notice($"+{amount} {Display(resourceType)}");
                world.SetTimestamp(nodeId, DateTimeOffset.UtcNow.ToUnixTimeSeconds() + Mathf.RoundToInt(respawnSeconds));
            }

            SetDepleted(true);
            _busy = false;
        }

        private void CreateMarker()
        {
            _marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _marker.name = "HIGHFLY_RESOURCE_MARKER";
            _marker.transform.SetParent(transform, false);
            _marker.transform.localPosition = Vector3.up * 1.45f;
            _marker.transform.localScale = new Vector3(0.42f, 0.025f, 0.42f);

            var col = _marker.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader != null)
            {
                var mat = new Material(shader);
                Color color = resourceType == HighflyResourceType.Ore
                    ? new Color(0.15f, 0.75f, 1f, 1f)
                    : resourceType == HighflyResourceType.Herb
                        ? new Color(0.25f, 1f, 0.35f, 1f)
                        : new Color(1f, 0.75f, 0.2f, 1f);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                _marker.GetComponent<Renderer>().material = mat;
            }
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
            if (_marker != null) _marker.SetActive(!depleted);

            if (_renderers != null)
            {
                foreach (var renderer in _renderers)
                    if (renderer != null) renderer.enabled = !depleted;
            }
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
}
