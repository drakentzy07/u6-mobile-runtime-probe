using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Highfly.Combat;
using Highfly.Run0H;
using Highfly.Run0I;

namespace Highfly.ClaudeBridge.Run0D
{
    /// <summary>
    /// Original-full visual LAB for four ClaudeCraft v0.43.3 melee abilities.
    /// No invented gap close/teleport. Original behind/stealth/combo gates are
    /// reported but bypassed only in this visual inspection LAB.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HighflyClaudeOriginalFiveSkillRuntime : MonoBehaviour
    {
        private enum Skill
        {
            Ambush = 2,
            Backstab = 3,
            Eviscerate = 4,
            Pummel = 5
        }

        private const float OriginalAttackTimeScale = 1.3f;
        private const float MeleeRange = 2.65f;

        private PlayerController _player;
        private PlayerStats _self;
        private global::Highfly.ClaudeBridge.Run0B.HighflyClaudeHeroicLeapRuntime _heroic;
        private HighflyClaudeOriginalVfxRunner _vfx;
        private AudioSource _audio;
        private AudioClip _swing;
        private AudioClip _longSwing;
        private AudioClip _hit;

        private bool _busy;
        private float _pummelReadyAt;
        private string _status = "RUN0D • ORIGINAL FULL • ACERCATE AL DUMMY • 1-5";
        private GUIStyle _buttonStyle;
        private GUIStyle _labelStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (FindAnyObjectByType<HighflyClaudeOriginalFiveSkillRuntime>() != null) return;
            GameObject root = new GameObject("HIGHFLY_CLAUDECRAFT_RUN0D_ORIGINAL_FULL");
            DontDestroyOnLoad(root);
            root.AddComponent<HighflyClaudeOriginalFiveSkillRuntime>();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _vfx = HighflyClaudeOriginalVfxRunner.Ensure(gameObject);

            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;

            // Legal HIGHFLY replacement bytes; ClaudeCraft restricted audio is not copied.
            _swing = Resources.Load<AudioClip>("HIGHFLY/Run0I/swing");
            _longSwing = Resources.Load<AudioClip>("HIGHFLY/Run0I/longSwing");
            _hit = Resources.Load<AudioClip>("HIGHFLY/Run0I/hit");
        }

        private void Update()
        {
            ResolveReferences();
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || _player == null) return;

            if (!_busy && keyboard.digit1Key.wasPressedThisFrame && _heroic != null)
                _heroic.TryCast();

            if (keyboard.digit2Key.wasPressedThisFrame) TryCast(Skill.Ambush);
            if (keyboard.digit3Key.wasPressedThisFrame) TryCast(Skill.Backstab);
            if (keyboard.digit4Key.wasPressedThisFrame) TryCast(Skill.Eviscerate);
            if (keyboard.digit5Key.wasPressedThisFrame) TryCast(Skill.Pummel);
        }

        private void ResolveReferences()
        {
            if (_player == null)
            {
                _player = FindAnyObjectByType<PlayerController>();
                if (_player != null) _self = _player.GetComponent<PlayerStats>();
            }

            if (_heroic == null)
                _heroic = FindAnyObjectByType<global::Highfly.ClaudeBridge.Run0B.HighflyClaudeHeroicLeapRuntime>();
        }

        private void OnGUI()
        {
            ResolveReferences();

            if (_buttonStyle == null)
            {
                _buttonStyle = new GUIStyle(GUI.skin.button) {
                    fontSize = 15,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                _labelStyle = new GUIStyle(GUI.skin.label) {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };
                _labelStyle.normal.textColor = Color.white;
            }

            float gap = 7f;
            float bw = Mathf.Min(220f, Screen.width * 0.18f);
            float bh = 58f;
            float total = bw * 4f + gap * 3f;
            float x = Mathf.Max(8f, (Screen.width - total) * 0.5f);
            float y = Screen.height - bh - 12f;

            GUI.Label(new Rect(x, y - 46f, total, 42f), _status, _labelStyle);

            DrawButton(new Rect(x + (bw + gap) * 0f, y, bw, bh),
                "2 • LURKER'S STRIKE\nAMBUSH", Skill.Ambush);
            DrawButton(new Rect(x + (bw + gap) * 1f, y, bw, bh),
                "3 • CRAVEN THRUST\nBACKSTAB", Skill.Backstab);
            DrawButton(new Rect(x + (bw + gap) * 2f, y, bw, bh),
                "4 • DIRT NAP\nEVISCERATE", Skill.Eviscerate);

            float remain = Mathf.Max(0f, _pummelReadyAt - Time.unscaledTime);
            string pummel = remain > 0f
                ? "5 • JAWCRACK\nPUMMEL " + remain.ToString("0.0") + "s"
                : "5 • JAWCRACK\nPUMMEL";
            DrawButton(new Rect(x + (bw + gap) * 3f, y, bw, bh), pummel, Skill.Pummel);
        }

        private void DrawButton(Rect rect, string label, Skill skill)
        {
            bool cd = skill != Skill.Pummel || Time.unscaledTime >= _pummelReadyAt;
            GUI.enabled = !_busy && _player != null && cd;
            if (GUI.Button(rect, label, _buttonStyle)) TryCast(skill);
            GUI.enabled = true;
        }

        private void TryCast(Skill skill)
        {
            ResolveReferences();
            if (_busy || _player == null) return;

            if (_player.currentState != PlayerState.Locomotion)
            {
                _status = "OCUPADO • termina la acción actual";
                return;
            }

            HighflyLucidCombatBridge combat = HighflyLucidCombatBridge.Instance;
            if (combat != null && combat.ActionBusy)
            {
                _status = "OCUPADO • CombatCore en acción";
                return;
            }

            if (skill == Skill.Pummel && Time.unscaledTime < _pummelReadyAt)
                return;

            CharacterStats target = ResolveTarget();
            if (target == null)
            {
                _status = "SIN OBJETIVO • acercate a un dummy";
                return;
            }

            float planarDistance = PlanarDistance(_player.transform.position, target.transform.position);
            if (planarDistance > MeleeRange)
            {
                _status = "ORIGINAL MELEE • estás a " + planarDistance.ToString("0.0") +
                          "m • acercate a menos de " + MeleeRange.ToString("0.0") + "m";
                return;
            }

            if (skill == Skill.Pummel)
                _pummelReadyAt = Time.unscaledTime + 10f;

            StartCoroutine(CastOriginal(skill, target));
        }

        private IEnumerator CastOriginal(Skill skill, CharacterStats target)
        {
            _busy = true;
            _player.currentState = PlayerState.Skill;

            HighflyRun0HCharacterVisual visual = HighflyRun0HCharacterVisual.Instance;
            string clip = ClipFor(skill);
            float clipLength = visual != null ? visual.GetActionClipLength(clip) : 0f;
            float bodyDuration = clipLength > 0f
                ? clipLength / OriginalAttackTimeScale
                : FallbackBodyDuration(skill);

            bool behind = IsBehind(target);
            string gate = GateText(skill, behind);
            _status = DisplayName(skill) + " • ORIGINAL FULL • " + gate;

            if (visual != null)
            {
                visual.SetWeaponTrail(false);
                visual.SetWeaponsVisible(skill != Skill.Pummel);
                LockFacing(visual, target);
                visual.PlayActionClip(clip, OriginalAttackTimeScale);
            }

            PlaySfx(skill == Skill.Eviscerate ? _longSwing : _swing);

            HighflyClaudeOriginalVfxRunner.Skill vfxSkill = ToVfxSkill(skill);
            _vfx.Play(vfxSkill, _player.transform, target);

            float impactAt = OriginalImpactAt(skill);
            float elapsed = 0f;
            while (elapsed < impactAt && target != null)
            {
                elapsed += Time.unscaledDeltaTime;
                LockFacing(visual, target);
                yield return null;
            }

            if (target != null)
            {
                ResolveGameplayEffect(skill, target);
                PlaySfx(_hit);
            }

            while (elapsed < bodyDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (target != null) LockFacing(visual, target);
                yield return null;
            }

            if (visual != null)
            {
                visual.SetWeaponTrail(false);
                visual.SetWeaponsVisible(true);
                visual.StopActionClip();
                visual.ClearActionFacing();
            }

            if (_player != null && _player.currentState == PlayerState.Skill)
                _player.currentState = PlayerState.Locomotion;

            _status = DisplayName(skill) + " • BODY + VFX ORIGINAL COMPLETADOS";
            _busy = false;
        }

        private void ResolveGameplayEffect(Skill skill, CharacterStats target)
        {
            if (target == null || _player == null) return;

            float weaponDamage = _self != null ? Mathf.Max(0f, _self.attackPower) : 0f;

            switch (skill)
            {
                case Skill.Ambush:
                    // Claude rank-1: 250% weapon damage + 28.
                    target.TakeDamage(weaponDamage * 2.5f + 28f, 0f, _player.transform);
                    break;
                case Skill.Backstab:
                    // Claude rank-1: 150% weapon damage + 11.
                    target.TakeDamage(weaponDamage * 1.5f + 11f, 0f, _player.transform);
                    break;
                case Skill.Eviscerate:
                    // Claude rank-1 finisher: base 4 + 7 per combo, variance 4.
                    // RUN0D feeds exactly five combo points for isolated visual testing.
                    target.TakeDamage(4f + 7f * 5f + Random.Range(-4, 5), 0f, _player.transform);
                    break;
                case Skill.Pummel:
                    // Claude effect is interrupt only: 4s school lockout and +10 rage
                    // when a cast is actually stopped. No fake damage/knockback here.
                    break;
            }
        }

        private CharacterStats ResolveTarget()
        {
            if (_player.LockOnTarget != null)
            {
                CharacterStats locked = _player.LockOnTarget.GetComponentInParent<CharacterStats>();
                if (locked != null && locked != _self) return locked;
            }

            Collider[] hits = Physics.OverlapSphere(
                _player.transform.position,
                MeleeRange + 1.0f,
                ~0,
                QueryTriggerInteraction.Collide);

            CharacterStats best = null;
            float bestSqr = float.PositiveInfinity;
            for (int i = 0; i < hits.Length; i++)
            {
                Collider c = hits[i];
                if (c == null) continue;
                CharacterStats stats = c.GetComponentInParent<CharacterStats>();
                if (stats == null || stats == _self) continue;

                Vector3 delta = stats.transform.position - _player.transform.position;
                delta.y = 0f;
                float sqr = delta.sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = stats;
                }
            }
            return best;
        }

        private void LockFacing(HighflyRun0HCharacterVisual visual, CharacterStats target)
        {
            if (_player == null || target == null) return;
            Vector3 forward = target.transform.position - _player.transform.position;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) return;
            forward.Normalize();

            _player.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            if (visual != null) visual.SetActionFacing(forward);
        }

        private bool IsBehind(CharacterStats target)
        {
            if (target == null || _player == null) return false;
            Vector3 fromTarget = _player.transform.position - target.transform.position;
            fromTarget.y = 0f;
            if (fromTarget.sqrMagnitude < 0.0001f) return false;
            fromTarget.Normalize();

            Vector3 targetForward = target.transform.forward;
            targetForward.y = 0f;
            if (targetForward.sqrMagnitude < 0.0001f) return false;
            targetForward.Normalize();

            return Vector3.Dot(targetForward, fromTarget) < -0.35f;
        }

        private static string GateText(Skill skill, bool behind)
        {
            switch (skill)
            {
                case Skill.Ambush:
                    return behind
                        ? "DETRÁS OK • STEALTH BYPASS LAB"
                        : "DETRÁS+STEALTH BYPASS LAB • SIN MOVIMIENTO INVENTADO";
                case Skill.Backstab:
                    return behind
                        ? "DETRÁS OK"
                        : "DETRÁS BYPASS LAB • SIN MOVIMIENTO INVENTADO";
                case Skill.Eviscerate:
                    return "5 COMBO POINTS SIMULADOS EN LAB";
                default:
                    return "INTERRUPT 4s • CD 10s";
            }
        }

        private static float OriginalImpactAt(Skill skill)
        {
            // sequencer.ts: non-projectile instant impact = windupDelay + 0.15s.
            switch (skill)
            {
                case Skill.Ambush: return 0.65f;
                case Skill.Eviscerate: return 0.35f;
                default: return 0.15f;
            }
        }

        private static string ClipFor(Skill skill)
        {
            switch (skill)
            {
                case Skill.Ambush: return "Claude_Rogue_Ambush";
                case Skill.Backstab: return "Claude_Rogue_Backstab";
                case Skill.Eviscerate: return "Claude_Rogue_Finisher_Slash";
                default: return "Claude_Punch_A";
            }
        }

        private static string DisplayName(Skill skill)
        {
            switch (skill)
            {
                case Skill.Ambush: return "LURKER'S STRIKE / AMBUSH";
                case Skill.Backstab: return "CRAVEN THRUST / BACKSTAB";
                case Skill.Eviscerate: return "DIRT NAP / EVISCERATE";
                default: return "JAWCRACK / PUMMEL";
            }
        }

        private static HighflyClaudeOriginalVfxRunner.Skill ToVfxSkill(Skill skill)
        {
            switch (skill)
            {
                case Skill.Ambush: return HighflyClaudeOriginalVfxRunner.Skill.Ambush;
                case Skill.Backstab: return HighflyClaudeOriginalVfxRunner.Skill.Backstab;
                case Skill.Eviscerate: return HighflyClaudeOriginalVfxRunner.Skill.Eviscerate;
                default: return HighflyClaudeOriginalVfxRunner.Skill.Pummel;
            }
        }

        private static float FallbackBodyDuration(Skill skill)
        {
            switch (skill)
            {
                case Skill.Backstab: return 0.62f / OriginalAttackTimeScale;
                case Skill.Pummel: return 0.70f / OriginalAttackTimeScale;
                default: return 1.15f / OriginalAttackTimeScale;
            }
        }

        private void PlaySfx(AudioClip clip)
        {
            if (_audio != null && clip != null) _audio.PlayOneShot(clip);
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            Vector3 d = b - a;
            d.y = 0f;
            return d.magnitude;
        }
    }
}
