using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Highfly.Combat;
using Highfly.Mobile;
using Highfly.Run0H;
using Highfly.Run0I2;

namespace Highfly.Skill5
{
    public enum HighflySkill5Action
    {
        None,
        SonicLeapReforged,
        HorizontalSquare,
        DoubleCircular,
        Apocalypse,
        SpinningShield
    }

    // HIGHFLY_SKILL5_PREMIUM_FIVE
    // SKILL4 is an immutable dependency. This runtime owns only SKILL5 execution.
    [DisallowMultipleComponent]
    public sealed class HighflySkill5Runtime : MonoBehaviour
    {
        public const string BuildMarker = "HIGHFLY_SKILL5_PREMIUM_FIVE";

        public const float SonicCooldown = 6f;
        public const float SonicVolition = 14f;
        public const float HorizontalSquareCooldown = 7f;
        public const float HorizontalSquareVolition = 18f;
        public const float DoubleCircularCooldown = 6f;
        public const float DoubleCircularVolition = 12f;
        public const float ApocalypseCooldown = 16f;
        public const float ApocalypseVolition = 28f;
        public const float SpinningShieldCooldown = 12f;
        public const float SpinningShieldVolition = 14f;

        private const float SonicDuration = 0.940f;
        private const float SquareDuration = 1.220f;
        private const float DoubleDuration = 0.930f;
        private const float ApocalypseDuration = 0.820f;
        private const float ShieldDuration = 1.200f;

        public static HighflySkill5Runtime Instance { get; private set; }

        private PlayerController _player;
        private PlayerStats _stats;
        private CharacterController _controller;
        private HighflyRun0HCharacterVisual _visual;
        private HighflyLucidCombatBridge _foundation;
        private AudioSource _audio;
        private AudioClip _swing;
        private AudioClip _longSwing;
        private AudioClip _hit;
        private GameObject _sparksPrefab;

        private HighflySkill5Action _action = HighflySkill5Action.None;
        private float _elapsed;
        private float _hitstopRemaining;
        private float _readySonic;
        private float _readySquare;
        private float _readyDouble;
        private float _readyApocalypse;
        private float _readyShield;

        private int _phase = -1;
        private int _window = -1;
        private bool _active;
        private bool _shieldSecondGuard;
        private bool _shieldMidPose;
        private HighflyCombatAction _bufferedAction = HighflyCombatAction.None;
        private Vector3 _facing;
        private Vector3 _previousMotionOffset;
        private Vector3 _previousPrimaryTip;
        private Vector3 _previousSecondaryTip;
        private bool _hasPreviousPrimaryTip;
        private bool _hasPreviousSecondaryTip;
        private readonly HashSet<CharacterStats> _hitThisWindow = new HashSet<CharacterStats>();
        private readonly Collider[] _overlap = new Collider[64];

        public bool ActionBusy => _action != HighflySkill5Action.None;
        public HighflySkill5Action CurrentAction => _action;
        public float ActionElapsed => _elapsed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Object.FindFirstObjectByType<HighflySkill5Bootstrap>() != null) return;
            var go = new GameObject("HIGHFLY_SKILL5_BOOTSTRAP");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<HighflySkill5Bootstrap>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            _player = GetComponent<PlayerController>();
            _stats = GetComponent<PlayerStats>();
            _controller = GetComponent<CharacterController>();
            _foundation = GetComponent<HighflyLucidCombatBridge>();

            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
            _swing = Resources.Load<AudioClip>("HIGHFLY/Run0I/swing");
            _longSwing = Resources.Load<AudioClip>("HIGHFLY/Run0I/longSwing");
            _hit = Resources.Load<AudioClip>("HIGHFLY/Run0I/hit");
            _sparksPrefab = Resources.Load<GameObject>("HIGHFLY/Run0I/SparksEffect");

            if (GetComponent<HighflySkill5InputAdapter>() == null)
                gameObject.AddComponent<HighflySkill5InputAdapter>();

            if (GetComponent<HighflySkill5QuickPanel>() == null)
                gameObject.AddComponent<HighflySkill5QuickPanel>();

            Debug.Log("[SKILL5] PREMIUM FIVE runtime online • SKILL4 foundation untouched");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            HandleDesktopPreviewKeys();

            if (!ActionBusy) return;

            float dt = Time.unscaledDeltaTime;
            if (_hitstopRemaining > 0f)
            {
                _hitstopRemaining -= dt;
                return;
            }

            _elapsed += dt;

            switch (_action)
            {
                case HighflySkill5Action.SonicLeapReforged:
                    UpdateSonicLeap(_elapsed);
                    if (_elapsed >= SonicDuration) FinishSkill5Action();
                    break;
                case HighflySkill5Action.HorizontalSquare:
                    UpdateHorizontalSquare(_elapsed);
                    if (_bufferedAction != HighflyCombatAction.None && LinkWindowOpen())
                        ExecuteBufferedLink();
                    else if (_elapsed >= SquareDuration)
                        FinishSkill5Action();
                    break;
                case HighflySkill5Action.DoubleCircular:
                    UpdateDoubleCircular(_elapsed);
                    if (_elapsed >= DoubleDuration) FinishSkill5Action();
                    break;
                case HighflySkill5Action.Apocalypse:
                    UpdateApocalypse(_elapsed);
                    if (_elapsed >= ApocalypseDuration) FinishSkill5Action();
                    break;
                case HighflySkill5Action.SpinningShield:
                    UpdateSpinningShield(_elapsed);
                    if (_elapsed >= ShieldDuration) FinishSkill5Action();
                    break;
            }
        }

        private void HandleDesktopPreviewKeys()
        {
            Keyboard k = Keyboard.current;
            if (k == null) return;

            // Desktop must enter through the same routing path as mobile so
            // buffer/link/cancel behavior is identical on PC and Android.
            if (k.digit1Key.wasPressedThisFrame) RouteAction(HighflyCombatAction.Skill1);
            else if (k.digit2Key.wasPressedThisFrame) RouteAction(HighflyCombatAction.Skill2);
            else if (k.digit3Key.wasPressedThisFrame) RouteAction(HighflyCombatAction.Skill3);
            else if (k.digit4Key.wasPressedThisFrame) RouteAction(HighflyCombatAction.Skill4);
            else if (k.digit5Key.wasPressedThisFrame) RouteAction(HighflyCombatAction.Ultimate);
        }

        public float GetCooldownRemaining(HighflyCombatAction action)
        {
            float now = Time.unscaledTime;
            switch (action)
            {
                case HighflyCombatAction.Skill1: return Mathf.Max(0f, _readySonic - now);
                case HighflyCombatAction.Skill2: return Mathf.Max(0f, _readySquare - now);
                case HighflyCombatAction.Skill3: return Mathf.Max(0f, _readyDouble - now);
                case HighflyCombatAction.Skill4: return Mathf.Max(0f, _readyShield - now);
                case HighflyCombatAction.Ultimate: return Mathf.Max(0f, _readyApocalypse - now);
                default: return 0f;
            }
        }

        public bool RouteAction(HighflyCombatAction action)
        {
            if (ActionBusy)
            {
                if ((action == HighflyCombatAction.Dodge || action == HighflyCombatAction.Parry) &&
                    DefensiveCancelOpen())
                {
                    Debug.Log("[SKILL5] " + _action + " recovery cancel -> " + action);
                    FinishSkill5Action();
                    return false;
                }

                if (BufferWindowOpen() && CanLinkTo(action))
                {
                    _bufferedAction = action;
                    Debug.Log("[SKILL5] BUFFER " + _action + " -> " + action);
                    return true;
                }

                if (LinkWindowOpen() && CanLinkTo(action))
                {
                    HighflySkill5Action from = _action;
                    Debug.Log("[SKILL5] LINK " + from + " -> " + action);
                    FinishSkill5Action();

                    if (IsPremiumInput(action))
                    {
                        StartMapped(action);
                        return true;
                    }

                    return false;
                }

                return true;
            }

            if (!IsPremiumInput(action)) return false;
            StartMapped(action);
            return true;
        }

        private static bool IsPremiumInput(HighflyCombatAction action)
        {
            return action == HighflyCombatAction.Skill1 ||
                   action == HighflyCombatAction.Skill2 ||
                   action == HighflyCombatAction.Skill3 ||
                   action == HighflyCombatAction.Skill4 ||
                   action == HighflyCombatAction.Ultimate;
        }

        private void StartMapped(HighflyCombatAction action)
        {
            if (ActionBusy) return;
            if (_foundation != null && _foundation.ActionBusy) return;

            switch (action)
            {
                case HighflyCombatAction.Skill1: StartSonicLeap(); break;
                case HighflyCombatAction.Skill2: StartHorizontalSquare(); break;
                case HighflyCombatAction.Skill3: StartDoubleCircular(); break;
                case HighflyCombatAction.Skill4: StartSpinningShield(); break;
                case HighflyCombatAction.Ultimate: StartApocalypse(); break;
            }
        }

        // -----------------------------------------------------------------
        // S1 • SONIC LEAP REFORGED
        // v2.1: 0.940s, CD 6, cost 14, ACTIVE 0.498-0.590.
        // -----------------------------------------------------------------
        private void StartSonicLeap()
        {
            if (!ReadyForNewSkill()) return;
            if (_visual.CurrentLoadout != HighflyLoadoutProfile.Sword1H)
            {
                Debug.Log("[SKILL5] Sonic Leap requires ESPADA 1H.");
                return;
            }
            if (Time.unscaledTime < _readySonic) return;
            if (!SpendVolition(SonicVolition, "Sonic Leap")) return;

            _readySonic = Time.unscaledTime + SonicCooldown;
            BeginAction(HighflySkill5Action.SonicLeapReforged, true);
            UpdateSonicLeap(0f);
            Debug.Log("[SKILL5] S1 SONIC LEAP REFORGED • CD6 • COST14 • 1 HIT");
        }

        private void UpdateSonicLeap(float now)
        {
            int phase = now < 0.380f ? 0 : now < 0.660f ? 1 : 2;
            if (phase != _phase)
            {
                _phase = phase;
                if (phase == 0) PlayClip("NinjaJump_Start", 1.15f, _swing);
                else if (phase == 1) PlayClip("Sword_Heavy_Combo", 1.30f, _longSwing);
            }

            float z;
            float y;
            if (now <= 0.380f)
            {
                float t = EaseOut(Mathf.Clamp01(now / 0.380f));
                z = 2.600f * t;
                y = 1.250f * Mathf.Sin(t * Mathf.PI * 0.5f);
            }
            else if (now <= 0.660f)
            {
                float t = Mathf.Clamp01((now - 0.380f) / 0.280f);
                z = 2.600f + 0.190f * t;
                y = 1.250f * (1f - Mathf.SmoothStep(0f, 1f, t));
            }
            else
            {
                z = 2.790f;
                y = 0f;
            }
            MoveToOffset(new Vector3(0f, y, z));

            bool active = now >= 0.498f && now <= 0.590f;
            bool opened = SetWindow(active, active ? 0 : -1);
            if (opened)
            {
                Vector3 pos = transform.position + Vector3.up * 1.05f + _facing * 1.55f;
                HighflySkill5Fx.SpawnSonicSlash(pos, _facing);
            }
            if (active)
                TraceWeapon(false, 0.17f, 34f, 0.042f, true,
                    new Color(0.20f, 0.78f, 1f, 1f), 2.05f, "SONIC");
        }

        // -----------------------------------------------------------------
        // S2 • HORIZONTAL SQUARE
        // v2.1 exact timeline: 1.220s, CD 7, cost 18, 4 hit windows.
        // -----------------------------------------------------------------
        private void StartHorizontalSquare()
        {
            if (!ReadyForNewSkill()) return;
            if (_visual.CurrentLoadout != HighflyLoadoutProfile.Sword1H)
            {
                Debug.Log("[SKILL5] Horizontal Square requires ESPADA 1H.");
                return;
            }
            if (Time.unscaledTime < _readySquare) return;
            if (!SpendVolition(HorizontalSquareVolition, "Horizontal Square")) return;

            _readySquare = Time.unscaledTime + HorizontalSquareCooldown;
            BeginAction(HighflySkill5Action.HorizontalSquare, true);
            UpdateHorizontalSquare(0f);
            Debug.Log("[SKILL5] S2 HORIZONTAL SQUARE • CD7 • COST18 • 4 REAL HIT WINDOWS");
        }

        private void UpdateHorizontalSquare(float now)
        {
            int phase = now < 0.240f ? 0 : now < 0.470f ? 1 : now < 0.710f ? 2 : now < 0.940f ? 3 : 4;
            if (phase < 4 && phase != _phase)
            {
                _phase = phase;
                if (phase == 0) PlayClip("Sword_Regular_A", 1.28f, _swing);
                else if (phase == 1) PlayClip("Sword_Regular_B", 1.28f, _swing);
                else if (phase == 2) PlayClip("Sword_Regular_C", 1.22f, _swing);
                else PlayClip("Warrior_B", 1.18f, _longSwing);
            }

            float z;
            if (now < 0.240f)
                z = 0.160f * Mathf.Clamp01(now / 0.240f);
            else if (now < 0.470f)
                z = 0.160f + 0.150f * Mathf.Clamp01((now - 0.240f) / 0.230f);
            else if (now < 0.710f)
                z = 0.310f + 0.120f * Mathf.Clamp01((now - 0.470f) / 0.240f);
            else if (now < 0.940f)
                z = 0.430f + 0.150f * Mathf.Clamp01((now - 0.710f) / 0.230f);
            else
                z = 0.580f;
            MoveToOffset(new Vector3(0f, 0f, z));

            bool active = false;
            int window = -1;
            if (now >= 0.096f && now <= 0.173f) { active = true; window = 0; }
            else if (now >= 0.327f && now <= 0.401f) { active = true; window = 1; }
            else if (now >= 0.537f && now <= 0.657f) { active = true; window = 2; }
            else if (now >= 0.797f && now <= 0.871f) { active = true; window = 3; }

            bool opened = SetWindow(active, window);
            if (opened)
            {
                Vector3 right = Vector3.Cross(Vector3.up, _facing).normalized;
                Vector3 center = transform.position + Vector3.up * 1.06f + _facing * 1.42f;
                HighflySkill5Fx.SpawnSquareScar(window, center, _facing, right);
            }

            if (active)
            {
                float damage = window == 3 ? 30f : 19f;
                float hitstop = window == 3 ? 0.060f : 0.028f;
                TraceWeapon(false, 0.16f, damage, hitstop, window == 3,
                    new Color(0.28f, 0.82f, 1f, 1f), 1.82f, "SQUARE-" + (window + 1));
            }
        }

        // -----------------------------------------------------------------
        // S3 • DOUBLE CIRCULAR
        // v2.1: 0.930s, CD 6, cost 12, 1.56m total entry, 2 traces.
        // -----------------------------------------------------------------
        private void StartDoubleCircular()
        {
            if (!ReadyForNewSkill()) return;
            if (!IsDualLoadout(_visual.CurrentLoadout))
            {
                Debug.Log("[SKILL5] Double Circular requires DUAL SWORD / DUAL DAGGERS / DUAL AXE.");
                return;
            }
            if (Time.unscaledTime < _readyDouble) return;
            if (!SpendVolition(DoubleCircularVolition, "Double Circular")) return;

            _readyDouble = Time.unscaledTime + DoubleCircularCooldown;
            BeginAction(HighflySkill5Action.DoubleCircular, true);
            UpdateDoubleCircular(0f);
            Debug.Log("[SKILL5] S3 DOUBLE CIRCULAR • CD6 • COST12 • 2 SEPARATE TRACES");
        }

        private void UpdateDoubleCircular(float now)
        {
            int phase = now < 0.180f ? 0 : now < 0.420f ? 1 : now < 0.650f ? 2 : 3;
            if (phase < 3 && phase != _phase)
            {
                _phase = phase;
                if (phase == 0) PlayClip("Sword_Dash", 1.35f, null);
                else if (phase == 1) PlayClip(DualClip(0), 1.28f, _swing);
                else PlayClip(DualClip(1), 1.28f, _longSwing);
            }

            float z;
            if (now < 0.180f)
                z = 1.250f * EaseOut(Mathf.Clamp01(now / 0.180f));
            else if (now < 0.420f)
                z = 1.250f + 0.160f * Mathf.Clamp01((now - 0.180f) / 0.240f);
            else if (now < 0.650f)
                z = 1.410f + 0.150f * Mathf.Clamp01((now - 0.420f) / 0.230f);
            else
                z = 1.560f;
            MoveToOffset(new Vector3(0f, 0f, z));

            bool active = false;
            int window = -1;
            bool secondary = false;
            if (now >= 0.276f && now <= 0.353f) { active = true; window = 0; secondary = false; }
            else if (now >= 0.507f && now <= 0.581f) { active = true; window = 1; secondary = true; }

            bool opened = SetWindow(active, window);
            if (opened)
            {
                Vector3 right = Vector3.Cross(Vector3.up, _facing).normalized;
                Vector3 pos = transform.position + Vector3.up * 1.02f + _facing * 1.42f;
                HighflySkill5Fx.SpawnDualSlash(window, pos, _facing, right);
            }

            if (active)
            {
                TraceWeapon(secondary, 0.16f, window == 0 ? 24f : 26f,
                    window == 0 ? 0.042f : 0.065f,
                    window == 1,
                    new Color(0.66f, 0.30f, 1f, 1f),
                    2.15f,
                    window == 0 ? "DOUBLE-R" : "DOUBLE-L");
            }
        }

        // -----------------------------------------------------------------
        // ULT • APOCALYPSE
        // v2.1: 0.820s, CD 16, cost 28, ACTIVE 0.204-0.290.
        // -----------------------------------------------------------------
        private void StartApocalypse()
        {
            if (!ReadyForNewSkill()) return;
            if (_visual.CurrentLoadout != HighflyLoadoutProfile.Sword1H)
            {
                Debug.Log("[SKILL5] Apocalypse currently binds to ESPADA 1H donor.");
                return;
            }
            if (Time.unscaledTime < _readyApocalypse) return;
            if (!SpendVolition(ApocalypseVolition, "Apocalypse")) return;

            _readyApocalypse = Time.unscaledTime + ApocalypseCooldown;
            BeginAction(HighflySkill5Action.Apocalypse, true);
            _visual.PlayActionPose("Sword_Block", 0.26f);
            UpdateApocalypse(0f);
            Debug.Log("[SKILL5] ULT APOCALYPSE • CD16 • COST28 • CRIMSON HEAVY");
        }

        private void UpdateApocalypse(float now)
        {
            int phase = now < 0.100f ? 0 : now < 0.360f ? 1 : 2;
            if (phase != _phase)
            {
                _phase = phase;
                if (phase == 1) PlayClip("Sword_Heavy_Combo", 0.90f, _longSwing);
            }

            float z = 0f;
            if (now > 0.100f && now < 0.360f)
                z = 0.170f * Mathf.Clamp01((now - 0.100f) / 0.260f);
            else if (now >= 0.360f)
                z = 0.170f;
            MoveToOffset(new Vector3(0f, 0f, z));

            bool active = now >= 0.204f && now <= 0.290f;
            bool opened = SetWindow(active, active ? 0 : -1);
            if (opened)
            {
                Vector3 pos = transform.position + Vector3.up * 1.12f + _facing * 1.62f;
                HighflySkill5Fx.SpawnApocalypseSlash(pos, _facing);
            }
            if (active)
                TraceWeapon(false, 0.19f, 52f, 0.070f, true,
                    new Color(1f, 0.14f, 0.06f, 1f), 3.20f, "APOCALYPSE");
        }

        // -----------------------------------------------------------------
        // S4 • SPINNING SHIELD / HIGHFLY SHIELD SKILL
        // Uses two real frozen Repel windows instead of patching PlayerStats.
        // SKILL5 adds offensive shield traces + spiral presentation around them.
        // -----------------------------------------------------------------
        private void StartSpinningShield()
        {
            if (!ReadyForNewSkill()) return;
            if (!IsShieldLoadout(_visual.CurrentLoadout))
            {
                Debug.Log("[SKILL5] Spinning Shield requires SWORD+SHIELD or AXE+SHIELD.");
                return;
            }
            if (Time.unscaledTime < _readyShield) return;
            if (!SpendVolition(SpinningShieldVolition, "Spinning Shield")) return;

            _readyShield = Time.unscaledTime + SpinningShieldCooldown;
            _shieldSecondGuard = false;
            _shieldMidPose = false;
            BeginAction(HighflySkill5Action.SpinningShield, false);

            // First real defensive window comes from frozen SKILL4 Repel.
            _foundation?.Request(HighflyCombatAction.Parry);
            UpdateSpinningShield(0f);
            Debug.Log("[SKILL5] S4 SPINNING SHIELD • CD12 • COST14 • FOUNDATION-BACKED BLOCK + IMPACT");
        }

        private void UpdateSpinningShield(float now)
        {
            // Keep motion ownership inside the action between the two frozen Repel pulses.
            if (_foundation != null && !_foundation.ActionBusy &&
                _player != null && _player.currentState == PlayerState.Locomotion &&
                now < ShieldDuration)
            {
                _player.currentState = PlayerState.Skill;
            }

            if (!_shieldMidPose && now >= 0.390f)
            {
                _shieldMidPose = true;
                _visual.PlayActionClip("Shield_Block", 1.15f);
            }

            // Frozen Repel has a 0.75s cooldown. Re-arm it once for the second
            // circular guard pulse; no reflection/damage hook is patched.
            if (!_shieldSecondGuard && now >= 0.760f &&
                _foundation != null && !_foundation.ActionBusy)
            {
                _shieldSecondGuard = true;
                _foundation.Request(HighflyCombatAction.Parry);
            }

            bool active = false;
            int window = -1;
            if (now >= 0.068f && now <= 0.334f) { active = true; window = 0; }
            else if (now >= 0.808f && now <= 1.074f) { active = true; window = 1; }

            bool opened = SetWindow(active, window);
            if (opened)
            {
                Vector3 right = Vector3.Cross(Vector3.up, _facing).normalized;
                Vector3 center = transform.position + Vector3.up * 1.08f + _facing * 1.05f;
                HighflySkill5Fx.SpawnShieldSpiral(center, _facing, right, window);
            }

            if (active)
            {
                TraceWeapon(true, 0.24f, window == 0 ? 14f : 18f, 0f, window == 1,
                    new Color(0.24f, 0.88f, 1f, 1f), 1.72f,
                    window == 0 ? "SHIELD-SPIN-1" : "SHIELD-SPIN-2");
            }
        }

        // -----------------------------------------------------------------
        // Common action / hit / link infrastructure.
        // -----------------------------------------------------------------
        private bool ReadyForNewSkill()
        {
            _visual = HighflyRun0HCharacterVisual.Instance;
            if (_player == null || _controller == null || _visual == null || !_visual.IsBound)
            {
                Debug.LogWarning("[SKILL5] Hunter foundation not ready.");
                return false;
            }
            if (_foundation != null && _foundation.ActionBusy) return false;
            return true;
        }

        private bool SpendVolition(float cost, string label)
        {
            if (_stats == null) return true;
            if (_stats.UseVolition(cost)) return true;
            Debug.Log("[SKILL5] " + label + " blocked: Volition < " + cost.ToString("0") + ".");
            return false;
        }

        private void BeginAction(HighflySkill5Action action, bool ownPlayerState)
        {
            _action = action;
            _elapsed = 0f;
            _hitstopRemaining = 0f;
            _phase = -1;
            _window = -1;
            _active = false;
            _previousMotionOffset = Vector3.zero;
            _bufferedAction = HighflyCombatAction.None;
            _hasPreviousPrimaryTip = false;
            _hasPreviousSecondaryTip = false;
            _hitThisWindow.Clear();
            _facing = ResolveCombatForward(_player.HighflyMobileMoveInput);

            if (ownPlayerState)
                _player.currentState = PlayerState.Skill;

            _visual.SetActionFacing(_facing);
            _visual.SetWeaponTrail(false);
        }

        private bool SetWindow(bool active, int window)
        {
            bool opened = false;

            if (active && (!_active || _window != window))
            {
                _window = window;
                _hitThisWindow.Clear();
                _hasPreviousPrimaryTip = false;
                _hasPreviousSecondaryTip = false;
                _visual.SetWeaponTrail(true);
                opened = true;
            }

            if (!active && _active)
            {
                _visual.SetWeaponTrail(false);
                _hasPreviousPrimaryTip = false;
                _hasPreviousSecondaryTip = false;
            }

            _active = active;
            if (!active) _window = -1;
            return opened;
        }

        private void TraceWeapon(
            bool secondary,
            float radius,
            float damage,
            float hitstop,
            bool finalImpact,
            Color impactColor,
            float impactSize,
            string source)
        {
            Transform weaponBase = secondary ? _visual.SecondaryBase : _visual.PrimaryBase;
            Transform weaponTip = secondary ? _visual.SecondaryTip : _visual.PrimaryTip;
            if (weaponBase == null || weaponTip == null) return;

            Vector3 a = weaponBase.position;
            Vector3 b = weaponTip.position;
            int count = Physics.OverlapCapsuleNonAlloc(
                a, b, radius, _overlap, ~0, QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
                ResolveHit(_overlap[i], weaponTip, damage, hitstop, finalImpact, impactColor, impactSize, source);

            Vector3 previous = secondary ? _previousSecondaryTip : _previousPrimaryTip;
            bool hasPrevious = secondary ? _hasPreviousSecondaryTip : _hasPreviousPrimaryTip;

            if (hasPrevious)
            {
                Vector3 delta = b - previous;
                float distance = delta.magnitude;
                if (distance > 0.001f)
                {
                    RaycastHit[] sweep = Physics.SphereCastAll(
                        previous,
                        Mathf.Max(0.10f, radius * 0.82f),
                        delta / distance,
                        distance,
                        ~0,
                        QueryTriggerInteraction.Collide);

                    for (int i = 0; i < sweep.Length; i++)
                        ResolveHit(sweep[i].collider, weaponTip, damage, hitstop, finalImpact, impactColor, impactSize, source);
                }
            }

            if (secondary)
            {
                _previousSecondaryTip = b;
                _hasPreviousSecondaryTip = true;
            }
            else
            {
                _previousPrimaryTip = b;
                _hasPreviousPrimaryTip = true;
            }
        }

        private void ResolveHit(
            Collider collider,
            Transform weaponTip,
            float damage,
            float hitstop,
            bool finalImpact,
            Color impactColor,
            float impactSize,
            string source)
        {
            if (collider == null) return;
            if (collider.transform == transform || collider.transform.IsChildOf(transform)) return;

            CharacterStats target = collider.GetComponentInParent<CharacterStats>();
            if (target == null || target == _stats) return;
            if (!_hitThisWindow.Add(target)) return;

            Vector3 probe = weaponTip != null
                ? weaponTip.position
                : transform.position + _facing;
            Vector3 contact = collider.ClosestPoint(probe);

            target.TakeDamage(damage, finalImpact ? 34f : 20f, transform);
            PlayOneShot(_hit);
            SpawnSparks(contact);
            _hitstopRemaining = Mathf.Max(_hitstopRemaining, hitstop);

            if (finalImpact)
                HighflySkill5Fx.SpawnImpactCross(
                    contact + Vector3.up * 0.04f,
                    _facing,
                    impactColor,
                    impactSize);

            Debug.Log("[SKILL5] HIT " + source + " • " + target.name + " • " + damage.ToString("0") + " dmg");
        }

        private void MoveToOffset(Vector3 desiredLocal)
        {
            Vector3 right = Vector3.Cross(Vector3.up, _facing).normalized;
            Vector3 desiredWorld =
                right * desiredLocal.x +
                Vector3.up * desiredLocal.y +
                _facing * desiredLocal.z;

            Vector3 previousWorld =
                right * _previousMotionOffset.x +
                Vector3.up * _previousMotionOffset.y +
                _facing * _previousMotionOffset.z;

            Vector3 delta = desiredWorld - previousWorld;
            if (delta.sqrMagnitude > 0.0000001f)
                _controller.Move(delta);

            _previousMotionOffset = desiredLocal;
        }

        private bool DefensiveCancelOpen()
        {
            switch (_action)
            {
                case HighflySkill5Action.SonicLeapReforged: return _elapsed >= 0.660f;
                case HighflySkill5Action.HorizontalSquare: return _elapsed >= 0.916f;
                case HighflySkill5Action.DoubleCircular: return _elapsed >= 0.626f;
                case HighflySkill5Action.Apocalypse: return _elapsed >= 0.335f;
                case HighflySkill5Action.SpinningShield: return _elapsed >= 1.090f;
                default: return false;
            }
        }

        private bool BufferWindowOpen()
        {
            switch (_action)
            {
                // v2.1: input buffer opens 0.090s before the penultimate node ends (0.710 - 0.090 = 0.620).
                // It hands off to the authored final LinkWindow at 1.110s, preserving hit 4 readability.
                case HighflySkill5Action.HorizontalSquare:
                    return _elapsed >= 0.620f && _elapsed < 1.110f;
                default:
                    return false;
            }
        }

        private bool LinkWindowOpen()
        {
            switch (_action)
            {
                case HighflySkill5Action.SonicLeapReforged:
                    return _elapsed >= 0.830f && _elapsed <= SonicDuration;
                case HighflySkill5Action.HorizontalSquare:
                    return _elapsed >= 1.110f && _elapsed <= SquareDuration;
                case HighflySkill5Action.DoubleCircular:
                    return _elapsed >= 0.820f && _elapsed <= DoubleDuration;
                case HighflySkill5Action.Apocalypse:
                    return _elapsed >= 0.790f && _elapsed <= ApocalypseDuration;
                case HighflySkill5Action.SpinningShield:
                    return _elapsed >= 1.090f && _elapsed <= ShieldDuration;
                default:
                    return false;
            }
        }

        private bool CanLinkTo(HighflyCombatAction next)
        {
            if (next == HighflyCombatAction.Light) return true;

            switch (_action)
            {
                case HighflySkill5Action.SonicLeapReforged:
                    return next == HighflyCombatAction.Skill2;
                case HighflySkill5Action.HorizontalSquare:
                    return next == HighflyCombatAction.Skill1 ||
                           next == HighflyCombatAction.Ultimate;
                case HighflySkill5Action.DoubleCircular:
                    return next == HighflyCombatAction.Skill1;
                default:
                    return false;
            }
        }

        private void ExecuteBufferedLink()
        {
            HighflyCombatAction next = _bufferedAction;
            if (next == HighflyCombatAction.None) return;

            HighflySkill5Action from = _action;
            _bufferedAction = HighflyCombatAction.None;
            Debug.Log("[SKILL5] BUFFER EXEC " + from + " -> " + next);

            FinishSkill5Action();

            if (IsPremiumInput(next))
                StartMapped(next);
            else if (_foundation != null)
                _foundation.Request(next);
        }

        private string DualClip(int index)
        {
            switch (_visual.CurrentLoadout)
            {
                case HighflyLoadoutProfile.DualDaggers:
                    return index == 0 ? "Dagger_A" : "Dagger_B";
                case HighflyLoadoutProfile.DualAxe:
                    return index == 0 ? "DualAxe_A" : "DualAxe_B";
                default:
                    return index == 0 ? "DualSword_A" : "DualSword_B";
            }
        }

        private static bool IsDualLoadout(HighflyLoadoutProfile profile)
        {
            return profile == HighflyLoadoutProfile.DualSword ||
                   profile == HighflyLoadoutProfile.DualDaggers ||
                   profile == HighflyLoadoutProfile.DualAxe;
        }

        private static bool IsShieldLoadout(HighflyLoadoutProfile profile)
        {
            return profile == HighflyLoadoutProfile.SwordShield ||
                   profile == HighflyLoadoutProfile.AxeShield;
        }

        private Vector3 ResolveCombatForward(Vector2 stick)
        {
            if (_player.LockOnTarget != null)
            {
                Vector3 to = _player.LockOnTarget.position - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.01f) return to.normalized;
            }

            if (stick.sqrMagnitude > 0.0225f && _player.cameraTransform != null)
            {
                Vector3 f = _player.cameraTransform.forward;
                f.y = 0f;
                if (f.sqrMagnitude > 0.0001f) f.Normalize();

                Vector3 r = _player.cameraTransform.right;
                r.y = 0f;
                if (r.sqrMagnitude > 0.0001f) r.Normalize();

                Vector3 dir = f * stick.y + r * stick.x;
                if (dir.sqrMagnitude > 0.01f) return dir.normalized;
            }

            Vector3 fallback = transform.forward;
            fallback.y = 0f;
            return fallback.sqrMagnitude > 0.01f ? fallback.normalized : Vector3.forward;
        }

        private void PlayClip(string clip, float speed, AudioClip sound)
        {
            if (_visual != null) _visual.PlayActionClip(clip, speed);
            PlayOneShot(sound);
        }

        private void SpawnSparks(Vector3 position)
        {
            if (_sparksPrefab == null) return;
            GameObject fx = Instantiate(_sparksPrefab, position, Quaternion.identity);
            Destroy(fx, 2f);
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (_audio != null && clip != null) _audio.PlayOneShot(clip);
        }

        private static float EaseOut(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - (1f - t) * (1f - t);
        }

        private void FinishSkill5Action()
        {
            if (!ActionBusy) return;
            HighflySkill5Action finished = _action;

            _action = HighflySkill5Action.None;
            _active = false;
            _window = -1;
            _phase = -1;
            _hitThisWindow.Clear();
            _hasPreviousPrimaryTip = false;
            _hasPreviousSecondaryTip = false;
            _previousMotionOffset = Vector3.zero;

            _visual?.SetWeaponTrail(false);

            bool foundationBusy = _foundation != null && _foundation.ActionBusy;
            if (!foundationBusy)
            {
                _visual?.StopActionClip();
                _visual?.ClearActionFacing();

                if (_player != null &&
                    (_player.currentState == PlayerState.Skill ||
                     _player.currentState == PlayerState.Parry))
                {
                    _player.currentState = PlayerState.Locomotion;
                }
            }

            Debug.Log("[SKILL5] FINISH " + finished);
        }
    }

    public sealed class HighflySkill5Bootstrap : MonoBehaviour
    {
        private float _nextAttempt;

        private void Update()
        {
            if (HighflySkill5Runtime.Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            if (Time.unscaledTime < _nextAttempt) return;
            _nextAttempt = Time.unscaledTime + 0.20f;

            HighflyLucidCombatBridge bridge = Object.FindFirstObjectByType<HighflyLucidCombatBridge>();
            if (bridge == null) return;

            if (bridge.GetComponent<HighflySkill5Runtime>() == null)
                bridge.gameObject.AddComponent<HighflySkill5Runtime>();

            Destroy(gameObject);
        }
    }

    // Runtime-only input adapter. Frozen SKILL4 UI code is not patched.
    [DisallowMultipleComponent]
    public sealed class HighflySkill5InputAdapter : MonoBehaviour
    {
        private HighflySkill5Runtime _runtime;
        private HighflyLucidCombatBridge _foundation;
        private float _nextScan;

        private void Awake()
        {
            _runtime = GetComponent<HighflySkill5Runtime>();
            _foundation = GetComponent<HighflyLucidCombatBridge>();
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextScan) return;
            _nextScan = Time.unscaledTime + 0.15f;
            InstallProxies();
        }

        private void InstallProxies()
        {
            HighflyActionButton[] buttons = Object.FindObjectsByType<HighflyActionButton>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (HighflyActionButton button in buttons)
            {
                if (button == null) continue;
                if (!TryMap(button.gameObject.name, out HighflyCombatAction action)) continue;

                HighflySkill5ActionProxy proxy = button.GetComponent<HighflySkill5ActionProxy>();
                if (proxy == null) proxy = button.gameObject.AddComponent<HighflySkill5ActionProxy>();
                proxy.Configure(_runtime, _foundation, action);

                button.enabled = false;
                UpdatePremiumLabel(button.gameObject, action);
            }
        }

        private void UpdatePremiumLabel(GameObject button, HighflyCombatAction action)
        {
            Text[] texts = button.GetComponentsInChildren<Text>(true);
            if (texts.Length == 0) return;

            string label;
            switch (action)
            {
                case HighflyCombatAction.Skill1: label = "S1\\nSONIC"; break;
                case HighflyCombatAction.Skill2: label = "S2\\nH.SQUARE"; break;
                case HighflyCombatAction.Skill3: label = "S3\\nDOUBLE"; break;
                case HighflyCombatAction.Skill4: label = "S4\\nSHIELD"; break;
                case HighflyCombatAction.Ultimate: label = "ULT\\nAPOC"; break;
                default: return;
            }

            float remaining = _runtime != null ? _runtime.GetCooldownRemaining(action) : 0f;
            texts[0].text = remaining > 0.05f
                ? label + "\\n" + remaining.ToString("0.0") + "s"
                : label;
        }

        private static bool TryMap(string name, out HighflyCombatAction action)
        {
            action = HighflyCombatAction.None;

            // Actual objects produced by frozen HighflyMobileCore are
            // ATQ, S1, S2_OFF, S3, S4, ULT, ESQUIVAR and PARRY.
            // *_BUTTON aliases are kept for compatibility with older shells.
            if (name == "ATQ" || name == "ATQ_BUTTON") action = HighflyCombatAction.Light;
            else if (name == "S1" || name == "S1_BUTTON") action = HighflyCombatAction.Skill1;
            else if (name == "S2_OFF" || name == "S2" ||
                     name == "S2_OFF_BUTTON" || name == "S2_BUTTON") action = HighflyCombatAction.Skill2;
            else if (name == "S3" || name == "S3_BUTTON") action = HighflyCombatAction.Skill3;
            else if (name == "S4" || name == "S4_BUTTON") action = HighflyCombatAction.Skill4;
            else if (name == "ULT" || name == "ULT_BUTTON") action = HighflyCombatAction.Ultimate;
            else if (name == "ESQUIVAR" || name == "ESQUIVAR_BUTTON") action = HighflyCombatAction.Dodge;
            else if (name == "PARRY" || name == "PARRY_BUTTON") action = HighflyCombatAction.Parry;
            return action != HighflyCombatAction.None;
        }
    }

    // SKILL5-only expandable preview panel. It does not modify frozen SKILL4 UI.
    // Selecting a skill auto-equips its canonical test loadout, then executes it.
    [DisallowMultipleComponent]
    public sealed class HighflySkill5QuickPanel : MonoBehaviour
    {
        private HighflySkill5Runtime _runtime;
        private GameObject _panel;
        private Text _status;
        private readonly Text[] _labels = new Text[5];
        private float _nextRefresh;

        private static readonly HighflyCombatAction[] Actions =
        {
            HighflyCombatAction.Skill1,
            HighflyCombatAction.Skill2,
            HighflyCombatAction.Skill3,
            HighflyCombatAction.Skill4,
            HighflyCombatAction.Ultimate
        };

        private static readonly string[] Names =
        {
            "S1 • SONIC LEAP",
            "S2 • HORIZONTAL SQUARE",
            "S3 • DOUBLE CIRCULAR",
            "S4 • SPINNING SHIELD",
            "ULT • APOCALYPSE"
        };

        private void Awake()
        {
            _runtime = GetComponent<HighflySkill5Runtime>();
            Build();
        }

        private void Update()
        {
            if (_panel == null || !_panel.activeSelf || _runtime == null) return;
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + 0.10f;

            for (int i = 0; i < Actions.Length; i++)
            {
                if (_labels[i] == null) continue;
                float cd = _runtime.GetCooldownRemaining(Actions[i]);
                _labels[i].text = cd > 0.05f
                    ? Names[i] + "   " + cd.ToString("0.0") + "s"
                    : Names[i];
            }
        }

        private void Build()
        {
            GameObject canvasGo = new GameObject(
                "SKILL5_PREMIUM_PANEL_CANVAS",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 8750;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Button toggle = CreateButton(
                canvasGo.transform,
                "SKILLS ▼",
                new Vector2(462f, -28f),
                new Vector2(180f, 56f),
                18);
            toggle.onClick.AddListener(() =>
            {
                if (_panel != null) _panel.SetActive(!_panel.activeSelf);
            });

            _panel = new GameObject(
                "SKILL5_PREMIUM_PANEL",
                typeof(RectTransform),
                typeof(Image));
            _panel.transform.SetParent(canvasGo.transform, false);

            RectTransform pr = _panel.GetComponent<RectTransform>();
            pr.anchorMin = pr.anchorMax = new Vector2(0f, 1f);
            pr.pivot = new Vector2(0f, 1f);
            pr.anchoredPosition = new Vector2(462f, -94f);
            pr.sizeDelta = new Vector2(390f, 430f);
            _panel.GetComponent<Image>().color = new Color(0.018f, 0.026f, 0.045f, 0.96f);

            Text title = CreateText(
                _panel.transform,
                "SKILL5 • PREMIUM FIVE\nTOCAR = EQUIPAR + PREVIEW",
                new Vector2(18f, -16f),
                new Vector2(354f, 62f),
                20,
                TextAnchor.UpperLeft);

            for (int i = 0; i < Actions.Length; i++)
            {
                int slot = i;
                Button b = CreateButton(
                    _panel.transform,
                    Names[i],
                    new Vector2(18f, -92f - i * 56f),
                    new Vector2(354f, 48f),
                    16);
                _labels[i] = b.GetComponentInChildren<Text>();
                b.onClick.AddListener(() => StartCoroutine(Preview(Actions[slot])));
            }

            _status = CreateText(
                _panel.transform,
                "Listo • el HUD derecho también queda conectado.",
                new Vector2(18f, -382f),
                new Vector2(354f, 32f),
                13,
                TextAnchor.MiddleLeft);

            _panel.SetActive(false);
        }

        private IEnumerator Preview(HighflyCombatAction action)
        {
            HighflyRun0HCharacterVisual visual = HighflyRun0HCharacterVisual.Instance;
            if (visual != null)
            {
                switch (action)
                {
                    case HighflyCombatAction.Skill1:
                    case HighflyCombatAction.Skill2:
                    case HighflyCombatAction.Ultimate:
                        visual.UseLoadout(HighflyLoadoutProfile.Sword1H);
                        break;
                    case HighflyCombatAction.Skill3:
                        visual.UseLoadout(HighflyLoadoutProfile.DualSword);
                        break;
                    case HighflyCombatAction.Skill4:
                        visual.UseLoadout(HighflyLoadoutProfile.SwordShield);
                        break;
                }
            }

            yield return null;
            yield return null;

            bool ok = _runtime != null && _runtime.RouteAction(action);
            if (_status != null)
                _status.text = ok
                    ? "Ejecutando: " + DisplayName(action)
                    : "No ejecutó: cooldown/estado todavía bloqueado.";
        }

        private static string DisplayName(HighflyCombatAction action)
        {
            for (int i = 0; i < Actions.Length; i++)
                if (Actions[i] == action) return Names[i];
            return action.ToString();
        }

        private static Button CreateButton(
            Transform parent,
            string label,
            Vector2 anchoredPosition,
            Vector2 size,
            int fontSize)
        {
            GameObject go = new GameObject(
                "SKILL5_" + label.Replace(" ", "_").Replace("•", ""),
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0f, 1f);
            r.anchoredPosition = anchoredPosition;
            r.sizeDelta = size;

            Image image = go.GetComponent<Image>();
            image.color = new Color(0.035f, 0.075f, 0.115f, 0.96f);

            Text t = CreateText(
                go.transform,
                label,
                new Vector2(10f, -4f),
                new Vector2(size.x - 20f, size.y - 8f),
                fontSize,
                TextAnchor.MiddleLeft);
            t.raycastTarget = false;

            return go.GetComponent<Button>();
        }

        private static Text CreateText(
            Transform parent,
            string value,
            Vector2 anchoredPosition,
            Vector2 size,
            int fontSize,
            TextAnchor alignment)
        {
            GameObject go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            RectTransform r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0f, 1f);
            r.anchoredPosition = anchoredPosition;
            r.sizeDelta = size;

            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.text = value;
            return text;
        }
    }

    public sealed class HighflySkill5ActionProxy :
        MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private HighflySkill5Runtime _runtime;
        private HighflyLucidCombatBridge _foundation;
        private HighflyCombatAction _action;
        private int _pointerId = int.MinValue;
        private RectTransform _rect;
        private Image _image;
        private Vector3 _restScale = Vector3.one;
        private Color _restColor;

        private void Awake()
        {
            _rect = transform as RectTransform;
            _image = GetComponent<Image>();
            if (_rect != null) _restScale = _rect.localScale;
            if (_image != null) _restColor = _image.color;
        }

        public void Configure(
            HighflySkill5Runtime runtime,
            HighflyLucidCombatBridge foundation,
            HighflyCombatAction action)
        {
            _runtime = runtime;
            _foundation = foundation;
            _action = action;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_pointerId != int.MinValue) return;
            if (!HighflyTouchOwnership.TryClaim(eventData.pointerId, HighflyTouchOwner.Combat)) return;
            _pointerId = eventData.pointerId;

            if (_rect != null) _rect.localScale = _restScale * 0.92f;
            if (_image != null)
            {
                Color x = _restColor;
                _image.color = new Color(
                    Mathf.Min(1f, x.r + 0.10f),
                    Mathf.Min(1f, x.g + 0.16f),
                    Mathf.Min(1f, x.b + 0.22f),
                    Mathf.Min(1f, x.a + 0.16f));
            }

            bool consumed = _runtime != null && _runtime.RouteAction(_action);
            if (!consumed && _foundation != null)
                _foundation.Request(_action);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;
            HighflyTouchOwnership.Release(_pointerId);
            _pointerId = int.MinValue;
            RestoreVisual();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            RestoreVisual();
        }

        private void OnDisable()
        {
            if (_pointerId != int.MinValue)
            {
                HighflyTouchOwnership.Release(_pointerId);
                _pointerId = int.MinValue;
            }
            RestoreVisual();
        }

        private void RestoreVisual()
        {
            if (_rect != null) _rect.localScale = _restScale;
            if (_image != null) _image.color = _restColor;
        }
    }
}
