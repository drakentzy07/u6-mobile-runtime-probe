using UnityEngine;

namespace Highfly.SkillLab
{
    [DisallowMultipleComponent]
    public sealed class HighflyAnimatorMirrorV027 : MonoBehaviour
    {
        public static HighflyAnimatorMirrorV027 Instance { get; private set; }

        private Animator _source;
        private Animator _visual;

        public static HighflyAnimatorMirrorV027 Install(PlayerController player)
        {
            if (player == null) return null;

            var m = player.GetComponent<HighflyAnimatorMirrorV027>();
            if (m == null)
                m = player.gameObject.AddComponent<HighflyAnimatorMirrorV027>();

            Instance = m;
            return m;
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Bind(Animator source, Animator visual)
        {
            _source = source;
            _visual = visual;

            if (_visual != null)
            {
                _visual.applyRootMotion = false;
                _visual.fireEvents = false;
                _visual.updateMode = AnimatorUpdateMode.Normal;
            }

            HighflyLabTestHistoryV026.Log(
                "ANIM MIRROR • source=" +
                (_source != null ? _source.name : "null") +
                " visual=" +
                (_visual != null ? _visual.name : "null"));
        }

        private void Update()
        {
            if (_source == null || _visual == null ||
                !_source.isActiveAndEnabled || !_visual.isActiveAndEnabled)
                return;

            CopyParameters();
        }

        private void LateUpdate()
        {
            if (_source == null || _visual == null ||
                !_source.isActiveAndEnabled || !_visual.isActiveAndEnabled)
                return;

            HighflyParkourAnimationV010 parkour = HighflyParkourAnimationV010.Instance;
            if (parkour != null &&
                parkour.IsPlaying &&
                parkour.BoundAnimator == _visual)
                return;

            int layers = Mathf.Min(_source.layerCount, _visual.layerCount);
            for (int layer = 0; layer < layers; layer++)
            {
                AnimatorStateInfo src = _source.GetCurrentAnimatorStateInfo(layer);
                AnimatorStateInfo dst = _visual.GetCurrentAnimatorStateInfo(layer);

                if (src.fullPathHash == 0)
                    continue;

                float srcNorm = Mathf.Repeat(src.normalizedTime, 1f);
                float dstNorm = Mathf.Repeat(dst.normalizedTime, 1f);
                float drift = Mathf.Abs(Mathf.DeltaAngle(srcNorm * 360f, dstNorm * 360f)) / 360f;

                if (dst.fullPathHash != src.fullPathHash || drift > 0.16f)
                    _visual.Play(src.fullPathHash, layer, srcNorm);
            }
        }

        private void CopyParameters()
        {
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
                    // Triggers are deliberately not copied. The visual animator follows
                    // the logical LUCID state machine by state hash in LateUpdate.
                    case AnimatorControllerParameterType.Trigger:
                        break;
                }
            }
        }
    }
}
