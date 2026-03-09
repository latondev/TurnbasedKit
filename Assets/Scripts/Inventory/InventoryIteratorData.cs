using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameSystems.Common;

namespace GameSystems.Inventory
{
    /// <summary>
    /// Inventory collection with query methods - uses ItemCollection base
    /// </summary>
    [Serializable]
    public class InventoryIteratorData : ItemCollection<Item>
    {
        /// <summary>
        /// Gets next item of specific type
        /// </summary>
        public Item NextOfType(ItemType type)
        {
            if (IsEmpty) return null;

            int startIndex = CurrentIndex;
            int loopCount = 0;
            int maxLoops = Items.Count;

            while (loopCount < maxLoops)
            {
                if (MoveNext())
                {
                    Item item = Current;
                    if (item != null && item.ItemType == type)
                        return item;

                    if (CurrentIndex == startIndex)
                        break;
                }
                loopCount++;
            }

            return null;
        }

        /// <summary>
        /// Gets next consumable item
        /// </summary>
        public Item NextConsumable()
        {
            if (IsEmpty) return null;

            int startIndex = CurrentIndex;
            int loopCount = 0;
            int maxLoops = Items.Count;

            while (loopCount < maxLoops)
            {
                if (MoveNext())
                {
                    Item item = Current;
                    if (item != null && item.IsConsumable && item.Quantity > 0)
                        return item;

                    if (CurrentIndex == startIndex)
                        break;
                }
                loopCount++;
            }

            return null;
        }

        /// <summary>
        /// Gets next equipped item
        /// </summary>
        public Item NextEquipped()
        {
            if (IsEmpty) return null;

            int startIndex = CurrentIndex;
            int loopCount = 0;
            int maxLoops = Items.Count;

            while (loopCount < maxLoops)
            {
                if (MoveNext())
                {
                    Item item = Current;
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
        /// Gets items by type
        /// </summary>
        public List<Item> GetItemsByType(ItemType type)
        {
            return Items.Where(item => item.ItemType == type).ToList();
        }

        /// <summary>
        /// Gets items by rarity
        /// </summary>
        public List<Item> GetItemsByRarity(ItemRarity rarity)
        {
            return Items.Where(item => item.Rarity == rarity).ToList();
        }

        /// <summary>
        /// Gets all consumable items
        /// </summary>
        public List<Item> GetConsumables()
        {
            return Items.Where(item => item.IsConsumable && item.Quantity > 0).ToList();
        }

        /// <summary>
        /// Gets all equipped items
        /// </summary>
        public List<Item> GetEquippedItems()
        {
            return Items.Where(item => item.IsEquipped).ToList();
        }

        /// <summary>
        /// Calculates total inventory value
        /// </summary>
        public int GetTotalValue()
        {
            return Items.Sum(item => item.Value * item.Quantity);
        }

        /// <summary>
        /// Sorts inventory by name
        /// </summary>
        public void SortByName()
        {
            Items.Sort((a, b) => string.Compare(a.ItemName, b.ItemName, StringComparison.Ordinal));
            Initialize();
        }

        /// <summary>
        /// Sorts inventory by type
        /// </summary>
        public void SortByType()
        {
            Items.Sort((a, b) => a.ItemType.CompareTo(b.ItemType));
            Initialize();
        }
    }
}
