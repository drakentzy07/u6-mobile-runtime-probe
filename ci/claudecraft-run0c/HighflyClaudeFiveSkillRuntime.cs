using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Highfly.Combat;
using Highfly.Run0H;
using Highfly.Run0I;

namespace Highfly.ClaudeBridge.Run0C
{
    /// <summary>
    /// RUN0C batch proof: keeps the approved Heroic Leap runtime untouched and
    /// adds four ClaudeCraft v0.43.3 skills through one shared HIGHFLY adapter.
    /// Donor balance is representative in LAB only; HIGHFLY CombatCore owns final balance.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HighflyClaudeFiveSkillRuntime : MonoBehaviour
    {
        private enum BatchSkill
        {
            Ambush = 2,
            Backstab = 3,
            Eviscerate = 4,
            Pummel = 5
        }

        private PlayerController _player;
        private PlayerStats _self;
        private CharacterController _controller;
        private global::Highfly.ClaudeBridge.Run0B.HighflyClaudeHeroicLeapRuntime _heroic;
        private AudioSource _audio;
        private AudioClip _swingSfx;
        private AudioClip _longSwingSfx;
        private AudioClip _hitSfx;

        private GameObject _smokeFx;
        private GameObject _sparksFx;
        private GameObject _electricFx;

        private bool _busy;
        private float _pummelReadyAt;
        private string _status = "RUN0C • LISTO • 1-5 EN PC";
        private GUIStyle _buttonStyle;
        private GUIStyle _labelStyle;

        private static readonly Color Shadow = new Color(0.31f, 0.16f, 0.58f, 0.95f);
        private static readonly Color ShadowRim = new Color(0.63f, 0.43f, 1f, 0.95f);
        private static readonly Color Blood = new Color(0.78f, 0.06f, 0.08f, 0.95f);
        private static readonly Color Physical = new Color(1f, 0.82f, 0.42f, 0.95f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (FindAnyObjectByType<HighflyClaudeFiveSkillRuntime>() != null) return;
            GameObject root = new GameObject("HIGHFLY_CLAUDECRAFT_RUN0C_FIVE_SKILLS");
            DontDestroyOnLoad(root);
            root.AddComponent<HighflyClaudeFiveSkillRuntime>();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;

            _swingSfx = Resources.Load<AudioClip>("HIGHFLY/Run0I/swing");
            _longSwingSfx = Resources.Load<AudioClip>("HIGHFLY/Run0I/longSwing");
            _hitSfx = Resources.Load<AudioClip>("HIGHFLY/Run0I/hit");

            _smokeFx = Resources.Load<GameObject>("HIGHFLY/ClaudeBridge/SmokeEffect");
            _sparksFx = Resources.Load<GameObject>("HIGHFLY/ClaudeBridge/SparksEffect");
            _electricFx = Resources.Load<GameObject>("HIGHFLY/ClaudeBridge/ElectricalSparksEffect");
        }

        private void Update()
        {
            ResolveReferences();

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || _player == null) return;

            // Key 1 deliberately routes into the already-approved RUN0B runtime.
            if (!_busy && keyboard.digit1Key.wasPressedThisFrame && _heroic != null)
                _heroic.TryCast();

            if (keyboard.digit2Key.wasPressedThisFrame) TryCast(BatchSkill.Ambush);
            if (keyboard.digit3Key.wasPressedThisFrame) TryCast(BatchSkill.Backstab);
            if (keyboard.digit4Key.wasPressedThisFrame) TryCast(BatchSkill.Eviscerate);
            if (keyboard.digit5Key.wasPressedThisFrame) TryCast(BatchSkill.Pummel);
        }

        private void ResolveReferences()
        {
            if (_player == null)
            {
                _player = FindAnyObjectByType<PlayerController>();
                if (_player != null)
                {
                    _self = _player.GetComponent<PlayerStats>();
                    _controller = _player.GetComponent<CharacterController>();
                }
            }
            if (_player != null && _controller == null)
                _controller = _player.GetComponent<CharacterController>();

            if (_heroic == null)
                _heroic = FindAnyObjectByType<global::Highfly.ClaudeBridge.Run0B.HighflyClaudeHeroicLeapRuntime>();
        }

        private void OnGUI()
        {
            ResolveReferences();

            if (_buttonStyle == null)
            {
                _buttonStyle = new GUIStyle(GUI.skin.button) {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                _labelStyle = new GUIStyle(GUI.skin.label) {
                    fontSize = 15,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                _labelStyle.normal.textColor = Color.white;
            }

            // Keep the batch strip away from the stable left joystick/right combat cluster.
            float gap = 7f;
            float bw = Mathf.Min(205f, Screen.width * 0.17f);
            float bh = 52f;
            float total = bw * 4f + gap * 3f;
            float x = Mathf.Max(8f, (Screen.width - total) * 0.5f);
            float y = Screen.height - bh - 14f;

            GUI.Label(new Rect(x, y - 25f, total, 22f), _status, _labelStyle);

            DrawButton(new Rect(x + (bw + gap) * 0f, y, bw, bh), "2 • AMBUSH", BatchSkill.Ambush);
            DrawButton(new Rect(x + (bw + gap) * 1f, y, bw, bh), "3 • BACKSTAB", BatchSkill.Backstab);
            DrawButton(new Rect(x + (bw + gap) * 2f, y, bw, bh), "4 • EVISCERATE", BatchSkill.Eviscerate);

            float remain = Mathf.Max(0f, _pummelReadyAt - Time.unscaledTime);
            string pummel = remain > 0f ? "5 • PUMMEL • " + remain.ToString("0.0") : "5 • PUMMEL";
            DrawButton(new Rect(x + (bw + gap) * 3f, y, bw, bh), pummel, BatchSkill.Pummel);
        }

        private void DrawButton(Rect rect, string label, BatchSkill skill)
        {
            bool cooldownReady = skill != BatchSkill.Pummel || Time.unscaledTime >= _pummelReadyAt;
            GUI.enabled = !_busy && _player != null && cooldownReady;
            if (GUI.Button(rect, label, _buttonStyle)) TryCast(skill);
            GUI.enabled = true;
        }

        private void TryCast(BatchSkill skill)
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

            if (skill == BatchSkill.Pummel && Time.unscaledTime < _pummelReadyAt)
                return;

            CharacterStats target = ResolveTarget();
            Vector3 targetPoint = target != null
                ? target.transform.position + Vector3.up * 0.9f
                : _player.transform.position + FlatForward() * 2.4f + Vector3.up * 0.9f;

            switch (skill)
            {
                case BatchSkill.Ambush:
                    StartCoroutine(CastAmbush(target, targetPoint));
                    break;
                case BatchSkill.Backstab:
                    StartCoroutine(CastBackstab(target, targetPoint));
                    break;
                case BatchSkill.Eviscerate:
                    StartCoroutine(CastEviscerate(target, targetPoint));
                    break;
                case BatchSkill.Pummel:
                    _pummelReadyAt = Time.unscaledTime + 10f; // ClaudeCraft canonical cooldown.
                    StartCoroutine(CastPummel(target, targetPoint));
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
                _player.transform.position, 12f, ~0, QueryTriggerInteraction.Collide);

            CharacterStats best = null;
            float bestSqr = float.PositiveInfinity;
            for (int i = 0; i < hits.Length; i++)
            {
                Collider c = hits[i];
                if (c == null) continue;
                CharacterStats stats = c.GetComponentInParent<CharacterStats>();
                if (stats == null || stats == _self) continue;

                float sqr = (stats.transform.position - _player.transform.position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = stats;
                }
            }
            return best;
        }

        private IEnumerator CastBackstab(CharacterStats target, Vector3 point)
        {
            HighflyRun0HCharacterVisual visual = BeginSkill(
                "Claude_Rogue_Backstab", target,
                "BACKSTAB • entrada real a melee • thrust");

            Vector3 fallback = DirectionTo(point);

            // Video pass 01 showed the thrust landing from several metres away.
            // Close that gap during the donor windup so contact and damage coincide.
            yield return HoldFacing(0.12f, visual, target, fallback, false);
            if (visual != null) visual.SetWeaponTrail(true);
            PlaySfx(_swingSfx);
            yield return MoveIntoMelee(target, fallback, 0.95f, 0.20f, visual, true);

            Vector3 hitPoint = TargetPoint(target, point);
            Quaternion hitRot = FacingRotation(target, fallback);
            SpawnFx(_electricFx, hitPoint, hitRot, ShadowRim, 0.28f, 0.45f);
            SpawnFx(_sparksFx, hitPoint, hitRot, ShadowRim, 0.42f, 0.55f);
            DealDamage(target, Random.Range(24, 31), 10f);
            PlaySfx(_hitSfx);

            if (visual != null) visual.SetWeaponTrail(false);
            yield return HoldFacing(0.30f, visual, target, fallback, false);
            EndSkill(visual, "BACKSTAB • contacto cuerpo/impacto sincronizado");
        }

        private IEnumerator CastAmbush(CharacterStats target, Vector3 point)
        {
            HighflyRun0HCharacterVisual visual = BeginSkill(
                "Claude_Rogue_Ambush", target,
                "AMBUSH • phase detrás • doble golpe");

            Vector3 fallback = DirectionTo(point);

            // The first video exposed a giant smoke cloud covering the complete action.
            // Keep two compact puffs instead: departure and reappearance.
            SpawnFx(
                _smokeFx,
                _player.transform.position + Vector3.up * 0.45f,
                Quaternion.LookRotation(fallback, Vector3.up),
                Shadow,
                0.22f,
                0.40f);

            // Preserve the crouched/stealth read, then actually reposition to the far side.
            yield return HoldFacing(0.18f, visual, target, fallback, false);

            if (target != null)
            {
                Vector3 behind = ResolveFarSideSeat(target, 0.95f, fallback);
                PhaseTo(behind);
                fallback = DirectionTo(TargetPoint(target, point));
                LockFacing(visual, fallback);
            }

            SpawnFx(
                _smokeFx,
                _player.transform.position + Vector3.up * 0.45f,
                Quaternion.LookRotation(fallback, Vector3.up),
                Shadow,
                0.18f,
                0.32f);

            yield return HoldFacing(0.40f, visual, target, fallback, false);

            if (visual != null) visual.SetWeaponTrail(true);
            PlaySfx(_swingSfx);

            yield return HoldFacing(0.12f, visual, target, fallback, true);
            Vector3 hitA = TargetPoint(target, point);
            Quaternion rotA = FacingRotation(target, fallback);
            SpawnFx(_sparksFx, hitA, rotA, ShadowRim, 0.42f, 0.50f);
            DealDamage(target, Random.Range(17, 22), 8f);

            yield return HoldFacing(0.15f, visual, target, fallback, true);
            Vector3 hitB = TargetPoint(target, point);
            Quaternion rotB = FacingRotation(target, fallback);
            SpawnFx(_electricFx, hitB, rotB, Shadow, 0.32f, 0.50f);
            SpawnFx(_sparksFx, hitB, rotB, ShadowRim, 0.48f, 0.55f);
            DealDamage(target, Random.Range(19, 24), 12f);
            PlaySfx(_hitSfx);

            if (visual != null) visual.SetWeaponTrail(false);
            yield return HoldFacing(0.28f, visual, target, fallback, false);
            EndSkill(visual, "AMBUSH • phase + doble impacto legible");
        }

        private IEnumerator CastEviscerate(CharacterStats target, Vector3 point)
        {
            HighflyRun0HCharacterVisual visual = BeginSkill(
                "Claude_Rogue_Finisher_Slash", target,
                "EVISCERATE • acercamiento + finisher 2 cortes");

            Vector3 fallback = DirectionTo(point);

            // The donor clip winds through 0.42s. Use that time to enter true melee range.
            yield return MoveIntoMelee(target, fallback, 1.05f, 0.24f, visual, false);
            yield return HoldFacing(0.18f, visual, target, fallback, false);

            if (visual != null) visual.SetWeaponTrail(true);
            PlaySfx(_longSwingSfx);

            // First readable slash at the 0.65 donor pose.
            yield return HoldFacing(0.23f, visual, target, fallback, true);
            Vector3 hitA = TargetPoint(target, point);
            Quaternion rotA = FacingRotation(target, fallback);
            SpawnFx(_sparksFx, hitA, rotA, Blood, 0.46f, 0.50f);
            DealDamage(target, Random.Range(17, 22), 12f);

            // Second slash at ~0.82: no giant plasma cloud; keep the body and weapon trail visible.
            yield return HoldFacing(0.17f, visual, target, fallback, true);
            Vector3 hitB = TargetPoint(target, point);
            Quaternion rotB = FacingRotation(target, fallback);
            SpawnFx(_electricFx, hitB, rotB, Blood, 0.30f, 0.42f);
            SpawnFx(_sparksFx, hitB, rotB, Blood, 0.58f, 0.55f);
            DealDamage(target, Random.Range(21, 27), 16f);
            PlaySfx(_hitSfx);

            if (visual != null) visual.SetWeaponTrail(false);
            yield return HoldFacing(0.33f, visual, target, fallback, false);
            EndSkill(visual, "EVISCERATE • dos cortes visibles • sin nube");
        }

        private IEnumerator CastPummel(CharacterStats target, Vector3 point)
        {
            HighflyRun0HCharacterVisual visual = BeginSkill(
                "Claude_Punch_A", target,
                "PUMMEL • entrada corta • uppercut • CD 10s");

            Vector3 fallback = DirectionTo(point);
            if (visual != null)
            {
                visual.SetWeaponTrail(false);
                visual.SetWeaponsVisible(false);
            }

            // Video pass 01 showed the punch animating in empty space.
            // Step into fist range during the authored 0.14 -> 0.32 windup.
            yield return MoveIntoMelee(target, fallback, 0.82f, 0.20f, visual, false);
            yield return HoldFacing(0.10f, visual, target, fallback, false);

            Vector3 hit = TargetPoint(target, point);
            Quaternion hitRot = FacingRotation(target, fallback);
            SpawnFx(_electricFx, hit, hitRot, Physical, 0.34f, 0.42f);
            SpawnFx(_sparksFx, hit, hitRot, Physical, 0.32f, 0.42f);
            DealDamage(target, Random.Range(8, 13), 7f);
            PlaySfx(_hitSfx);

            if (target != null)
            {
                HighflyRun0IAttackDummy dummy = target.GetComponentInParent<HighflyRun0IAttackDummy>();
                if (dummy != null)
                {
                    Vector3 repel = target.transform.position - _player.transform.position;
                    repel.y = 0f;
                    dummy.ReceiveRepel(repel, 0.75f);
                }
            }

            yield return HoldFacing(0.40f, visual, target, fallback, false);

            if (visual != null) visual.SetWeaponsVisible(true);
            EndSkill(visual, "PUMMEL • contacto + knockback legible");
        }

        private HighflyRun0HCharacterVisual BeginSkill(
            string clip, CharacterStats target, string status)
        {
            _busy = true;
            _status = status;
            _player.currentState = PlayerState.Skill;

            HighflyRun0HCharacterVisual visual = HighflyRun0HCharacterVisual.Instance;
            Vector3 facing = target != null
                ? target.transform.position - _player.transform.position
                : FlatForward();
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.0001f) facing = FlatForward();
            LockFacing(visual, facing);

            if (visual != null)
            {
                visual.SetWeaponTrail(false);
                visual.PlayActionClip(clip, 1f);
            }
            return visual;
        }

        private void EndSkill(HighflyRun0HCharacterVisual visual, string status)
        {
            if (visual != null)
            {
                visual.SetWeaponTrail(false);
                visual.SetWeaponsVisible(true);
                visual.StopActionClip();
                visual.ClearActionFacing();
            }

            if (_player != null && _player.currentState == PlayerState.Skill)
                _player.currentState = PlayerState.Locomotion;

            _status = status;
            _busy = false;
        }

        private IEnumerator HoldFacing(
            float seconds,
            HighflyRun0HCharacterVisual visual,
            CharacterStats target,
            Vector3 fallback,
            bool trail)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                Vector3 facing = target != null
                    ? target.transform.position - _player.transform.position
                    : fallback;
                facing.y = 0f;
                if (facing.sqrMagnitude < 0.0001f) facing = fallback;
                LockFacing(visual, facing);
                if (visual != null) visual.SetWeaponTrail(trail);
                yield return null;
            }
        }

        private IEnumerator MoveIntoMelee(
            CharacterStats target,
            Vector3 fallback,
            float standoff,
            float seconds,
            HighflyRun0HCharacterVisual visual,
            bool trail)
        {
            Vector3 from = _player.transform.position;
            Vector3 desired;

            if (target != null)
            {
                Vector3 toTarget = target.transform.position - from;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude < 0.0001f) toTarget = fallback;
                Vector3 dir = toTarget.normalized;
                desired = GroundSeat(target.transform.position - dir * standoff, from.y);
            }
            else
            {
                desired = GroundSeat(from + fallback.normalized * 1.35f, from.y);
            }

            float t = 0f;
            Vector3 previous = from;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / Mathf.Max(0.01f, seconds));
                float eased = 1f - Mathf.Pow(1f - u, 3f);
                Vector3 next = Vector3.Lerp(from, desired, eased);
                Vector3 delta = next - previous;

                if (_controller != null && _controller.enabled)
                    _controller.Move(delta);
                else
                    _player.transform.position = next;

                previous = next;

                Vector3 facing = target != null
                    ? target.transform.position - _player.transform.position
                    : fallback;
                facing.y = 0f;
                if (facing.sqrMagnitude < 0.0001f) facing = fallback;
                LockFacing(visual, facing);
                if (visual != null) visual.SetWeaponTrail(trail);
                yield return null;
            }
        }

        private Vector3 ResolveFarSideSeat(CharacterStats target, float distance, Vector3 fallback)
        {
            Vector3 toTarget = target.transform.position - _player.transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f) toTarget = fallback;
            Vector3 dir = toTarget.normalized;

            // Continue through the target to the opposite side: readable "appear behind" motion.
            return GroundSeat(target.transform.position + dir * distance, _player.transform.position.y);
        }

        private void PhaseTo(Vector3 point)
        {
            bool restoreController = _controller != null && _controller.enabled;
            if (restoreController) _controller.enabled = false;

            _player.transform.position = point;

            if (restoreController) _controller.enabled = true;
        }

        private static Vector3 GroundSeat(Vector3 point, float fallbackY)
        {
            RaycastHit hit;
            Vector3 origin = new Vector3(point.x, fallbackY + 6f, point.z);
            if (Physics.Raycast(origin, Vector3.down, out hit, 12f, ~0, QueryTriggerInteraction.Ignore))
                return hit.point + Vector3.up * 0.02f;

            point.y = fallbackY;
            return point;
        }

        private void LockFacing(HighflyRun0HCharacterVisual visual, Vector3 forward)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) return;
            forward.Normalize();
            _player.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            if (visual != null) visual.SetActionFacing(forward);
        }

        private Vector3 FlatForward()
        {
            Vector3 f = _player != null ? _player.transform.forward : Vector3.forward;
            f.y = 0f;
            return f.sqrMagnitude > 0.0001f ? f.normalized : Vector3.forward;
        }

        private Vector3 DirectionTo(Vector3 point)
        {
            Vector3 d = point - _player.transform.position;
            d.y = 0f;
            return d.sqrMagnitude > 0.0001f ? d.normalized : FlatForward();
        }

        private static Vector3 TargetPoint(CharacterStats target, Vector3 fallback)
        {
            return target != null ? target.transform.position + Vector3.up * 0.9f : fallback;
        }

        private Quaternion FacingRotation(CharacterStats target, Vector3 fallback)
        {
            Vector3 f = target != null
                ? target.transform.position - _player.transform.position
                : fallback;
            f.y = 0f;
            if (f.sqrMagnitude < 0.0001f) f = fallback;
            return Quaternion.LookRotation(f.normalized, Vector3.up);
        }

        private void DealDamage(CharacterStats target, int damage, float force)
        {
            if (target == null || _player == null) return;
            target.TakeDamage(damage, force, _player.transform);
        }

        private void PlaySfx(AudioClip clip)
        {
            if (_audio != null && clip != null) _audio.PlayOneShot(clip);
        }

        private static void SpawnFx(
            GameObject prefab,
            Vector3 position,
            Quaternion rotation,
            Color tint,
            float scale,
            float life)
        {
            if (prefab == null) return;

            GameObject fx = Instantiate(prefab, position, rotation);
            fx.transform.localScale = Vector3.one;

            float visualScale = Mathf.Clamp(scale, 0.12f, 1.25f);
            ParticleSystem[] particles = fx.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem.MainModule main = particles[i].main;
                main.startColor = new ParticleSystem.MinMaxGradient(tint);
                main.startSizeMultiplier *= visualScale;
                main.startSpeedMultiplier *= Mathf.Lerp(0.45f, 1f, visualScale);
                main.startLifetimeMultiplier = Mathf.Min(main.startLifetimeMultiplier, Mathf.Max(0.20f, life));
            }

            Destroy(fx, life);
        }
    }
}
