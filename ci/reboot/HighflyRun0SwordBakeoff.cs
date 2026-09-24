using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UI;
using Highfly.Mobile;

namespace Highfly.SkillLab
{
    [DisallowMultipleComponent]
    public sealed class HighflyRun0SwordBakeoff : MonoBehaviour
    {
        public static HighflyRun0SwordBakeoff Instance { get; private set; }

        private enum Bank
        {
            UAL2 = 0,
            KayKit = 1,
            UAL1 = 2
        }

        private enum FxMode
        {
            Off = 0,
            Trail = 1,
            TrailImpact = 2
        }

        private const string Ual2Path = "HIGHFLY/Run0/UAL2_Standard";
        private const string KayPath = "HIGHFLY/Run0/KayKit_CombatMelee";
        private const string Ual1Path = "HIGHFLY/Run0/UAL1_Standard";

        private PlayerController _player;
        private Animator _animator;
        private Transform _weaponBase;
        private Transform _weaponTip;
        private HighflyRun0Dummy _dummy;

        private readonly Dictionary<Bank, AnimationClip[]> _sequences =
            new Dictionary<Bank, AnimationClip[]>();

        private Bank _bank = Bank.UAL2;
        private FxMode _fxMode = FxMode.Off;
        private PlayableGraph _graph;
        private Coroutine _attackRoutine;
        private bool _busy;

        private Canvas _canvas;
        private Text _status;
        private Text _attackLabel;
        private Text _fxLabel;
        private TrailRenderer _trail;
        private GameObject _impactPrefab;

        private readonly List<string> _log = new List<string>();
        private int _hitCount;
        private float _lastDuration;

        public bool Busy => _busy;

        public static HighflyRun0SwordBakeoff Install(PlayerController player, Transform uiParent)
        {
            if (player == null) return null;

            HighflyRun0SwordBakeoff existing =
                player.GetComponent<HighflyRun0SwordBakeoff>();

            if (existing != null) return existing;

            HighflyRun0SwordBakeoff result =
                player.gameObject.AddComponent<HighflyRun0SwordBakeoff>();

            result.Initialize(player, uiParent);
            return result;
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            StopAttackImmediate();
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) SelectBank(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SelectBank(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SelectBank(2);
            if (Input.GetKeyDown(KeyCode.F)) TriggerAttack();
            if (Input.GetKeyDown(KeyCode.V)) CycleFx();
            if (Input.GetKeyDown(KeyCode.R)) ResetRun0();

            RefreshStatus();
        }

        private void Initialize(PlayerController player, Transform uiParent)
        {
            _player = player;

            HighflyRun0Hunter hunter = HighflyRun0Hunter.Instance;
            if (hunter == null)
                hunter = HighflyRun0Hunter.Install(player);

            _animator = hunter != null ? hunter.Animator : null;
            _weaponBase = hunter != null ? hunter.WeaponBase : null;
            _weaponTip = hunter != null ? hunter.WeaponTip : null;
            _dummy = UnityEngine.Object.FindFirstObjectByType<HighflyRun0Dummy>();

            LoadBanks();
            BuildFx();
            BuildUi(uiParent);

            SelectBank(0);
            Log("RUN0 READY • LUCID locomotion • KAYKIT direct animator • VFX OFF");
        }

        private void LoadBanks()
        {
            AnimationClip[] ual2 = Clean(Resources.LoadAll<AnimationClip>(Ual2Path));
            AnimationClip[] kay = Clean(Resources.LoadAll<AnimationClip>(KayPath));
            AnimationClip[] ual1 = Clean(Resources.LoadAll<AnimationClip>(Ual1Path));

            _sequences[Bank.UAL2] = BuildUal2(ual2);
            _sequences[Bank.KayKit] = BuildKayKit(kay);
            _sequences[Bank.UAL1] = BuildUal1(ual1);

            LogBank(Bank.UAL2, ual2, _sequences[Bank.UAL2]);
            LogBank(Bank.KayKit, kay, _sequences[Bank.KayKit]);
            LogBank(Bank.UAL1, ual1, _sequences[Bank.UAL1]);
        }

        private static AnimationClip[] Clean(AnimationClip[] input)
        {
            return input
                .Where(c => c != null)
                .Where(c => !c.name.Contains("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        private static AnimationClip[] BuildUal2(AnimationClip[] clips)
        {
            AnimationClip a = Find(clips, "Sword_Regular_A", "SwordRegularA");
            AnimationClip b = Find(clips, "Sword_Regular_B", "SwordRegularB");
            AnimationClip c = Find(clips, "Sword_Regular_C", "SwordRegularC");

            return FillThree(
                clips,
                new[] { a, b, c },
                clip => Has(clip, "Sword") && (Has(clip, "Regular") || Has(clip, "Attack")));
        }

        private static AnimationClip[] BuildKayKit(AnimationClip[] clips)
        {
            AnimationClip a = Find(
                clips,
                "Melee_1H_Attack_Slice_Horizontal",
                "1H_Attack_Slice_Horizontal",
                "Slice_Horizontal");

            AnimationClip b = Find(
                clips,
                "Melee_1H_Attack_Slice_Diagonal",
                "1H_Attack_Slice_Diagonal",
                "Slice_Diagonal");

            AnimationClip c = Find(
                clips,
                "Melee_1H_Attack_Chop",
                "1H_Attack_Chop",
                "Attack_Chop");

            return FillThree(
                clips,
                new[] { a, b, c },
                clip => Has(clip, "Melee") && Has(clip, "Attack"));
        }

        private static AnimationClip[] BuildUal1(AnimationClip[] clips)
        {
            List<AnimationClip> sword = clips
                .Where(c => Has(c, "Sword") && Has(c, "Attack"))
                .OrderBy(c => c.name)
                .ToList();

            if (sword.Count == 0)
            {
                AnimationClip single = Find(clips, "Sword_Attack", "SwordAttack");
                if (single != null) sword.Add(single);
            }

            return FillThree(
                clips,
                sword.Take(3).ToArray(),
                clip => Has(clip, "Sword"));
        }

        private static AnimationClip[] FillThree(
            AnimationClip[] all,
            AnimationClip[] preferred,
            Func<AnimationClip, bool> fallbackPredicate)
        {
            List<AnimationClip> result = new List<AnimationClip>();

            for (int i = 0; i < preferred.Length; i++)
            {
                AnimationClip c = preferred[i];
                if (c != null && !result.Contains(c))
                    result.Add(c);
            }

            foreach (AnimationClip c in all.Where(fallbackPredicate))
            {
                if (!result.Contains(c))
                    result.Add(c);
                if (result.Count >= 3) break;
            }

            if (result.Count == 0 && all.Length > 0)
                result.Add(all[0]);

            while (result.Count > 0 && result.Count < 3)
                result.Add(result[result.Count - 1]);

            return result.Take(3).ToArray();
        }

        private static AnimationClip Find(AnimationClip[] clips, params string[] names)
        {
            foreach (string wanted in names)
            {
                string w = Normalize(wanted);
                AnimationClip exact = clips.FirstOrDefault(c => Normalize(c.name) == w);
                if (exact != null) return exact;
            }

            foreach (string wanted in names)
            {
                string w = Normalize(wanted);
                AnimationClip contains = clips.FirstOrDefault(c => Normalize(c.name).Contains(w));
                if (contains != null) return contains;
            }

            return null;
        }

        private static bool Has(AnimationClip clip, string token)
        {
            return clip != null && Normalize(clip.name).Contains(Normalize(token));
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return new string(value
                .Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant)
                .ToArray());
        }

        private void LogBank(Bank bank, AnimationClip[] all, AnimationClip[] selected)
        {
            string selectedText = selected != null && selected.Length > 0
                ? string.Join(" -> ", selected.Select(c => c != null ? c.name : "NULL"))
                : "NONE";

            Log(
                "BANK " + bank +
                " • imported=" + all.Length +
                " • selected=" + selectedText);

            Debug.Log(
                "[RUN0] " + bank + " all clips: " +
                string.Join(" | ", all.Select(c => c.name)));
        }

        public void TriggerAttack()
        {
            if (_busy || _animator == null) return;

            AnimationClip[] sequence;
            if (!_sequences.TryGetValue(_bank, out sequence) ||
                sequence == null ||
                sequence.Length == 0)
            {
                Log("ERROR • bank " + _bank + " has no playable clips");
                return;
            }

            _attackRoutine = StartCoroutine(PlaySequence(sequence));
        }

        private IEnumerator PlaySequence(AnimationClip[] sequence)
        {
            _busy = true;
            _hitCount = 0;
            float started = Time.unscaledTime;

            if (_player != null)
            {
                _player.SetHighflyMobileMove(Vector2.zero);
                _player.HighflyLabForceLocomotion();
                _player.enabled = false;
            }

            HighflyRun0Hunter.Instance?.ResetVisualFacing();

            BuildGraph(sequence);

            const float blend = 0.065f;
            float[] lengths = new float[sequence.Length];
            float[] starts = new float[sequence.Length];

            for (int i = 0; i < sequence.Length; i++)
            {
                lengths[i] = Mathf.Clamp(sequence[i].length, 0.32f, 1.20f);
                starts[i] = i == 0
                    ? 0f
                    : starts[i - 1] + lengths[i - 1] - blend;
            }

            float total = starts[sequence.Length - 1] + lengths[sequence.Length - 1];
            bool[] hit = new bool[sequence.Length];
            float t = 0f;

            Log(
                "ATTACK START • bank=" + _bank +
                " • clips=" + string.Join(" -> ", sequence.Select(c => c.name)));

            while (t < total)
            {
                t += Time.unscaledDeltaTime;
                bool trailActive = false;

                AnimationMixerPlayable mixer =
                    (AnimationMixerPlayable)_graph.GetRootPlayable(0);

                for (int i = 0; i < sequence.Length; i++)
                {
                    float local = t - starts[i];
                    float len = lengths[i];
                    float n = Mathf.Clamp01(local / Mathf.Max(0.001f, len));

                    float weight = 0f;
                    if (local >= 0f && local <= len)
                    {
                        weight = 1f;

                        if (i > 0 && local < blend)
                            weight = Mathf.Clamp01(local / blend);

                        if (i < sequence.Length - 1 && local > len - blend)
                            weight = Mathf.Min(
                                weight,
                                Mathf.Clamp01((len - local) / blend));
                    }

                    mixer.SetInputWeight(i, weight);

                    Playable p = mixer.GetInput(i);
                    if (p.IsValid())
                    {
                        double sourceTime =
                            Mathf.Clamp01(n) *
                            Mathf.Max(0.01f, sequence[i].length);

                        p.SetTime(sourceTime);
                    }

                    if (weight > 0.01f && n >= 0.24f && n <= 0.72f)
                        trailActive = true;

                    if (!hit[i] && local >= 0f && n >= 0.52f)
                    {
                        hit[i] = true;
                        RegisterHit(i + 1, sequence[i]);
                    }
                }

                SetTrail(trailActive);
                _graph.Evaluate(0f);
                yield return null;
            }

            SetTrail(false);
            StopGraph();

            if (_player != null)
            {
                _player.enabled = true;
                _player.HighflyLabForceLocomotion();
            }

            _lastDuration = Time.unscaledTime - started;
            _busy = false;
            _attackRoutine = null;

            Log(
                "ATTACK END • bank=" + _bank +
                " • hits=" + _hitCount +
                " • duration=" + _lastDuration.ToString("0.000") + "s");
        }

        private void BuildGraph(AnimationClip[] sequence)
        {
            StopGraph();

            _graph = PlayableGraph.Create("HIGHFLY_RUN0_" + _bank);
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

            AnimationPlayableOutput output =
                AnimationPlayableOutput.Create(
                    _graph,
                    "RUN0_HUNTER",
                    _animator);

            AnimationMixerPlayable mixer =
                AnimationMixerPlayable.Create(
                    _graph,
                    sequence.Length,
                    true);

            output.SetSourcePlayable(mixer);

            for (int i = 0; i < sequence.Length; i++)
            {
                AnimationClipPlayable cp =
                    AnimationClipPlayable.Create(
                        _graph,
                        sequence[i]);

                cp.SetApplyFootIK(false);
                cp.SetApplyPlayableIK(false);
                cp.SetSpeed(0f);
                mixer.ConnectInput(i, cp, 0);
                mixer.SetInputWeight(i, i == 0 ? 1f : 0f);
            }

            _graph.Play();
        }

        private void RegisterHit(int stage, AnimationClip clip)
        {
            _hitCount++;

            if (_dummy == null)
                _dummy = UnityEngine.Object.FindFirstObjectByType<HighflyRun0Dummy>();

            if (_dummy != null && _player != null)
            {
                float d = Vector3.Distance(
                    _player.transform.position,
                    _dummy.transform.position);

                if (d <= 4.0f)
                    _dummy.TakeDamage(1f, 0f, _player.transform);
            }

            if (_fxMode == FxMode.TrailImpact)
                SpawnImpact();

            Log(
                "HIT " + stage +
                " • bank=" + _bank +
                " • clip=" + (clip != null ? clip.name : "-"));
        }

        private void BuildFx()
        {
            if (_weaponTip != null)
            {
                _trail = _weaponTip.gameObject.GetComponent<TrailRenderer>();
                if (_trail == null)
                    _trail = _weaponTip.gameObject.AddComponent<TrailRenderer>();

                _trail.time = 0.095f;
                _trail.minVertexDistance = 0.025f;
                _trail.startWidth = 0.11f;
                _trail.endWidth = 0.015f;
                _trail.emitting = false;
                _trail.shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.Off;
                _trail.receiveShadows = false;

                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("Unlit/Texture");
                if (shader != null)
                {
                    Material mat = new Material(shader);
                    Texture2D slash =
                        Resources.Load<Texture2D>("HIGHFLY/Run0/slash_02");

                    if (slash != null)
                        mat.mainTexture = slash;

                    _trail.sharedMaterial = mat;
                }
            }

            _impactPrefab =
                Resources.Load<GameObject>("HIGHFLY/Run0/Sparks");
        }

        private void SetTrail(bool active)
        {
            if (_trail == null) return;
            _trail.emitting = active && _fxMode != FxMode.Off;
        }

        private void SpawnImpact()
        {
            if (_impactPrefab == null) return;

            Vector3 p = _dummy != null
                ? _dummy.transform.position + Vector3.up * 1.0f
                : (_weaponTip != null ? _weaponTip.position : transform.position);

            GameObject fx = Instantiate(_impactPrefab, p, Quaternion.identity);
            Destroy(fx, 2.5f);
        }

        public void SelectBank(int index)
        {
            if (_busy) return;
            _bank = (Bank)Mathf.Clamp(index, 0, 2);
            Log("SELECT • " + _bank);
            RefreshStatus();
        }

        public void CycleFx()
        {
            _fxMode = (FxMode)(((int)_fxMode + 1) % 3);
            SetTrail(false);
            Log("VFX • " + _fxMode);
            RefreshStatus();
        }

        public void ResetRun0()
        {
            StopAttackImmediate();

            if (_player == null) return;

            CharacterController cc = _player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            _player.transform.position =
                HighflySkillLabBootstrap.Run0Spawn;

            _player.transform.rotation = Quaternion.identity;

            if (cc != null) cc.enabled = true;

            _player.SetHighflyMobileMove(Vector2.zero);
            _player.HighflyLabForceLocomotion();
            HighflyRun0Hunter.Instance?.ResetVisualFacing();
            HighflyThirdPersonMobileCamera.Instance?.SnapBehindPlayer(11f);

            Log("RESET • locomotion restored");
        }

        private void StopAttackImmediate()
        {
            if (_attackRoutine != null)
            {
                StopCoroutine(_attackRoutine);
                _attackRoutine = null;
            }

            SetTrail(false);
            StopGraph();

            if (_player != null)
                _player.enabled = true;

            _busy = false;
        }

        private void StopGraph()
        {
            if (_graph.IsValid())
                _graph.Destroy();
        }

        public void CopyLog()
        {
            GUIUtility.systemCopyBuffer =
                string.Join("\n", _log);

            Log("LOG COPIED • entries=" + _log.Count);
        }

        private void Log(string message)
        {
            string line =
                Time.unscaledTime.ToString("0000.000") +
                " • " + message;

            _log.Add(line);
            if (_log.Count > 220)
                _log.RemoveAt(0);

            Debug.Log("[RUN0] " + line);
        }

        private void BuildUi(Transform parent)
        {
            GameObject go =
                new GameObject(
                    "RUN0_UI",
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));

            go.transform.SetParent(parent, false);
            _canvas = go.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 9990;

            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.45f;

            GameObject panel =
                new GameObject(
                    "RUN0_PANEL",
                    typeof(RectTransform),
                    typeof(Image));

            panel.transform.SetParent(go.transform, false);
            RectTransform pr = panel.GetComponent<RectTransform>();
            pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 1f);
            pr.pivot = new Vector2(0.5f, 1f);
            pr.anchoredPosition = new Vector2(0f, -18f);
            pr.sizeDelta = new Vector2(930f, 214f);
            panel.GetComponent<Image>().color =
                new Color(0.01f, 0.016f, 0.028f, 0.92f);

            _status = MakeText(
                panel.transform,
                "RUN0",
                17,
                TextAnchor.UpperLeft,
                new Vector2(18f, -14f),
                new Vector2(894f, 90f));

            Button a = MakeButton(panel.transform, "A • UAL2", -340f, -118f);
            Button b = MakeButton(panel.transform, "B • KAYKIT", -140f, -118f);
            Button c = MakeButton(panel.transform, "C • UAL1", 60f, -118f);
            Button fx = MakeButton(panel.transform, "VFX", 260f, -118f);
            Button copy = MakeButton(panel.transform, "COPIAR LOG", 460f, -118f);

            a.onClick.AddListener(() => SelectBank(0));
            b.onClick.AddListener(() => SelectBank(1));
            c.onClick.AddListener(() => SelectBank(2));
            fx.onClick.AddListener(CycleFx);
            copy.onClick.AddListener(CopyLog);

            _fxLabel = fx.GetComponentInChildren<Text>();

            GameObject attackGo =
                new GameObject(
                    "RUN0_ATTACK",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button));

            attackGo.transform.SetParent(go.transform, false);
            RectTransform ar = attackGo.GetComponent<RectTransform>();
            ar.anchorMin = ar.anchorMax = new Vector2(1f, 0f);
            ar.pivot = new Vector2(1f, 0f);
            ar.anchoredPosition = new Vector2(-42f, 58f);
            ar.sizeDelta = new Vector2(220f, 96f);

            attackGo.GetComponent<Image>().color =
                new Color(0.05f, 0.22f, 0.34f, 0.96f);

            attackGo.GetComponent<Button>().onClick.AddListener(TriggerAttack);

            _attackLabel = MakeText(
                attackGo.transform,
                "ATAQUE RUN0",
                22,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                new Vector2(220f, 96f));

            RectTransform atr = _attackLabel.rectTransform;
            atr.anchorMin = Vector2.zero;
            atr.anchorMax = Vector2.one;
            atr.offsetMin = Vector2.zero;
            atr.offsetMax = Vector2.zero;

            Button reset = MakeButton(go.transform, "RESET", 0f, 0f);
            RectTransform rr = reset.GetComponent<RectTransform>();
            rr.anchorMin = rr.anchorMax = new Vector2(1f, 0f);
            rr.pivot = new Vector2(1f, 0f);
            rr.anchoredPosition = new Vector2(-284f, 74f);
            rr.sizeDelta = new Vector2(130f, 58f);
            reset.onClick.AddListener(ResetRun0);
        }

        private Button MakeButton(
            Transform parent,
            string label,
            float x,
            float y)
        {
            GameObject go =
                new GameObject(
                    label,
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button));

            go.transform.SetParent(parent, false);
            RectTransform r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
            r.pivot = new Vector2(0.5f, 1f);
            r.anchoredPosition = new Vector2(x, y);
            r.sizeDelta = new Vector2(182f, 58f);

            go.GetComponent<Image>().color =
                new Color(0.035f, 0.07f, 0.11f, 0.97f);

            Text t = MakeText(
                go.transform,
                label,
                16,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                r.sizeDelta);

            RectTransform tr = t.rectTransform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;

            return go.GetComponent<Button>();
        }

        private static Text MakeText(
            Transform parent,
            string text,
            int size,
            TextAnchor anchor,
            Vector2 position,
            Vector2 sizeDelta)
        {
            GameObject go =
                new GameObject(
                    "Text",
                    typeof(RectTransform),
                    typeof(Text));

            go.transform.SetParent(parent, false);
            Text t = go.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = text;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform r = t.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0f, 1f);
            r.anchoredPosition = position;
            r.sizeDelta = sizeDelta;

            return t;
        }

        private void RefreshStatus()
        {
            if (_status == null) return;

            AnimationClip[] seq;
            _sequences.TryGetValue(_bank, out seq);

            string clips =
                seq != null && seq.Length > 0
                    ? string.Join(
                        " → ",
                        seq.Select(c => c != null ? c.name : "NULL"))
                    : "NO CLIPS";

            _status.text =
                "HIGHFLY COMBAT REBOOT • RUN 0 • SWORD BAKE-OFF\n" +
                "Hunter: KAYKIT DIRECT ANIMATOR • Movement: LUCID • Mirror: OFF\n" +
                "Banco: " + _bank +
                " • VFX: " + _fxMode +
                " • Estado: " + (_busy ? "ATTACK" : "LOCOMOTION") +
                " • Últimos hits: " + _hitCount +
                " • Duración: " + _lastDuration.ToString("0.00") + "s\n" +
                "Clips: " + clips +
                "\nPC: 1/2/3 banco • F ataque • V VFX • R reset";

            if (_fxLabel != null)
                _fxLabel.text = "VFX • " + _fxMode;

            if (_attackLabel != null)
                _attackLabel.text = _busy ? "ATACANDO..." : "ATAQUE RUN0";
        }
    }
}
