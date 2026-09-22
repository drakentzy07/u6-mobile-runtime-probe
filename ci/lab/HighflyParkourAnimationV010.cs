using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Highfly.SkillLab
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class HighflyParkourAnimationV010 : MonoBehaviour
    {
        public static HighflyParkourAnimationV010 Instance { get; private set; }

        private Animator _animator;
        private PlayableGraph _graph;
        private Coroutine _stopRoutine;
        private int _token;

        private void Awake()
        {
            Instance = this;
            _animator = GetComponent<Animator>();
        }

        private void OnDestroy()
        {
            StopGraph();
            if (Instance == this) Instance = null;
        }

        public bool Play(string clipName, float speed = 1f, float maxDuration = -1f)
        {
            if (_animator == null || string.IsNullOrEmpty(clipName))
                return false;

            AnimationClip clip =
                Resources.Load<AnimationClip>(
                    "HIGHFLY/Parkour/" + clipName);

            if (clip == null)
            {
                Debug.LogWarning(
                    "[HIGHFLY v0.10] Parkour clip missing: " +
                    clipName);
                return false;
            }

            StopGraph();
            _token++;

            _graph =
                PlayableGraph.Create(
                    "HIGHFLY_PARKOUR_" + clipName);

            _graph.SetTimeUpdateMode(
                DirectorUpdateMode.UnscaledGameTime);

            AnimationPlayableOutput output =
                AnimationPlayableOutput.Create(
                    _graph,
                    "Hunter",
                    _animator);

            AnimationClipPlayable playable =
                AnimationClipPlayable.Create(
                    _graph,
                    clip);

            playable.SetApplyFootIK(false);
            playable.SetApplyPlayableIK(false);
            playable.SetSpeed(Mathf.Max(0.05f, speed));

            output.SetSourcePlayable(playable);
            _graph.Play();

            float duration =
                maxDuration > 0f
                    ? maxDuration
                    : Mathf.Max(
                        0.08f,
                        clip.length /
                        Mathf.Max(0.05f, speed));

            int myToken = _token;
            _stopRoutine =
                StartCoroutine(
                    StopAfter(duration, myToken));

            return true;
        }

        private IEnumerator StopAfter(
            float seconds,
            int token)
        {
            yield return new WaitForSecondsRealtime(seconds);

            if (token == _token)
                StopGraph();
        }

        public void StopNow()
        {
            _token++;
            StopGraph();
        }

        private void StopGraph()
        {
            if (_stopRoutine != null)
            {
                StopCoroutine(_stopRoutine);
                _stopRoutine = null;
            }

            if (_graph.IsValid())
                _graph.Destroy();
        }
    }
}
