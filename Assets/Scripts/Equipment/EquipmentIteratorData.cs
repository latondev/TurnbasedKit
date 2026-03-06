using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameSystems.Common;

namespace GameSystems.Equipment
{
    /// <summary>
    /// Base iterator data for Equipment - extends GenericIteratorData
    /// </summary>
    [Serializable]
    public class EquipmentIteratorData : GenericIteratorData<EquipmentItem>
    {
        /// <summary>
        /// Gets next equipped item
        /// </summary>
        public EquipmentItem NextEquipped()
        {
            if (CurrentIterator == null) return default;

            int startIndex = CurrentIterator.CurrentIndex;

            while (HasNext())
            {
                EquipmentItem item = Next();
                if (item != null && item.IsEquipped)
                    return item;

                if (CurrentIterator.CurrentIndex == startIndex)
                    break;
            }

            return default;
        }

        /// <summary>
        /// Gets next item by slot
        /// </summary>
        public EquipmentItem NextOfSlot(EquipmentSlot slot)
        {
            if (CurrentIterator == null) return default;

            int startIndex = CurrentIterator.CurrentIndex;

            while (HasNext())
            {
                EquipmentItem item = Next();
                if (item != null && item.Slot == slot)
                    return item;

                if (CurrentIterator.CurrentIndex == startIndex)
                    break;
            }

            return default;
        }

        /// <summary>
        /// Gets next item by rarity
        /// </summary>
        public EquipmentItem NextOfRarity(EquipmentRarity rarity)
        {
            if (CurrentIterator == null) return default;

            int startIndex = CurrentIterator.CurrentIndex;

            while (HasNext())
            {
                EquipmentItem item = Next();
                if (item != null && item.Rarity == rarity)
                    return item;

                if (CurrentIterator.CurrentIndex == startIndex)
                    break;
            }

            return default;
        }

        /// <summary>
        /// Gets items by slot
        /// </summary>
        public List<EquipmentItem> GetItemsBySlot(EquipmentSlot slot)
        {
            return Collection.Where(item => item.Slot == slot).ToList();
        }

        /// <summary>
        /// Gets items by rarity
        /// </summary>
        public List<EquipmentItem> GetItemsByRarity(EquipmentRarity rarity)
        {
            return Collection.Where(item => item.Rarity == rarity).ToList();
        }

        /// <summary>
        /// Gets all equipped items
        /// </summary>
        public List<EquipmentItem> GetEquippedItems()
        {
            return Collection.Where(item => item.IsEquipped).ToList();
        }

        /// <summary>
        /// Gets items by set
        /// </summary>
        public List<EquipmentItem> GetItemsBySet(string setName)
        {
            return Collection.Where(item => item.SetName == setName).ToList();
        }

        /// <summary>
        /// Gets equipped set pieces
        /// </summary>
        public Dictionary<string, int> GetEquippedSetCounts()
        {
            var setCounts = new Dictionary<string, int>();

            foreach (var item in GetEquippedItems())
            {
                if (item.IsPartOfSet())
                {
                    if (!setCounts.ContainsKey(item.SetName))
                        setCounts[item.SetName] = 0;

                    setCounts[item.SetName]++;
                }
            }

            return setCounts;
        }

        /// <summary>
        /// Gets item by slot (first equipped or null)
        /// </summary>
        public EquipmentItem GetEquippedItemInSlot(EquipmentSlot slot)
        {
            return Collection.FirstOrDefault(item => item.Slot == slot && item.IsEquipped);
        }

        /// <summary>
        /// Calculates total stats from equipped items
        /// </summary>
        public EquipmentStats GetTotalStats()
        {
            var stats = new EquipmentStats();

            foreach (var item in GetEquippedItems())
            {
                stats.Attack += item.AttackBonus;
                stats.Defense += item.DefenseBonus;
                stats.Health += item.HealthBonus;
                stats.Mana += item.ManaBonus;
                stats.CritRate += item.CritRateBonus;
                stats.CritDamage += item.CritDamageBonus;
                stats.Speed += item.SpeedBonus;
            }

            return stats;
        }

        /// <summary>
        /// Sorts by power score
        /// </summary>
        public void SortByPowerScore()
        {
            Collection.Sort((a, b) => b.GetPowerScore().CompareTo(a.GetPowerScore()));
            Initialize();
        }

        /// <summary>
        /// Sorts by rarity
        /// </summary>
        public void SortByRarity()
        {
            Collection.Sort((a, b) => b.Rarity.CompareTo(a.Rarity));
            Initialize();
        }

        /// <summary>
        /// Sorts by enhancement level
        /// </summary>
        public void SortByEnhancement()
        {
            Collection.Sort((a, b) => b.EnhancementLevel.CompareTo(a.EnhancementLevel));
            Initialize();
        }

        /// <summary>
        /// Sorts by slot
        /// </summary>
        public void SortBySlot()
        {
            Collection.Sort((a, b) => a.Slot.CompareTo(b.Slot));
            Initialize();
        }
    }

    /// <summary>
    /// Helper class for total equipment stats
    /// </summary>
    [Serializable]
    public class EquipmentStats
    {
        public int Attack;
        public int Defense;
        public int Health;
        public int Mana;
        public float CritRate;
        public float CritDamage;
        public int Speed;

        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            if (Attack > 0) sb.Append($"+{Attack} ATK  ");
            if (Defense > 0) sb.Append($"+{Defense} DEF  ");
            if (Health > 0) sb.Append($"+{Health} HP  ");
            if (Mana > 0) sb.Append($"+{Mana} MP  ");
            if (Speed > 0) sb.Append($"+{Speed} SPD  ");
            if (CritRate > 0) sb.Append($"+{CritRate * 100:F1}% CRIT  ");
            if (CritDamage > 0) sb.Append($"+{CritDamage * 100:F1}% CRIT DMG");

            return sb.ToString().Trim();
        }
    }
}
