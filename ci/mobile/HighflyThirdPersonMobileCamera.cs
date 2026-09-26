using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Highfly.Mobile
{
    /// <summary>
    /// Touch-first third-person camera for HIGHFLY mobile.
    /// The camera owns its yaw/pitch independently from player facing.
    /// Only HighflyLookZone may call AddLookDelta.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HighflyThirdPersonMobileCamera : MonoBehaviour
    {
        [Header("Framing")]
        [SerializeField] private float pivotHeight = 1.55f;
        [SerializeField] private float distance = 5.6f;
        [SerializeField] private float minDistance = 2.2f;
        [SerializeField] private float maxDistance = 7.5f;

        [Header("Look")]
        [SerializeField] private float yawDegreesPerPixel = 0.22f;
        [SerializeField] private float pitchDegreesPerPixel = 0.18f;
        [SerializeField] private float minPitch = -32f;
        [SerializeField] private float maxPitch = 62f;

        [Header("Follow")]
        [SerializeField] private float positionSmoothTime = 0.035f;
        [SerializeField] private float collisionRadius = 0.18f;
        [SerializeField] private float collisionPadding = 0.12f;
        [SerializeField] private float lockYawSpeed = 14f;

        private Camera _camera;
        private PlayerController _player;
        private Vector3 _positionVelocity;
        private float _yaw;
        private float _pitch;
        private bool _initialized;
        private readonly RaycastHit[] _hits = new RaycastHit[24];

        public static HighflyThirdPersonMobileCamera Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void AddLookDelta(Vector2 screenDelta)
        {
            EnsureReady();
            if (!_initialized) return;

            _yaw += screenDelta.x * yawDegreesPerPixel;
            _pitch = Mathf.Clamp(
                _pitch - screenDelta.y * pitchDegreesPerPixel,
                minPitch,
                maxPitch);
        }

        private void Update()
        {
            if (HighflyMobileBootstrap.TouchInputActive) return;

            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.enabled || !mouse.rightButton.isPressed) return;

            Vector2 delta = mouse.delta.ReadValue();
            if (delta.sqrMagnitude > 0.0001f)
                AddLookDelta(delta);
        }

        // LAB-only convenience API: same Golden camera, just re-seeded after a lab teleport.
        public void SnapBehindPlayer(float pitchDegrees = 14f)
        {
            EnsureReady();
            if (!_initialized || _camera == null || _player == null) return;

            _yaw = _player.transform.eulerAngles.y;
            _pitch = Mathf.Clamp(pitchDegrees, minPitch, maxPitch);
            _positionVelocity = Vector3.zero;

            Vector3 pivot = GetPivot();
            Quaternion orbit = Quaternion.Euler(_pitch, _yaw, 0f);
            _camera.transform.position =
                pivot - orbit * Vector3.forward * Mathf.Clamp(distance, minDistance, maxDistance);

            Vector3 lookDirection = pivot - _camera.transform.position;
            if (lookDirection.sqrMagnitude > 0.0001f)
                _camera.transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }

        private void LateUpdate()
        {
            EnsureReady();
            if (!_initialized || _camera == null || _player == null) return;

            // Lock-on may steer horizontal heading, but free-look yaw is otherwise
            // never derived from player rotation.
            if (_player.IsLockOn && _player.LockOnTarget != null)
            {
                Vector3 pivot = GetPivot();
                Vector3 toTarget = _player.LockOnTarget.position - pivot;
                toTarget.y = 0f;

                if (toTarget.sqrMagnitude > 0.001f)
                {
                    float targetYaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
                    _yaw = Mathf.LerpAngle(
                        _yaw,
                        targetYaw,
                        1f - Mathf.Exp(-lockYawSpeed * Time.unscaledDeltaTime));
                }
            }

            Quaternion orbit = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 pivotPosition = GetPivot();
            Vector3 desiredPosition =
                pivotPosition - orbit * Vector3.forward * Mathf.Clamp(distance, minDistance, maxDistance);

            desiredPosition = ResolveCollision(pivotPosition, desiredPosition);

            float smooth = Mathf.Max(0.001f, positionSmoothTime);
            _camera.transform.position = Vector3.SmoothDamp(
                _camera.transform.position,
                desiredPosition,
                ref _positionVelocity,
                smooth,
                Mathf.Infinity,
                Time.unscaledDeltaTime);

            Vector3 lookTarget = pivotPosition;
            if (_player.IsLockOn && _player.LockOnTarget != null)
            {
                Vector3 targetPoint = _player.LockOnTarget.position + Vector3.up * 0.8f;
                lookTarget = Vector3.Lerp(pivotPosition, targetPoint, 0.34f);
            }

            Vector3 lookDirection = lookTarget - _camera.transform.position;
            if (lookDirection.sqrMagnitude > 0.0001f)
                _camera.transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);

            // Lucid movement remains camera-relative, but this camera transform
            // no longer inherits player yaw.
            _player.cameraTransform = _camera.transform;
        }

        private void EnsureReady()
        {
            if (_camera == null)
                _camera = Camera.main;

            if (_player == null)
                _player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();

            if (_camera == null || _player == null)
                return;

            DisableCinemachineBrain(_camera);

            if (_initialized)
                return;

            Vector3 pivot = GetPivot();
            Vector3 offset = _camera.transform.position - pivot;

            if (offset.sqrMagnitude < 0.25f)
                offset = new Vector3(0f, 1.2f, -distance);

            float derivedDistance = offset.magnitude;
            distance = Mathf.Clamp(derivedDistance, minDistance, maxDistance);

            Vector3 forward = (pivot - _camera.transform.position).normalized;
            if (forward.sqrMagnitude > 0.001f)
            {
                _yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
                _pitch = Mathf.Asin(Mathf.Clamp(forward.y, -1f, 1f)) * Mathf.Rad2Deg;
                _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
            }
            else
            {
                _yaw = _player.transform.eulerAngles.y;
                _pitch = 14f;
            }

            _player.cameraTransform = _camera.transform;
            _initialized = true;
        }

        private Vector3 GetPivot()
        {
            return _player.transform.position + Vector3.up * pivotHeight;
        }

        private Vector3 ResolveCollision(Vector3 pivot, Vector3 desired)
        {
            Vector3 delta = desired - pivot;
            float length = delta.magnitude;
            if (length <= 0.001f) return desired;

            Vector3 direction = delta / length;
            int count = Physics.SphereCastNonAlloc(
                pivot,
                collisionRadius,
                direction,
                _hits,
                length,
                ~0,
                QueryTriggerInteraction.Ignore);

            float nearest = length;

            for (int i = 0; i < count; i++)
            {
                var hit = _hits[i];
                if (hit.collider == null) continue;

                Transform hitTransform = hit.collider.transform;
                if (hitTransform == _player.transform ||
                    hitTransform.IsChildOf(_player.transform))
                    continue;

                if (hit.distance > 0f && hit.distance < nearest)
                    nearest = hit.distance;
            }

            if (nearest < length)
                return pivot + direction * Mathf.Max(0.35f, nearest - collisionPadding);

            return desired;
        }

        private static void DisableCinemachineBrain(Camera camera)
        {
            var behaviours = camera.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == null || !behaviour.enabled) continue;

                Type type = behaviour.GetType();
                string name = type.FullName ?? type.Name;

                if (name.IndexOf("CinemachineBrain", StringComparison.OrdinalIgnoreCase) >= 0)
                    behaviour.enabled = false;
            }
        }
    }
}
