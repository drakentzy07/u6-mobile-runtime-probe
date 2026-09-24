using UnityEngine;

namespace Highfly.Clean
{
    public sealed class HighflyCameraRig : MonoBehaviour
    {
        private Transform _target;
        private float _yaw;
        private float _pitch = 15f;

        public float Distance = 5.2f;
        public float Height = 1.45f;
        public float LookSensitivity = 1f;

        public void Bind(Transform target)
        {
            _target = target;
            if (target != null) _yaw = target.eulerAngles.y;
        }

        private void LateUpdate()
        {
            if (_target == null || HighflyInputRouter.Instance == null) return;

            Vector2 look = HighflyInputRouter.Instance.LookDelta;
            _yaw += look.x * LookSensitivity;
            _pitch -= look.y * LookSensitivity;
            _pitch = Mathf.Clamp(_pitch, -15f, 50f);

            Quaternion orbit = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 focus = _target.position + Vector3.up * Height;
            Vector3 desired = focus - orbit * Vector3.forward * Distance;

            transform.position = Vector3.Lerp(
                transform.position,
                desired,
                1f - Mathf.Exp(-18f * Time.deltaTime));

            transform.rotation = Quaternion.LookRotation(
                focus - transform.position,
                Vector3.up);
        }

        public Vector3 FlatForward
        {
            get
            {
                Vector3 f = transform.forward;
                f.y = 0f;
                return f.sqrMagnitude > 0.001f ? f.normalized : Vector3.forward;
            }
        }

        public Vector3 FlatRight
        {
            get
            {
                Vector3 r = transform.right;
                r.y = 0f;
                return r.sqrMagnitude > 0.001f ? r.normalized : Vector3.right;
            }
        }
    }
}
