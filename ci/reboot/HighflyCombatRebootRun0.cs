using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace Highfly.SkillLab
{
    [DisallowMultipleComponent]
    public sealed class HighflyCombatRebootRun0 : MonoBehaviour
    {
        public static HighflyCombatRebootRun0 Instance { get; private set; }

        private const string HunterResource = "HIGHFLY/CharacterCompare/KayKitKnight";
        private const string SwordResource = "HIGHFLY/CharacterCompare/KayKitSword1H";
        private const float TargetHeight = 1.64f;

        private PlayerController _player;
        private Animator _lucidAnimator;
        private Animator _hunterAnimator;
        private RuntimeAnimatorController _lucidController;
        private Renderer[] _lucidRenderers;

        private GameObject _visualPivot;
        private GameObject _hunterRoot;
        private GameObject _sword;
        private Transform _weaponTip;
        private TrailRenderer _trail;
        private HighflyRun0AnimationDriver _driver;

        private Text _status;
        private Text _logText;
        private float _playSpeed = 1f;
        private bool _trailEnabled;
        private bool _busy;
        private int _runToken;
        private float _savedMoveSpeed;
        private float _savedSprintSpeed;
        private float _savedRotationSpeed;
        private float _savedAttackCost;

        private readonly List<string> _log = new List<string>(64);

        public static HighflyCombatRebootRun0 Install(PlayerController player, Transform uiParent)
        {
            if (player == null) return null;

            var existing = player.GetComponent<HighflyCombatRebootRun0>();
            if (existing != null) return existing;

            var run0 = player.gameObject.AddComponent<HighflyCombatRebootRun0>();
            run0.Initialize(player, uiParent);
            return run0;
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Initialize(PlayerController player, Transform uiParent)
        {
            _player = player;
            _savedMoveSpeed = player.moveSpeed;
            _savedSprintSpeed = player.sprintSpeed;
            _savedRotationSpeed = player.rotationSpeed;
            _savedAttackCost = player.attackVolitionCost;

            _lucidAnimator = player.animator != null
                ? player.animator
                : player.GetComponentInChildren<Animator>(true);

            if (_lucidAnimator == null)
            {
                Debug.LogError("[HIGHFLY RUN0] LUCID Animator missing.");
                return;
            }

            _lucidController = _lucidAnimator.runtimeAnimatorController;
            _lucidRenderers = player.GetComponentsInChildren<Renderer>(true);

            BuildHunterSingleAnimator();
            BuildTargetDummy();
            BuildUi(uiParent);

            Log("RUN0 READY");
            Log("HUNTER=KAYKIT • LOCOMOTION=LUCID • ANIMATOR=ONE");
            Log("A=UAL2 • B=KAYKIT NATIVE • C=LUCID BASELINE");
            RefreshStatus("LISTO • Elegí A / B / C");
        }

        private void BuildHunterSingleAnimator()
        {
            GameObject prefab = Resources.Load<GameObject>(HunterResource);
            if (prefab == null)
            {
                Debug.LogError("[HIGHFLY RUN0] KayKit Hunter resource missing: " + HunterResource);
                return;
            }

            for (int i = 0; i < _lucidRenderers.Length; i++)
                if (_lucidRenderers[i] != null)
                    _lucidRenderers[i].enabled = false;

            _visualPivot = new GameObject("HIGHFLY_REBOOT_VISUAL_PIVOT");
            _visualPivot.transform.SetParent(_player.transform, false);

            _hunterRoot = Instantiate(prefab, _visualPivot.transform);
            _hunterRoot.name = "HIGHFLY_REBOOT_KAYKIT_HUNTER";
            _hunterRoot.transform.localPosition = Vector3.zero;
            _hunterRoot.transform.localRotation = Quaternion.identity;
            _hunterRoot.transform.localScale = Vector3.one;

            foreach (Collider c in _hunterRoot.GetComponentsInChildren<Collider>(true))
                c.enabled = false;

            foreach (Rigidbody rb in _hunterRoot.GetComponentsInChildren<Rigidbody>(true))
            {
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }

            _hunterAnimator = _hunterRoot.GetComponent<Animator>();
            if (_hunterAnimator == null)
                _hunterAnimator = _hunterRoot.AddComponent<Animator>();

            Avatar[] avatars = Resources.LoadAll<Avatar>(HunterResource);
            for (int i = 0; i < avatars.Length; i++)
            {
                if (avatars[i] != null && avatars[i].isValid && avatars[i].isHuman)
                {
                    _hunterAnimator.avatar = avatars[i];
                    break;
                }
            }

            _hunterAnimator.runtimeAnimatorController = _lucidController;
            _hunterAnimator.applyRootMotion = false;
            _hunterAnimator.fireEvents = true;
            _hunterAnimator.updateMode = AnimatorUpdateMode.Normal;

            var eventForwarder = _hunterRoot.GetComponent<HighflyRun0AnimationEventForwarder>();
            if (eventForwarder == null)
                eventForwarder = _hunterRoot.AddComponent<HighflyRun0AnimationEventForwarder>();
            eventForwarder.Bind(_player);

            NormalizeHunterVisual();
            AttachSword();

            // RUN 0 architecture: one active Animator only.
            _lucidAnimator.enabled = false;
            _player.animator = _hunterAnimator;

            _driver = _hunterRoot.GetComponent<HighflyRun0AnimationDriver>();
            if (_driver == null)
                _driver = _hunterRoot.AddComponent<HighflyRun0AnimationDriver>();
            _driver.Bind(_hunterAnimator);

            Log("SINGLE ANIMATOR ACTIVE=" + _hunterAnimator.name);
        }

        private void NormalizeHunterVisual()
        {
            if (_hunterRoot == null || _player == null) return;

            Renderer[] renderers = _hunterRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0) return;

            bool hasBounds = false;
            Bounds bounds = new Bounds();

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null) continue;
                if (!hasBounds)
                {
                    bounds = r.bounds;
                    hasBounds = true;
                }
                else bounds.Encapsulate(r.bounds);
            }

            if (!hasBounds || bounds.size.y < 0.01f) return;

            float rawHeight = bounds.size.y;
            float scale = Mathf.Clamp(TargetHeight / rawHeight, 0.12f, 2.0f);
            _hunterRoot.transform.localScale = Vector3.one * scale;
            Physics.SyncTransforms();

            hasBounds = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null) continue;
                if (!hasBounds)
                {
                    bounds = r.bounds;
                    hasBounds = true;
                }
                else bounds.Encapsulate(r.bounds);
            }

            if (hasBounds)
            {
                float correction = _player.transform.position.y - bounds.min.y;
                _hunterRoot.transform.position += Vector3.up * correction;
            }

            Log("HUNTER SCALE raw=" + rawHeight.ToString("0.00") +
                " target=" + TargetHeight.ToString("0.00") +
                " scale=" + scale.ToString("0.000"));
        }

        private void AttachSword()
        {
            if (_hunterAnimator == null || !_hunterAnimator.isHuman) return;

            Transform hand = _hunterAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            GameObject swordPrefab = Resources.Load<GameObject>(SwordResource);
            if (hand == null || swordPrefab == null)
            {
                Log("WARN sword/hand missing");
                return;
            }

            _sword = Instantiate(swordPrefab, hand);
            _sword.name = "HIGHFLY_REBOOT_SWORD_1H";
            _sword.transform.localPosition = Vector3.zero;
            _sword.transform.localRotation = Quaternion.identity;
            _sword.transform.localScale = Vector3.one;

            foreach (Collider c in _sword.GetComponentsInChildren<Collider>(true))
                c.enabled = false;

            _weaponTip = BuildWeaponTip(_sword.transform);
            BuildTrail();
        }

        private Transform BuildWeaponTip(Transform swordRoot)
        {
            var tip = new GameObject("WeaponTip").transform;
            tip.SetParent(swordRoot, false);

            MeshFilter mf = swordRoot.GetComponentInChildren<MeshFilter>(true);
            if (mf != null && mf.sharedMesh != null)
            {
                Bounds b = mf.sharedMesh.bounds;
                Vector3 ext = b.extents;
                Vector3 axis;
                float len;

                if (ext.x >= ext.y && ext.x >= ext.z)
                {
                    axis = Vector3.right;
                    len = ext.x;
                }
                else if (ext.y >= ext.x && ext.y >= ext.z)
                {
                    axis = Vector3.up;
                    len = ext.y;
                }
                else
                {
                    axis = Vector3.forward;
                    len = ext.z;
                }

                tip.localPosition = b.center + axis * (len * 0.96f);
            }
            else
            {
                tip.localPosition = new Vector3(0f, 0.65f, 0f);
            }

            return tip;
        }

        private void BuildTrail()
        {
            if (_weaponTip == null) return;

            _trail = _weaponTip.gameObject.AddComponent<TrailRenderer>();
            _trail.time = 0.12f;
            _trail.startWidth = 0.055f;
            _trail.endWidth = 0.006f;
            _trail.minVertexDistance = 0.025f;
            _trail.autodestruct = false;
            _trail.emitting = false;
            _trail.enabled = true;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader != null)
            {
                Material mat = new Material(shader);
                mat.color = new Color(0.55f, 0.92f, 1f, 0.82f);
                _trail.material = mat;
            }
        }

        private void BuildTargetDummy()
        {
            if (_player == null) return;

            GameObject dummy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            dummy.name = "RUN0_HUMAN_SCALE_DUMMY";
            dummy.transform.position = _player.transform.position + _player.transform.forward * 4.0f;
            dummy.transform.localScale = new Vector3(0.72f, 0.92f, 0.72f);

            Renderer r = dummy.GetComponent<Renderer>();
            if (r != null)
            {
                Shader s = Shader.Find("Standard");
                if (s == null) s = Shader.Find("Universal Render Pipeline/Lit");
                if (s != null)
                {
                    Material m = new Material(s);
                    m.color = new Color(0.38f, 0.40f, 0.44f, 1f);
                    r.material = m;
                }
            }

            TextMesh label = new GameObject("Label").AddComponent<TextMesh>();
            label.transform.SetParent(dummy.transform, false);
            label.transform.localPosition = new Vector3(0f, 1.55f, 0f);
            label.transform.localScale = Vector3.one * 0.12f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 48;
            label.text = "DUMMY 1.8m\nRUN 0";
            label.color = Color.white;
        }

        private void Update()
        {
            if (_busy) return;

            if (Input.GetKeyDown(KeyCode.Alpha1)) PlayA();
            if (Input.GetKeyDown(KeyCode.Alpha2)) PlayB();
            if (Input.GetKeyDown(KeyCode.Alpha3)) PlayC();
            if (Input.GetKeyDown(KeyCode.T)) ToggleTrail();
        }

        public void PlayA()
        {
            AnimationClip[] clips =
            {
                LoadClip("HIGHFLY/Parkour/Sword_Regular_A"),
                LoadClip("HIGHFLY/Parkour/Sword_Regular_B"),
                LoadClip("HIGHFLY/Parkour/Sword_Regular_C")
            };

            PlayDonorSequence("A • UAL2 A→B→C", clips);
        }

        public void PlayB()
        {
            AnimationClip[] clips =
            {
                LoadClip("HIGHFLY/Reboot/KayKit/Melee_1H_Attack_Chop"),
                LoadClip("HIGHFLY/Reboot/KayKit/Melee_1H_Attack_Slice_Horizontal"),
                LoadClip("HIGHFLY/Reboot/KayKit/Melee_1H_Attack_Stab")
            };

            PlayDonorSequence("B • KAYKIT CHOP→H-SLICE→STAB", clips);
        }

        public void PlayC()
        {
            if (_busy || _player == null || _hunterAnimator == null) return;
            _runToken++;
            StartCoroutine(PlayLucidBaseline(_runToken));
        }

        private AnimationClip LoadClip(string path)
        {
            AnimationClip clip = Resources.Load<AnimationClip>(path);
            if (clip == null)
                Log("MISSING CLIP " + path);
            return clip;
        }

        private void PlayDonorSequence(string label, AnimationClip[] clips)
        {
            if (_busy || _driver == null) return;

            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] == null)
                {
                    RefreshStatus("FALTA CLIP • " + label);
                    return;
                }
            }

            _runToken++;
            int token = _runToken;
            BeginBakeOff(label);

            float total = 0f;
            for (int i = 0; i < clips.Length; i++)
                total += clips[i].length / Mathf.Max(0.05f, _playSpeed);

            Log(label + " • speed=" + _playSpeed.ToString("0.00") +
                " • rawTotal=" + total.ToString("0.00") + "s");

            _driver.PlaySequence(
                clips,
                _playSpeed,
                0.065f,
                delegate
                {
                    if (token != _runToken) return;
                    EndBakeOff(label + " • FIN");
                },
                delegate(string msg)
                {
                    Log(label + " • " + msg);
                });
        }

        private IEnumerator PlayLucidBaseline(int token)
        {
            BeginBakeOff("C • LUCID BASELINE 1→2→3");
            _savedAttackCost = _player.attackVolitionCost;
            _player.attackVolitionCost = 0f;
            _player.HighflyLabForceLocomotion();

            yield return null;
            _player.HighflyMobileAttack();

            float timeout = Time.unscaledTime + 4.5f;
            while (_player.currentState != PlayerState.Attack && Time.unscaledTime < timeout)
                yield return null;

            yield return new WaitForSecondsRealtime(0.12f);
            _player.HighflyMobileAttack();
            Log("C • queued hit2");

            int comboHash = Animator.StringToHash("ComboStep");
            while (_hunterAnimator.GetInteger(comboHash) < 1 &&
                   _player.currentState == PlayerState.Attack &&
                   Time.unscaledTime < timeout)
                yield return null;

            if (_player.currentState == PlayerState.Attack)
            {
                yield return new WaitForSecondsRealtime(0.08f);
                _player.HighflyMobileAttack();
                Log("C • queued hit3");
            }

            while (_player.currentState == PlayerState.Attack &&
                   Time.unscaledTime < timeout)
                yield return null;

            _player.attackVolitionCost = _savedAttackCost;

            if (token == _runToken)
                EndBakeOff("C • LUCID BASELINE • FIN");
        }

        private void BeginBakeOff(string label)
        {
            _busy = true;
            _driver?.StopNow();
            _player.ReleaseHighflyMobileInput();
            _player.HighflyLabForceLocomotion();

            _savedMoveSpeed = _player.moveSpeed;
            _savedSprintSpeed = _player.sprintSpeed;
            _savedRotationSpeed = _player.rotationSpeed;

            _player.moveSpeed = 0f;
            _player.sprintSpeed = 0f;
            _player.rotationSpeed = 0f;

            if (_trail != null)
            {
                _trail.Clear();
                _trail.emitting = _trailEnabled;
            }

            RefreshStatus(label + "\nREPRODUCIENDO...");
            Log("BEGIN " + label);
        }

        private void EndBakeOff(string label)
        {
            _driver?.StopNow();

            _player.moveSpeed = _savedMoveSpeed;
            _player.sprintSpeed = _savedSprintSpeed;
            _player.rotationSpeed = _savedRotationSpeed;
            _player.attackVolitionCost = _savedAttackCost;
            _player.HighflyLabForceLocomotion();

            if (_trail != null)
                _trail.emitting = false;

            _busy = false;
            RefreshStatus(label + "\nLOCOMOTION RESTAURADA");
            Log("END " + label + " • locomotion=restored");
        }

        public void ResetPose()
        {
            _runToken++;
            _driver?.StopNow();
            _busy = false;

            if (_player != null)
            {
                _player.moveSpeed = _savedMoveSpeed > 0f ? _savedMoveSpeed : 5f;
                _player.sprintSpeed = _savedSprintSpeed > 0f ? _savedSprintSpeed : 8f;
                _player.rotationSpeed = _savedRotationSpeed > 0f ? _savedRotationSpeed : 15f;
                _player.attackVolitionCost = _savedAttackCost;
                _player.ReleaseHighflyMobileInput();
                _player.HighflyLabForceLocomotion();
            }

            if (_trail != null)
            {
                _trail.emitting = false;
                _trail.Clear();
            }

            RefreshStatus("RESET • LOCOMOTION");
            Log("MANUAL RESET");
        }

        public void ToggleTrail()
        {
            _trailEnabled = !_trailEnabled;
            if (_trail != null && !_busy)
                _trail.emitting = false;

            RefreshStatus("TRAIL " + (_trailEnabled ? "ON" : "OFF"));
            Log("TRAIL=" + (_trailEnabled ? "ON" : "OFF"));
        }

        private void SetSpeed(float value)
        {
            _playSpeed = value;
            RefreshStatus("SPEED " + _playSpeed.ToString("0.00") + "x");
            Log("SPEED=" + _playSpeed.ToString("0.00"));
        }

        private void BuildUi(Transform parent)
        {
            GameObject canvasGo = new GameObject(
                "Run0Canvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            canvasGo.transform.SetParent(parent, false);

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9990;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.35f;

            GameObject panel = new GameObject("RUN0_PANEL", typeof(RectTransform), typeof(Image), typeof(Outline));
            panel.transform.SetParent(canvasGo.transform, false);
            RectTransform pr = panel.GetComponent<RectTransform>();
            pr.anchorMin = pr.anchorMax = new Vector2(0f, 1f);
            pr.pivot = new Vector2(0f, 1f);
            pr.anchoredPosition = new Vector2(20f, -18f);
            pr.sizeDelta = new Vector2(690f, 430f);

            panel.GetComponent<Image>().color = new Color(0.01f, 0.016f, 0.03f, 0.95f);
            panel.GetComponent<Outline>().effectColor = new Color(0.18f, 0.82f, 1f, 0.90f);

            _status = CreateText(panel.transform, "Status", 24, TextAnchor.UpperLeft);
            RectTransform sr = _status.GetComponent<RectTransform>();
            sr.anchorMin = sr.anchorMax = new Vector2(0f, 1f);
            sr.pivot = new Vector2(0f, 1f);
            sr.anchoredPosition = new Vector2(18f, -16f);
            sr.sizeDelta = new Vector2(654f, 104f);

            Button a = CreateButton(panel.transform, "A • UAL2 1→2→3", PlayA);
            Place(a, 18f, -128f, 210f, 58f);

            Button b = CreateButton(panel.transform, "B • KAYKIT 1→2→3", PlayB);
            Place(b, 238f, -128f, 210f, 58f);

            Button c = CreateButton(panel.transform, "C • LUCID BASE", PlayC);
            Place(c, 458f, -128f, 210f, 58f);

            Button s08 = CreateButton(panel.transform, "0.85x", delegate { SetSpeed(0.85f); });
            Place(s08, 18f, -198f, 116f, 48f);

            Button s10 = CreateButton(panel.transform, "1.00x", delegate { SetSpeed(1.00f); });
            Place(s10, 144f, -198f, 116f, 48f);

            Button s115 = CreateButton(panel.transform, "1.15x", delegate { SetSpeed(1.15f); });
            Place(s115, 270f, -198f, 116f, 48f);

            Button trail = CreateButton(panel.transform, "TRAIL ON/OFF", ToggleTrail);
            Place(trail, 396f, -198f, 132f, 48f);

            Button reset = CreateButton(panel.transform, "RESET", ResetPose);
            Place(reset, 538f, -198f, 130f, 48f);

            _logText = CreateText(panel.transform, "RunLog", 18, TextAnchor.UpperLeft);
            RectTransform lr = _logText.GetComponent<RectTransform>();
            lr.anchorMin = lr.anchorMax = new Vector2(0f, 1f);
            lr.pivot = new Vector2(0f, 1f);
            lr.anchoredPosition = new Vector2(18f, -260f);
            lr.sizeDelta = new Vector2(650f, 150f);

            RefreshLog();
        }

        private static Text CreateText(Transform parent, string name, int size, TextAnchor anchor)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            Text t = go.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.alignment = anchor;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private static Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            GameObject go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
            go.transform.SetParent(parent, false);

            Image image = go.GetComponent<Image>();
            image.color = new Color(0.08f, 0.12f, 0.18f, 0.96f);

            Outline outline = go.GetComponent<Outline>();
            outline.effectColor = new Color(0.22f, 0.72f, 1f, 0.85f);

            Button button = go.GetComponent<Button>();
            button.onClick.AddListener(action);

            Text text = CreateText(go.transform, "Text", 18, TextAnchor.MiddleCenter);
            RectTransform tr = text.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;
            text.text = label;

            return button;
        }

        private static void Place(Button button, float x, float y, float w, float h)
        {
            RectTransform r = button.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0f, 1f);
            r.anchoredPosition = new Vector2(x, y);
            r.sizeDelta = new Vector2(w, h);
        }

        private void RefreshStatus(string message)
        {
            if (_status == null) return;

            _status.text =
                "HIGHFLY COMBAT REBOOT • RUN 0 • SWORD BAKE-OFF\n" +
                "KAYKIT HUNTER • LUCID LOCOMOTION • SINGLE ANIMATOR\n" +
                message;
        }

        private void Log(string message)
        {
            string line = Time.unscaledTime.ToString("0000.00") + " • " + message;
            _log.Add(line);
            while (_log.Count > 6)
                _log.RemoveAt(0);

            Debug.Log("[HIGHFLY RUN0] " + message);
            RefreshLog();
        }

        private void RefreshLog()
        {
            if (_logText == null) return;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("TELEMETRÍA RUN 0");
            for (int i = 0; i < _log.Count; i++)
                sb.AppendLine(_log[i]);

            _logText.text = sb.ToString();
        }
    }

    [DisallowMultipleComponent]
    public sealed class HighflyRun0AnimationEventForwarder : MonoBehaviour
    {
        private PlayerController _player;

        public void Bind(PlayerController player)
        {
            _player = player;
        }

        public void OnAnimationEnd()
        {
            if (_player != null) _player.OnAnimationEnd();
        }

        public void OnCounterEnd()
        {
            if (_player != null) _player.OnCounterEnd();
        }

        public void WeaponEnable()
        {
            if (_player != null) _player.WeaponEnable();
        }

        public void WeaponDisable()
        {
            if (_player != null) _player.WeaponDisable();
        }

        public void OnFootstep()
        {
            if (_player != null) _player.OnFootstep();
        }
    }

    [DisallowMultipleComponent]
    public sealed class HighflyRun0AnimationDriver : MonoBehaviour
    {
        private Animator _animator;
        private PlayableGraph _graph;
        private Coroutine _routine;
        private int _token;

        public bool IsPlaying => _graph.IsValid();

        public void Bind(Animator animator)
        {
            _animator = animator;
        }

        public void PlaySequence(
            AnimationClip[] clips,
            float speed,
            float blendSeconds,
            Action onDone,
            Action<string> onLog)
        {
            if (_animator == null || clips == null || clips.Length == 0)
                return;

            StopNow();
            _token++;
            int token = _token;
            _routine = StartCoroutine(
                RunSequence(clips, Mathf.Max(0.05f, speed), Mathf.Max(0.01f, blendSeconds), token, onDone, onLog));
        }

        public void StopNow()
        {
            _token++;

            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            if (_graph.IsValid())
                _graph.Destroy();
        }

        private IEnumerator RunSequence(
            AnimationClip[] clips,
            float speed,
            float blendSeconds,
            int token,
            Action onDone,
            Action<string> onLog)
        {
            _graph = PlayableGraph.Create("HIGHFLY_RUN0_SEQUENCE");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.UnscaledGameTime);

            AnimationPlayableOutput output =
                AnimationPlayableOutput.Create(_graph, "RUN0_HUNTER", _animator);

            AnimationMixerPlayable mixer =
                AnimationMixerPlayable.Create(_graph, clips.Length);

            AnimationClipPlayable[] playables =
                new AnimationClipPlayable[clips.Length];

            for (int i = 0; i < clips.Length; i++)
            {
                playables[i] = AnimationClipPlayable.Create(_graph, clips[i]);
                playables[i].SetApplyFootIK(false);
                playables[i].SetApplyPlayableIK(false);
                playables[i].SetSpeed(0.0);
                _graph.Connect(playables[i], 0, mixer, i);
                mixer.SetInputWeight(i, 0f);
            }

            output.SetSourcePlayable(mixer);

            playables[0].SetTime(0);
            playables[0].SetSpeed(speed);
            mixer.SetInputWeight(0, 1f);
            _graph.Play();

            for (int i = 0; i < clips.Length; i++)
            {
                if (token != _token) yield break;

                AnimationClip clip = clips[i];
                float duration = Mathf.Max(0.08f, clip.length / speed);
                float fade = i < clips.Length - 1
                    ? Mathf.Min(blendSeconds, duration * 0.25f)
                    : 0f;

                if (onLog != null)
                    onLog("clip " + (i + 1) + "/" + clips.Length +
                          " " + clip.name +
                          " len=" + duration.ToString("0.00") + "s");

                float hold = Mathf.Max(0f, duration - fade);
                if (hold > 0f)
                    yield return new WaitForSecondsRealtime(hold);

                if (i < clips.Length - 1)
                {
                    int next = i + 1;
                    playables[next].SetTime(0);
                    playables[next].SetSpeed(speed);

                    float elapsed = 0f;
                    while (elapsed < fade)
                    {
                        if (token != _token) yield break;

                        elapsed += Time.unscaledDeltaTime;
                        float t = fade > 0f ? Mathf.Clamp01(elapsed / fade) : 1f;
                        mixer.SetInputWeight(i, 1f - t);
                        mixer.SetInputWeight(next, t);
                        yield return null;
                    }

                    mixer.SetInputWeight(i, 0f);
                    mixer.SetInputWeight(next, 1f);
                    playables[i].SetSpeed(0);
                }
            }

            if (_graph.IsValid())
                _graph.Destroy();

            _routine = null;

            if (token == _token && onDone != null)
                onDone();
        }
    }
}
