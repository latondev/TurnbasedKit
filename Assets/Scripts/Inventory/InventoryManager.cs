using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameSystems.Inventory;
using GameSystems.Equipment;
using GameSystems.Common;
using SlotEnum = GameSystems.Equipment.EquipmentSlot;

namespace GameSystems.Inventory
{
    /// <summary>
    /// Inventory Manager - kết nối Inventory và Equipment
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InventoryIteratorController inventory;
        [SerializeField] private EquipmentController equipment;
        [SerializeField] private GameStateManager gameState;

        [Header("Settings")]
        [SerializeField] private bool debugMode = true;

        // Events
        public event Action<Item> OnItemPurchased;
        public event Action<Item> OnItemSold;
        public event Action<Item, EquipmentSlot> OnItemEquipped;
        public event Action<Item> OnItemUnequipped;

        // Singleton
        public static InventoryManager Instance { get; private set; }

        void Awake()
        {
            if (Instance == null) Instance = this;
        }

        #region Buy / Sell

        /// <summary>
        /// Mua item (cần GameStateManager để trừ gold)
        /// </summary>
        public bool BuyItem(Item item, int quantity = 1)
        {
            if (gameState == null)
            {
                LogDebug("<color=red>GameStateManager not found!</color>");
                return false;
            }

            int totalCost = item.Value * quantity;

            if (!gameState.SpendGold(totalCost))
            {
                LogDebug($"<color=red>Not enough gold! Need {totalCost}, have {gameState.Gold}</color>");
                return false;
            }

            // Thêm vào inventory
            var newItem = new Item(
                item.ItemId + "_" + Guid.NewGuid().ToString().Substring(0, 6),
                item.ItemName,
                item.Description,
                item.ItemType,
                item.Rarity,
                item.MaxStack,
                item.Value
            );
            newItem.AddQuantity(quantity - 1);

            inventory.AddItem(newItem);
            OnItemPurchased?.Invoke(newItem);

            LogDebug($"<color=green>Bought:</color> {item.ItemName} x{quantity} for {totalCost} gold");
            return true;
        }

        /// <summary>
        /// Bán item (hoàn lại 50% giá trị)
        /// </summary>
        public int SellItem(Item item)
        {
            if (item == null) return 0;

            int sellValue = Mathf.RoundToInt(item.Value * 0.5f) * item.Quantity;

            // Thêm gold
            gameState?.AddGold(sellValue);

            // Xóa khỏi inventory
            inventory.InventoryData.Collection.Remove(item);

            OnItemSold?.Invoke(item);
            LogDebug($"<color=yellow>Sold:</color> {item.ItemName} for {sellValue} gold");

            return sellValue;
        }

        #endregion

        #region Equipment Management

        /// <summary>
        /// Equip item từ inventory
        /// </summary>
        public bool EquipItem(Item item)
        {
            if (item == null)
            {
                LogDebug("<color=red>No item selected!</color>");
                return false;
            }

            if (item.ItemType != ItemType.Weapon && item.ItemType != ItemType.Armor)
            {
                LogDebug($"<color=orange>Cannot equip {item.ItemName} - not equipment!</color>");
                return false;
            }

            // Xác định slot
            EquipmentSlot slot = item.ItemType == ItemType.Weapon
                ? EquipmentSlot.Weapon
                : EquipmentSlot.Armor;

            // Kiểm tra xem đã có item equipped chưa
            var equippedItem = GetEquippedItem(slot);
            if (equippedItem != null)
            {
                // Unequip trước
                UnequipItem(slot);
            }

            // Equip
            item.Equip();
            OnItemEquipped?.Invoke(item, slot);

            LogDebug($"<color=green>Equipped:</color> {item.ItemName} ({slot})");
            return true;
        }

        /// <summary>
        /// Unequip item vào inventory
        /// </summary>
        public bool UnequipItem(EquipmentSlot slot)
        {
            var equippedItem = GetEquippedItem(slot);
            if (equippedItem == null)
            {
                LogDebug($"<color=orange>No item equipped in {slot}</color>");
                return false;
            }

            equippedItem.Unequip();
            OnItemUnequipped?.Invoke(equippedItem);

            LogDebug($"<color=yellow>Unequipped:</color> {equippedItem.ItemName}");
            return true;
        }

        /// <summary>
        /// Lấy item đang equipped ở slot cụ thể
        /// </summary>
        public Item GetEquippedItem(EquipmentSlot slot)
        {
            foreach (var item in inventory.InventoryData.Collection)
            {
                if (item.IsEquipped && IsItemInSlot(item, slot))
                {
                    return item;
                }
            }
            return null;
        }

        /// <summary>
        /// Kiểm tra item có ở slot không
        /// </summary>
        private bool IsItemInSlot(Item item, EquipmentSlot slot)
        {
            // Weapon goes to Weapon slot
            if (slot == EquipmentSlot.Weapon && item.ItemType == ItemType.Weapon)
                return true;
            // Armor (Helmet, Armor) goes to Armor slot
            if (slot == EquipmentSlot.Armor && item.ItemType == ItemType.Armor)
                return true;

            return false;
        }

        /// <summary>
        /// Lấy tất cả equipped items
        /// </summary>
        public List<Item> GetAllEquippedItems()
        {
            return inventory.InventoryData.Collection
                .Where(i => i.IsEquipped)
                .ToList();
        }

        #endregion

        #region Inventory Operations

        /// <summary>
        /// Thêm item vào inventory
        /// </summary>
        public void AddItem(Item item)
        {
            inventory.AddItem(item);
        }

        /// <summary>
        /// Xóa item khỏi inventory
        /// </summary>
        public void RemoveItem(Item item)
        {
            inventory.InventoryData.Collection.Remove(item);
            LogDebug($"<color=red>Removed:</color> {item.ItemName}");
        }

        /// <summary>
        /// Sử dụng consumable item
        /// </summary>
        public void UseItem(Item item)
        {
            if (item == null || !item.IsConsumable)
            {
                LogDebug("<color=orange>Item is not consumable!</color>");
                return;
            }

            // Apply effects based on item
            ApplyConsumableEffect(item);

            inventory.UseCurrentItem();
        }

        /// <summary>
        /// Áp dụng effect của consumable
        /// </summary>
        private void ApplyConsumableEffect(Item item)
        {
            if (gameState == null) return;

            // Health Potion
            if (item.ItemId.Contains("health") || item.ItemId.Contains("potion"))
            {
                gameState.AddHealth(50);
                LogDebug("<color=green>Restored 50 HP!</color>");
            }
            // Mana Potion
            else if (item.ItemId.Contains("mana"))
            {
                gameState.AddMana(30);
                LogDebug("<color=blue>Restored 30 Mana!</color>");
            }
            // EXP Scroll
            else if (item.ItemId.Contains("exp") || item.ItemId.Contains("scroll"))
            {
                gameState.AddExperience(100);
                LogDebug("<color=yellow>Gained 100 EXP!</color>");
            }
        }

        /// <summary>
        /// Lấy tổng giá trị inventory
        /// </summary>
        public int GetTotalInventoryValue()
        {
            return inventory.InventoryData.GetTotalValue();
        }

        /// <summary>
        /// Kiểm tra inventory có full không
        /// </summary>
        public bool IsInventoryFull()
        {
            return inventory.InventoryData.Collection.Count >= inventory.MaxSlots;
        }

        #endregion

        #region Info

        public void ShowInventorySummary()
        {
            Debug.Log("\n<color=cyan>═══════════ Inventory Summary ═══════════</color>");

            Debug.Log($"<color=yellow>📦 Inventory:</color> {inventory.InventoryData.Collection.Count}/{inventory.MaxSlots}");
            Debug.Log($"Total Value: {GetTotalInventoryValue()} gold");

            Debug.Log($"\n<color=yellow>⚔️ Equipped:</color>");
            foreach (var item in GetAllEquippedItems())
            {
                Debug.Log($"  {item}");
            }

            Debug.Log($"\n<color=yellow>💰 Player Resources:</color>");
            if (gameState != null)
            {
                Debug.Log($"  Gold: {gameState.Gold}");
                Debug.Log($"  Gems: {gameState.Gems}");
                Debug.Log($"  Level: {gameState.PlayerLevel}");
                Debug.Log($"  EXP: {gameState.Experience}/{gameState.ExperienceToNextLevel}");
            }

            Debug.Log("<color=cyan>═════════════════════════════════════════════</color>\n");
        }

        private void LogDebug(string message)
        {
            if (debugMode)
            {
                Debug.Log($"<color=orange>[InventoryManager]</color> {message}");
            }
        }

        #endregion
    }
}
