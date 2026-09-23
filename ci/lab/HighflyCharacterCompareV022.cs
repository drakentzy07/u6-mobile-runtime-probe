using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Highfly.SkillLab
{
    [DisallowMultipleComponent]
    public sealed class HighflyCharacterCompareV022 : MonoBehaviour
    {
        public static HighflyCharacterCompareV022 Instance { get; private set; }

        private const string KayKitResource = "HIGHFLY/CharacterCompare/KayKitKnight";
        private const string KayKitSwordResource = "HIGHFLY/CharacterCompare/KayKitSword1H";

        private PlayerController _player;
        private Animator _lucidAnimator;
        private RuntimeAnimatorController _sharedController;
        private Renderer[] _lucidRenderers;

        private GameObject _kayVisualPivot;
        private GameObject _kayRoot;
        private Animator _kayAnimator;
        private Text _status;
        private bool _usingKayKit;

        public static HighflyCharacterCompareV022 Install(PlayerController player, Transform uiParent)
        {
            if (player == null) return null;

            var existing = player.GetComponent<HighflyCharacterCompareV022>();
            if (existing != null) return existing;

            var compare = player.gameObject.AddComponent<HighflyCharacterCompareV022>();
            compare.Initialize(player, uiParent);
            return compare;
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
            _lucidAnimator = player.animator != null
                ? player.animator
                : player.GetComponentInChildren<Animator>(true);

            if (_lucidAnimator != null)
                _sharedController = _lucidAnimator.runtimeAnimatorController;

            _lucidRenderers = player.GetComponentsInChildren<Renderer>(true);
            BuildUi(uiParent);

            // v2.7: KayKit is now the default HIGHFLY Hunter visual.
            // LUCID remains alive underneath as the logical locomotion/combat animator.
            UseKayKit();
        }

        public void UseLucid()
        {
            if (_player == null || _lucidAnimator == null) return;

            _player.HighflyLabForceLocomotion();
            HighflyParkourAnimationV010.Instance?.StopNow();
            _usingKayKit = false;

            if (_kayVisualPivot != null)
                _kayVisualPivot.SetActive(false);

            for (int i = 0; i < _lucidRenderers.Length; i++)
                if (_lucidRenderers[i] != null)
                    _lucidRenderers[i].enabled = true;

            _lucidAnimator.enabled = true;
            _player.animator = _lucidAnimator;
            HighflyAnimatorMirrorV027.Instance?.Bind(_lucidAnimator, null);
            HighflyCombatFacingV027.Instance?.BindVisual(null);
            HighflyParkourAnimationV010.BindTo(_lucidAnimator);

            RefreshStatus("LUCID CANDIDATO A");
            HighflySkillLabMetrics.RecordAction("CHARACTER A/B • LUCID CANDIDATO A", 0);
            HighflyLabTestHistoryV026.Log("CHARACTER -> LUCID");
        }

        public void UseKayKit()
        {
            if (_player == null) return;

            _player.HighflyLabForceLocomotion();
            HighflyParkourAnimationV010.Instance?.StopNow();
            EnsureKayKit();
            if (_kayRoot == null || _kayAnimator == null || _kayVisualPivot == null)
            {
                RefreshStatus("KAYKIT LOAD FAILED");
                return;
            }

            _usingKayKit = true;

            for (int i = 0; i < _lucidRenderers.Length; i++)
                if (_lucidRenderers[i] != null)
                    _lucidRenderers[i].enabled = false;

            // LUCID's Animator remains enabled and remains PlayerController.animator.
            // It owns state, combo Animation Events and root motion. Only its renderers are hidden.
            if (_lucidAnimator != null)
                _lucidAnimator.enabled = true;

            _kayVisualPivot.SetActive(true);
            _kayAnimator.enabled = true;
            _kayAnimator.runtimeAnimatorController = _sharedController;
            _kayAnimator.applyRootMotion = false;
            _kayAnimator.fireEvents = false;

            _player.animator = _lucidAnimator;
            HighflyAnimatorMirrorV027.Instance?.Bind(_lucidAnimator, _kayAnimator);
            HighflyCombatFacingV027.Instance?.BindVisual(_kayVisualPivot.transform);
            HighflyParkourAnimationV010.BindTo(_kayAnimator);

            RefreshStatus("KAYKIT HUNTER • LUCID CORE");
            HighflySkillLabMetrics.RecordAction("KAYKIT HUNTER • LUCID CORE • AUTO SCALE", 0);
            HighflyLabTestHistoryV026.Log("CHARACTER -> KAYKIT HUNTER • logicalAnimator=LUCID");
        }

        private void EnsureKayKit()
        {
            if (_kayRoot != null) return;

            GameObject prefab = Resources.Load<GameObject>(KayKitResource);
            if (prefab == null)
            {
                Debug.LogError("[HIGHFLY 2.2] KayKit resource missing: " + KayKitResource);
                return;
            }

            _kayVisualPivot = new GameObject("HIGHFLY_KAYKIT_VISUAL_PIVOT");
            _kayVisualPivot.transform.SetParent(_player.transform, false);
            _kayVisualPivot.transform.localPosition = Vector3.zero;
            _kayVisualPivot.transform.localRotation = Quaternion.identity;
            _kayVisualPivot.transform.localScale = Vector3.one;

            _kayRoot = Instantiate(prefab, _kayVisualPivot.transform);
            _kayRoot.name = "HIGHFLY_KAYKIT_HUNTER_VISUAL";
            _kayRoot.transform.localPosition = Vector3.zero;
            _kayRoot.transform.localRotation = Quaternion.identity;
            _kayRoot.transform.localScale = Vector3.one;

            foreach (Collider c in _kayRoot.GetComponentsInChildren<Collider>(true))
                c.enabled = false;
            foreach (Rigidbody rb in _kayRoot.GetComponentsInChildren<Rigidbody>(true))
            {
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }

            _kayAnimator = _kayRoot.GetComponent<Animator>();
            if (_kayAnimator == null)
                _kayAnimator = _kayRoot.AddComponent<Animator>();

            Avatar[] avatars = Resources.LoadAll<Avatar>(KayKitResource);
            for (int i = 0; i < avatars.Length; i++)
            {
                if (avatars[i] != null && avatars[i].isValid && avatars[i].isHuman)
                {
                    _kayAnimator.avatar = avatars[i];
                    break;
                }
            }

            _kayAnimator.runtimeAnimatorController = _sharedController;
            _kayAnimator.applyRootMotion = false;
            _kayAnimator.fireEvents = false;

            NormalizeKayKitVisual();
            AttachKayKitSword();
            _kayVisualPivot.SetActive(false);
        }

        private void NormalizeKayKitVisual()
        {
            if (_kayRoot == null || _player == null) return;

            const float targetHeight = 1.64f;
            Renderer[] renderers = _kayRoot.GetComponentsInChildren<Renderer>(true);
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
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            if (!hasBounds || bounds.size.y < 0.01f) return;

            float rawHeight = bounds.size.y;
            float scale = Mathf.Clamp(targetHeight / rawHeight, 0.12f, 2.0f);
            _kayRoot.transform.localScale = Vector3.one * scale;
            Physics.SyncTransforms();

            // Recompute after scaling and place the visual feet on LUCID root ground.
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
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            if (hasBounds)
            {
                float footCorrection = _player.transform.position.y - bounds.min.y;
                _kayRoot.transform.position += Vector3.up * footCorrection;
            }

            HighflyLabTestHistoryV026.Log(
                "KAYKIT AUTOSCALE • rawHeight=" + rawHeight.ToString("0.00") +
                " target=" + targetHeight.ToString("0.00") +
                " scale=" + scale.ToString("0.000"));
        }

        private void AttachKayKitSword()
        {
            if (_kayAnimator == null || !_kayAnimator.isHuman) return;

            Transform hand = _kayAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            GameObject swordPrefab = Resources.Load<GameObject>(KayKitSwordResource);
            if (hand == null || swordPrefab == null) return;

            GameObject sword = Instantiate(swordPrefab, hand);
            sword.name = "HIGHFLY_KAYKIT_SWORD_VISUAL";
            sword.transform.localPosition = Vector3.zero;
            sword.transform.localRotation = Quaternion.identity;
            sword.transform.localScale = Vector3.one;

            foreach (Collider c in sword.GetComponentsInChildren<Collider>(true))
                c.enabled = false;
        }

        private void BuildUi(Transform parent)
        {
            GameObject canvasGo = new GameObject(
                "CharacterCompareCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(parent, false);

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9940;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.35f;

            GameObject bar = new GameObject("CharacterABBar", typeof(RectTransform), typeof(Image), typeof(Outline));
            bar.transform.SetParent(canvasGo.transform, false);
            RectTransform br = bar.GetComponent<RectTransform>();
            br.anchorMin = br.anchorMax = new Vector2(0.57f, 1f);
            br.pivot = new Vector2(0.5f, 1f);
            br.anchoredPosition = new Vector2(0f, -20f);
            br.sizeDelta = new Vector2(520f, 154f);
            bar.GetComponent<Image>().color = new Color(0.010f, 0.016f, 0.028f, 0.94f);
            bar.GetComponent<Outline>().effectColor = new Color(0.18f, 0.72f, 1f, 0.92f);

            Button lucid = Button(bar.transform, "DEBUG • LUCID BODY");
            RectTransform lr = lucid.GetComponent<RectTransform>();
            lr.anchorMin = lr.anchorMax = new Vector2(0f, 1f);
            lr.pivot = new Vector2(0f, 1f);
            lr.anchoredPosition = new Vector2(14f, -18f);
            lr.sizeDelta = new Vector2(230f, 52f);
            lucid.onClick.AddListener(UseLucid);

            Button kay = Button(bar.transform, "KAYKIT HUNTER • FINAL");
            RectTransform kr = kay.GetComponent<RectTransform>();
            kr.anchorMin = kr.anchorMax = new Vector2(0f, 1f);
            kr.pivot = new Vector2(0f, 1f);
            kr.anchoredPosition = new Vector2(254f, -18f);
            kr.sizeDelta = new Vector2(230f, 52f);
            kay.onClick.AddListener(UseKayKit);

            _status = Label(bar.transform, "KAYKIT HUNTER • LUCID MOVEMENT/COMBAT CORE", 14, TextAnchor.UpperLeft);
            RectTransform sr = _status.rectTransform;
            sr.anchorMin = sr.anchorMax = new Vector2(0f, 1f);
            sr.pivot = new Vector2(0f, 1f);
            sr.anchoredPosition = new Vector2(14f, -78f);
            sr.sizeDelta = new Vector2(490f, 62f);
        }

        private void RefreshStatus(string label)
        {
            if (_status == null) return;

            Animator a = _usingKayKit ? _kayAnimator : _lucidAnimator;
            int renderers = 0;
            int bones = 0;
            int vertices = 0;

            if (_usingKayKit && _kayRoot != null)
            {
                Renderer[] rr = _kayRoot.GetComponentsInChildren<Renderer>(true);
                renderers = rr.Length;
                SkinnedMeshRenderer[] skinned = _kayRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                for (int i = 0; i < skinned.Length; i++)
                {
                    if (skinned[i] != null)
                    {
                        bones += skinned[i].bones != null ? skinned[i].bones.Length : 0;
                        if (skinned[i].sharedMesh != null) vertices += skinned[i].sharedMesh.vertexCount;
                    }
                }
            }
            else if (_lucidRenderers != null)
            {
                renderers = _lucidRenderers.Length;
                SkinnedMeshRenderer[] skinned = _player != null
                    ? _player.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    : new SkinnedMeshRenderer[0];
                for (int i = 0; i < skinned.Length; i++)
                {
                    if (skinned[i] != null)
                    {
                        bones += skinned[i].bones != null ? skinned[i].bones.Length : 0;
                        if (skinned[i].sharedMesh != null) vertices += skinned[i].sharedMesh.vertexCount;
                    }
                }
            }

            string avatar = a != null && a.avatar != null
                ? (a.avatar.isValid && a.avatar.isHuman ? "HUMANOID OK" : "AVATAR INVALID")
                : "NO AVATAR";

            _status.text =
                "PERSONAJE: " + label +
                " • SAME PlayerController / SAME AnimatorController\n" +
                avatar + " • Renderers " + renderers +
                " • Bones " + bones +
                " • Vertices " + vertices;
        }

        private static Button Button(Transform parent, string text)
        {
            GameObject go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.035f, 0.060f, 0.090f, 0.98f);
            Outline outline = go.GetComponent<Outline>();
            outline.effectColor = new Color(0.20f, 0.68f, 1f, 0.95f);
            outline.effectDistance = new Vector2(1f, -1f);

            Text t = Label(go.transform, text, 15, TextAnchor.MiddleCenter);
            RectTransform tr = t.rectTransform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(8f, 4f);
            tr.offsetMax = new Vector2(-8f, -4f);
            return go.GetComponent<Button>();
        }

        private static Text Label(Transform parent, string value, int size, TextAnchor anchor)
        {
            GameObject go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text t = go.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = value;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }
    }
}
