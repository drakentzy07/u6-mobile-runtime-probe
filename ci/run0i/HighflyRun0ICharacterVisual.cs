using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using Highfly.Run0I2;

namespace Highfly.Run0H
{
    public enum HighflyRun0HCharacter { Warrior, Assassin }

    [DisallowMultipleComponent]
    public sealed class HighflyRun0HCharacterVisual : MonoBehaviour
    {
        public static HighflyRun0HCharacterVisual Instance { get; private set; }

        private const string KnightResource = "HIGHFLY/Run0H/KayKitKnight";
        private const string BarbarianResource = "HIGHFLY/Run0H/KayKitBarbarian";
        private const string RogueResource = "HIGHFLY/Run0H/KayKitRogue";
        private const string RogueHoodedResource = "HIGHFLY/Run0H/KayKitRogueHooded";
        private const string SwordResource = "HIGHFLY/Run0H/KayKitSword1H";
        private const string AxeResource = "HIGHFLY/Run0H/KayKitAxe1H";
        private const string DaggerResource = "HIGHFLY/Run0H/KayKitDagger";
        private const string ShieldResource = "HIGHFLY/Run0H/KayKitShieldRound";
        private const string SpearResource = "HIGHFLY/Run0H/QuaterniusSpear";
        private const float TargetHeight = 1.72f;

        private PlayerController _player;
        private Animator _sourceAnimator;
        private Renderer[] _sourceRenderers = Array.Empty<Renderer>();
        private GameObject _visualPivot;
        private GameObject _visualRoot;
        private Animator _visualAnimator;
        private HighflyRun0HAnimatorMirror _mirror;
        private HighflyRun0HCharacter _current = HighflyRun0HCharacter.Warrior;
        private readonly HighflyEquipmentState _equipment = new HighflyEquipmentState();
        private HighflyLoadoutProfile _loadout = HighflyLoadoutProfile.Unarmed;
        private bool _bound;

        private Transform _primaryBase, _primaryTip, _secondaryBase, _secondaryTip;
        private GameObject _primaryWeaponObject, _secondaryWeaponObject, _shieldObject;
        private TrailRenderer _primaryTrail, _secondaryTrail;
        private PlayableGraph _actionGraph;
        private bool _actionGraphValid;
        private Coroutine _landingRecovery;

        public bool IsBound => _bound;
        public HighflyRun0HCharacter Current => _current;
        public HighflyLoadoutProfile CurrentLoadout => _loadout;
        public HighflyEquipmentState Equipment => _equipment;
        public bool UsesSecondaryTrace => HighflyMeleeLibrary.Get(_loadout).UsesSecondaryTrace;
        public PlayerController Player => _player;
        public Animator VisualAnimator => _visualAnimator;
        public Transform PrimaryBase => _primaryBase;
        public Transform PrimaryTip => _primaryTip;
        public Transform SecondaryBase => _secondaryBase;
        public Transform SecondaryTip => _secondaryTip;
        public string CurrentLabel => HighflyMeleeLibrary.Get(_loadout).Label;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (FindAnyObjectByType<HighflyRun0HCharacterVisual>() != null) return;
            var root = new GameObject("HIGHFLY_RUN0I_CHARACTER_VISUAL");
            DontDestroyOnLoad(root);
            root.AddComponent<HighflyRun0HCharacterVisual>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            StartCoroutine(BindLoop());
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            StopActionClip();
            if (Instance == this) Instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _bound = false;
            _player = null;
            _sourceAnimator = null;
            _sourceRenderers = Array.Empty<Renderer>();
            DestroyCurrentVisual();
        }

        private IEnumerator BindLoop()
        {
            var wait = new WaitForSecondsRealtime(0.20f);
            while (true)
            {
                if (!_bound) TryBind();
                yield return wait;
            }
        }

        private void TryBind()
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player == null) return;

            Animator source = player.animator != null
                ? player.animator
                : player.GetComponentInChildren<Animator>(true);
            if (source == null || source.runtimeAnimatorController == null) return;

            _player = player;
            _sourceAnimator = source;
            _sourceRenderers = player.GetComponentsInChildren<Renderer>(true);
            ForceHideLucidRenderers();
            SpawnCurrent();
            _player.animator = _sourceAnimator;
            _bound = _visualAnimator != null;
            Debug.Log("[RUN0I.2] CHARACTER BOUND • " + CurrentLabel);
        }

        public void UseWarrior() => UseLoadout(HighflyLoadoutProfile.SwordShield);
        public void UseAssassin() => UseLoadout(HighflyLoadoutProfile.DualDaggers);

        public void UseLoadout(HighflyLoadoutProfile profile) => EquipPreset(profile);

        public void EquipPreset(HighflyLoadoutProfile profile)
        {
            _equipment.SetPreset(profile);
            ResolveEquipmentLoadout();
        }

        public void ResolveEquipmentLoadout()
        {
            _loadout = HighflyLoadoutResolver.Resolve(_equipment);
            _current = _loadout == HighflyLoadoutProfile.DualDaggers
                ? HighflyRun0HCharacter.Assassin
                : HighflyRun0HCharacter.Warrior;
            if (_player != null && _sourceAnimator != null) SpawnCurrent();
            Debug.Log("[RUN0I.2] EQUIPMENT -> "+_loadout+
                " • main="+_equipment.MainHand+
                " • off="+_equipment.OffHand+
                " • two="+_equipment.TwoHand);
        }

        public string ResolveGuardClip()
        {
            switch (_loadout)
            {
                case HighflyLoadoutProfile.DualSword: return "DualSword_C";
                case HighflyLoadoutProfile.DualDaggers: return "Dagger_C";
                case HighflyLoadoutProfile.DualAxe: return "DualAxe_C";
                case HighflyLoadoutProfile.SwordShield:
                case HighflyLoadoutProfile.AxeShield:
                case HighflyLoadoutProfile.Spear2H:
                case HighflyLoadoutProfile.Unarmed: return "Sword_Block";
                default: return "Sword_Block";
            }
        }

        public bool PlayGuardPose()
        {
            string clipName=ResolveGuardClip();
            HighflyGuardStyle guard=HighflyMeleeLibrary.Get(_loadout).Guard;
            if (guard==HighflyGuardStyle.CrossGuard)
                return PlayActionPose(clipName,0.48f);
            return PlayActionClip(clipName,1f);
        }

        public bool PlayActionPose(string clipName,float normalizedTime)
        {
            if (_visualAnimator==null || string.IsNullOrWhiteSpace(clipName)) return false;
            AnimationClip clip=Resources.Load<AnimationClip>("HIGHFLY/Run0I/Animations/"+clipName);
            if (clip==null)
            {
                Debug.LogWarning("[RUN0I.3] Missing guard pose clip: "+clipName);
                return false;
            }

            StopActionClip();
            if (_mirror!=null) _mirror.enabled=false;

            AnimationClipPlayable playable=AnimationPlayableUtilities.PlayClip(_visualAnimator,clip,out _actionGraph);
            playable.SetApplyFootIK(false);
            playable.SetTime(Mathf.Clamp01(normalizedTime)*clip.length);
            playable.SetSpeed(0d);
            _actionGraph.Evaluate(0f);
            _actionGraphValid=true;
            return true;
        }

        public float GetActionClipLength(string clipName)
        {
            if (string.IsNullOrWhiteSpace(clipName)) return 0f;
            AnimationClip clip = Resources.Load<AnimationClip>("HIGHFLY/Run0I/Animations/" + clipName);
            return clip != null ? clip.length : 0f;
        }

        public bool PlayActionClip(string clipName, float speed = 1f)
        {
            if (_visualAnimator == null || string.IsNullOrWhiteSpace(clipName)) return false;
            AnimationClip clip = Resources.Load<AnimationClip>("HIGHFLY/Run0I/Animations/" + clipName);
            if (clip == null)
            {
                Debug.LogWarning("[RUN0I] Missing action clip: " + clipName);
                return false;
            }

            StopActionClip();
            if (_mirror != null) _mirror.enabled = false;

            AnimationClipPlayable playable =
                AnimationPlayableUtilities.PlayClip(_visualAnimator, clip, out _actionGraph);
            playable.SetSpeed(Mathf.Max(0.05f, speed));
            playable.SetApplyFootIK(false);
            _actionGraphValid = true;
            return true;
        }

        public void StopActionClip()
        {
            if (_actionGraphValid)
            {
                try { if (_actionGraph.IsValid()) _actionGraph.Destroy(); } catch { }
                _actionGraphValid = false;
            }
            if (_mirror != null) _mirror.enabled = true;
        }

        public void RecoverFromLanding()
        {
            StopActionClip();
            if (_landingRecovery != null) StopCoroutine(_landingRecovery);
            _landingRecovery = StartCoroutine(LandingRecoveryPulse());
        }

        private IEnumerator LandingRecoveryPulse()
        {
            Animator source = _sourceAnimator;
            Animator visual = _visualAnimator;
            float sourceSpeed = source != null ? source.speed : 1f;
            float visualSpeed = visual != null ? visual.speed : 1f;

            // The donor landing state is a little too long for the compact lab jump.
            // Briefly accelerate only the recovery frames, then return to normal.
            if (source != null) source.speed = Mathf.Max(sourceSpeed, 1.9f);
            if (visual != null) visual.speed = Mathf.Max(visualSpeed, 1.9f);

            yield return new WaitForSecondsRealtime(0.16f);

            if (source != null) source.speed = sourceSpeed;
            if (visual != null) visual.speed = visualSpeed;
            _landingRecovery = null;
        }

        public void SetActionFacing(Vector3 forward)
        {
            if (_visualPivot == null) return;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) return;
            _visualPivot.transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        public void ClearActionFacing()
        {
            if (_visualPivot != null) _visualPivot.transform.localRotation = Quaternion.identity;
        }

        public void SetWeaponsVisible(bool visible)
        {
            if (_primaryWeaponObject != null) _primaryWeaponObject.SetActive(visible);
            if (_secondaryWeaponObject != null) _secondaryWeaponObject.SetActive(visible);
            if (_shieldObject != null) _shieldObject.SetActive(visible);
        }

        public void SetWeaponTrail(bool enabled)
        {
            if (_primaryTrail != null)
            {
                _primaryTrail.emitting = enabled;
                if (!enabled) _primaryTrail.Clear();
            }
            if (_secondaryTrail != null)
            {
                _secondaryTrail.emitting = enabled;
                if (!enabled) _secondaryTrail.Clear();
            }
        }

        private void SpawnCurrent()
        {
            DestroyCurrentVisual();
            string resource = ResolveCharacterResource(_loadout);
            GameObject prefab = Resources.Load<GameObject>(resource);
            if (prefab == null) { Debug.LogError("[RUN0I.2] Missing resource: " + resource); return; }

            _visualPivot = new GameObject("HIGHFLY_RUN0I2_VISUAL_PIVOT");
            _visualPivot.transform.SetParent(_player.transform, false);

            _visualRoot = Instantiate(prefab, _visualPivot.transform);
            _visualRoot.name = "HIGHFLY_" + _loadout.ToString().ToUpperInvariant();
            _visualRoot.transform.localPosition = Vector3.zero;
            _visualRoot.transform.localRotation = Quaternion.identity;
            _visualRoot.transform.localScale = Vector3.one;

            foreach (Collider c in _visualRoot.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            foreach (Rigidbody rb in _visualRoot.GetComponentsInChildren<Rigidbody>(true))
            {
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }

            _visualAnimator = _visualRoot.GetComponent<Animator>();
            if (_visualAnimator == null) _visualAnimator = _visualRoot.AddComponent<Animator>();

            Avatar[] avatars = Resources.LoadAll<Avatar>(resource);
            foreach (Avatar avatar in avatars)
            {
                if (avatar != null && avatar.isValid && avatar.isHuman)
                {
                    _visualAnimator.avatar = avatar;
                    break;
                }
            }

            _visualAnimator.runtimeAnimatorController = _sourceAnimator.runtimeAnimatorController;
            _visualAnimator.applyRootMotion = false;
            _visualAnimator.fireEvents = false;
            _visualAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            RemoveExistingHandEquipment();
            NormalizeVisual();

            AttachLoadoutEquipment();

            _mirror = _visualRoot.AddComponent<HighflyRun0HAnimatorMirror>();
            _mirror.Bind(_sourceAnimator, _visualAnimator);
            ForceHideLucidRenderers();
        }

        private void RemoveExistingHandEquipment()
        {
            if (_visualAnimator == null || !_visualAnimator.isHuman) return;
            Transform right = _visualAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            Transform left = _visualAnimator.GetBoneTransform(HumanBodyBones.LeftHand);

            // RUN0I.2: never hide a body renderer by proximity to the hands.
            // Only explicit equipment-named child renderers may be hidden.
            Renderer[] renderers = _visualRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null) continue;
                string path = RelativePathBelowRoot(r.transform,_visualRoot.transform).ToLowerInvariant();
                if (path.Contains("sword") || path.Contains("weapon") || path.Contains("shield") ||
                    path.Contains("dagger") || path.Contains("axe") || path.Contains("mace") ||
                    path.Contains("bow") || path.Contains("quiver") || path.Contains("staff") ||
                    path.Contains("spear"))
                    r.enabled = false;
            }
        }

        private static string RelativePathBelowRoot(Transform t,Transform root)
        {
            string path=t.name;
            for (Transform p=t.parent; p!=null && p!=root; p=p.parent)
                path=p.name+"/"+path;
            return path;
        }

        private void NormalizeVisual()
        {
            Renderer[] renderers = _visualRoot.GetComponentsInChildren<Renderer>(true);
            bool found = false;
            Bounds bounds = default;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || !r.enabled) continue;
                if (!found) { bounds = r.bounds; found = true; } else bounds.Encapsulate(r.bounds);
            }
            if (!found || bounds.size.y < 0.01f) return;

            float scale = Mathf.Clamp(TargetHeight / bounds.size.y, 0.12f, 2.5f);
            _visualRoot.transform.localScale = Vector3.one * scale;
            Physics.SyncTransforms();

            found = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || !r.enabled) continue;
                if (!found) { bounds = r.bounds; found = true; } else bounds.Encapsulate(r.bounds);
            }
            if (found) _visualRoot.transform.position += Vector3.up * (_player.transform.position.y - bounds.min.y);
        }

        private static string ResolveCharacterResource(HighflyLoadoutProfile profile)
        {
            // One persistent Hunter: equipment changes combat language, never the body/model.
            return KnightResource;
        }

        private void AttachLoadoutEquipment()
        {
            switch (_loadout)
            {
                case HighflyLoadoutProfile.Unarmed:
                    break;
                case HighflyLoadoutProfile.Sword1H:
                    AttachPrimary(SwordResource,"HIGHFLY_SWORD_R");
                    break;
                case HighflyLoadoutProfile.DualSword:
                    AttachPrimary(SwordResource,"HIGHFLY_SWORD_R");
                    AttachSecondary(SwordResource,"HIGHFLY_SWORD_L");
                    break;
                case HighflyLoadoutProfile.SwordShield:
                    AttachPrimary(SwordResource,"HIGHFLY_SWORD_R");
                    AttachShield();
                    break;
                case HighflyLoadoutProfile.Axe1H:
                    AttachPrimary(AxeResource,"HIGHFLY_AXE_R");
                    break;
                case HighflyLoadoutProfile.DualAxe:
                    AttachPrimary(AxeResource,"HIGHFLY_AXE_R");
                    AttachSecondary(AxeResource,"HIGHFLY_AXE_L");
                    break;
                case HighflyLoadoutProfile.AxeShield:
                    AttachPrimary(AxeResource,"HIGHFLY_AXE_R");
                    AttachShield();
                    break;
                case HighflyLoadoutProfile.DualDaggers:
                    AttachPrimary(DaggerResource,"HIGHFLY_DAGGER_R");
                    AttachSecondary(DaggerResource,"HIGHFLY_DAGGER_L");
                    break;
                case HighflyLoadoutProfile.Spear2H:
                    AttachPrimary(SpearResource,"HIGHFLY_SPEAR_2H");
                    break;
            }
        }

        private void AttachPrimary(string resource,string name)
        {
            Transform hand=_visualAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            GameObject prefab=Resources.Load<GameObject>(resource);
            if (hand==null || prefab==null) { Debug.LogError("[RUN0I.2] Missing primary "+resource); return; }
            GameObject weapon=AttachWeapon(prefab,hand,name);
            TuneWeaponTransform(weapon,_loadout,true);
            _primaryWeaponObject=weapon;
            BuildWeaponSockets(weapon,out _primaryBase,out _primaryTip);
            _primaryTrail=BuildTrail(_primaryTip);
        }

        private void AttachSecondary(string resource,string name)
        {
            Transform hand=_visualAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
            GameObject prefab=Resources.Load<GameObject>(resource);
            if (hand==null || prefab==null) { Debug.LogError("[RUN0I.2] Missing secondary "+resource); return; }
            GameObject weapon=AttachWeapon(prefab,hand,name);
            TuneWeaponTransform(weapon,_loadout,false);
            _secondaryWeaponObject=weapon;
            BuildWeaponSockets(weapon,out _secondaryBase,out _secondaryTip);
            _secondaryTrail=BuildTrail(_secondaryTip);
        }

        private void AttachShield()
        {
            // Shield belongs to the hand for this Hunter rig. Parenting it to the lower arm
            // pushed the mesh toward the shoulder as the forearm rotated.
            Transform hand=_visualAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
            GameObject prefab=Resources.Load<GameObject>(ShieldResource);
            if (hand==null || prefab==null) { Debug.LogError("[RUN0I.3] Missing shield"); return; }
            _shieldObject=AttachWeapon(prefab,hand,"HIGHFLY_SHIELD_L");
            TuneWeaponTransform(_shieldObject,_loadout,false);
            BuildWeaponSockets(_shieldObject,out _secondaryBase,out _secondaryTip);
        }

        private void TuneWeaponTransform(GameObject weapon,HighflyLoadoutProfile loadout,bool primary)
        {
            if (weapon==null) return;

            float targetLength=0.90f;
            bool alignSpearGrip=false;

            // ClaudeCraft uses the authored KayKit handslot accessory transform before
            // falling back to numeric grip tables. We can do even better here because
            // the original KayKit accessory nodes are still present in this exact rig:
            // their renderers are hidden, but their transforms remain authoritative.
            bool authoredGrip=TryApplyAuthoredKayKitGrip(weapon,loadout,primary);

            switch (loadout)
            {
                case HighflyLoadoutProfile.Spear2H:
                    targetLength=2.05f;
                    // Quaternius spear is not a KayKit handslot accessory. Keep the
                    // known shaft orientation, then seat a real grip point on the hand.
                    weapon.transform.localRotation=Quaternion.Euler(0f,90f,90f);
                    weapon.transform.localPosition=Vector3.zero;
                    alignSpearGrip=true;
                    break;

                case HighflyLoadoutProfile.DualDaggers:
                    targetLength=0.48f;
                    if(!authoredGrip)
                    {
                        weapon.transform.localRotation=primary
                            ? Quaternion.Euler(0f,180f,0f)
                            : Quaternion.identity;
                        weapon.transform.localPosition=new Vector3(primary?-0.0095f:0.0095f,0.378f,0f);
                    }
                    break;

                case HighflyLoadoutProfile.DualSword:
                    targetLength=0.95f;
                    if(!authoredGrip)
                    {
                        weapon.transform.localRotation=primary
                            ? Quaternion.Euler(0f,180f,0f)
                            : Quaternion.identity;
                        weapon.transform.localPosition=new Vector3(0f,0.555f,0f);
                    }
                    break;

                case HighflyLoadoutProfile.DualAxe:
                    targetLength=0.72f;
                    if(!authoredGrip)
                    {
                        weapon.transform.localRotation=primary
                            ? Quaternion.Euler(0f,180f,0f)
                            : Quaternion.identity;
                        weapon.transform.localPosition=new Vector3(primary?0.232f:-0.232f,0.382f,0f);
                    }
                    break;

                case HighflyLoadoutProfile.Axe1H:
                    targetLength=0.72f;
                    if(!authoredGrip)
                    {
                        weapon.transform.localRotation=Quaternion.Euler(0f,180f,0f);
                        weapon.transform.localPosition=new Vector3(0.232f,0.382f,0f);
                    }
                    break;

                case HighflyLoadoutProfile.AxeShield:
                    if(primary)
                    {
                        targetLength=0.72f;
                        if(!authoredGrip)
                        {
                            weapon.transform.localRotation=Quaternion.Euler(0f,180f,0f);
                            weapon.transform.localPosition=new Vector3(0.232f,0.382f,0f);
                        }
                    }
                    else
                    {
                        targetLength=0.68f;
                        if(!authoredGrip)
                        {
                            weapon.transform.localRotation=Quaternion.identity;
                            weapon.transform.localPosition=new Vector3(0f,0.017f,0.177f);
                        }
                    }
                    break;

                case HighflyLoadoutProfile.SwordShield:
                    if(primary)
                    {
                        targetLength=0.95f;
                        if(!authoredGrip)
                        {
                            weapon.transform.localRotation=Quaternion.Euler(0f,180f,0f);
                            weapon.transform.localPosition=new Vector3(0f,0.555f,0f);
                        }
                    }
                    else
                    {
                        targetLength=0.68f;
                        if(!authoredGrip)
                        {
                            weapon.transform.localRotation=Quaternion.identity;
                            weapon.transform.localPosition=new Vector3(0f,0.017f,0.177f);
                        }
                    }
                    break;

                case HighflyLoadoutProfile.Sword1H:
                    targetLength=0.95f;
                    if(!authoredGrip)
                    {
                        weapon.transform.localRotation=Quaternion.Euler(0f,180f,0f);
                        weapon.transform.localPosition=new Vector3(0f,0.555f,0f);
                    }
                    break;
            }

            NormalizeWeaponWorldLength(weapon,targetLength);

            if (alignSpearGrip)
                AlignSpearGripToHand(weapon,0.30f);
        }

        private bool TryApplyAuthoredKayKitGrip(GameObject weapon,HighflyLoadoutProfile loadout,bool primary)
        {
            if(weapon==null || _visualAnimator==null) return false;

            HumanBodyBones bone=primary ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand;
            Transform hand=_visualAnimator.GetBoneTransform(bone);
            if(hand==null) return false;

            string referenceName=null;
            switch(loadout)
            {
                case HighflyLoadoutProfile.Sword1H:
                    if(primary) referenceName="1H_Sword";
                    break;
                case HighflyLoadoutProfile.DualSword:
                    referenceName=primary ? "1H_Sword" : "1H_Sword_Offhand";
                    break;
                case HighflyLoadoutProfile.SwordShield:
                    referenceName=primary ? "1H_Sword" : "Round_Shield";
                    break;
                case HighflyLoadoutProfile.Axe1H:
                    if(primary) referenceName="1H_Axe";
                    break;
                case HighflyLoadoutProfile.DualAxe:
                    referenceName=primary ? "1H_Axe" : "1H_Axe_Offhand";
                    break;
                case HighflyLoadoutProfile.AxeShield:
                    referenceName=primary ? "1H_Axe" : "Round_Shield";
                    break;
                case HighflyLoadoutProfile.DualDaggers:
                    referenceName=primary ? "Knife" : "Knife_Offhand";
                    break;
            }

            if(string.IsNullOrEmpty(referenceName)) return false;

            Transform reference=FindNamedDescendant(hand,referenceName);
            if(reference==null)
            {
                Debug.Log("[RUN0I.3] authored grip missing under "+bone+": "+referenceName+"; using audited fallback");
                return false;
            }

            weapon.transform.localPosition=hand.InverseTransformPoint(reference.position);
            weapon.transform.localRotation=Quaternion.Inverse(hand.rotation)*reference.rotation;
            Debug.Log("[RUN0I.3] authored grip applied "+referenceName+" -> "+weapon.name);
            return true;
        }

        private static Transform FindNamedDescendant(Transform root,string exactName)
        {
            if(root==null) return null;
            string wanted=NormalizeGripName(exactName);

            Transform[] all=root.GetComponentsInChildren<Transform>(true);
            for(int i=0;i<all.Length;i++)
            {
                Transform t=all[i];
                if(t==null) continue;
                if(NormalizeGripName(t.name)==wanted) return t;
            }
            return null;
        }

        private static string NormalizeGripName(string value)
        {
            if(string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("[","").Replace("]","").Replace(".","").Replace(":","").Replace("_","").ToLowerInvariant();
        }

        private static void AlignSpearGripToHand(GameObject weapon,float gripFraction)
        {
            if (weapon==null) return;

            MeshFilter[] filters=weapon.GetComponentsInChildren<MeshFilter>(true);
            bool found=false;
            Vector3 min=Vector3.zero, max=Vector3.zero;

            for (int i=0;i<filters.Length;i++)
            {
                MeshFilter mf=filters[i];
                if (mf==null || mf.sharedMesh==null) continue;
                Bounds mb=mf.sharedMesh.bounds;
                Vector3[] corners=new Vector3[8];
                int k=0;
                for (int xi=-1;xi<=1;xi+=2)
                for (int yi=-1;yi<=1;yi+=2)
                for (int zi=-1;zi<=1;zi+=2)
                {
                    Vector3 p=mb.center+Vector3.Scale(mb.extents,new Vector3(xi,yi,zi));
                    Vector3 world=mf.transform.TransformPoint(p);
                    Vector3 local=weapon.transform.InverseTransformPoint(world);
                    corners[k++]=local;
                }

                for (int j=0;j<corners.Length;j++)
                {
                    Vector3 p=corners[j];
                    if(!found){min=max=p;found=true;}
                    else {min=Vector3.Min(min,p);max=Vector3.Max(max,p);}
                }
            }

            if(!found) return;

            Vector3 size=max-min;
            int axis=size.x>=size.y && size.x>=size.z ? 0 : (size.y>=size.z ? 1 : 2);
            Vector3 grip=(min+max)*0.5f;
            float aMin=axis==0?min.x:(axis==1?min.y:min.z);
            float aMax=axis==0?max.x:(axis==1?max.y:max.z);
            float a=Mathf.Lerp(aMin,aMax,Mathf.Clamp01(gripFraction));
            if(axis==0) grip.x=a;
            else if(axis==1) grip.y=a;
            else grip.z=a;

            Vector3 scaled=Vector3.Scale(weapon.transform.localScale,grip);
            Vector3 rotated=weapon.transform.localRotation*scaled;
            weapon.transform.localPosition=-rotated;
        }

        private static void NormalizeWeaponWorldLength(GameObject weapon,float targetLength)
        {
            Renderer[] rs=weapon.GetComponentsInChildren<Renderer>(true);
            bool found=false;
            Bounds b=default;
            for(int i=0;i<rs.Length;i++)
            {
                if(rs[i]==null) continue;
                if(!found){b=rs[i].bounds;found=true;} else b.Encapsulate(rs[i].bounds);
            }
            if(!found) return;
            float longest=Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z));
            if(longest<0.0001f) return;
            float factor=Mathf.Clamp(targetLength/longest,0.05f,20f);
            weapon.transform.localScale*=factor;
        }

        private static GameObject AttachWeapon(GameObject prefab, Transform hand, string name)
        {
            GameObject weapon = Instantiate(prefab, hand);
            weapon.name = name;
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;
            weapon.transform.localScale = Vector3.one;
            foreach (Collider c in weapon.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            return weapon;
        }

        private static void BuildWeaponSockets(GameObject weapon, out Transform weaponBase, out Transform weaponTip)
        {
            GameObject b = new GameObject("WeaponBase");
            b.transform.SetParent(weapon.transform, false);
            b.transform.localPosition = Vector3.zero;
            weaponBase = b.transform;

            Vector3 bestLocal = new Vector3(0f, 0f, 0.65f);
            float bestSqr = bestLocal.sqrMagnitude;
            MeshFilter[] filters = weapon.GetComponentsInChildren<MeshFilter>(true);
            for (int f = 0; f < filters.Length; f++)
            {
                MeshFilter mf = filters[f];
                if (mf == null || mf.sharedMesh == null) continue;
                Bounds mb = mf.sharedMesh.bounds;
                Vector3 e = mb.extents;
                for (int xi=-1; xi<=1; xi+=2)
                for (int yi=-1; yi<=1; yi+=2)
                for (int zi=-1; zi<=1; zi+=2)
                {
                    Vector3 corner = mb.center + Vector3.Scale(e, new Vector3(xi,yi,zi));
                    Vector3 world = mf.transform.TransformPoint(corner);
                    Vector3 local = weapon.transform.InverseTransformPoint(world);
                    if (local.sqrMagnitude > bestSqr) { bestSqr = local.sqrMagnitude; bestLocal = local; }
                }
            }

            GameObject t = new GameObject("WeaponTip");
            t.transform.SetParent(weapon.transform, false);
            t.transform.localPosition = bestLocal;
            weaponTip = t.transform;
        }

        private static TrailRenderer BuildTrail(Transform tip)
        {
            if (tip == null) return null;
            TrailRenderer trail = tip.gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.16f;
            trail.minVertexDistance = 0.015f;
            trail.widthMultiplier = 0.075f;
            trail.numCapVertices = 2;
            trail.numCornerVertices = 2;
            trail.emitting = false;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            Material mat = new Material(shader);
            Texture2D slash = Resources.Load<Texture2D>("HIGHFLY/Run0I/slash_02");
            if (slash != null) mat.mainTexture = slash;
            trail.material = mat;
            return trail;
        }

        private void LateUpdate() => ForceHideLucidRenderers();

        private void ForceHideLucidRenderers()
        {
            for (int i=0; i<_sourceRenderers.Length; i++)
            {
                Renderer r = _sourceRenderers[i];
                if (r != null && r.enabled) r.enabled = false;
            }
        }

        private void DestroyCurrentVisual()
        {
            StopActionClip();
            if (_visualPivot != null) Destroy(_visualPivot);
            _visualPivot = null; _visualRoot = null; _visualAnimator = null; _mirror = null;
            _primaryBase = null; _primaryTip = null; _secondaryBase = null; _secondaryTip = null;
            _primaryWeaponObject = null; _secondaryWeaponObject = null; _shieldObject = null;
            _primaryTrail = null; _secondaryTrail = null;
        }
    }

    [DefaultExecutionOrder(5000)]
    public sealed class HighflyRun0HAnimatorMirror : MonoBehaviour
    {
        private Animator _source, _visual;
        public void Bind(Animator source, Animator visual) { _source = source; _visual = visual; }

        private void Update()
        {
            if (_source == null || _visual == null) return;
            AnimatorControllerParameter[] parameters = _source.parameters;
            for (int i=0; i<parameters.Length; i++)
            {
                AnimatorControllerParameter p = parameters[i];
                switch (p.type)
                {
                    case AnimatorControllerParameterType.Float: _visual.SetFloat(p.nameHash,_source.GetFloat(p.nameHash)); break;
                    case AnimatorControllerParameterType.Int: _visual.SetInteger(p.nameHash,_source.GetInteger(p.nameHash)); break;
                    case AnimatorControllerParameterType.Bool: _visual.SetBool(p.nameHash,_source.GetBool(p.nameHash)); break;
                }
            }
        }

        private void LateUpdate()
        {
            if (_source == null || _visual == null) return;
            int layers = Mathf.Min(_source.layerCount,_visual.layerCount);
            for (int layer=0; layer<layers; layer++)
            {
                AnimatorStateInfo src = _source.GetCurrentAnimatorStateInfo(layer);
                AnimatorStateInfo dst = _visual.GetCurrentAnimatorStateInfo(layer);
                if (src.fullPathHash == 0) continue;
                float srcNorm = Mathf.Repeat(src.normalizedTime,1f);
                float dstNorm = Mathf.Repeat(dst.normalizedTime,1f);
                float drift = Mathf.Abs(Mathf.DeltaAngle(srcNorm*360f,dstNorm*360f))/360f;
                if (dst.fullPathHash != src.fullPathHash || drift > 0.14f)
                    _visual.Play(src.fullPathHash,layer,srcNorm);
            }
        }
    }
}
