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
            _controller.center = new Vector3(0f, 0.86f, 0f);
            _cameraRig = cameraRig;

            BuildHunter();

            _animation = gameObject.AddComponent<HighflyAnimationDriver>();
            _animation.Initialize(_animator);
        }

        private void BuildHunter()
        {
            GameObject prefab =
                Resources.Load<GameObject>("HIGHFLY/Run0/KayKitKnight");

            if (prefab == null)
            {
                Debug.LogError("[CLEAN-RUN0A] KayKitKnight resource missing.");
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

            Avatar[] avatars =
                Resources.LoadAll<Avatar>("HIGHFLY/Run0/KayKitKnight");

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
            AttachSword();

            Debug.Log(
                "[CLEAN-RUN0A] Hunter ready • human=" +
                (_animator.avatar != null && _animator.avatar.isHuman));
        }

        private void NormalizeVisual()
        {
            Renderer[] renderers = _visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            if (bounds.size.y <= 0.01f) return;

            float scale = Mathf.Clamp(1.72f / bounds.size.y, 0.1f, 3f);
            _visual.transform.localScale = Vector3.one * scale;
            Physics.SyncTransforms();

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            _visual.transform.position +=
                Vector3.up * (transform.position.y - bounds.min.y);
        }

        private void ApplyFallbackTexture()
        {
            Texture2D texture =
                Resources.Load<Texture2D>("HIGHFLY/Run0/knight_texture");

            if (texture == null) return;

            Shader shader = Shader.Find("Standard");
            if (shader == null) return;

            foreach (Renderer r in _visual.GetComponentsInChildren<Renderer>(true))
            {
                Material m = new Material(shader);
                m.mainTexture = texture;
                r.material = m;
            }
        }

        private void AttachSword()
        {
            if (_animator == null || !_animator.isHuman) return;

            Transform hand =
                _animator.GetBoneTransform(HumanBodyBones.RightHand);

            GameObject swordPrefab =
                Resources.Load<GameObject>("HIGHFLY/Run0/KayKitSword1H");

            if (hand == null || swordPrefab == null) return;

            GameObject sword = Instantiate(swordPrefab, hand);
            sword.name = "HIGHFLY_RUN0_SWORD";
            sword.transform.localPosition = Vector3.zero;
            sword.transform.localRotation = Quaternion.identity;
            sword.transform.localScale = Vector3.one;

            foreach (Collider c in sword.GetComponentsInChildren<Collider>(true))
                c.enabled = false;
        }

        private void Update()
        {
            if (_controller == null ||
                _cameraRig == null ||
                HighflyInputRouter.Instance == null)
                return;

            HighflyInputRouter input = HighflyInputRouter.Instance;

            if (input.ConsumeAttack())
                _animation?.TryAttack();

            bool attacking = _animation != null && _animation.IsAttacking;
            Vector2 raw = attacking ? Vector2.zero : input.Move;

            Vector3 desired =
                _cameraRig.FlatForward * raw.y +
                _cameraRig.FlatRight * raw.x;

            if (desired.sqrMagnitude > 1f) desired.Normalize();

            Vector3 desiredVelocity = desired * MoveSpeed;

            _velocity = Vector3.MoveTowards(
                _velocity,
                desiredVelocity,
                Acceleration * Time.deltaTime);

            if (_controller.isGrounded)
            {
                if (_verticalVelocity < 0f) _verticalVelocity = -2f;
                if (!attacking && input.ConsumeJump()) _verticalVelocity = JumpSpeed;
            }

            _verticalVelocity -= Gravity * Time.deltaTime;

            Vector3 motion = _velocity;
            motion.y = _verticalVelocity;
            _controller.Move(motion * Time.deltaTime);

            Vector3 planar = new Vector3(_velocity.x, 0f, _velocity.z);
            if (planar.sqrMagnitude > 0.04f)
            {
                Quaternion wanted =
                    Quaternion.LookRotation(planar.normalized, Vector3.up);

                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    wanted,
                    RotationSpeed * Time.deltaTime);
            }

            float normalized = Mathf.Clamp01(planar.magnitude / MoveSpeed);
            _animation?.PlayLocomotion(normalized);
        }
    }
}
