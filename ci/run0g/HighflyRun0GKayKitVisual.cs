using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Highfly.Run0G
{
    [DisallowMultipleComponent]
    public sealed class HighflyRun0GKayKitVisual : MonoBehaviour
    {
        private const string KnightResource = "HIGHFLY/Run0G/KayKitKnight";
        private const string SwordResource = "HIGHFLY/Run0G/KayKitSword1H";

        private PlayerController _player;
        private Animator _sourceAnimator;
        private Animator _visualAnimator;
        private GameObject _visualPivot;
        private GameObject _visualRoot;
        private bool _bound;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            var root = new GameObject("HIGHFLY_RUN0G_VISUAL");
            DontDestroyOnLoad(root);
            root.AddComponent<HighflyRun0GKayKitVisual>();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            StartCoroutine(BindLoop());
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _bound = false;
            _player = null;
            _sourceAnimator = null;
            _visualAnimator = null;
            _visualPivot = null;
            _visualRoot = null;
        }

        private IEnumerator BindLoop()
        {
            var wait = new WaitForSecondsRealtime(0.25f);

            while (true)
            {
                if (!_bound)
                    TryBind();

                yield return wait;
            }
        }

        private void TryBind()
        {
            var player = Object.FindFirstObjectByType<PlayerController>();
            if (player == null) return;

            Animator source = player.animator != null
                ? player.animator
                : player.GetComponentInChildren<Animator>(true);

            if (source == null || source.runtimeAnimatorController == null)
                return;

            GameObject prefab = Resources.Load<GameObject>(KnightResource);
            if (prefab == null)
            {
                Debug.LogError("[RUN0G] Missing KayKitKnight resource.");
                return;
            }

            _player = player;
            _sourceAnimator = source;

            // Hide only Lucid's body/weapon mesh renderers. Keep the logical
            // player, Animator, colliders, damage and combat systems alive.
            foreach (var skinned in player.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                skinned.enabled = false;
            foreach (var mesh in player.GetComponentsInChildren<MeshRenderer>(true))
                mesh.enabled = false;

            _visualPivot = new GameObject("HIGHFLY_RUN0G_KAYKIT_PIVOT");
            _visualPivot.transform.SetParent(player.transform, false);

            _visualRoot = Instantiate(prefab, _visualPivot.transform);
            _visualRoot.name = "HIGHFLY_RUN0G_KAYKIT_HUNTER";
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

            Avatar[] avatars = Resources.LoadAll<Avatar>(KnightResource);
            foreach (Avatar avatar in avatars)
            {
                if (avatar != null && avatar.isValid && avatar.isHuman)
                {
                    _visualAnimator.avatar = avatar;
                    break;
                }
            }

            _visualAnimator.runtimeAnimatorController =
                _sourceAnimator.runtimeAnimatorController;
            _visualAnimator.applyRootMotion = false;
            _visualAnimator.fireEvents = false;
            _visualAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            NormalizeVisual();
            AttachSword();

            var mirror = _visualRoot.AddComponent<HighflyRun0GAnimatorMirror>();
            mirror.Bind(_sourceAnimator, _visualAnimator);

            // PlayerController continues to own the original Lucid Animator.
            _player.animator = _sourceAnimator;
            _bound = true;

            Debug.Log("[RUN0G] CLEAN FOUNDATION READY • LUCID LOGIC + DRAGON COMBAT + KAYKIT VISUAL.");
        }

        private void NormalizeVisual()
        {
            Renderer[] renderers = _visualRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;

            bool found = false;
            Bounds bounds = default;
            foreach (Renderer r in renderers)
            {
                if (r == null) continue;
                if (!found) { bounds = r.bounds; found = true; }
                else bounds.Encapsulate(r.bounds);
            }

            if (!found || bounds.size.y < 0.01f) return;

            const float targetHeight = 1.64f;
            float scale = Mathf.Clamp(targetHeight / bounds.size.y, 0.12f, 2.0f);
            _visualRoot.transform.localScale = Vector3.one * scale;
            Physics.SyncTransforms();

            found = false;
            foreach (Renderer r in renderers)
            {
                if (r == null) continue;
                if (!found) { bounds = r.bounds; found = true; }
                else bounds.Encapsulate(r.bounds);
            }

            if (found)
                _visualRoot.transform.position +=
                    Vector3.up * (_player.transform.position.y - bounds.min.y);
        }

        private void AttachSword()
        {
            if (_visualAnimator == null || !_visualAnimator.isHuman) return;

            Transform hand = _visualAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            GameObject swordPrefab = Resources.Load<GameObject>(SwordResource);
            if (hand == null || swordPrefab == null) return;

            GameObject sword = Instantiate(swordPrefab, hand);
            sword.name = "HIGHFLY_RUN0G_SWORD_VISUAL";
            sword.transform.localPosition = Vector3.zero;
            sword.transform.localRotation = Quaternion.identity;
            sword.transform.localScale = Vector3.one;

            foreach (Collider c in sword.GetComponentsInChildren<Collider>(true))
                c.enabled = false;
        }

        private void OnGUI()
        {
            GUI.depth = -1000;
            GUI.Box(
                new Rect(14, 14, 620, 88),
                "HIGHFLY RUN0G CLEAN • UNITY 6000.6.2\n" +
                "LUCID LOCOMOTION / ANIMATOR LOGIC • DRAGON COMBAT CORE\n" +
                "KAYKIT VISUAL • DONOR LAB UI REMOVED • bound=" + _bound);
        }
    }

    [DefaultExecutionOrder(5000)]
    public sealed class HighflyRun0GAnimatorMirror : MonoBehaviour
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

                if (dst.fullPathHash != src.fullPathHash || drift > 0.16f)
                    _visual.Play(src.fullPathHash, layer, srcNorm);
            }
        }
    }
}
