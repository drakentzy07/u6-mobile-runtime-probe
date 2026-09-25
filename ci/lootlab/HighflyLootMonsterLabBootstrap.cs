using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Highfly.LootLab
{
    [DisallowMultipleComponent]
    public sealed class HighflyLootMonsterLabBootstrap : MonoBehaviour
    {
        private const string SaveKey = "HIGHFLY_LOOT_MONSTER_LAB_RUN0A_SAVE";
        private readonly HighflyLootLabInventory _inventory = new HighflyLootLabInventory();
        private readonly HighflyRewardLedger _ledger = new HighflyRewardLedger();
        private readonly EquipmentState _equipment = new EquipmentState();

        private GameObject _goblin;
        private GameObject _worldDrop;
        private GameObject _swordVisual;
        private GameObject _hunter;
        private RewardManifest _pendingManifest;
        private RewardContext _lastContext;
        private Text _statusText;
        private Text _eventText;
        private Font _font;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (FindFirstObjectByType<HighflyLootMonsterLabBootstrap>() != null) return;
            var root = new GameObject("HIGHFLY_LOOT_MONSTER_LAB_RUN0A");
            DontDestroyOnLoad(root);
            root.AddComponent<HighflyLootMonsterLabBootstrap>();
        }

        private IEnumerator Start()
        {
            yield return null;
            yield return null;
            SetupLab();
        }

        private void Update()
        {
            if (_worldDrop != null)
            {
                _worldDrop.transform.Rotate(0f, 70f * Time.unscaledDeltaTime, 0f, Space.World);
                var p = _worldDrop.transform.position;
                p.y = 0.65f + 0.15f * Mathf.Sin(Time.unscaledTime * 3f);
                _worldDrop.transform.position = p;
            }
            RefreshStatus();
        }

        private void SetupLab()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            foreach (Camera camera in FindObjectsByType<Camera>(FindObjectsSortMode.None)) camera.enabled = false;
            foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) canvas.enabled = false;

            CreateCamera();
            CreateLight();
            CreateArena();
            CreateHunter();
            CreateUi();
            EnsureEventSystem();
            SetEvent("Listo. Spawn Goblin para comenzar.");
        }

        private void CreateCamera()
        {
            var go = new GameObject("HF_LOOTLAB_CAMERA");
            DontDestroyOnLoad(go);
            var camera = go.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 9.5f, -14f);
            camera.transform.rotation = Quaternion.Euler(25f, 0f, 0f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.035f, 0.065f, 1f);
            camera.fieldOfView = 52f;
            go.tag = "MainCamera";
        }

        private void CreateLight()
        {
            var go = new GameObject("HF_LOOTLAB_LIGHT");
            DontDestroyOnLoad(go);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.35f;
            go.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
        }

        private void CreateArena()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "HF_LOOTLAB_FLOOR";
            floor.transform.localScale = new Vector3(2.4f, 1f, 1.65f);
            SetMaterialColor(floor, new Color(0.12f, 0.14f, 0.19f, 1f));

            var forge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            forge.name = "HF_LOOTLAB_FORGE";
            forge.transform.position = new Vector3(6.5f, 1f, -2.5f);
            forge.transform.localScale = new Vector3(2.4f, 2f, 2.2f);
            SetMaterialColor(forge, new Color(0.36f, 0.19f, 0.08f, 1f));

            CreateWorldLabel("FORJA", new Vector3(6.5f, 2.6f, -2.5f));
            CreateWorldLabel("ARENA GOBLIN", new Vector3(5.3f, 0.15f, 3.6f));
            CreateWorldLabel("LOOT DROP", new Vector3(0.2f, 0.15f, 2.8f));
        }

        private void CreateHunter()
        {
            _hunter = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _hunter.name = "HF_LOOTLAB_HUNTER";
            _hunter.transform.position = new Vector3(-5.5f, 1f, -1.5f);
            _hunter.transform.localScale = new Vector3(0.9f, 1.05f, 0.9f);
            SetMaterialColor(_hunter, new Color(0.1f, 0.8f, 0.95f, 1f));
            CreateWorldLabel("HUNTER", new Vector3(-5.5f, 2.65f, -1.5f));
        }

        private void CreateWorldLabel(string value, Vector3 position)
        {
            var root = new GameObject("Label_" + value);
            root.transform.position = position;
            var mesh = root.AddComponent<TextMesh>();
            mesh.text = value;
            mesh.fontSize = 42;
            mesh.characterSize = 0.08f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = Color.white;
            mesh.font = _font;
            root.transform.rotation = Quaternion.Euler(35f, 0f, 0f);
        }

        private void CreateUi()
        {
            var canvasGo = new GameObject("HF_LOOTLAB_CANVAS");
            DontDestroyOnLoad(canvasGo);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9000;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var header = CreatePanel(canvas.transform, "Header", new Vector2(0.015f, 0.74f), new Vector2(0.51f, 0.985f));
            _statusText = CreateText(header.transform, "Status", 27, TextAnchor.UpperLeft);
            Stretch(_statusText.rectTransform, 20f, 20f, 20f, 20f);

            var controls = CreatePanel(canvas.transform, "Controls", new Vector2(0.66f, 0.08f), new Vector2(0.985f, 0.96f));
            CreateControls(controls.transform);

            var eventPanel = CreatePanel(canvas.transform, "Event", new Vector2(0.015f, 0.025f), new Vector2(0.62f, 0.14f));
            _eventText = CreateText(eventPanel.transform, "EventText", 25, TextAnchor.MiddleLeft);
            Stretch(_eventText.rectTransform, 18f, 18f, 12f, 12f);

            var title = CreateText(canvas.transform, "BuildTitle", 23, TextAnchor.UpperCenter);
            title.text = "HIGHFLY LOOT / MONSTER LAB • RUN0A";
            var tr = title.rectTransform;
            tr.anchorMin = new Vector2(0.23f, 0.95f);
            tr.anchorMax = new Vector2(0.77f, 0.995f);
            tr.offsetMin = tr.offsetMax = Vector2.zero;
        }

        private void CreateControls(Transform parent)
        {
            string[] labels = { "SPAWN GOBLIN", "KILL GOBLIN", "PICKUP LOOT", "FORJAR ESPADA", "EQUIPAR ESPADA", "SAVE", "LOAD", "RESET", "AUTO RUN 0A" };
            Action[] actions = { SpawnGoblin, KillGoblin, PickupLoot, CraftSword, EquipSword, Save, Load, ResetLab, RunAutomatedHappyPath };
            float top = 0.93f;
            const float height = 0.085f;
            const float gap = 0.012f;
            for (int i = 0; i < labels.Length; i++)
            {
                float yMax = top - i * (height + gap);
                float yMin = yMax - height;
                CreateButton(parent, labels[i], new Vector2(0.07f, yMin), new Vector2(0.93f, yMax), actions[i]);
            }
        }

        private void SpawnGoblin()
        {
            if (_goblin != null) Destroy(_goblin);
            _goblin = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _goblin.name = "GOBLIN_SCAVENGER";
            _goblin.transform.position = new Vector3(5.2f, 1f, 2.2f);
            _goblin.transform.localScale = new Vector3(0.86f, 0.82f, 0.86f);
            SetMaterialColor(_goblin, new Color(0.28f, 0.72f, 0.25f, 1f));
            SetEvent("Goblin Scavenger spawneado.");
        }

        private void KillGoblin()
        {
            if (_goblin == null) { SetEvent("No hay Goblin vivo."); return; }
            Destroy(_goblin);
            _goblin = null;

            _lastContext = new RewardContext
            {
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
            if (_pendingManifest == null) { SetEvent("RewardContext duplicado: Loot bloqueado."); return; }
            SpawnWorldDrop();
            SetEvent("RewardContext v1.2 resuelto → WorldDrop creado.");
        }

        private void SpawnWorldDrop()
        {
            if (_worldDrop != null) Destroy(_worldDrop);
            _worldDrop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _worldDrop.name = "WORLD_DROP_GOBLIN_POUCH";
            _worldDrop.transform.position = new Vector3(0.2f, 0.65f, 2.3f);
            _worldDrop.transform.localScale = Vector3.one * 0.8f;
            SetMaterialColor(_worldDrop, new Color(0.75f, 0.45f, 0.08f, 1f));
        }

        private void PickupLoot()
        {
            if (_pendingManifest == null) { SetEvent("No hay WorldDrop pendiente."); return; }
            foreach (var reward in _pendingManifest.materials) _inventory.AddMaterial(reward.definitionId, reward.quantity);
            _pendingManifest = null;
            if (_worldDrop != null) Destroy(_worldDrop);
            _worldDrop = null;
            SetEvent("Pickup OK → +3 Iron / +1 Leather.");
        }

        private void CraftSword()
        {
            if (_inventory.GetMaterial(HighflyLootIds.ScrapIron) < 6 || _inventory.GetMaterial(HighflyLootIds.WarbandLeather) < 2)
            {
                SetEvent("FORJA: faltan 6 Iron + 2 Leather.");
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
            SetEvent("FORJA OK → Espada creada UID " + Short(item.instanceId));
        }

        private void EquipSword()
        {
            ItemInstance sword = _inventory.FindFirstDefinition(HighflyLootIds.ScavengerSword);
            if (sword == null) { SetEvent("No hay Espada del Saqueador."); return; }
            _equipment.mainHand = sword.instanceId;
            _equipment.offHand = null;
            SpawnSwordVisual();
            SetEvent("EQUIP OK → Sword1H.");
        }

        private void SpawnSwordVisual()
        {
            if (_swordVisual != null) Destroy(_swordVisual);
            _swordVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _swordVisual.name = "SCAVENGER_SWORD_VISUAL";
            _swordVisual.transform.SetParent(_hunter.transform, false);
            _swordVisual.transform.localPosition = new Vector3(0.72f, 0.25f, 0f);
            _swordVisual.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
            _swordVisual.transform.localScale = new Vector3(0.12f, 1.25f, 0.16f);
            SetMaterialColor(_swordVisual, new Color(0.72f, 0.76f, 0.82f, 1f));
        }

        private void Save()
        {
            var data = new LootCraftSaveData
            {
                materials = _inventory.ExportMaterials(),
                items = _inventory.ExportItems(),
                equipment = new EquipmentState { mainHand = _equipment.mainHand, offHand = _equipment.offHand },
                processedRewardContextIds = _ledger.Export()
            };
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
            SetEvent("SAVE OK.");
        }

        private void Load()
        {
            if (!PlayerPrefs.HasKey(SaveKey)) { SetEvent("No existe save."); return; }
            var data = JsonUtility.FromJson<LootCraftSaveData>(PlayerPrefs.GetString(SaveKey));
            if (data == null) { SetEvent("LOAD ERROR."); return; }
            _inventory.Import(data.materials, data.items);
            _ledger.Import(data.processedRewardContextIds);
            _equipment.mainHand = data.equipment != null ? data.equipment.mainHand : null;
            _equipment.offHand = data.equipment != null ? data.equipment.offHand : null;
            if (!string.IsNullOrWhiteSpace(_equipment.mainHand)) SpawnSwordVisual();
            SetEvent("LOAD OK → " + CurrentStyle());
        }

        private void ResetLab()
        {
            if (_goblin != null) Destroy(_goblin);
            if (_worldDrop != null) Destroy(_worldDrop);
            if (_swordVisual != null) Destroy(_swordVisual);
            _goblin = null; _worldDrop = null; _swordVisual = null; _pendingManifest = null; _lastContext = null;
            _inventory.Clear(); _ledger.Clear(); _equipment.mainHand = null; _equipment.offHand = null;
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
            SetEvent("LAB reseteado.");
        }

        private void RunAutomatedHappyPath()
        {
            ResetLab();
            SpawnGoblin(); KillGoblin(); PickupLoot();
            SpawnGoblin(); KillGoblin(); PickupLoot();
            CraftSword();
            ItemInstance before = _inventory.FindFirstDefinition(HighflyLootIds.ScavengerSword);
            if (before == null) { SetEvent("AUTO RUN FAIL: no sword."); return; }
            string uid = before.instanceId;
            EquipSword();
            if (CurrentStyle() != CombatStyle.SWORD_1H) { SetEvent("AUTO RUN FAIL: loadout."); return; }
            Save();
            _inventory.Clear(); _ledger.Clear(); _equipment.mainHand = null; _equipment.offHand = null;
            Load();
            ItemInstance after = _inventory.FindFirstDefinition(HighflyLootIds.ScavengerSword);
            bool green = after != null && after.instanceId == uid && CurrentStyle() == CombatStyle.SWORD_1H &&
                         _inventory.GetMaterial(HighflyLootIds.ScrapIron) == 0 &&
                         _inventory.GetMaterial(HighflyLootIds.WarbandLeather) == 0;
            SetEvent(green ? "AUTO RUN 0A GREEN ✓ UID + Sword1H + persistence OK" : "AUTO RUN 0A FAIL ✕");
        }

        private CombatStyle CurrentStyle() => HighflyLoadoutResolver.Resolve(_equipment, _inventory);

        private void RefreshStatus()
        {
            if (_statusText == null) return;
            var sword = _inventory.FindFirstDefinition(HighflyLootIds.ScavengerSword);
            _statusText.text =
                "<b>RUN 0A • LOOT / MONSTER LAB</b>\n" +
                "Goblin: " + (_goblin != null ? "ALIVE" : "—") + "\n" +
                "RewardContext v1.2: " + (_lastContext != null ? Short(_lastContext.rewardContextId) : "—") + "\n" +
                "WorldDrop: " + (_pendingManifest != null ? "PENDING" : "—") + "\n" +
                "Iron Goblin: " + _inventory.GetMaterial(HighflyLootIds.ScrapIron) + "\n" +
                "Leather Goblin: " + _inventory.GetMaterial(HighflyLootIds.WarbandLeather) + "\n" +
                "Sword UID: " + (sword != null ? Short(sword.instanceId) : "—") + "\n" +
                "MainHand: " + (string.IsNullOrWhiteSpace(_equipment.mainHand) ? "—" : Short(_equipment.mainHand)) + "\n" +
                "CombatStyle: <b>" + CurrentStyle() + "</b>\n" +
                "RewardLedger: " + _ledger.Count;
        }

        private void SetEvent(string message)
        {
            if (_eventText != null) _eventText.text = message;
            Debug.Log("[HF-LOOT-LAB] " + message);
        }

        private static string Short(string value) => string.IsNullOrWhiteSpace(value) ? "—" : (value.Length <= 8 ? value : value.Substring(0, 8));

        private static void SetMaterialColor(GameObject go, Color color)
        {
            var renderer = go != null ? go.GetComponent<Renderer>() : null;
            if (renderer == null) return;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) return;
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            renderer.material = mat;
        }

        private GameObject CreatePanel(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.74f);
            return go;
        }

        private Text CreateText(Transform parent, string name, int size, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = _font; text.fontSize = size; text.alignment = alignment; text.color = Color.white; text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private void CreateButton(Transform parent, string label, Vector2 min, Vector2 max, Action action)
        {
            var go = new GameObject("BTN_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0.08f, 0.16f, 0.28f, 0.96f);
            go.GetComponent<Button>().onClick.AddListener(() => action());
            var text = CreateText(go.transform, "Label", 25, TextAnchor.MiddleCenter);
            text.text = label;
            Stretch(text.rectTransform, 4f, 4f, 4f, 4f);
        }

        private static void Stretch(RectTransform rect, float left, float right, float bottom, float top)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom); rect.offsetMax = new Vector2(-right, -top);
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("HF_LOOTLAB_EVENT_SYSTEM");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
            DontDestroyOnLoad(go);
        }
    }
}
