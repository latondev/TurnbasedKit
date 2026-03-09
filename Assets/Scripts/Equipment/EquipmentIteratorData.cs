using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameSystems.Common;

namespace GameSystems.Equipment
{
    /// <summary>
    /// Equipment collection with query methods - uses ItemCollection base
    /// </summary>
    [Serializable]
    public class EquipmentIteratorData : ItemCollection<EquipmentItem>
    {
        /// <summary>
        /// Gets next equipped item
        /// </summary>
        public EquipmentItem NextEquipped()
        {
            if (IsEmpty) return null;

            int startIndex = CurrentIndex;
            int loopCount = 0;
            int maxLoops = Items.Count;

            while (loopCount < maxLoops)
            {
                if (MoveNext())
                {
                    EquipmentItem item = Current;
                    if (item != null && item.IsEquipped)
                        return item;

                    if (CurrentIndex == startIndex)
                        break;
                }
                loopCount++;
            }

            return null;
        }

        /// <summary>
        /// Gets next item by slot
        /// </summary>
        public EquipmentItem NextOfSlot(EquipmentSlot slot)
        {
            if (IsEmpty) return null;

            int startIndex = CurrentIndex;
            int loopCount = 0;
            int maxLoops = Items.Count;

            while (loopCount < maxLoops)
            {
                if (MoveNext())
                {
                    EquipmentItem item = Current;
                    if (item != null && item.Slot == slot)
                        return item;

                    if (CurrentIndex == startIndex)
                        break;
                }
                loopCount++;
            }

            return null;
        }

        /// <summary>
        /// Gets next item by rarity
        /// </summary>
        public EquipmentItem NextOfRarity(EquipmentRarity rarity)
        {
            if (IsEmpty) return null;

            int startIndex = CurrentIndex;
            int loopCount = 0;
            int maxLoops = Items.Count;

            while (loopCount < maxLoops)
            {
                if (MoveNext())
                {
                    EquipmentItem item = Current;
                    if (item != null && item.Rarity == rarity)
                        return item;

                    if (CurrentIndex == startIndex)
                        break;
                }
                loopCount++;
            }

            return null;
        }

        /// <summary>
        /// Gets items by slot
        /// </summary>
        public List<EquipmentItem> GetItemsBySlot(EquipmentSlot slot)
        {
            return Items.Where(item => item.Slot == slot).ToList();
        }

        /// <summary>
        /// Gets items by rarity
        /// </summary>
        public List<EquipmentItem> GetItemsByRarity(EquipmentRarity rarity)
        {
            return Items.Where(item => item.Rarity == rarity).ToList();
        }

        /// <summary>
        /// Gets all equipped items
        /// </summary>
        public List<EquipmentItem> GetEquippedItems()
        {
            return Items.Where(item => item.IsEquipped).ToList();
        }

        /// <summary>
        /// Gets items by set
        /// </summary>
        public List<EquipmentItem> GetItemsBySet(string setName)
        {
            return Items.Where(item => item.SetName == setName).ToList();
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
            return Items.FirstOrDefault(item => item.Slot == slot && item.IsEquipped);
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
            Items.Sort((a, b) => b.GetPowerScore().CompareTo(a.GetPowerScore()));
            Initialize();
        }

        /// <summary>
        /// Sorts by rarity
        /// </summary>
        public void SortByRarity()
        {
            Items.Sort((a, b) => b.Rarity.CompareTo(a.Rarity));
            Initialize();
        }

        /// <summary>
        /// Sorts by enhancement level
        /// </summary>
        public void SortByEnhancement()
        {
            Items.Sort((a, b) => b.EnhancementLevel.CompareTo(a.EnhancementLevel));
            Initialize();
        }

        /// <summary>
        /// Sorts by slot
        /// </summary>
        public void SortBySlot()
        {
            Items.Sort((a, b) => a.Slot.CompareTo(b.Slot));
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
