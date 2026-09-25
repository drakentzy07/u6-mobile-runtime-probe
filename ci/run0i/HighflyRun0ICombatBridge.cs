using System.Collections.Generic;
using UnityEngine;
using Highfly.Run0H;
using Highfly.Run0I;

namespace Highfly.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController))]
    public sealed class HighflyLucidCombatBridge : MonoBehaviour
    {
        public static HighflyLucidCombatBridge Instance { get; private set; }

        private enum ActionKind { None, Basic1, Basic2, Basic3, SonicLeap, HorizontalSquare, Slide, Repel }

        private PlayerController _player;
        private CharacterController _controller;
        private HighflyCombatCore _core;
        private ActionKind _action;
        private float _elapsed,_duration;
        private Vector3 _facing,_previousMotionOffset;
        private int _phase=-1,_activeWindow=-1;
        private bool _activeNow,_queuedLight,_parryConfirmed;
        private int _comboStep;
        private float _comboExpire,_hitstopRemaining,_sonicReadyAt,_squareReadyAt,_slideReadyAt,_repelReadyAt;
        private Vector3 _prevPrimaryTip,_prevSecondaryTip;
        private bool _hasPrevTips;
        private readonly HashSet<int> _hitThisWindow = new HashSet<int>();
        private readonly Collider[] _hits = new Collider[64];

        private AudioSource _audio;
        private AudioClip _swing,_longSwing,_hitSfx;
        private GameObject _impactPrefab;

        public bool ActionBusy => _action != ActionKind.None;
        public string DebugAction => _action.ToString();
        public static int TotalHits { get; private set; }
        public static float TotalDamage { get; private set; }
        public static string LastHit { get; private set; } = "-";

        private void Awake()
        {
            Instance = this;
            _player = GetComponent<PlayerController>();
            _controller = GetComponent<CharacterController>();
            _core = GetComponent<HighflyCombatCore>();
            if (_core == null) _core = gameObject.AddComponent<HighflyCombatCore>();

            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false; _audio.spatialBlend = 0f;
            _swing = Resources.Load<AudioClip>("HIGHFLY/Run0I/swing");
            _longSwing = Resources.Load<AudioClip>("HIGHFLY/Run0I/longSwing");
            _hitSfx = Resources.Load<AudioClip>("HIGHFLY/Run0I/hit");
            _impactPrefab = Resources.Load<GameObject>("HIGHFLY/Run0I/SparksEffect");
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Update()
        {
            if (_action == ActionKind.None) return;
            float dt = Time.unscaledDeltaTime;
            if (_hitstopRemaining > 0f) { _hitstopRemaining -= dt; return; }
            float previous = _elapsed;
            _elapsed += dt;
            UpdateAction(previous,_elapsed);
            if (_action != ActionKind.None && _elapsed >= _duration) FinishAction();
        }

        public void Request(HighflyCombatAction action)
        {
            _core?.Buffer(action);
            switch (action)
            {
                case HighflyCombatAction.Light: RequestLight(); break;
                case HighflyCombatAction.Dodge:
                    if (!ActionBusy && Time.unscaledTime >= _slideReadyAt) StartSlide();
                    break;
                case HighflyCombatAction.Parry:
                    if (!ActionBusy && Time.unscaledTime >= _repelReadyAt) StartRepel();
                    break;
                case HighflyCombatAction.Skill1:
                    if (!ActionBusy && Time.unscaledTime >= _sonicReadyAt) StartSonicLeap();
                    break;
                case HighflyCombatAction.Skill2:
                    if (!ActionBusy && Time.unscaledTime >= _squareReadyAt) StartHorizontalSquare();
                    break;
                case HighflyCombatAction.Skill3:
                case HighflyCombatAction.Skill4:
                case HighflyCombatAction.Ultimate:
                    Debug.Log("[RUN0I] Locked by quality gate: " + action);
                    break;
            }
            HighflyBufferedAction consumed;
            _core?.TryConsumeAny(out consumed);
        }

        public bool TryParryIncoming(float damage,float composureDamage,Transform attacker)
        {
            if (_action != ActionKind.Repel || _parryConfirmed || _elapsed < 0.050f || _elapsed > 0.140f)
                return false;

            _parryConfirmed = true;
            _hitstopRemaining = 0.090f;
            SpawnImpact(transform.position + Vector3.up*1.05f);
            PlayOneShot(_hitSfx);
            if (_elapsed < 0.180f) _elapsed = 0.180f;
            SetPhase(1,"Sword_Regular_A",1.05f);
            return true;
        }

        private void RequestLight()
        {
            if (ActionBusy)
            {
                if (_action == ActionKind.Basic1 || _action == ActionKind.Basic2 || _action == ActionKind.Basic3)
                    _queuedLight = true;
                return;
            }
            if (Time.unscaledTime > _comboExpire) _comboStep = 0;
            StartBasic((_comboStep % 3)+1);
        }

        private void StartBasic(int step)
        {
            _comboStep = step;
            _comboExpire = Time.unscaledTime + 0.72f;
            _queuedLight = false;
            ActionKind kind = step==1?ActionKind.Basic1:step==2?ActionKind.Basic2:ActionKind.Basic3;
            float duration = step==1?0.40f:step==2?0.37f:0.57f;
            BeginAction(kind,duration,PlayerState.Attack);

            bool assassin = IsAssassin();
            string clip = !assassin
                ? (step==1?"Sword_Regular_A":step==2?"Sword_Regular_B":"Sword_Regular_C")
                : (step==1?"Assassin_A":step==2?"Assassin_B":"Assassin_C");
            SetPhase(0,clip,1f);
            PlayOneShot(_swing);
        }

        private void StartSonicLeap()
        {
            _sonicReadyAt = Time.unscaledTime + 6f;
            BeginAction(ActionKind.SonicLeap,0.940f,PlayerState.Skill);
            SetPhase(0,"NinjaJump_Start",1f);
            PlayOneShot(_longSwing);
        }

        private void StartHorizontalSquare()
        {
            _squareReadyAt = Time.unscaledTime + 7f;
            BeginAction(ActionKind.HorizontalSquare,1.150f,PlayerState.Skill);
            SetPhase(0,"Sword_Regular_A",1f);
            PlayOneShot(_swing);
        }

        private void StartSlide()
        {
            _slideReadyAt = Time.unscaledTime + 10f;
            BeginAction(ActionKind.Slide,0.580f,PlayerState.Roll);
            SetPhase(0,"Slide_Start",1f);
        }

        private void StartRepel()
        {
            _repelReadyAt = Time.unscaledTime + 10f;
            _parryConfirmed = false;
            BeginAction(ActionKind.Repel,0.690f,PlayerState.Parry);
            SetPhase(0,"Sword_Block",1f);
        }

        private void BeginAction(ActionKind kind,float duration,PlayerState state)
        {
            _action=kind; _duration=duration; _elapsed=0f; _phase=-1; _activeWindow=-1; _activeNow=false;
            _hitThisWindow.Clear(); _previousMotionOffset=Vector3.zero; _hasPrevTips=false;
            _player.currentState=state; _player.SetHighflyMobileMove(Vector2.zero);
            _facing=ResolveCombatForward();
            HighflyRun0HCharacterVisual.Instance?.SetActionFacing(_facing);
            HighflyRun0HCharacterVisual.Instance?.SetWeaponTrail(false);
            Debug.Log("[RUN0I] ACTION START " + kind);
        }

        private void UpdateAction(float previous,float now)
        {
            switch (_action)
            {
                case ActionKind.Basic1: UpdateBasic(now,0.120f,0.210f,0.16f); break;
                case ActionKind.Basic2: UpdateBasic(now,0.100f,0.190f,0.16f); break;
                case ActionKind.Basic3: UpdateBasic(now,0.160f,0.280f,0.24f); break;
                case ActionKind.SonicLeap: UpdateSonicLeap(now); break;
                case ActionKind.HorizontalSquare: UpdateHorizontalSquare(now); break;
                case ActionKind.Slide: UpdateSlide(now); break;
                case ActionKind.Repel: UpdateRepel(now); break;
            }
        }

        private void UpdateBasic(float now,float activeStart,float activeEnd,float movement)
        {
            MoveToOffset(_facing * movement * Mathf.Clamp01(now/Mathf.Max(0.01f,activeEnd)));
            SetActiveWindow(now>=activeStart && now<=activeEnd,0,18f+_comboStep*4f,_comboStep==3?0.065f:0.045f);
            if (_queuedLight && now>=activeEnd+0.045f)
            {
                int next=(_comboStep%3)+1;
                FinishAction(false);
                StartBasic(next);
            }
        }

        private void UpdateSonicLeap(float now)
        {
            Vector3 desired=Vector3.zero;
            if (now<=0.380f)
            {
                float t=Mathf.Clamp01(now/0.380f);
                float y=4f*1.25f*t*(1f-t);
                desired=_facing*(2.60f*t)+Vector3.up*y;
                if (_phase!=0) SetPhase(0,"NinjaJump_Start",1f);
            }
            else if (now<=0.660f)
            {
                float t=Mathf.InverseLerp(0.380f,0.660f,now);
                desired=_facing*(2.60f+0.19f*t);
                if (_phase!=1) { SetPhase(1,"Sword_Heavy_Combo",1.10f); PlayOneShot(_swing); }
            }
            else desired=_facing*2.79f;

            MoveToOffset(desired);
            SetActiveWindow(now>=0.498f && now<=0.590f,0,34f,0.042f);
        }

        private void UpdateHorizontalSquare(float now)
        {
            string[] clips={"Sword_Regular_A","Sword_Regular_B","Sword_Regular_C","Sword_Regular_A"};
            int phase=now<0.24f?0:now<0.48f?1:now<0.74f?2:now<0.99f?3:4;
            if (phase<4 && phase!=_phase) { SetPhase(phase,clips[phase],1f); PlayOneShot(_swing); }

            MoveToOffset(_facing*(1.20f*Mathf.Clamp01(now/0.99f)));
            bool active=false; int window=-1;
            if (now>=0.100f && now<=0.180f) { active=true; window=0; }
            else if (now>=0.340f && now<=0.420f) { active=true; window=1; }
            else if (now>=0.580f && now<=0.660f) { active=true; window=2; }
            else if (now>=0.840f && now<=0.920f) { active=true; window=3; }

            SetActiveWindow(active,window,window==3?30f:19f,window==3?0.055f:0.028f);
        }

        private void UpdateSlide(float now)
        {
            int phase=now<0.100f?0:now<0.420f?1:2;
            if (phase!=_phase) SetPhase(phase,phase==0?"Slide_Start":phase==1?"Slide_Loop":"Slide_Exit",1f);
            MoveToOffset(_facing*(2.40f*Mathf.Clamp01(now/0.420f)));
            SetActiveWindow(false,-1,0f,0f);
        }

        private void UpdateRepel(float now)
        {
            if (_parryConfirmed)
            {
                if (now>=0.180f && _phase!=1) SetPhase(1,"Sword_Regular_A",1.05f);
                MoveToOffset(_facing*(0.18f*Mathf.Clamp01(now/0.410f)));
                SetActiveWindow(now>=0.267f && now<=0.341f,0,26f,0.070f);
            }
            else
            {
                SetActiveWindow(false,-1,0f,0f);
                if (now>=0.220f) { FinishAction(); return; }
            }
        }

        private void SetPhase(int phase,string clip,float speed)
        {
            _phase=phase;
            HighflyRun0HCharacterVisual.Instance?.PlayActionClip(clip,speed);
        }

        private void SetActiveWindow(bool active,int windowId,float damage,float hitstop)
        {
            if (active && (!_activeNow || _activeWindow!=windowId))
            {
                _activeWindow=windowId; _hitThisWindow.Clear();
                HighflyRun0HCharacterVisual.Instance?.SetWeaponTrail(true);
            }
            if (!active && _activeNow) HighflyRun0HCharacterVisual.Instance?.SetWeaponTrail(false);
            _activeNow=active;
            if (!active) { _hasPrevTips=false; return; }
            TraceWeapons(damage,hitstop);
        }

        private void TraceWeapons(float damage,float hitstop)
        {
            HighflyRun0HCharacterVisual visual=HighflyRun0HCharacterVisual.Instance;
            if (visual==null) return;
            TraceOne(visual.PrimaryBase,visual.PrimaryTip,ref _prevPrimaryTip,damage,hitstop);
            if (IsAssassin()) TraceOne(visual.SecondaryBase,visual.SecondaryTip,ref _prevSecondaryTip,damage,hitstop);
            _hasPrevTips=true;
        }

        private void TraceOne(Transform weaponBase,Transform weaponTip,ref Vector3 previousTip,float damage,float hitstop)
        {
            if (weaponBase==null || weaponTip==null) return;
            Vector3 a=weaponBase.position,b=weaponTip.position;
            int count=Physics.OverlapCapsuleNonAlloc(a,b,0.16f,_hits,~0,QueryTriggerInteraction.Collide);
            for (int i=0;i<count;i++) ResolveHit(_hits[i],damage,hitstop);

            if (_hasPrevTips)
            {
                Vector3 delta=b-previousTip; float distance=delta.magnitude;
                if (distance>0.001f)
                {
                    RaycastHit[] sweep=Physics.SphereCastAll(previousTip,0.13f,delta/distance,distance,~0,QueryTriggerInteraction.Collide);
                    for (int i=0;i<sweep.Length;i++) ResolveHit(sweep[i].collider,damage,hitstop);
                }
            }
            previousTip=b;
        }

        private void ResolveHit(Collider collider,float damage,float hitstop)
        {
            if (collider==null) return;
            if (collider.transform==transform || collider.transform.IsChildOf(transform)) return;
            CharacterStats stats=collider.GetComponentInParent<CharacterStats>();
            if (stats==null || stats==_player.GetComponent<PlayerStats>()) return;
            int id=stats.GetInstanceID();
            if (!_hitThisWindow.Add(id)) return;

            Vector3 contact=collider.ClosestPoint(
                HighflyRun0HCharacterVisual.Instance?.PrimaryTip!=null
                    ? HighflyRun0HCharacterVisual.Instance.PrimaryTip.position
                    : transform.position+transform.forward);

            stats.TakeDamage(damage,20f,transform);
            TotalHits++; TotalDamage+=damage;
            LastHit=stats.name+" • "+damage.ToString("0")+" dmg";
            SpawnImpact(contact); PlayOneShot(_hitSfx);
            _hitstopRemaining=Mathf.Max(_hitstopRemaining,hitstop);

            HighflyRun0IAttackDummy dummy=stats.GetComponent<HighflyRun0IAttackDummy>();
            if (dummy!=null && _action==ActionKind.Repel) dummy.ReceiveRepel(_facing,3f);
        }

        private void SpawnImpact(Vector3 position)
        {
            if (_impactPrefab==null) return;
            GameObject fx=Instantiate(_impactPrefab,position,Quaternion.identity);
            Destroy(fx,2f);
        }

        private void MoveToOffset(Vector3 desiredOffset)
        {
            if (_controller==null) return;
            Vector3 delta=desiredOffset-_previousMotionOffset;
            if (delta.sqrMagnitude>0f) _controller.Move(delta);
            _previousMotionOffset=desiredOffset;
        }

        private Vector3 ResolveCombatForward()
        {
            if (_player.LockOnTarget!=null)
            {
                Vector3 to=_player.LockOnTarget.position-transform.position; to.y=0f;
                if (to.sqrMagnitude>0.01f) return to.normalized;
            }
            Transform cam=_player.cameraTransform;
            if (cam!=null)
            {
                Vector3 f=cam.forward; f.y=0f;
                if (f.sqrMagnitude>0.01f) return f.normalized;
            }
            Vector3 fallback=transform.forward; fallback.y=0f;
            return fallback.sqrMagnitude>0.01f?fallback.normalized:Vector3.forward;
        }

        private bool IsAssassin()
        {
            HighflyRun0HCharacterVisual visual=HighflyRun0HCharacterVisual.Instance;
            return visual!=null && visual.Current==HighflyRun0HCharacter.Assassin;
        }

        private void PlayOneShot(AudioClip clip) { if (_audio!=null && clip!=null) _audio.PlayOneShot(clip); }

        private void FinishAction(bool allowQueued=true)
        {
            ActionKind finished=_action;
            HighflyRun0HCharacterVisual.Instance?.SetWeaponTrail(false);
            HighflyRun0HCharacterVisual.Instance?.StopActionClip();
            HighflyRun0HCharacterVisual.Instance?.ClearActionFacing();

            _action=ActionKind.None; _elapsed=0f; _duration=0f; _phase=-1; _activeWindow=-1; _activeNow=false;
            _hitThisWindow.Clear(); _previousMotionOffset=Vector3.zero; _hasPrevTips=false;

            if (_player.currentState!=PlayerState.Die && _player.currentState!=PlayerState.Interact &&
                _player.currentState!=PlayerState.UseItem) _player.currentState=PlayerState.Locomotion;

            if (allowQueued && _queuedLight && (finished==ActionKind.Basic1 || finished==ActionKind.Basic2))
            {
                int next=(_comboStep%3)+1;
                _queuedLight=false;
                StartBasic(next);
            }
            Debug.Log("[RUN0I] ACTION FINISH "+finished);
        }
    }
}
