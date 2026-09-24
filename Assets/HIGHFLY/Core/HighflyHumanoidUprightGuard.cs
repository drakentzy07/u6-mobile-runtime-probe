using UnityEngine;

namespace Highfly.Clean
{
    [DefaultExecutionOrder(10000)]
    public sealed class HighflyHumanoidUprightGuard : MonoBehaviour
    {
        private Animator _animator;
        private HumanPoseHandler _handler;
        private HumanPose _pose;
        private Quaternion _referenceBodyRotation;
        private Vector3 _referenceLocalPosition;
        private Quaternion _referenceLocalRotation;
        private bool _ready;

        public void Initialize(Animator animator)
        {
            _animator = animator;

            if (_animator == null ||
                _animator.avatar == null ||
                !_animator.avatar.isValid ||
                !_animator.avatar.isHuman)
            {
                Debug.LogWarning(
                    "[RUN0E] UprightGuard disabled: valid humanoid avatar missing.");
                return;
            }

            try
            {
                _referenceLocalPosition = _animator.transform.localPosition;
                _referenceLocalRotation = _animator.transform.localRotation;

                _handler = new HumanPoseHandler(
                    _animator.avatar,
                    _animator.transform);

                _pose = new HumanPose();
                _handler.GetHumanPose(ref _pose);

                // Capture KayKit's correct neutral humanoid orientation before
                // UAL2 starts driving the avatar. We restore this body frame
                // after animation evaluation while keeping all muscle values.
                _referenceBodyRotation = _pose.bodyRotation;
                _ready = true;

                Debug.Log(
                    "[RUN0E] UprightGuard ready • bodyRotation=" +
                    _referenceBodyRotation.eulerAngles);
            }
            catch (System.Exception e)
            {
                Debug.LogError(
                    "[RUN0E] UprightGuard init failed: " +
                    e.Message);
            }
        }

        private void LateUpdate()
        {
            if (!_ready || _handler == null)
                return;

            try
            {
                _handler.GetHumanPose(ref _pose);

                // Preserve animation muscles, but never let the imported clip
                // rotate/tilt the whole humanoid body away from KayKit's
                // upright reference frame.
                _pose.bodyRotation = _referenceBodyRotation;

                Vector3 bodyPosition = _pose.bodyPosition;
                bodyPosition.x = 0f;
                bodyPosition.z = 0f;
                _pose.bodyPosition = bodyPosition;

                _handler.SetHumanPose(ref _pose);

                // Last line of defense: Playables/retargeting may also touch
                // the Animator GameObject transform itself.
                _animator.transform.localRotation =
                    _referenceLocalRotation;

                Vector3 local =
                    _animator.transform.localPosition;

                local.x = _referenceLocalPosition.x;
                local.z = _referenceLocalPosition.z;
                _animator.transform.localPosition = local;
            }
            catch (System.Exception e)
            {
                Debug.LogError(
                    "[RUN0E] UprightGuard runtime failed: " +
                    e.Message);

                _ready = false;
            }
        }

        private void OnDestroy()
        {
            if (_handler != null)
            {
                _handler.Dispose();
                _handler = null;
            }
        }
    }
}
