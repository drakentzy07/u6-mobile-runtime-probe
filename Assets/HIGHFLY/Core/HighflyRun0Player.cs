using System;
using UnityEngine;

namespace Highfly.Clean
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class HighflyRun0Player : MonoBehaviour
    {
        private CharacterController _controller;
        private HighflyCameraRig _cameraRig;
        private HighflyAnimationDriver _animation;
        private Animator _animator;
        private GameObject _visual;
        private float _verticalVelocity;
        private Vector3 _velocity;

        public float MoveSpeed = 4.4f;
        public float Acceleration = 18f;
        public float RotationSpeed = 720f;
        public float JumpSpeed = 6.0f;
        public float Gravity = 20f;

        public HighflyAnimationDriver Animation => _animation;

        public void Initialize(HighflyCameraRig cameraRig)
        {
            _controller = GetComponent<CharacterController>();
            _controller.height = 1.72f;
            _controller.radius = 0.32f;
            _controller.center = new Vector3(0f,0.86f,0f);
            _cameraRig = cameraRig;

            BuildHunter();

            if (_animator != null)
            {
                HighflyHumanoidUprightGuard upright =
                    _animator.gameObject.AddComponent<HighflyHumanoidUprightGuard>();
                upright.Initialize(_animator);
            }

            _animation = gameObject.AddComponent<HighflyAnimationDriver>();
            _animation.Initialize(_animator);
        }

        private void BuildHunter()
        {
            GameObject prefab = Resources.Load<GameObject>("HIGHFLY/Run0/KayKitKnight");
            if (prefab == null)
            {
                Debug.LogError("[RUN0E] KayKitKnight resource missing.");
                return;
            }

            _visual = Instantiate(prefab, transform);
            _visual.name = "HIGHFLY_KAYKIT_HUNTER";
            _visual.transform.localPosition = Vector3.zero;
            _visual.transform.localRotation = Quaternion.identity;

            foreach (Collider c in _visual.GetComponentsInChildren<Collider>(true))
                c.enabled = false;

            _animator = _visual.GetComponent<Animator>();
            if (_animator == null) _animator = _visual.AddComponent<Animator>();

            Avatar[] avatars = Resources.LoadAll<Avatar>("HIGHFLY/Run0/KayKitKnight");
            foreach (Avatar avatar in avatars)
            {
                if (avatar != null && avatar.isValid && avatar.isHuman)
                {
                    _animator.avatar = avatar;
                    break;
                }
            }

            _animator.applyRootMotion = false;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            NormalizeVisual();
            ApplyFallbackTexture();
            RemoveEmbeddedEquipmentVisuals();
            AttachSingleSword();

            Debug.Log("[RUN0E] Hunter ready • sanitized equipment • one explicit sword.");
        }

        private void RemoveEmbeddedEquipmentVisuals()
        {
            Renderer[] renderers = _visual.GetComponentsInChildren<Renderer>(true);
            int disabled = 0;

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
                    disabled++;
                    Debug.Log("[RUN0E] disabled embedded equipment renderer: " + path);
                }
            }

            Debug.Log("[RUN0E] embedded equipment renderers disabled=" + disabled);
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

        private void AttachSingleSword()
        {
            if (_animator == null || !_animator.isHuman) return;

            Transform hand = _animator.GetBoneTransform(HumanBodyBones.RightHand);
            GameObject prefab = Resources.Load<GameObject>("HIGHFLY/Run0/KayKitSword1H");

            if (hand == null || prefab == null)
            {
                Debug.LogWarning("[RUN0E] single sword not attached: hand/prefab missing.");
                return;
            }

            GameObject sword = Instantiate(prefab, hand);
            sword.name = "HIGHFLY_SINGLE_SWORD";
            sword.transform.localPosition = Vector3.zero;
            sword.transform.localRotation = Quaternion.identity;
            sword.transform.localScale = Vector3.one;

            foreach (Collider c in sword.GetComponentsInChildren<Collider>(true))
                c.enabled = false;

            Debug.Log("[RUN0E] exactly one explicit KayKit sword attached.");
        }

        private void NormalizeVisual()
        {
            Renderer[] rr = _visual.GetComponentsInChildren<Renderer>(true);
            if (rr.Length == 0) return;

            Bounds bounds = rr[0].bounds;
            for (int i=1;i<rr.Length;i++) bounds.Encapsulate(rr[i].bounds);
            if (bounds.size.y <= 0.01f) return;

            float scale = Mathf.Clamp(1.72f / bounds.size.y,0.1f,3f);
            _visual.transform.localScale = Vector3.one * scale;
            Physics.SyncTransforms();

            bounds = rr[0].bounds;
            for (int i=1;i<rr.Length;i++) bounds.Encapsulate(rr[i].bounds);

            _visual.transform.position += Vector3.up * (transform.position.y - bounds.min.y);
        }

        private void ApplyFallbackTexture()
        {
            Texture2D texture = Resources.Load<Texture2D>("HIGHFLY/Run0/knight_texture");
            Shader shader = Shader.Find("Standard");
            if (texture == null || shader == null) return;

            foreach (Renderer r in _visual.GetComponentsInChildren<Renderer>(true))
            {
                Material m = new Material(shader);
                m.mainTexture = texture;
                r.material = m;
            }
        }

        private void Update()
        {
            if (_controller == null || _cameraRig == null || HighflyInputRouter.Instance == null) return;

            HighflyInputRouter input = HighflyInputRouter.Instance;
            if (input.ConsumeAttack()) _animation?.TryAttack();

            bool attacking = _animation != null && _animation.IsAttacking;
            Vector2 raw = attacking ? Vector2.zero : input.Move;

            Vector3 desired =
                _cameraRig.FlatForward * raw.y +
                _cameraRig.FlatRight * raw.x;

            if (desired.sqrMagnitude > 1f) desired.Normalize();

            Vector3 desiredVelocity = desired * MoveSpeed;
            _velocity = Vector3.MoveTowards(
                _velocity, desiredVelocity, Acceleration * Time.deltaTime);

            if (_controller.isGrounded)
            {
                if (_verticalVelocity < 0f) _verticalVelocity = -2f;
                if (!attacking && input.ConsumeJump()) _verticalVelocity = JumpSpeed;
            }

            _verticalVelocity -= Gravity * Time.deltaTime;

            Vector3 motion = _velocity;
            motion.y = _verticalVelocity;
            _controller.Move(motion * Time.deltaTime);

            Vector3 planar = new Vector3(_velocity.x,0f,_velocity.z);
            if (planar.sqrMagnitude > 0.04f)
            {
                Quaternion wanted = Quaternion.LookRotation(planar.normalized,Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,wanted,RotationSpeed*Time.deltaTime);
            }

            _animation?.PlayLocomotion(Mathf.Clamp01(planar.magnitude/MoveSpeed));
        }
    }
}
