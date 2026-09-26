using UnityEngine;
using Highfly.Run0H;

namespace Highfly.Run0I
{
    [DisallowMultipleComponent]
    public sealed class HighflyRun0IParkour : MonoBehaviour
    {
        public static HighflyRun0IParkour Instance { get; private set; }
        private PlayerController _player;
        private CharacterController _controller;
        private int _jumpCount;
        private Vector3 _wallImpulse;
        private float _wallImpulseUntil;
        private float _clipUntil;
        private bool _wasGrounded = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (FindAnyObjectByType<HighflyRun0IParkour>() != null) return;
            GameObject go = new GameObject("HIGHFLY_RUN0I_PARKOUR");
            DontDestroyOnLoad(go);
            go.AddComponent<HighflyRun0IParkour>();
        }

        private void Awake() { Instance = this; DontDestroyOnLoad(gameObject); }

        private void Update()
        {
            if (_player == null)
            {
                _player = FindAnyObjectByType<PlayerController>();
                if (_player != null) _controller = _player.GetComponent<CharacterController>();
                return;
            }

            bool groundedNow = _player.HighflyIsGrounded && _player.HighflyVerticalSpeed <= 0f;
            if (groundedNow)
            {
                _jumpCount = 0;
                if (!_wasGrounded)
                {
                    // End any parkour playable immediately on contact and fast-forward the
                    // donor's long crouched landing recovery so control feels responsive.
                    _clipUntil = 0f;
                    HighflyRun0HCharacterVisual.Instance?.RecoverFromLanding();
                }
            }
            _wasGrounded = groundedNow;

            if (_controller != null && Time.unscaledTime < _wallImpulseUntil)
                _controller.Move(_wallImpulse * Time.deltaTime);

            if (_clipUntil > 0f && Time.unscaledTime >= _clipUntil)
            {
                _clipUntil = 0f;
                HighflyRun0HCharacterVisual.Instance?.StopActionClip();
            }
        }

        public bool RequestJump()
        {
            if (_player == null) return false;
            if (Highfly.Combat.HighflyLucidCombatBridge.Instance != null &&
                Highfly.Combat.HighflyLucidCombatBridge.Instance.ActionBusy) return true;

            if (_player.HighflyIsGrounded)
            {
                _jumpCount = 1;
                _player.HighflyLabSetVerticalSpeed(6.4f,false);
                Play("NinjaJump_Start",0.48f);
                return true;
            }

            RaycastHit hit;
            if (TryFindWall(out hit))
            {
                _jumpCount = Mathf.Max(_jumpCount,1);
                _player.HighflyLabSetVerticalSpeed(7.1f,false);
                Vector3 away = hit.normal; away.y = 0f;
                if (away.sqrMagnitude < 0.01f) away = -_player.transform.forward;
                _wallImpulse = away.normalized * 3.0f;
                _wallImpulseUntil = Time.unscaledTime + 0.18f;
                Play("ClimbUp_1m",0.62f);
                return true;
            }

            if (_jumpCount < 2)
            {
                _jumpCount = 2;
                _player.HighflyLabSetVerticalSpeed(6.0f,false);
                Play("NinjaJump_Start",0.45f);
                return true;
            }

            return true;
        }

        private bool TryFindWall(out RaycastHit best)
        {
            best = default;
            if (_controller == null) return false;
            Vector3 origin = _player.transform.position + Vector3.up;
            Vector3[] dirs = { _player.transform.forward, _player.transform.right, -_player.transform.right };
            for (int i=0; i<dirs.Length; i++)
            {
                RaycastHit hit;
                if (Physics.SphereCast(origin,0.22f,dirs[i],out hit,0.85f,~0,QueryTriggerInteraction.Ignore))
                {
                    if (hit.transform == _player.transform || hit.transform.IsChildOf(_player.transform)) continue;
                    if (Mathf.Abs(hit.normal.y) < 0.35f) { best = hit; return true; }
                }
            }
            return false;
        }

        private void Play(string clip,float seconds)
        {
            HighflyRun0HCharacterVisual.Instance?.PlayActionClip(clip);
            _clipUntil = Time.unscaledTime + seconds;
        }
    }
}
