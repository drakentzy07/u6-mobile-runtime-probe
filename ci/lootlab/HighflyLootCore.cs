using System;
using System.Collections.Generic;

namespace Highfly.LootLab
{
    public static class HighflyLootIds
    {
        public const string GoblinFamily = "GOBLIN";
        public const string GoblinScavenger = "GOBLIN_SCAVENGER";
        public const string ScrapIron = "MAT_GOBLIN_SCRAP_IRON";
        public const string WarbandLeather = "MAT_GOBLIN_WARBAND_LEATHER";
        public const string ScavengerSword = "ITEM_GOBLIN_SCAVENGER_SWORD";
        public const string ScavengerSwordRecipe = "RCP_GOBLIN_SCAVENGER_SWORD";
    }

    public enum RewardScope { MONSTER, ENCOUNTER, SCENARIO }
    public enum PartOutcomeState { INTACT, DAMAGED, BROKEN, SEVERED, DESTROYED, HIDDEN, EXPOSED, DISRUPTED }
    public enum CombatStyle { UNARMED, SWORD_1H, DUAL_SWORD, SWORD_SHIELD, AXE_1H, DUAL_AXE, AXE_SHIELD, DUAL_DAGGERS, SPEAR_2H, UNSUPPORTED }

    [Serializable]
    public sealed class PartOutcome
    {
        public string partId;
        public PartOutcomeState state;
    }

    [Serializable]
    public sealed class RewardContext
    {
        public string schemaVersion = "1.2";
        public string rewardContextId;
        public RewardScope rewardScope = RewardScope.MONSTER;
        public string encounterInstanceId;
        public string sourceRuntimeId;
        public string monsterFamilyId;
        public string monsterDefinitionId;
        public string monsterVariantId;
        public string powerClass;
        public string identityClass;
        public string evolutionState;
        public string rank;
        public string bossDefinitionId;
        public string scenarioId;
        public string zoneId;
        public int zoneTier;
        public List<string> worldTags = new List<string>();
        public List<string> worldStateFlags = new List<string>();
        public List<string> encounterTags = new List<string>();
        public List<PartOutcome> partOutcomes = new List<PartOutcome>();
        public List<string> performanceFlags = new List<string>();
        public bool firstKill;
        public bool firstVariantKill;
        public bool firstUniqueKill;
        public bool firstScenarioClear;
        public string clearMethod;
        public int rewardSeed;
    }

    [Serializable]
    public sealed class MaterialReward
    {
        public string definitionId;
        public int quantity;
        public MaterialReward() { }
        public MaterialReward(string definitionId, int quantity)
        {
            this.definitionId = definitionId;
            this.quantity = quantity;
        }
    }

    [Serializable]
    public sealed class RewardManifest
    {
        public string manifestId;
        public string rewardContextId;
        public List<MaterialReward> materials = new List<MaterialReward>();
    }

    [Serializable]
    public sealed class MaterialStack
    {
        public string definitionId;
        public int quantity;
        public string quality = "NORMAL";
    }

    [Serializable]
    public sealed class ItemInstance
    {
        public string instanceId;
        public string definitionId;
        public string rarity = "COMMON";
        public int itemLevel = 1;
        public float rolledPower;
        public float rolledArmor;
        public int upgradeLevel;
        public int seed;
        public bool locked;
    }

    [Serializable]
    public sealed class EquipmentState
    {
        public string mainHand;
        public string offHand;
    }

    [Serializable]
    public sealed class LootCraftSaveData
    {
        public int schemaVersion = 1;
        public List<MaterialStack> materials = new List<MaterialStack>();
        public List<ItemInstance> items = new List<ItemInstance>();
        public EquipmentState equipment = new EquipmentState();
        public List<string> processedRewardContextIds = new List<string>();
    }

    public sealed class HighflyLootLabInventory
    {
        private readonly Dictionary<string, int> _materials = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly List<ItemInstance> _items = new List<ItemInstance>();
        public IReadOnlyList<ItemInstance> Items => _items;

        public int GetMaterial(string id) => _materials.TryGetValue(id, out int amount) ? amount : 0;

        public void AddMaterial(string id, int quantity)
        {
            if (string.IsNullOrWhiteSpace(id) || quantity <= 0) return;
            _materials[id] = GetMaterial(id) + quantity;
        }

        public bool TryConsumeMaterial(string id, int quantity)
        {
            if (quantity <= 0) return true;
            int current = GetMaterial(id);
            if (current < quantity) return false;
            int remaining = current - quantity;
            if (remaining <= 0) _materials.Remove(id); else _materials[id] = remaining;
            return true;
        }

        public void AddItem(ItemInstance item)
        {
            if (item != null && !string.IsNullOrWhiteSpace(item.instanceId)) _items.Add(item);
        }

        public ItemInstance FindItem(string instanceId) => _items.Find(x => x != null && x.instanceId == instanceId);
        public ItemInstance FindFirstDefinition(string definitionId) => _items.Find(x => x != null && x.definitionId == definitionId);

        public List<MaterialStack> ExportMaterials()
        {
            var result = new List<MaterialStack>();
            foreach (var kv in _materials)
                result.Add(new MaterialStack { definitionId = kv.Key, quantity = kv.Value, quality = "NORMAL" });
            return result;
        }

        public List<ItemInstance> ExportItems() => new List<ItemInstance>(_items);

        public void Import(List<MaterialStack> materials, List<ItemInstance> items)
        {
            _materials.Clear();
            _items.Clear();
            if (materials != null)
                foreach (var stack in materials)
                    if (stack != null && stack.quantity > 0 && !string.IsNullOrWhiteSpace(stack.definitionId))
                        AddMaterial(stack.definitionId, stack.quantity);
            if (items != null)
                foreach (var item in items)
                    if (item != null && !string.IsNullOrWhiteSpace(item.instanceId))
                        _items.Add(item);
        }

        public void Clear()
        {
            _materials.Clear();
            _items.Clear();
        }
    }

    public sealed class HighflyRewardLedger
    {
        private readonly HashSet<string> _processed = new HashSet<string>(StringComparer.Ordinal);
        public int Count => _processed.Count;
        public bool HasProcessed(string id) => !string.IsNullOrWhiteSpace(id) && _processed.Contains(id);
        public bool TryMarkProcessed(string id) => !string.IsNullOrWhiteSpace(id) && _processed.Add(id);
        public List<string> Export() => new List<string>(_processed);
        public void Import(List<string> values)
        {
            _processed.Clear();
            if (values == null) return;
            foreach (string value in values)
                if (!string.IsNullOrWhiteSpace(value)) _processed.Add(value);
        }
        public void Clear() => _processed.Clear();
    }

    public static class HighflyLootLabResolver
    {
        public static RewardManifest Resolve(RewardContext context, HighflyRewardLedger ledger)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (ledger == null) throw new ArgumentNullException(nameof(ledger));
            if (context.schemaVersion != "1.2") throw new InvalidOperationException("RUN0A requires RewardContext v1.2.");
            if (context.rewardScope != RewardScope.MONSTER) throw new InvalidOperationException("RUN0A only resolves MONSTER scope.");
            if (context.monsterFamilyId != HighflyLootIds.GoblinFamily) throw new InvalidOperationException("RUN0A only resolves GOBLIN family.");
            if (ledger.HasProcessed(context.rewardContextId)) return null;

            var manifest = new RewardManifest
            {
                manifestId = "MANIFEST_" + context.rewardContextId,
                rewardContextId = context.rewardContextId
            };
            manifest.materials.Add(new MaterialReward(HighflyLootIds.ScrapIron, 3));
            manifest.materials.Add(new MaterialReward(HighflyLootIds.WarbandLeather, 1));
            ledger.TryMarkProcessed(context.rewardContextId);
            return manifest;
        }
    }

    public static class HighflyLoadoutResolver
    {
        public static CombatStyle Resolve(EquipmentState equipment, HighflyLootLabInventory inventory)
        {
            if (equipment == null || inventory == null) return CombatStyle.UNARMED;
            ItemInstance main = inventory.FindItem(equipment.mainHand);
            ItemInstance off = inventory.FindItem(equipment.offHand);
            bool mainSword = main != null && main.definitionId == HighflyLootIds.ScavengerSword;
            bool offSword = off != null && off.definitionId == HighflyLootIds.ScavengerSword;
            if (main == null && off == null) return CombatStyle.UNARMED;
            if (mainSword && off == null) return CombatStyle.SWORD_1H;
            if (mainSword && offSword) return CombatStyle.DUAL_SWORD;
            return CombatStyle.UNSUPPORTED;
        }
    }
}
