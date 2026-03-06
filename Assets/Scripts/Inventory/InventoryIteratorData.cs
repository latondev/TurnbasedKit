using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameSystems.Common;

namespace GameSystems.Inventory
{
    /// <summary>
    /// Base iterator data for Inventory - extends GenericIteratorData
    /// </summary>
    [Serializable]
    public class InventoryIteratorData : GenericIteratorData<Item>
    {
        /// <summary>
        /// Gets next item of specific type
        /// </summary>
        public Item NextOfType(ItemType type)
        {
            if (CurrentIterator == null) return default;

            int startIndex = CurrentIterator.CurrentIndex;

            while (HasNext())
            {
                Item item = Next();
                if (item != null && item.ItemType == type)
                    return item;

                if (CurrentIterator.CurrentIndex == startIndex)
                    break;
            }

            return default;
        }

        /// <summary>
        /// Gets next consumable item
        /// </summary>
        public Item NextConsumable()
        {
            if (CurrentIterator == null) return default;

            int startIndex = CurrentIterator.CurrentIndex;

            while (HasNext())
            {
                Item item = Next();
                if (item != null && item.IsConsumable && item.Quantity > 0)
                    return item;

                if (CurrentIterator.CurrentIndex == startIndex)
                    break;
            }

            return default;
        }

        /// <summary>
        /// Gets next equipped item
        /// </summary>
        public Item NextEquipped()
        {
            if (CurrentIterator == null) return default;

            int startIndex = CurrentIterator.CurrentIndex;

            while (HasNext())
            {
                Item item = Next();
                if (item != null && item.IsEquipped)
                    return item;

                if (CurrentIterator.CurrentIndex == startIndex)
                    break;
            }

            return default;
        }

        /// <summary>
        /// Gets items by type
        /// </summary>
        public List<Item> GetItemsByType(ItemType type)
        {
            return Collection.Where(item => item.ItemType == type).ToList();
        }

        /// <summary>
        /// Gets items by rarity
        /// </summary>
        public List<Item> GetItemsByRarity(ItemRarity rarity)
        {
            return Collection.Where(item => item.Rarity == rarity).ToList();
        }

        /// <summary>
        /// Gets all consumable items
        /// </summary>
        public List<Item> GetConsumables()
        {
            return Collection.Where(item => item.IsConsumable && item.Quantity > 0).ToList();
        }

        /// <summary>
        /// Gets all equipped items
        /// </summary>
        public List<Item> GetEquippedItems()
        {
            return Collection.Where(item => item.IsEquipped).ToList();
        }

        /// <summary>
        /// Calculates total inventory value
        /// </summary>
        public int GetTotalValue()
        {
            return Collection.Sum(item => item.Value * item.Quantity);
        }

        /// <summary>
        /// Sorts inventory by name
        /// </summary>
        public void SortByName()
        {
            Collection.Sort((a, b) => string.Compare(a.ItemName, b.ItemName, StringComparison.Ordinal));
            Initialize();
        }

        /// <summary>
        /// Sorts inventory by type
        /// </summary>
        public void SortByType()
        {
            Collection.Sort((a, b) => a.ItemType.CompareTo(b.ItemType));
            Initialize();
        }

        /// <summary>
        /// Sorts inventory by rarity
        /// </summary>
        public void SortByRarity()
        {
            Collection.Sort((a, b) => b.Rarity.CompareTo(a.Rarity));
            Initialize();
        }

        /// <summary>
        /// Sorts inventory by value
        /// </summary>
        public void SortByValue()
        {
            Collection.Sort((a, b) => b.Value.CompareTo(a.Value));
            Initialize();
        }
    }
}
