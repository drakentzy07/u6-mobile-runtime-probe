using UnityEngine;

namespace Highfly.SkillLab
{
    [DisallowMultipleComponent]
    public sealed class HighflyRun0Hunter : MonoBehaviour
    {
        public static HighflyRun0Hunter Instance { get; private set; }

        private const string HunterResource = "HIGHFLY/Run0/KayKitKnight";
        private const string SwordResource = "HIGHFLY/Run0/KayKitSword1H";

        private PlayerController _player;
        private Animator _lucidAnimator;
        private Renderer[] _lucidRenderers;
        private GameObject _visualPivot;
        private GameObject _hunterRoot;
        private Animator _hunterAnimator;
        private GameObject _sword;
        private Transform _weaponBase;
        private Transform _weaponTip;

        public PlayerController Player => _player;
        public Animator Animator => _hunterAnimator;
        public Transform VisualPivot => _visualPivot != null ? _visualPivot.transform : null;
        public Transform WeaponBase => _weaponBase;
        public Transform WeaponTip => _weaponTip;
        public GameObject Sword => _sword;

        public static HighflyRun0Hunter Install(PlayerController player)
        {
            if (player == null) return null;

            HighflyRun0Hunter existing = player.GetComponent<HighflyRun0Hunter>();
            if (existing != null) return existing;

            HighflyRun0Hunter result = player.gameObject.AddComponent<HighflyRun0Hunter>();
            result.Initialize(player);
            return result;
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Initialize(PlayerController player)
        {
            _player = player;
            _lucidAnimator = player.animator != null
                ? player.animator
                : player.GetComponentInChildren<Animator>(true);

            _lucidRenderers = player.GetComponentsInChildren<Renderer>(true);

            GameObject prefab = Resources.Load<GameObject>(HunterResource);
            if (prefab == null)
            {
                Debug.LogError("[RUN0] Missing KayKit hunter resource: " + HunterResource);
                return;
            }

            _visualPivot = new GameObject("HIGHFLY_RUN0_VISUAL_PIVOT");
            _visualPivot.transform.SetParent(player.transform, false);
            _visualPivot.transform.localPosition = Vector3.zero;
            _visualPivot.transform.localRotation = Quaternion.identity;
            _visualPivot.transform.localScale = Vector3.one;

            _hunterRoot = Instantiate(prefab, _visualPivot.transform);
            _hunterRoot.name = "HIGHFLY_RUN0_KAYKIT_HUNTER";
            _hunterRoot.transform.localPosition = Vector3.zero;
            _hunterRoot.transform.localRotation = Quaternion.identity;
            _hunterRoot.transform.localScale = Vector3.one;

            foreach (Collider c in _hunterRoot.GetComponentsInChildren<Collider>(true))
                c.enabled = false;
            foreach (Rigidbody rb in _hunterRoot.GetComponentsInChildren<Rigidbody>(true))
            {
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }

            _hunterAnimator = _hunterRoot.GetComponent<Animator>();
            if (_hunterAnimator == null)
                _hunterAnimator = _hunterRoot.AddComponent<Animator>();

            Avatar[] avatars = Resources.LoadAll<Avatar>(HunterResource);
            for (int i = 0; i < avatars.Length; i++)
            {
                Avatar a = avatars[i];
                if (a != null && a.isValid && a.isHuman)
                {
                    _hunterAnimator.avatar = a;
                    break;
                }
            }

            if (_lucidAnimator != null)
            {
                _hunterAnimator.runtimeAnimatorController = _lucidAnimator.runtimeAnimatorController;
                _lucidAnimator.enabled = false;
            }

            _hunterAnimator.applyRootMotion = false;
            _hunterAnimator.fireEvents = true;
            _hunterAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            for (int i = 0; i < _lucidRenderers.Length; i++)
            {
                Renderer r = _lucidRenderers[i];
                if (r != null && !r.transform.IsChildOf(_hunterRoot.transform))
                    r.enabled = false;
            }

            player.animator = _hunterAnimator;

            NormalizeHunter();
            AttachSword();

            Debug.Log(
                "[RUN0] KAYKIT DIRECT ANIMATOR READY" +
                " human=" + (_hunterAnimator.avatar != null && _hunterAnimator.avatar.isHuman) +
                " controller=" + (_hunterAnimator.runtimeAnimatorController != null));
        }

        private void NormalizeHunter()
        {
            Renderer[] renderers = _hunterRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;

            bool valid = false;
            Bounds b = new Bounds();
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                if (!valid)
                {
                    b = renderers[i].bounds;
                    valid = true;
                }
                else b.Encapsulate(renderers[i].bounds);
            }

            if (!valid || b.size.y < 0.01f) return;

            const float targetHeight = 1.72f;
            float scale = Mathf.Clamp(targetHeight / b.size.y, 0.12f, 2.50f);
            _hunterRoot.transform.localScale = Vector3.one * scale;
            Physics.SyncTransforms();

            valid = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                if (!valid)
                {
                    b = renderers[i].bounds;
                    valid = true;
                }
                else b.Encapsulate(renderers[i].bounds);
            }

            if (valid)
            {
                float footOffset = _player.transform.position.y - b.min.y;
                _hunterRoot.transform.position += Vector3.up * footOffset;
            }

            Debug.Log("[RUN0] Hunter autoscale=" + scale.ToString("0.000"));
        }

        private void AttachSword()
        {
            if (_hunterAnimator == null || !_hunterAnimator.isHuman) return;

            Transform hand = _hunterAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            GameObject swordPrefab = Resources.Load<GameObject>(SwordResource);
            if (hand == null || swordPrefab == null)
            {
                Debug.LogWarning("[RUN0] Sword attach skipped: hand/prefab missing");
                return;
            }

            _sword = Instantiate(swordPrefab, hand);
            _sword.name = "HIGHFLY_RUN0_SWORD";
            _sword.transform.localPosition = Vector3.zero;
            _sword.transform.localRotation = Quaternion.identity;
            _sword.transform.localScale = Vector3.one;

            foreach (Collider c in _sword.GetComponentsInChildren<Collider>(true))
                c.enabled = false;

            BuildWeaponSockets(hand);
        }

        private void BuildWeaponSockets(Transform hand)
        {
            _weaponBase = new GameObject("WeaponBase").transform;
            _weaponBase.SetParent(hand, false);
            _weaponBase.localPosition = Vector3.zero;
            _weaponBase.localRotation = Quaternion.identity;

            _weaponTip = new GameObject("WeaponTip").transform;
            _weaponTip.SetParent(_sword != null ? _sword.transform : hand, false);
            _weaponTip.localRotation = Quaternion.identity;

            MeshFilter mf = _sword != null ? _sword.GetComponentInChildren<MeshFilter>(true) : null;
            if (mf != null && mf.sharedMesh != null)
            {
                Bounds b = mf.sharedMesh.bounds;
                Vector3 e = b.extents;
                Vector3 axis = Vector3.forward;
                float extent = e.z;

                if (e.y > extent)
                {
                    axis = Vector3.up;
                    extent = e.y;
                }
                if (e.x > extent)
                {
                    axis = Vector3.right;
                    extent = e.x;
                }

                _weaponTip.localPosition = b.center + axis * extent;
            }
            else
            {
                _weaponTip.localPosition = new Vector3(0f, 0f, 0.85f);
            }
        }

        public void ResetVisualFacing()
        {
            if (_visualPivot != null)
                _visualPivot.transform.localRotation = Quaternion.identity;
        }
    }
}
