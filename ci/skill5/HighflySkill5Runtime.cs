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
    // HIGHFLY_SKILL5_HORIZONTAL_SQUARE_PREMIUM
    // SKILL4 is an immutable dependency. This runtime owns only SKILL5 execution.
    [DisallowMultipleComponent]
    public sealed class HighflySkill5Runtime : MonoBehaviour
    {
        public const string BuildMarker = "HIGHFLY_SKILL5_HORIZONTAL_SQUARE_PREMIUM";
        public const float HorizontalSquareCooldown = 7f;
        public const float HorizontalSquareVolition = 18f;
        private const float Duration = 1.28f;
        private const float CancelOpen = 1.00f;
        private const float LinkOpen = 1.02f;
        private const float LinkClose = 1.20f;

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

        private bool _busy;
        private float _elapsed;
        private float _readyAt;
        private float _hitstopRemaining;
        private int _phase = -1;
        private int _window = -1;
        private bool _active;
        private Vector3 _facing;
        private float _previousForward;
        private Vector3 _previousTip;
        private bool _hasPreviousTip;
        private readonly HashSet<CharacterStats> _hitThisWindow = new HashSet<CharacterStats>();
        private readonly Collider[] _overlap = new Collider[64];

        public bool ActionBusy => _busy;
        public float CooldownRemaining => Mathf.Max(0f, _readyAt - Time.unscaledTime);
        public float NormalizedTime => _busy ? Mathf.Clamp01(_elapsed / Duration) : 0f;
        public bool CancelWindowOpen => _busy && _elapsed >= CancelOpen;
        public bool LinkWindowOpen => _busy && _elapsed >= LinkOpen && _elapsed <= LinkClose;

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
            Debug.Log("[SKILL5] Runtime online • SKILL4 foundation untouched • Horizontal Square armed");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame)
                RequestHorizontalSquare();

            if (!_busy) return;

            float dt = Time.unscaledDeltaTime;
            if (_hitstopRemaining > 0f)
            {
                _hitstopRemaining -= dt;
                return;
            }

            _elapsed += dt;
            UpdateHorizontalSquare(_elapsed);
            if (_busy && _elapsed >= Duration) FinishSkill5Action();
        }

        public bool RouteAction(HighflyCombatAction action)
        {
            if (_busy)
            {
                if ((action == HighflyCombatAction.Dodge || action == HighflyCombatAction.Parry) &&
                    CancelWindowOpen)
                {
                    Debug.Log("[SKILL5] Horizontal Square recovery cancel -> " + action);
                    FinishSkill5Action();
                    return false;
                }

                if ((action == HighflyCombatAction.Light || action == HighflyCombatAction.Skill1) &&
                    LinkWindowOpen)
                {
                    Debug.Log("[SKILL5] Horizontal Square link -> " + action);
                    FinishSkill5Action();
                    return false;
                }

                return true;
            }

            if (action != HighflyCombatAction.Skill2) return false;
            RequestHorizontalSquare();
            return true;
        }

        public void RequestHorizontalSquare()
        {
            if (_busy) return;
            if (_foundation != null && _foundation.ActionBusy) return;

            _visual = HighflyRun0HCharacterVisual.Instance;
            if (_player == null || _controller == null || _visual == null || !_visual.IsBound)
            {
                Debug.LogWarning("[SKILL5] Horizontal Square waiting for Hunter foundation.");
                return;
            }

            if (_visual.CurrentLoadout != HighflyLoadoutProfile.Sword1H)
            {
                Debug.Log("[SKILL5] Horizontal Square requires ESPADA 1H.");
                return;
            }

            if (Time.unscaledTime < _readyAt) return;
            if (_stats != null && !_stats.UseVolition(HorizontalSquareVolition))
            {
                Debug.Log("[SKILL5] Horizontal Square blocked: Volition < 18.");
                return;
            }

            _readyAt = Time.unscaledTime + HorizontalSquareCooldown;
            _busy = true;
            _elapsed = 0f;
            _hitstopRemaining = 0f;
            _phase = -1;
            _window = -1;
            _active = false;
            _previousForward = 0f;
            _hasPreviousTip = false;
            _hitThisWindow.Clear();
            _facing = ResolveCombatForward(_player.HighflyMobileMoveInput);
            _player.currentState = PlayerState.Skill;
            _visual.SetActionFacing(_facing);
            _visual.SetWeaponTrail(false);
            SetPhase(0);
            Debug.Log("[SKILL5] HORIZONTAL SQUARE START • CD 7s • COST 18 • 4 REAL HIT WINDOWS");
        }

        private void UpdateHorizontalSquare(float now)
        {
            int nextPhase = now < 0.25f ? 0 : now < 0.50f ? 1 : now < 0.77f ? 2 : now < 1.03f ? 3 : 4;
            if (nextPhase < 4 && nextPhase != _phase) SetPhase(nextPhase);

            float travelT = Mathf.Clamp01(now / 0.98f);
            MoveForward(1.15f * Mathf.SmoothStep(0f, 1f, travelT));

            bool active = false;
            int window = -1;
            if (now >= 0.12f && now <= 0.20f) { active = true; window = 0; }
            else if (now >= 0.36f && now <= 0.44f) { active = true; window = 1; }
            else if (now >= 0.62f && now <= 0.70f) { active = true; window = 2; }
            else if (now >= 0.89f && now <= 0.98f) { active = true; window = 3; }

            float damage = window == 3 ? 30f : 19f;
            float hitstop = window == 3 ? 0.060f : 0.030f;
            SetActiveWindow(active, window, damage, hitstop);
        }

        private void SetPhase(int phase)
        {
            _phase = phase;
            string clip;
            float speed;
            switch (phase)
            {
                case 0: clip = "Sword_Regular_A"; speed = 1.35f; break;
                case 1: clip = "Sword_Regular_B"; speed = 1.35f; break;
                case 2: clip = "Sword_Regular_C"; speed = 1.30f; break;
                default: clip = "Warrior_B"; speed = 1.24f; break;
            }

            _visual.PlayActionClip(clip, speed);
            PlayOneShot(phase == 3 ? _longSwing : _swing);
        }

        private void SetActiveWindow(bool active, int window, float damage, float hitstop)
        {
            if (active && (!_active || _window != window))
            {
                _window = window;
                _hitThisWindow.Clear();
                _hasPreviousTip = false;
                _visual.SetWeaponTrail(true);

                Vector3 right = Vector3.Cross(Vector3.up, _facing).normalized;
                Vector3 center = transform.position + Vector3.up * 1.06f + _facing * 1.42f;
                HighflySkill5Fx.SpawnSquareScar(window, center, _facing, right);
            }

            if (!active && _active)
            {
                _visual.SetWeaponTrail(false);
                _hasPreviousTip = false;
            }

            _active = active;
            if (!active) return;
            TracePrimaryWeapon(damage, hitstop);
        }

        private void TracePrimaryWeapon(float damage, float hitstop)
        {
            Transform weaponBase = _visual.PrimaryBase;
            Transform weaponTip = _visual.PrimaryTip;
            if (weaponBase == null || weaponTip == null) return;

            Vector3 a = weaponBase.position;
            Vector3 b = weaponTip.position;
            int count = Physics.OverlapCapsuleNonAlloc(a, b, 0.16f, _overlap, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++) ResolveHit(_overlap[i], damage, hitstop);

            if (_hasPreviousTip)
            {
                Vector3 delta = b - _previousTip;
                float distance = delta.magnitude;
                if (distance > 0.001f)
                {
                    RaycastHit[] sweep = Physics.SphereCastAll(
                        _previousTip, 0.13f, delta / distance, distance, ~0, QueryTriggerInteraction.Collide);
                    for (int i = 0; i < sweep.Length; i++) ResolveHit(sweep[i].collider, damage, hitstop);
                }
            }

            _previousTip = b;
            _hasPreviousTip = true;
        }

        private void ResolveHit(Collider collider, float damage, float hitstop)
        {
            if (collider == null) return;
            if (collider.transform == transform || collider.transform.IsChildOf(transform)) return;

            CharacterStats target = collider.GetComponentInParent<CharacterStats>();
            if (target == null || target == _stats) return;
            if (!_hitThisWindow.Add(target)) return;

            Vector3 contact = collider.ClosestPoint(
                _visual.PrimaryTip != null ? _visual.PrimaryTip.position : transform.position + _facing);

            target.TakeDamage(damage, _window == 3 ? 34f : 20f, transform);
            PlayOneShot(_hit);
            SpawnSparks(contact);
            _hitstopRemaining = Mathf.Max(_hitstopRemaining, hitstop);

            if (_window == 3)
                HighflySkill5Fx.SpawnFinalCross(contact + Vector3.up * 0.05f, _facing);

            Debug.Log("[SKILL5] HS HIT " + (_window + 1) + "/4 • " + target.name + " • " + damage.ToString("0") + " dmg");
        }

        private void SpawnSparks(Vector3 position)
        {
            if (_sparksPrefab == null) return;
            GameObject fx = Instantiate(_sparksPrefab, position, Quaternion.identity);
            Destroy(fx, 2f);
        }

        private void MoveForward(float desiredDistance)
        {
            float delta = desiredDistance - _previousForward;
            if (Mathf.Abs(delta) > 0.00001f)
                _controller.Move(_facing * delta);
            _previousForward = desiredDistance;
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
                Vector3 f = _player.cameraTransform.forward; f.y = 0f; f.Normalize();
                Vector3 r = _player.cameraTransform.right; r.y = 0f; r.Normalize();
                Vector3 dir = f * stick.y + r * stick.x;
                if (dir.sqrMagnitude > 0.01f) return dir.normalized;
            }

            Vector3 fallback = transform.forward;
            fallback.y = 0f;
            return fallback.sqrMagnitude > 0.01f ? fallback.normalized : Vector3.forward;
        }

        private void FinishSkill5Action()
        {
            if (!_busy) return;
            _busy = false;
            _active = false;
            _window = -1;
            _phase = -1;
            _hitThisWindow.Clear();
            _hasPreviousTip = false;
            _visual?.SetWeaponTrail(false);
            _visual?.StopActionClip();
            _visual?.ClearActionFacing();

            if (_player != null && _player.currentState == PlayerState.Skill)
                _player.currentState = PlayerState.Locomotion;

            Debug.Log("[SKILL5] HORIZONTAL SQUARE FINISH");
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (_audio != null && clip != null) _audio.PlayOneShot(clip);
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

    // Runtime adapter: replaces only input delivery. All SKILL4 components/files stay byte-identical.
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
            _nextScan = Time.unscaledTime + 0.35f;
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

                if (action == HighflyCombatAction.Skill2)
                    UpdateSkill2Label(button.gameObject);
            }
        }

        private void UpdateSkill2Label(GameObject button)
        {
            Text[] texts = button.GetComponentsInChildren<Text>(true);
            if (texts.Length == 0) return;

            Text label = texts[0];
            float remaining = _runtime != null ? _runtime.CooldownRemaining : 0f;
            label.text = remaining > 0.05f
                ? "S2\\nH.SQUARE\\n" + remaining.ToString("0.0") + "s"
                : "S2\\nH.SQUARE";
        }

        private static bool TryMap(string name, out HighflyCombatAction action)
        {
            action = HighflyCombatAction.None;
            if (name == "ATQ_BUTTON") action = HighflyCombatAction.Light;
            else if (name == "S1_BUTTON") action = HighflyCombatAction.Skill1;
            else if (name == "S2_OFF_BUTTON" || name == "S2_BUTTON") action = HighflyCombatAction.Skill2;
            else if (name == "S3_BUTTON") action = HighflyCombatAction.Skill3;
            else if (name == "S4_BUTTON") action = HighflyCombatAction.Skill4;
            else if (name == "ULT_BUTTON") action = HighflyCombatAction.Ultimate;
            else if (name == "ESQUIVAR_BUTTON") action = HighflyCombatAction.Dodge;
            else if (name == "PARRY_BUTTON") action = HighflyCombatAction.Parry;
            return action != HighflyCombatAction.None;
        }
    }

    public sealed class HighflySkill5ActionProxy : MonoBehaviour, IPointerDownHandler
    {
        private HighflySkill5Runtime _runtime;
        private HighflyLucidCombatBridge _foundation;
        private HighflyCombatAction _action;

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
            bool consumed = _runtime != null && _runtime.RouteAction(_action);
            if (!consumed && _foundation != null)
                _foundation.Request(_action);
        }
    }
}
