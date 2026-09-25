using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Highfly.Run0H
{
    public enum HighflyRun0HCharacter
    {
        Warrior,
        Assassin
    }

    [DisallowMultipleComponent]
    public sealed class HighflyRun0HCharacterVisual : MonoBehaviour
    {
        public static HighflyRun0HCharacterVisual Instance { get; private set; }

        private const string KnightResource = "HIGHFLY/Run0H/KayKitKnight";
        private const string RogueResource = "HIGHFLY/Run0H/KayKitRogue";
        private const string SwordResource = "HIGHFLY/Run0H/KayKitSword1H";
        private const string DaggerResource = "HIGHFLY/Run0H/KayKitDagger";
        private const float TargetHeight = 1.72f;

        private PlayerController _player;
        private Animator _sourceAnimator;
        private Renderer[] _sourceRenderers = System.Array.Empty<Renderer>();
        private GameObject _visualPivot;
        private GameObject _visualRoot;
        private Animator _visualAnimator;
        private HighflyRun0HAnimatorMirror _mirror;
        private HighflyRun0HCharacter _current = HighflyRun0HCharacter.Warrior;
        private bool _bound;

        public bool IsBound => _bound;
        public HighflyRun0HCharacter Current => _current;
        public string CurrentLabel => _current == HighflyRun0HCharacter.Warrior
            ? "KAYKIT WARRIOR • 1 ESPADA"
            : "KAYKIT ASSASSIN • 2 DAGAS";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (FindAnyObjectByType<HighflyRun0HCharacterVisual>() != null) return;

            var root = new GameObject("HIGHFLY_RUN0H_CHARACTER_VISUAL");
            DontDestroyOnLoad(root);
            root.AddComponent<HighflyRun0HCharacterVisual>();
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
            SceneManager.sceneLoaded += OnSceneLoaded;
            StartCoroutine(BindLoop());
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (Instance == this) Instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _bound = false;
            _player = null;
            _sourceAnimator = null;
            _sourceRenderers = System.Array.Empty<Renderer>();
            DestroyCurrentVisual();
        }

        private IEnumerator BindLoop()
        {
            var wait = new WaitForSecondsRealtime(0.20f);

            while (true)
            {
                if (!_bound)
                    TryBind();

                yield return wait;
            }
        }

        private void TryBind()
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player == null) return;

            Animator source = player.animator != null
                ? player.animator
                : player.GetComponentInChildren<Animator>(true);

            if (source == null || source.runtimeAnimatorController == null)
                return;

            _player = player;
            _sourceAnimator = source;
            _sourceRenderers = player.GetComponentsInChildren<Renderer>(true);

            ForceHideLucidRenderers();
            SpawnCurrent();
            _player.animator = _sourceAnimator;
            _bound = _visualAnimator != null;

            Debug.Log("[RUN0H] CHARACTER BOUND • " + CurrentLabel);
        }

        public void UseWarrior()
        {
            _current = HighflyRun0HCharacter.Warrior;
            if (_player != null && _sourceAnimator != null)
                SpawnCurrent();
        }

        public void UseAssassin()
        {
            _current = HighflyRun0HCharacter.Assassin;
            if (_player != null && _sourceAnimator != null)
                SpawnCurrent();
        }

        private void SpawnCurrent()
        {
            DestroyCurrentVisual();

            string resource = _current == HighflyRun0HCharacter.Warrior
                ? KnightResource
                : RogueResource;

            GameObject prefab = Resources.Load<GameObject>(resource);
            if (prefab == null)
            {
                Debug.LogError("[RUN0H] Missing character resource: " + resource);
                return;
            }

            _visualPivot = new GameObject("HIGHFLY_RUN0H_VISUAL_PIVOT");
            _visualPivot.transform.SetParent(_player.transform, false);
            _visualPivot.transform.localPosition = Vector3.zero;
            _visualPivot.transform.localRotation = Quaternion.identity;
            _visualPivot.transform.localScale = Vector3.one;

            _visualRoot = Instantiate(prefab, _visualPivot.transform);
            _visualRoot.name = _current == HighflyRun0HCharacter.Warrior
                ? "HIGHFLY_KAYKIT_WARRIOR"
                : "HIGHFLY_KAYKIT_ASSASSIN";
            _visualRoot.transform.localPosition = Vector3.zero;
            _visualRoot.transform.localRotation = Quaternion.identity;
            _visualRoot.transform.localScale = Vector3.one;

            foreach (Collider c in _visualRoot.GetComponentsInChildren<Collider>(true))
                c.enabled = false;

            foreach (Rigidbody rb in _visualRoot.GetComponentsInChildren<Rigidbody>(true))
            {
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }

            _visualAnimator = _visualRoot.GetComponent<Animator>();
            if (_visualAnimator == null)
                _visualAnimator = _visualRoot.AddComponent<Animator>();

            Avatar[] avatars = Resources.LoadAll<Avatar>(resource);
            foreach (Avatar avatar in avatars)
            {
                if (avatar != null && avatar.isValid && avatar.isHuman)
                {
                    _visualAnimator.avatar = avatar;
                    break;
                }
            }

            _visualAnimator.runtimeAnimatorController = _sourceAnimator.runtimeAnimatorController;
            _visualAnimator.applyRootMotion = false;
            _visualAnimator.fireEvents = false;
            _visualAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            RemoveEmbeddedEquipment();
            NormalizeVisual();

            if (_current == HighflyRun0HCharacter.Warrior)
                AttachWarriorSword();
            else
                AttachAssassinDaggers();

            _mirror = _visualRoot.AddComponent<HighflyRun0HAnimatorMirror>();
            _mirror.Bind(_sourceAnimator, _visualAnimator);

            ForceHideLucidRenderers();
            Debug.Log("[RUN0H] CHARACTER SWITCH -> " + CurrentLabel);
        }

        private void LateUpdate()
        {
            // Lucid remains the invisible logical body. Its combat scripts can
            // re-enable weapon renderers during animation events, so force-hide
            // every captured Lucid renderer each frame.
            ForceHideLucidRenderers();
        }

        private void ForceHideLucidRenderers()
        {
            for (int i = 0; i < _sourceRenderers.Length; i++)
            {
                Renderer r = _sourceRenderers[i];
                if (r != null && r.enabled)
                    r.enabled = false;
            }
        }

        private void RemoveEmbeddedEquipment()
        {
            Renderer[] renderers = _visualRoot.GetComponentsInChildren<Renderer>(true);

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null) continue;

                string path = FullPath(r.transform).ToLowerInvariant();
                if (path.Contains("sword") ||
                    path.Contains("weapon") ||
                    path.Contains("shield") ||
                    path.Contains("dagger") ||
                    path.Contains("axe") ||
                    path.Contains("mace") ||
                    path.Contains("bow") ||
                    path.Contains("quiver") ||
                    path.Contains("staff") ||
                    path.Contains("spear"))
                {
                    r.enabled = false;
                }
            }
        }

        private static string FullPath(Transform t)
        {
            string path = t.name;
            Transform p = t.parent;
            while (p != null)
            {
                path = p.name + "/" + path;
                p = p.parent;
            }
            return path;
        }

        private void NormalizeVisual()
        {
            Renderer[] renderers = _visualRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0) return;

            bool found = false;
            Bounds bounds = default;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || !r.enabled) continue;

                if (!found)
                {
                    bounds = r.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            if (!found || bounds.size.y < 0.01f) return;

            float scale = Mathf.Clamp(TargetHeight / bounds.size.y, 0.12f, 2.5f);
            _visualRoot.transform.localScale = Vector3.one * scale;
            Physics.SyncTransforms();

            found = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || !r.enabled) continue;

                if (!found)
                {
                    bounds = r.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            if (found)
                _visualRoot.transform.position +=
                    Vector3.up * (_player.transform.position.y - bounds.min.y);
        }

        private void AttachWarriorSword()
        {
            if (_visualAnimator == null || !_visualAnimator.isHuman) return;

            Transform hand = _visualAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            GameObject swordPrefab = Resources.Load<GameObject>(SwordResource);
            if (hand == null || swordPrefab == null) return;

            GameObject sword = Instantiate(swordPrefab, hand);
            sword.name = "HIGHFLY_RUN0H_WARRIOR_SWORD";
            sword.transform.localPosition = Vector3.zero;
            sword.transform.localRotation = Quaternion.identity;
            sword.transform.localScale = Vector3.one;

            foreach (Collider c in sword.GetComponentsInChildren<Collider>(true))
                c.enabled = false;
        }

        private void AttachAssassinDaggers()
        {
            if (_visualAnimator == null || !_visualAnimator.isHuman) return;

            GameObject daggerPrefab = Resources.Load<GameObject>(DaggerResource);
            if (daggerPrefab == null) return;

            Transform right = _visualAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            Transform left = _visualAnimator.GetBoneTransform(HumanBodyBones.LeftHand);

            if (right != null)
                AttachDagger(daggerPrefab, right, "HIGHFLY_RUN0H_DAGGER_R");

            if (left != null)
                AttachDagger(daggerPrefab, left, "HIGHFLY_RUN0H_DAGGER_L");
        }

        private static void AttachDagger(GameObject prefab, Transform hand, string name)
        {
            GameObject dagger = Instantiate(prefab, hand);
            dagger.name = name;
            dagger.transform.localPosition = Vector3.zero;
            dagger.transform.localRotation = Quaternion.identity;
            dagger.transform.localScale = Vector3.one;

            foreach (Collider c in dagger.GetComponentsInChildren<Collider>(true))
                c.enabled = false;
        }

        private void DestroyCurrentVisual()
        {
            if (_visualPivot != null)
                Destroy(_visualPivot);

            _visualPivot = null;
            _visualRoot = null;
            _visualAnimator = null;
            _mirror = null;
        }
    }

    [DefaultExecutionOrder(5000)]
    public sealed class HighflyRun0HAnimatorMirror : MonoBehaviour
    {
        private Animator _source;
        private Animator _visual;

        public void Bind(Animator source, Animator visual)
        {
            _source = source;
            _visual = visual;
        }

        private void Update()
        {
            if (_source == null || _visual == null) return;

            AnimatorControllerParameter[] parameters = _source.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter p = parameters[i];

                switch (p.type)
                {
                    case AnimatorControllerParameterType.Float:
                        _visual.SetFloat(p.nameHash, _source.GetFloat(p.nameHash));
                        break;
                    case AnimatorControllerParameterType.Int:
                        _visual.SetInteger(p.nameHash, _source.GetInteger(p.nameHash));
                        break;
                    case AnimatorControllerParameterType.Bool:
                        _visual.SetBool(p.nameHash, _source.GetBool(p.nameHash));
                        break;
                }
            }
        }

        private void LateUpdate()
        {
            if (_source == null || _visual == null) return;

            int layers = Mathf.Min(_source.layerCount, _visual.layerCount);
            for (int layer = 0; layer < layers; layer++)
            {
                AnimatorStateInfo src = _source.GetCurrentAnimatorStateInfo(layer);
                AnimatorStateInfo dst = _visual.GetCurrentAnimatorStateInfo(layer);

                if (src.fullPathHash == 0) continue;

                float srcNorm = Mathf.Repeat(src.normalizedTime, 1f);
                float dstNorm = Mathf.Repeat(dst.normalizedTime, 1f);
                float drift = Mathf.Abs(
                    Mathf.DeltaAngle(srcNorm * 360f, dstNorm * 360f)) / 360f;

                if (dst.fullPathHash != src.fullPathHash || drift > 0.14f)
                    _visual.Play(src.fullPathHash, layer, srcNorm);
            }
        }
    }
}
