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
        private HighflyLoadoutProfile _loadout = HighflyLoadoutProfile.SwordShield;
        private bool _bound;

        private Transform _primaryBase, _primaryTip, _secondaryBase, _secondaryTip;
        private GameObject _primaryWeaponObject, _secondaryWeaponObject, _shieldObject;
        private TrailRenderer _primaryTrail, _secondaryTrail;
        private PlayableGraph _actionGraph;
        private bool _actionGraphValid;

        public bool IsBound => _bound;
        public HighflyRun0HCharacter Current => _current;
        public HighflyLoadoutProfile CurrentLoadout => _loadout;
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

        public void UseLoadout(HighflyLoadoutProfile profile)
        {
            _loadout = profile;
            _current = profile == HighflyLoadoutProfile.DualDaggers
                ? HighflyRun0HCharacter.Assassin
                : HighflyRun0HCharacter.Warrior;
            if (_player != null && _sourceAnimator != null) SpawnCurrent();
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

            MeshRenderer[] meshes = _visualRoot.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < meshes.Length; i++)
            {
                MeshRenderer mr = meshes[i];
                if (mr == null) continue;
                bool nearRight = right != null && Vector3.Distance(mr.bounds.center, right.position) < 0.85f;
                bool nearLeft = left != null && Vector3.Distance(mr.bounds.center, left.position) < 0.85f;
                if (nearRight || nearLeft) mr.enabled = false;
            }

            Renderer[] renderers = _visualRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null) continue;
                string path = FullPath(r.transform).ToLowerInvariant();
                if (path.Contains("sword") || path.Contains("weapon") || path.Contains("shield") ||
                    path.Contains("dagger") || path.Contains("axe") || path.Contains("mace") ||
                    path.Contains("bow") || path.Contains("quiver") || path.Contains("staff") ||
                    path.Contains("spear"))
                    r.enabled = false;
            }
        }

        private static string FullPath(Transform t)
        {
            string path = t.name;
            for (Transform p = t.parent; p != null; p = p.parent) path = p.name + "/" + path;
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
            switch (profile)
            {
                case HighflyLoadoutProfile.Axe1H:
                case HighflyLoadoutProfile.DualAxe:
                case HighflyLoadoutProfile.AxeShield:
                    return BarbarianResource;
                case HighflyLoadoutProfile.DualDaggers:
                    return RogueResource;
                case HighflyLoadoutProfile.DualSword:
                case HighflyLoadoutProfile.Spear2H:
                    return RogueHoodedResource;
                default:
                    return KnightResource;
            }
        }

        private void AttachLoadoutEquipment()
        {
            switch (_loadout)
            {
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
            _secondaryWeaponObject=weapon;
            BuildWeaponSockets(weapon,out _secondaryBase,out _secondaryTip);
            _secondaryTrail=BuildTrail(_secondaryTip);
        }

        private void AttachShield()
        {
            Transform hand=_visualAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
            GameObject prefab=Resources.Load<GameObject>(ShieldResource);
            if (hand==null || prefab==null) { Debug.LogError("[RUN0I.2] Missing shield"); return; }
            _shieldObject=AttachWeapon(prefab,hand,"HIGHFLY_SHIELD_L");
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
