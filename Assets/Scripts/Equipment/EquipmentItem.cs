using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameSystems.Equipment
{
    /// <summary>
    /// Represents an equippable item with stats and bonuses
    /// </summary>
    [Serializable]
    public class EquipmentItem
    {
        [SerializeField] private string itemId;
        [SerializeField] private string itemName;
        [SerializeField] private string description;
        [SerializeField] private EquipmentSlot slot;
        [SerializeField] private EquipmentRarity rarity;
        [SerializeField] private int requiredLevel;
        
        // Stats
        [SerializeField] private int attackBonus;
        [SerializeField] private int defenseBonus;
        [SerializeField] private int healthBonus;
        [SerializeField] private int manaBonus;
        [SerializeField] private float critRateBonus;
        [SerializeField] private float critDamageBonus;
        [SerializeField] private int speedBonus;
        
        // Set info
        [SerializeField] private string setName;
        [SerializeField] private int setPieceNumber; // Which piece of the set (1-6)
        
        // Enhancement
        [SerializeField] private int enhancementLevel;
        [SerializeField] private int maxEnhancement;
        
        // Durability
        [SerializeField] private int currentDurability;
        [SerializeField] private int maxDurability;
        
        [SerializeField] private bool isEquipped;
        [SerializeField] private Sprite icon;

        // Properties
        public string ItemId => itemId;
        public string ItemName => itemName;
        public string Description => description;
        public EquipmentSlot Slot => slot;
        public EquipmentRarity Rarity => rarity;
        public int RequiredLevel => requiredLevel;
        public int AttackBonus => GetScaledStat(attackBonus);
        public int DefenseBonus => GetScaledStat(defenseBonus);
        public int HealthBonus => GetScaledStat(healthBonus);
        public int ManaBonus => GetScaledStat(manaBonus);
        public float CritRateBonus => critRateBonus;
        public float CritDamageBonus => critDamageBonus;
        public int SpeedBonus => GetScaledStat(speedBonus);
        public string SetName => setName;
        public int SetPieceNumber => setPieceNumber;
        public int EnhancementLevel => enhancementLevel;
        public int MaxEnhancement => maxEnhancement;
        public int CurrentDurability => currentDurability;
        public int MaxDurability => maxDurability;
        public bool IsEquipped => isEquipped;
        public Sprite Icon => icon;

        public EquipmentItem(string id, string name, string description, EquipmentSlot slot, 
            EquipmentRarity rarity, int requiredLevel)
        {
            this.itemId = id;
            this.itemName = name;
            this.description = description;
            this.slot = slot;
            this.rarity = rarity;
            this.requiredLevel = requiredLevel;
            
            this.enhancementLevel = 0;
            this.maxEnhancement = 15;
            this.currentDurability = 100;
            this.maxDurability = 100;
            this.isEquipped = false;
            this.setName = "";
            this.setPieceNumber = 0;
        }

        /// <summary>
        /// Sets base stats
        /// </summary>
        public void SetStats(int atk = 0, int def = 0, int hp = 0, int mp = 0, 
            float critRate = 0f, float critDmg = 0f, int spd = 0)
        {
            this.attackBonus = atk;
            this.defenseBonus = def;
            this.healthBonus = hp;
            this.manaBonus = mp;
            this.critRateBonus = critRate;
            this.critDamageBonus = critDmg;
            this.speedBonus = spd;
        }

        /// <summary>
        /// Sets equipment set info
        /// </summary>
        public void SetEquipmentSet(string setName, int pieceNumber)
        {
            this.setName = setName;
            this.setPieceNumber = pieceNumber;
        }

        /// <summary>
        /// Equips the item
        /// </summary>
        public void Equip()
        {
            isEquipped = true;
            Debug.Log($"<color=green>⚔️ Equipped:</color> {itemName}");
        }

        /// <summary>
        /// Unequips the item
        /// </summary>
        public void Unequip()
        {
            isEquipped = false;
            Debug.Log($"<color=yellow>Unequipped:</color> {itemName}");
        }

        /// <summary>
        /// Enhances the equipment
        /// </summary>
        public bool Enhance()
        {
            if (enhancementLevel >= maxEnhancement)
            {
                Debug.Log($"<color=orange>{itemName} is already max enhancement!</color>");
                return false;
            }

            enhancementLevel++;
            Debug.Log($"<color=cyan>⬆ Enhanced:</color> {itemName} +{enhancementLevel}");
            return true;
        }

        /// <summary>
        /// Repairs equipment durability
        /// </summary>
        public void Repair(int amount = -1)
        {
            if (amount < 0)
                amount = maxDurability;
            
            currentDurability = Mathf.Min(currentDurability + amount, maxDurability);
            Debug.Log($"<color=cyan>🔧 Repaired:</color> {itemName} ({currentDurability}/{maxDurability})");
        }

        /// <summary>
        /// Reduces durability
        /// </summary>
        public void ReduceDurability(int amount = 1)
        {
            currentDurability = Mathf.Max(currentDurability - amount, 0);
            
            if (currentDurability == 0)
            {
                Debug.Log($"<color=red>⚠️ {itemName} is broken!</color>");
            }
        }

        /// <summary>
        /// Gets stat scaled by enhancement level
        /// </summary>
        private int GetScaledStat(int baseStat)
        {
            if (baseStat == 0) return 0;
            
            // Each enhancement level adds 5% to stats
            float multiplier = 1f + (enhancementLevel * 0.05f);
            return Mathf.RoundToInt(baseStat * multiplier);
        }

        /// <summary>
        /// Gets total power score
        /// </summary>
        public int GetPowerScore()
        {
            return AttackBonus + DefenseBonus + (HealthBonus / 10) + (ManaBonus / 10) + SpeedBonus;
        }

        /// <summary>
        /// Checks if item belongs to a set
        /// </summary>
        public bool IsPartOfSet()
        {
            return !string.IsNullOrEmpty(setName);
        }

        /// <summary>
        /// Gets durability percentage (0-1)
        /// </summary>
        public float GetDurabilityPercentage()
        {
            if (maxDurability <= 0) return 1f;
            return (float)currentDurability / maxDurability;
        }

        public override string ToString()
        {
            string enhancement = enhancementLevel > 0 ? $" +{enhancementLevel}" : "";
            string equipped = isEquipped ? " [E]" : "";
            return $"{GetRarityIcon()} {itemName}{enhancement}{equipped}";
        }

        public string GetSlotIcon()
        {
            return slot switch
            {
                EquipmentSlot.Weapon => "⚔️",
                EquipmentSlot.Helmet => "🪖",
                EquipmentSlot.Armor => "🛡️",
                EquipmentSlot.Gloves => "🧤",
                EquipmentSlot.Boots => "👢",
                EquipmentSlot.Ring => "💍",
                EquipmentSlot.Necklace => "📿",
                EquipmentSlot.Accessory => "✨",
                _ => "•"
            };
        }

        public string GetRarityIcon()
        {
            return rarity switch
            {
                EquipmentRarity.Common => "⚪",
                EquipmentRarity.Uncommon => "🟢",
                EquipmentRarity.Rare => "🔵",
                EquipmentRarity.Epic => "🟣",
                EquipmentRarity.Legendary => "🟠",
                EquipmentRarity.Mythic => "🔴",
                _ => "⚪"
            };
        }

        public Color GetRarityColor()
        {
            return rarity switch
            {
                EquipmentRarity.Common => new Color(0.7f, 0.7f, 0.7f),
                EquipmentRarity.Uncommon => new Color(0.3f, 1f, 0.3f),
                EquipmentRarity.Rare => new Color(0.3f, 0.5f, 1f),
                EquipmentRarity.Epic => new Color(0.8f, 0.3f, 1f),
                EquipmentRarity.Legendary => new Color(1f, 0.6f, 0f),
                EquipmentRarity.Mythic => new Color(1f, 0.2f, 0.2f),
                _ => Color.white
            };
        }

        public Color GetSlotColor()
        {
            return slot switch
            {
                EquipmentSlot.Weapon => new Color(1f, 0.5f, 0.3f),
                EquipmentSlot.Helmet => new Color(0.7f, 0.7f, 0.7f),
                EquipmentSlot.Armor => new Color(0.5f, 0.7f, 1f),
                EquipmentSlot.Gloves => new Color(0.9f, 0.7f, 0.5f),
                EquipmentSlot.Boots => new Color(0.6f, 0.5f, 0.4f),
                EquipmentSlot.Ring => new Color(1f, 0.8f, 0.3f),
                EquipmentSlot.Necklace => new Color(0.8f, 0.5f, 1f),
                EquipmentSlot.Accessory => new Color(0.7f, 1f, 0.9f),
                _ => Color.white
            };
        }
    }

    public enum EquipmentSlot
    {
        Weapon,
        Helmet,
        Armor,
        Gloves,
        Boots,
        Ring,
        Necklace,
        Accessory
    }

    public enum EquipmentRarity
    {
        Common,      // ⚪
        Uncommon,    // 🟢
        Rare,        // 🔵
        Epic,        // 🟣
        Legendary,   // 🟠
        Mythic       // 🔴
    }
}
