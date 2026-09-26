using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Highfly.Mobile;

namespace Highfly.LootLab
{
    [DisallowMultipleComponent]
    public sealed class HighflyLootMonsterLabBootstrap : MonoBehaviour
    {
        private const string SaveKey = "HIGHFLY_LOOT_MONSTER_LAB_RUN0B_SAVE";
        private const float LabY = 120f;
        private static readonly Vector3 LabSpawn = new Vector3(0f, LabY + 0.9f, -7.2f);

        private readonly HighflyLootLabInventory _inventory = new HighflyLootLabInventory();
        private readonly HighflyRewardLedger _ledger = new HighflyRewardLedger();
        private readonly EquipmentState _equipment = new EquipmentState();

        private PlayerController _player;
        private GameObject _goblin;
        private GameObject _worldDrop;
        private GameObject _swordVisual;
        private RewardManifest _pendingManifest;
        private RewardContext _lastContext;
        private Text _statusText;
        private Text _eventText;
        private Font _font;
        private bool _setup;
        private bool _proximityPickupArmed;
        private float _nextBoundsCheck;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (FindFirstObjectByType<HighflyLootMonsterLabBootstrap>() != null) return;
            var root = new GameObject("HIGHFLY_LOOT_MONSTER_LAB_RUN0B");
            DontDestroyOnLoad(root);
            root.AddComponent<HighflyLootMonsterLabBootstrap>();
        }

        private IEnumerator Start()
        {
            while (_player == null)
            {
                _player = FindFirstObjectByType<PlayerController>();
                if (_player == null)
                    yield return null;
            }

            SetupLab(_player);
            yield return null;
            yield return null;
            HighflyThirdPersonMobileCamera.Instance?.SnapBehindPlayer(13f);
        }

        private void Update()
        {
            if (!_setup || _player == null) return;

            if (_worldDrop != null)
            {
                _worldDrop.transform.Rotate(0f, 75f * Time.unscaledDeltaTime, 0f, Space.World);
                Vector3 p = _worldDrop.transform.position;
                p.y = LabY + 0.70f + 0.16f * Mathf.Sin(Time.unscaledTime * 3.1f);
                _worldDrop.transform.position = p;

                if (_pendingManifest != null && _proximityPickupArmed)
                {
                    float distance = Vector3.Distance(_player.transform.position, _worldDrop.transform.position);
                    if (distance <= 1.45f)
                    {
                        _proximityPickupArmed = false;
                        PickupLoot();
                        SetEvent("PICKUP POR PROXIMIDAD ✓");
                    }
                }
            }

            if (Time.unscaledTime >= _nextBoundsCheck)
            {
                _nextBoundsCheck = Time.unscaledTime + 0.12f;
                CheckBounds();
            }

            RefreshStatus();
        }

        private void SetupLab(PlayerController player)
        {
            _setup = true;
            _player = player;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            BuildArena();
            TeleportPlayer(LabSpawn);

            if (_player.GetComponent<HighflyLootLabDesktopControls>() == null)
                _player.gameObject.AddComponent<HighflyLootLabDesktopControls>();

            CreateUi();

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.025f, 0.035f, 0.065f, 1f);
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = !Application.isMobilePlatform;

            SetEvent("GOLDEN MOVEMENT/CAMERA cargado. Mové al Hunter y probá el pickup.");
        }

        private static void BuildArena()
        {
            CreateBlock("HF_LOOT_FLOOR",
                new Vector3(0f, LabY - 0.35f, 2.5f),
                new Vector3(26f, 0.7f, 25f),
                new Color(0.22f, 0.25f, 0.31f, 1f));

            CreateBlock("HF_LOOT_BACK",
                new Vector3(0f, LabY + 3.5f, 14.6f),
                new Vector3(26f, 7f, 0.5f),
                new Color(0.12f, 0.14f, 0.18f, 1f));

            CreateBlock("HF_LOOT_LEFT",
                new Vector3(-12.7f, LabY + 3.5f, 2.5f),
                new Vector3(0.5f, 7f, 25f),
                new Color(0.12f, 0.14f, 0.18f, 1f));

            CreateBlock("HF_LOOT_RIGHT",
                new Vector3(12.7f, LabY + 3.5f, 2.5f),
                new Vector3(0.5f, 7f, 25f),
                new Color(0.12f, 0.14f, 0.18f, 1f));

            var forge = CreateBlock("HF_LOOT_FORGE",
                new Vector3(8.4f, LabY + 1.25f, -5.1f),
                new Vector3(3.2f, 2.5f, 2.6f),
                new Color(0.38f, 0.19f, 0.07f, 1f));

            CreateWorldLabel("FORJA", forge.transform.position + Vector3.up * 2f, 0.10f);
            CreateWorldLabel("ARENA GOBLIN", new Vector3(0f, LabY + 0.08f, 5.8f), 0.085f);
            CreateWorldLabel("DROP / PICKUP", new Vector3(0f, LabY + 0.08f, 0.5f), 0.08f);
        }

        private static GameObject CreateBlock(string name, Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;
            SetMaterialColor(go, color);
            return go;
        }

        private static void CreateWorldLabel(string value, Vector3 position, float size)
        {
            var root = new GameObject("Label_" + value);
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(78f, 0f, 0f);

            var text = root.AddComponent<TextMesh>();
            text.text = value;
            text.fontSize = 42;
            text.characterSize = size;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private void CreateUi()
        {
            var canvasGo = new GameObject("HF_LOOTLAB_OVERLAY");
            DontDestroyOnLoad(canvasGo);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 12000;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            var header = CreatePanel(canvas.transform, "StatusPanel",
                new Vector2(0.012f, 0.76f),
                new Vector2(0.36f, 0.985f),
                new Color(0f, 0f, 0f, 0.70f));

            _statusText = CreateText(header.transform, "Status", 22, TextAnchor.UpperLeft);
            Stretch(_statusText.rectTransform, 14f, 14f, 12f, 12f);

            var controls = CreatePanel(canvas.transform, "DebugControls",
                new Vector2(0.37f, 0.895f),
                new Vector2(0.985f, 0.985f),
                new Color(0f, 0f, 0f, 0.55f));

            string[] labels = { "SPAWN", "KILL", "PICKUP", "FORJAR", "EQUIPAR", "AUTO 0A" };
            Action[] actions = { SpawnGoblin, KillGoblin, PickupLoot, CraftSword, EquipSword, RunAutomatedHappyPath };
            CreateHorizontalButtons(controls.transform, labels, actions);

            var eventPanel = CreatePanel(canvas.transform, "EventPanel",
                new Vector2(0.30f, 0.80f),
                new Vector2(0.78f, 0.88f),
                new Color(0f, 0f, 0f, 0.64f));

            _eventText = CreateText(eventPanel.transform, "EventText", 20, TextAnchor.MiddleCenter);
            Stretch(_eventText.rectTransform, 10f, 10f, 8f, 8f);

            var title = CreateText(canvas.transform, "BuildTitle", 18, TextAnchor.UpperCenter);
            title.text = "HIGHFLY LOOT / MONSTER LAB • RUN0B PREP • GOLDEN MOVE + GOLDEN CAMERA";
            RectTransform tr = title.rectTransform;
            tr.anchorMin = new Vector2(0.27f, 0.985f);
            tr.anchorMax = new Vector2(0.83f, 1f);
            tr.offsetMin = tr.offsetMax = Vector2.zero;
        }

        private void CreateHorizontalButtons(Transform parent, string[] labels, Action[] actions)
        {
            float gap = 0.008f;
            float width = (1f - gap * (labels.Length + 1)) / labels.Length;

            for (int i = 0; i < labels.Length; i++)
            {
                float xMin = gap + i * (width + gap);
                float xMax = xMin + width;

                var go = new GameObject("BTN_" + labels[i], typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(parent, false);

                RectTransform rect = (RectTransform)go.transform;
                rect.anchorMin = new Vector2(xMin, 0.12f);
                rect.anchorMax = new Vector2(xMax, 0.88f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;

                go.GetComponent<Image>().color = new Color(0.045f, 0.13f, 0.25f, 0.96f);
                int capture = i;
                go.GetComponent<Button>().onClick.AddListener(() => actions[capture]());

                var label = CreateText(go.transform, "Text", 17, TextAnchor.MiddleCenter);
                label.text = labels[i];
                Stretch(label.rectTransform, 2f, 2f, 2f, 2f);
            }
        }

        private void SpawnGoblin()
        {
            if (_goblin != null) Destroy(_goblin);

            _goblin = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _goblin.name = "GOBLIN_SCAVENGER_PLACEHOLDER";
            _goblin.transform.position = new Vector3(0f, LabY + 1f, 5.5f);
            _goblin.transform.localScale = new Vector3(0.86f, 0.82f, 0.86f);
            SetMaterialColor(_goblin, new Color(0.28f, 0.72f, 0.25f, 1f));

            SetEvent("Goblin placeholder spawneado. Próximo paso: Monster Core real.");
        }

        private void KillGoblin()
        {
            if (_goblin == null)
            {
                SetEvent("No hay Goblin vivo.");
                return;
            }

            Vector3 deathPoint = _goblin.transform.position;
            Destroy(_goblin);
            _goblin = null;

            _lastContext = new RewardContext
            {
                schemaVersion = "1.2",
                rewardContextId = Guid.NewGuid().ToString("N"),
                encounterInstanceId = Guid.NewGuid().ToString("N"),
                sourceRuntimeId = "LAB_GOBLIN_001",
                rewardScope = RewardScope.MONSTER,
                monsterFamilyId = HighflyLootIds.GoblinFamily,
                monsterDefinitionId = HighflyLootIds.GoblinScavenger,
                powerClass = "NORMAL",
                identityClass = "STANDARD",
                evolutionState = "BASE",
                rank = "F",
                zoneId = "LOOT_MONSTER_LAB",
                zoneTier = 1,
                clearMethod = "defeated",
                rewardSeed = UnityEngine.Random.Range(1, int.MaxValue)
            };

            _pendingManifest = HighflyLootLabResolver.Resolve(_lastContext, _ledger);
            if (_pendingManifest == null)
            {
                SetEvent("RewardContext duplicado bloqueado por Ledger.");
                return;
            }

            SpawnWorldDrop(new Vector3(deathPoint.x, LabY + 0.70f, deathPoint.z));
            SetEvent("RewardContext v1.2 ✓  Caminá hasta el drop para recogerlo.");
        }

        private void SpawnWorldDrop(Vector3 position)
        {
            if (_worldDrop != null) Destroy(_worldDrop);

            _worldDrop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _worldDrop.name = "WORLD_DROP_GOBLIN_POUCH";
            _worldDrop.transform.position = position;
            _worldDrop.transform.localScale = Vector3.one * 0.75f;
            SetMaterialColor(_worldDrop, new Color(1f, 0.52f, 0.05f, 1f));
            _proximityPickupArmed = true;
        }

        private void PickupLoot()
        {
            if (_pendingManifest == null)
            {
                SetEvent("No hay loot pendiente.");
                return;
            }

            foreach (MaterialReward reward in _pendingManifest.materials)
                _inventory.AddMaterial(reward.definitionId, reward.quantity);

            _pendingManifest = null;
            _proximityPickupArmed = false;

            if (_worldDrop != null) Destroy(_worldDrop);
            _worldDrop = null;

            SetEvent("LOOT ✓ +3 Chatarra / +1 Cuero");
        }

        private void CraftSword()
        {
            if (_inventory.GetMaterial(HighflyLootIds.ScrapIron) < 6 ||
                _inventory.GetMaterial(HighflyLootIds.WarbandLeather) < 2)
            {
                SetEvent("FORJA: necesitás 6 Chatarra + 2 Cuero.");
                return;
            }

            _inventory.TryConsumeMaterial(HighflyLootIds.ScrapIron, 6);
            _inventory.TryConsumeMaterial(HighflyLootIds.WarbandLeather, 2);

            var item = new ItemInstance
            {
                instanceId = Guid.NewGuid().ToString("N"),
                definitionId = HighflyLootIds.ScavengerSword,
                rarity = "COMMON",
                itemLevel = 1,
                rolledPower = 10f,
                seed = UnityEngine.Random.Range(1, int.MaxValue)
            };

            _inventory.AddItem(item);
            SetEvent("FORJA ✓ Espada del Saqueador UID " + Short(item.instanceId));
        }

        private void EquipSword()
        {
            ItemInstance sword = _inventory.FindFirstDefinition(HighflyLootIds.ScavengerSword);
            if (sword == null)
            {
                SetEvent("No hay Espada del Saqueador.");
                return;
            }

            _equipment.mainHand = sword.instanceId;
            _equipment.offHand = null;
            SpawnSwordVisual();
            SetEvent("EQUIP ✓ MainHand → Sword1H");
        }

        private void SpawnSwordVisual()
        {
            if (_swordVisual != null) Destroy(_swordVisual);
            if (_player == null) return;

            Transform socket = _player.transform;
            Animator animator = _player.animator != null ? _player.animator : _player.GetComponentInChildren<Animator>();
            if (animator != null && animator.isHuman)
            {
                Transform hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                if (hand != null) socket = hand;
            }

            _swordVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _swordVisual.name = "SCAVENGER_SWORD_VISUAL";
            Collider col = _swordVisual.GetComponent<Collider>();
            if (col != null) Destroy(col);

            _swordVisual.transform.SetParent(socket, false);
            _swordVisual.transform.localPosition = socket == _player.transform
                ? new Vector3(0.48f, 0.45f, 0.1f)
                : new Vector3(0.02f, 0.18f, 0.03f);
            _swordVisual.transform.localRotation = Quaternion.Euler(8f, 2f, 4f);
            _swordVisual.transform.localScale = new Vector3(0.08f, 0.08f, 0.95f);
            SetMaterialColor(_swordVisual, new Color(0.76f, 0.82f, 0.90f, 1f));
        }

        private void Save()
        {
            var data = new LootCraftSaveData
            {
                materials = _inventory.ExportMaterials(),
                items = _inventory.ExportItems(),
                equipment = new EquipmentState
                {
                    mainHand = _equipment.mainHand,
                    offHand = _equipment.offHand
                },
                processedRewardContextIds = _ledger.Export()
            };

            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
            SetEvent("SAVE ✓");
        }

        private void Load()
        {
            if (!PlayerPrefs.HasKey(SaveKey))
            {
                SetEvent("No existe save del Loot Lab.");
                return;
            }

            LootCraftSaveData data = JsonUtility.FromJson<LootCraftSaveData>(PlayerPrefs.GetString(SaveKey));
            if (data == null)
            {
                SetEvent("LOAD ERROR");
                return;
            }

            _inventory.Import(data.materials, data.items);
            _ledger.Import(data.processedRewardContextIds);
            _equipment.mainHand = data.equipment != null ? data.equipment.mainHand : null;
            _equipment.offHand = data.equipment != null ? data.equipment.offHand : null;

            if (!string.IsNullOrWhiteSpace(_equipment.mainHand))
                SpawnSwordVisual();

            SetEvent("LOAD ✓ " + CurrentStyle());
        }

        private void RunAutomatedHappyPath()
        {
            ResetRuntimeState();

            for (int i = 0; i < 2; i++)
            {
                SpawnGoblin();
                KillGoblin();
                PickupLoot();
            }

            CraftSword();
            ItemInstance before = _inventory.FindFirstDefinition(HighflyLootIds.ScavengerSword);
            if (before == null)
            {
                SetEvent("AUTO 0A FAIL: craft");
                return;
            }

            string uid = before.instanceId;
            EquipSword();

            if (CurrentStyle() != CombatStyle.SWORD_1H)
            {
                SetEvent("AUTO 0A FAIL: loadout");
                return;
            }

            Save();

            _inventory.Clear();
            _ledger.Clear();
            _equipment.mainHand = null;
            _equipment.offHand = null;

            Load();

            ItemInstance after = _inventory.FindFirstDefinition(HighflyLootIds.ScavengerSword);
            bool green = after != null &&
                         after.instanceId == uid &&
                         CurrentStyle() == CombatStyle.SWORD_1H;

            SetEvent(green
                ? "AUTO RUN 0A GREEN ✓ UID + equip + persistence"
                : "AUTO RUN 0A FAIL ✕");
        }

        private void ResetRuntimeState()
        {
            if (_goblin != null) Destroy(_goblin);
            if (_worldDrop != null) Destroy(_worldDrop);
            if (_swordVisual != null) Destroy(_swordVisual);

            _goblin = null;
            _worldDrop = null;
            _swordVisual = null;
            _pendingManifest = null;
            _lastContext = null;
            _proximityPickupArmed = false;

            _inventory.Clear();
            _ledger.Clear();
            _equipment.mainHand = null;
            _equipment.offHand = null;

            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();

            TeleportPlayer(LabSpawn);
        }

        private void CheckBounds()
        {
            Vector3 p = _player.transform.position;
            if (p.y < LabY - 4f ||
                Mathf.Abs(p.x) > 15f ||
                p.z < -12f ||
                p.z > 17f)
            {
                TeleportPlayer(LabSpawn);
                HighflyThirdPersonMobileCamera.Instance?.SnapBehindPlayer(13f);
                SetEvent("Safety reset del Hunter.");
            }
        }

        private void TeleportPlayer(Vector3 position)
        {
            if (_player == null) return;

            CharacterController cc = _player.GetComponent<CharacterController>();
            bool wasEnabled = cc != null && cc.enabled;

            if (cc != null) cc.enabled = false;
            _player.transform.position = position;
            _player.transform.rotation = Quaternion.identity;
            if (cc != null) cc.enabled = wasEnabled;

            _player.currentState = PlayerState.Locomotion;
            _player.ReleaseHighflyMobileInput();
        }

        private CombatStyle CurrentStyle()
        {
            return HighflyLoadoutResolver.Resolve(_equipment, _inventory);
        }

        private void RefreshStatus()
        {
            if (_statusText == null) return;

            ItemInstance sword = _inventory.FindFirstDefinition(HighflyLootIds.ScavengerSword);

            _statusText.text =
                "<b>LOOT LAB • GOLDEN CORE</b>\n" +
                "PlayerController: REAL ✓\n" +
                "Camera: GOLDEN ✓\n" +
                "Goblin: " + (_goblin != null ? "ALIVE" : "—") + "\n" +
                "Reward v1.2: " + (_lastContext != null ? Short(_lastContext.rewardContextId) : "—") + "\n" +
                "Iron: " + _inventory.GetMaterial(HighflyLootIds.ScrapIron) + "\n" +
                "Leather: " + _inventory.GetMaterial(HighflyLootIds.WarbandLeather) + "\n" +
                "Sword UID: " + (sword != null ? Short(sword.instanceId) : "—") + "\n" +
                "CombatStyle: <b>" + CurrentStyle() + "</b>";
        }

        private void SetEvent(string message)
        {
            if (_eventText != null) _eventText.text = message;
            Debug.Log("[HF-LOOT-LAB] " + message);
        }

        private static string Short(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "—";
            return value.Length <= 8 ? value : value.Substring(0, 8);
        }

        private static void SetMaterialColor(GameObject go, Color color)
        {
            Renderer renderer = go != null ? go.GetComponent<Renderer>() : null;
            if (renderer == null) return;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) return;

            var material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            renderer.material = material;
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            go.GetComponent<Image>().color = color;
            return go;
        }

        private Text CreateText(Transform parent, string name, int size, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            Text text = go.GetComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void Stretch(RectTransform rect, float left, float right, float bottom, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }
    }

    /// <summary>
    /// Desktop bridge for Loot Lab: same PlayerController movement and same Golden camera.
    /// No second controller/camera is created.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HighflyLootLabDesktopControls : MonoBehaviour
    {
        private PlayerController _player;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
        }

        private void Update()
        {
            if (_player == null || Application.isMobilePlatform) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                Vector2 move = Vector2.zero;
                if (keyboard.aKey.isPressed) move.x -= 1f;
                if (keyboard.dKey.isPressed) move.x += 1f;
                if (keyboard.sKey.isPressed) move.y -= 1f;
                if (keyboard.wKey.isPressed) move.y += 1f;
                _player.SetHighflyMobileMove(Vector2.ClampMagnitude(move, 1f));

                if (keyboard.rKey.wasPressedThisFrame)
                    HighflyThirdPersonMobileCamera.Instance?.SnapBehindPlayer(13f);
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
                HighflyThirdPersonMobileCamera.Instance?.AddLookDelta(mouse.delta.ReadValue());
        }

        private void OnDisable()
        {
            if (_player != null)
                _player.SetHighflyMobileMove(Vector2.zero);
        }
    }
}
