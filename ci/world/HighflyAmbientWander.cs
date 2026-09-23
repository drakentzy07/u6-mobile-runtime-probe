using System;
using UnityEngine;

namespace Highfly.World
{
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
            if (_animator == null) return;

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
                if (Physics.Raycast(next + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 5f, ~0, QueryTriggerInteraction.Ignore))
                    next.y = hit.point.y;

                transform.position = next;
            }

            if (_speedParam != -1)
                _animator.SetFloat(_speedParam, moving ? 1f : 0f);
        }
    }
}
