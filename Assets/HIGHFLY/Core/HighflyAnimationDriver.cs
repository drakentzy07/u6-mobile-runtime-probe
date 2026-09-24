using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Highfly.Clean
{
    public sealed class HighflyAnimationDriver : MonoBehaviour
    {
        private Animator _animator;
        private PlayableGraph _graph;
        private AnimationClip _idle;
        private AnimationClip _move;
        private AnimationClip _run;
        private AnimationClip _attack;
        private AnimationClip _currentLoop;
        private Coroutine _attackRoutine;

        public bool IsAttacking { get; private set; }
        public string AttackClipName => _attack != null ? _attack.name : "MISSING";

        public void Initialize(Animator animator)
        {
            _animator = animator;

            AnimationClip[] clips =
                Resources.LoadAll<AnimationClip>("HIGHFLY/Run0/UAL2_Standard")
                    .Where(c => c != null && !c.name.Contains("__preview__"))
                    .ToArray();

            _attack = Find(clips, "Sword_Regular_A", "SwordRegularA");
            _idle = FindContains(clips, "Idle");
            _move = FindContains(clips, "Walk");
            _run = FindContains(clips, "Run");

            if (_move == null) _move = _idle;
            if (_run == null) _run = _move;

            Debug.Log(
                "[CLEAN-RUN0A] clips=" + clips.Length +
                " attack=" + (_attack != null ? _attack.name : "MISSING") +
                " idle=" + (_idle != null ? _idle.name : "MISSING") +
                " move=" + (_move != null ? _move.name : "MISSING") +
                " run=" + (_run != null ? _run.name : "MISSING"));

            PlayLocomotion(0f);
        }

        public void PlayLocomotion(float normalizedSpeed)
        {
            if (IsAttacking || _animator == null) return;

            AnimationClip wanted =
                normalizedSpeed > 0.72f ? _run :
                normalizedSpeed > 0.08f ? _move :
                _idle;

            if (wanted == null || wanted == _currentLoop) return;

            _currentLoop = wanted;
            PlayClip(wanted, true, 1f);
        }

        public bool TryAttack()
        {
            if (IsAttacking || _attack == null || _animator == null) return false;

            if (_attackRoutine != null) StopCoroutine(_attackRoutine);
            _attackRoutine = StartCoroutine(AttackRoutine());
            return true;
        }

        private IEnumerator AttackRoutine()
        {
            IsAttacking = true;
            _currentLoop = null;
            PlayClip(_attack, false, 1f);
            float duration = Mathf.Clamp(_attack.length, 0.32f, 1.35f);
            yield return new WaitForSeconds(duration);
            IsAttacking = false;
            _attackRoutine = null;
        }

        private void PlayClip(AnimationClip clip, bool loop, float speed)
        {
            StopGraph();

            _graph = PlayableGraph.Create("HIGHFLY_CLEAN_" + clip.name);
            _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            AnimationPlayableOutput output =
                AnimationPlayableOutput.Create(_graph, "Hunter", _animator);

            AnimationClipPlayable playable =
                AnimationClipPlayable.Create(_graph, clip);

            playable.SetApplyFootIK(false);
            playable.SetApplyPlayableIK(false);
            playable.SetSpeed(speed);

            if (loop)
                playable.SetDuration(double.PositiveInfinity);

            output.SetSourcePlayable(playable);
            _graph.Play();
        }

        private static AnimationClip Find(AnimationClip[] clips, params string[] names)
        {
            foreach (string wanted in names)
            {
                string n = Normalize(wanted);
                AnimationClip exact = clips.FirstOrDefault(c => Normalize(c.name) == n);
                if (exact != null) return exact;
            }

            foreach (string wanted in names)
            {
                string n = Normalize(wanted);
                AnimationClip partial = clips.FirstOrDefault(c => Normalize(c.name).Contains(n));
                if (partial != null) return partial;
            }

            return null;
        }

        private static AnimationClip FindContains(AnimationClip[] clips, string token)
        {
            string n = Normalize(token);
            return clips.FirstOrDefault(c => Normalize(c.name).Contains(n));
        }

        private static string Normalize(string value)
        {
            return new string(
                value.Where(char.IsLetterOrDigit)
                     .Select(char.ToLowerInvariant)
                     .ToArray());
        }

        private void OnDestroy() => StopGraph();

        private void StopGraph()
        {
            if (_graph.IsValid()) _graph.Destroy();
        }
    }
}
