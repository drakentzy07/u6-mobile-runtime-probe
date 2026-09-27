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
        private HighflyClaudeBridge.Run0B.HighflyClaudeHeroicLeapRuntime _heroic;
        private AudioSource _audio;
        private AudioClip _swingSfx;
        private AudioClip _longSwingSfx;
        private AudioClip _hitSfx;

        private GameObject _smokeFx;
        private GameObject _sparksFx;
        private GameObject _electricFx;
        private GameObject _plasmaFx;

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
            _plasmaFx = Resources.Load<GameObject>("HIGHFLY/ClaudeBridge/PlasmaExplosionEffect");
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
                if (_player != null) _self = _player.GetComponent<PlayerStats>();
            }
            if (_heroic == null)
                _heroic = FindAnyObjectByType<HighflyClaudeBridge.Run0B.HighflyClaudeHeroicLeapRuntime>();
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
                "Claude_Rogue_Backstab", target, "BACKSTAB • detrás simulado en LAB");

            Vector3 fallback = DirectionTo(point);
            yield return HoldFacing(0.15f, visual, target, fallback, false);
            if (visual != null) visual.SetWeaponTrail(true);
            PlaySfx(_swingSfx);
            yield return HoldFacing(0.20f, visual, target, fallback, true);

            Vector3 hitPoint = TargetPoint(target, point);
            SpawnFx(_sparksFx, hitPoint, FacingRotation(target, fallback), ShadowRim, 0.75f, 1.2f);
            DealDamage(target, Random.Range(24, 31), 10f);
            PlaySfx(_hitSfx);

            if (visual != null) visual.SetWeaponTrail(false);
            yield return HoldFacing(0.27f, visual, target, fallback, false);
            EndSkill(visual, "BACKSTAB • impacto limpio");
        }

        private IEnumerator CastAmbush(CharacterStats target, Vector3 point)
        {
            HighflyRun0HCharacterVisual visual = BeginSkill(
                "Claude_Rogue_Ambush", target,
                "AMBUSH • stealth+detrás simulados • windup shadow");

            Vector3 fallback = DirectionTo(point);
            Vector3 windupPoint = TargetPoint(target, point);
            SpawnFx(_smokeFx, windupPoint, FacingRotation(target, fallback), Shadow, 1.15f, 0.75f);

            // Claude clip holds the crouched guard until 0.40, then coils into two strikes.
            yield return HoldFacing(0.52f, visual, target, fallback, false);
            if (visual != null) visual.SetWeaponTrail(true);
            PlaySfx(_swingSfx);

            yield return HoldFacing(0.22f, visual, target, fallback, true);
            Vector3 hitA = TargetPoint(target, point);
            SpawnFx(_sparksFx, hitA, FacingRotation(target, fallback), ShadowRim, 0.9f, 1.2f);
            DealDamage(target, Random.Range(17, 22), 8f);

            yield return HoldFacing(0.11f, visual, target, fallback, true);
            Vector3 hitB = TargetPoint(target, point);
            SpawnFx(_plasmaFx, hitB, FacingRotation(target, fallback), Shadow, 0.72f, 1.4f);
            SpawnFx(_sparksFx, hitB, FacingRotation(target, fallback), ShadowRim, 1.05f, 1.2f);
            DealDamage(target, Random.Range(19, 24), 12f);
            PlaySfx(_hitSfx);

            if (visual != null) visual.SetWeaponTrail(false);
            yield return HoldFacing(0.30f, visual, target, fallback, false);
            EndSkill(visual, "AMBUSH • doble impacto shadow");
        }

        private IEnumerator CastEviscerate(CharacterStats target, Vector3 point)
        {
            HighflyRun0HCharacterVisual visual = BeginSkill(
                "Claude_Rogue_Finisher_Slash", target,
                "EVISCERATE • finisher • 5 combo simulados en LAB");

            Vector3 fallback = DirectionTo(point);
            yield return HoldFacing(0.42f, visual, target, fallback, false);
            if (visual != null) visual.SetWeaponTrail(true);
            PlaySfx(_longSwingSfx);

            yield return HoldFacing(0.23f, visual, target, fallback, true);
            Vector3 hitA = TargetPoint(target, point);
            SpawnFx(_sparksFx, hitA, FacingRotation(target, fallback), Blood, 1.0f, 1.3f);
            DealDamage(target, Random.Range(17, 22), 12f);

            yield return HoldFacing(0.17f, visual, target, fallback, true);
            Vector3 hitB = TargetPoint(target, point);
            SpawnFx(_plasmaFx, hitB, FacingRotation(target, fallback), Blood, 0.82f, 1.5f);
            SpawnFx(_sparksFx, hitB, FacingRotation(target, fallback), Blood, 1.35f, 1.3f);
            DealDamage(target, Random.Range(21, 27), 16f);
            PlaySfx(_hitSfx);

            if (visual != null) visual.SetWeaponTrail(false);
            yield return HoldFacing(0.33f, visual, target, fallback, false);
            EndSkill(visual, "EVISCERATE • X finisher completado");
        }

        private IEnumerator CastPummel(CharacterStats target, Vector3 point)
        {
            HighflyRun0HCharacterVisual visual = BeginSkill(
                "Claude_Punch_A", target,
                "PUMMEL • interrupt 4s • CD 10s");

            Vector3 fallback = DirectionTo(point);
            if (visual != null)
            {
                visual.SetWeaponTrail(false);
                visual.SetWeaponsVisible(false);
            }

            yield return HoldFacing(0.30f, visual, target, fallback, false);

            Vector3 hit = TargetPoint(target, point);
            SpawnFx(_electricFx, hit, FacingRotation(target, fallback), Physical, 0.72f, 1.0f);
            SpawnFx(_sparksFx, hit, FacingRotation(target, fallback), Physical, 0.65f, 1.0f);
            DealDamage(target, Random.Range(8, 13), 5f);
            PlaySfx(_hitSfx);

            if (target != null)
            {
                HighflyRun0IAttackDummy dummy = target.GetComponentInParent<HighflyRun0IAttackDummy>();
                if (dummy != null) dummy.ReceiveRepel(fallback, 0.35f);
            }

            yield return HoldFacing(0.40f, visual, target, fallback, false);

            if (visual != null) visual.SetWeaponsVisible(true);
            EndSkill(visual, "PUMMEL • interrupt marcado 4s");
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
            fx.transform.localScale = Vector3.one * Mathf.Max(0.1f, scale);

            ParticleSystem[] particles = fx.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem.MainModule main = particles[i].main;
                main.startColor = new ParticleSystem.MinMaxGradient(tint);
            }

            Destroy(fx, life);
        }
    }
}
