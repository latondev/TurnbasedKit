using System;
using UnityEngine;

namespace GameSystems.Inventory
{
    /// <summary>
    /// Represents an item in the inventory
    /// </summary>
    [Serializable]
    public class Item
    {
        [SerializeField] private string itemId;
        [SerializeField] private string itemName;
        [SerializeField] private string description;
        [SerializeField] private ItemType itemType;
        [SerializeField] private ItemRarity rarity;
        [SerializeField] private int quantity;
        [SerializeField] private int maxStack;
        [SerializeField] private bool isEquipped;
        [SerializeField] private bool isConsumable;
        [SerializeField] private int value; // Gold value
        [SerializeField] private Sprite icon;

        public string ItemId => itemId;
        public string ItemName => itemName;
        public string Description => description;
        public ItemType ItemType => itemType;
        public ItemRarity Rarity => rarity;
        public int Quantity => quantity;
        public int MaxStack => maxStack;
        public bool IsEquipped => isEquipped;
        public bool IsConsumable => isConsumable;
        public int Value => value;
        public Sprite Icon => icon;

        public Item(string id, string name, string description, ItemType type, ItemRarity rarity, int maxStack = 1, int value = 0)
        {
            this.itemId = id;
            this.itemName = name;
            this.description = description;
            this.itemType = type;
            this.rarity = rarity;
            this.maxStack = maxStack;
            this.value = value;
            this.quantity = 1;
            this.isEquipped = false;
            this.isConsumable = type == ItemType.Consumable;
        }

        public bool CanStack()
        {
            return quantity < maxStack;
        }

        public void AddQuantity(int amount = 1)
        {
            quantity = Mathf.Min(quantity + amount, maxStack);
            Debug.Log($"<color=cyan>+{amount}</color> {itemName} (Total: {quantity})");
        }

        public bool RemoveQuantity(int amount = 1)
        {
            if (quantity >= amount)
            {
                quantity -= amount;
                Debug.Log($"<color=yellow>-{amount}</color> {itemName} (Remaining: {quantity})");
                return true;
            }
            return false;
        }

        public void Equip()
        {
            if (itemType == ItemType.Weapon || itemType == ItemType.Armor)
            {
                isEquipped = true;
                Debug.Log($"<color=green>⚔️ Equipped:</color> {itemName}");
            }
        }

        public void Unequip()
        {
            isEquipped = false;
            Debug.Log($"<color=yellow>Unequipped:</color> {itemName}");
        }

        public void Use()
        {
            if (isConsumable)
            {
                Debug.Log($"<color=cyan>✨ Used:</color> {itemName}");
                RemoveQuantity(1);
            }
            else
            {
                Debug.Log($"<color=orange>{itemName} is not consumable!</color>");
            }
        }

        public override string ToString()
        {
            string quantityText = maxStack > 1 ? $" x{quantity}" : "";
            string equippedText = isEquipped ? " [E]" : "";
            string rarityIcon = GetRarityIcon();
            
            return $"{rarityIcon} {itemName}{quantityText}{equippedText}";
        }

        public string GetTypeIcon()
        {
            return itemType switch
            {
                ItemType.Weapon => "⚔️",
                ItemType.Armor => "🛡️",
                ItemType.Consumable => "🧪",
                ItemType.Material => "📦",
                ItemType.Quest => "📜",
                ItemType.Misc => "💎",
                _ => "•"
            };
        }

        public string GetRarityIcon()
        {
            return rarity switch
            {
                ItemRarity.Common => "⚪",
                ItemRarity.Uncommon => "🟢",
                ItemRarity.Rare => "🔵",
                ItemRarity.Epic => "🟣",
                ItemRarity.Legendary => "🟠",
                _ => "⚪"
            };
        }

        public Color GetRarityColor()
        {
            return rarity switch
            {
                ItemRarity.Common => new Color(0.8f, 0.8f, 0.8f),
                ItemRarity.Uncommon => new Color(0.3f, 1f, 0.3f),
                ItemRarity.Rare => new Color(0.3f, 0.5f, 1f),
                ItemRarity.Epic => new Color(0.8f, 0.3f, 1f),
                ItemRarity.Legendary => new Color(1f, 0.6f, 0f),
                _ => Color.white
            };
        }
    }

    public enum ItemType
    {
        Weapon,
        Armor,
        Consumable,
        Material,
        Quest,
        Misc
    }

    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
}
