using UnityEngine;

namespace GameSystems.Inventory
{
    /// <summary>
    /// Inventory Iterator Controller với Inspector support
    /// </summary>
    public class InventoryIteratorController : MonoBehaviour
    {
        [SerializeField] private string inventoryName = "Player Inventory";
        [SerializeField] private bool debugMode = true;
        [SerializeField] private int maxSlots = 30;
        
        [SerializeField] private InventoryIteratorData inventoryData = new InventoryIteratorData();

        [Header("Runtime Info")]
        [SerializeField] private int currentIndex = -1;
        [SerializeField] private int totalIterations = 0;
        [SerializeField] private int totalItems = 0;
        [SerializeField] private int totalValue = 0;

        public string InventoryName 
        { 
            get => inventoryName; 
            set => inventoryName = value; 
        }

        public InventoryIteratorData InventoryData => inventoryData;
        public Item CurrentItem => inventoryData.Current;
        public int MaxSlots => maxSlots;

        // Events
        public event System.Action<Item> OnItemAdded;
        public event System.Action<Item> OnItemRemoved;
        public event System.Action<Item> OnItemUsed;
        public event System.Action<Item> OnItemEquipped;

        void Start()
        {
            if (inventoryData.Items.Count == 0)
            {
                SetupStarterItems();
            }

            inventoryData.Initialize();
            UpdateRuntimeInfo();
            LogDebug("✅ Inventory ready!");
        }

        void Update()
        {
            // Keyboard shortcuts
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                Next();
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                Previous();
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                EquipCurrentItem();
            }
            else if (Input.GetKeyDown(KeyCode.U))
            {
                UseCurrentItem();
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                DropCurrentItem();
            }
        }

        #region Setup

        private void SetupStarterItems()
        {
            // Weapons
            AddItem(new Item("sword_iron", "Iron Sword", "A basic iron sword", 
                ItemType.Weapon, ItemRarity.Common, 1, 50));
            
            AddItem(new Item("bow_wooden", "Wooden Bow", "A simple wooden bow", 
                ItemType.Weapon, ItemRarity.Common, 1, 40));

            // Armor
            AddItem(new Item("armor_leather", "Leather Armor", "Basic leather protection", 
                ItemType.Armor, ItemRarity.Common, 1, 60));

            // Consumables
            AddItem(new Item("potion_health", "Health Potion", "Restores 50 HP", 
                ItemType.Consumable, ItemRarity.Common, 99, 10));
            
            AddItem(new Item("potion_mana", "Mana Potion", "Restores 30 MP", 
                ItemType.Consumable, ItemRarity.Uncommon, 99, 15));

            // Materials
            AddItem(new Item("ore_iron", "Iron Ore", "Raw iron ore", 
                ItemType.Material, ItemRarity.Common, 999, 5));
            
            AddItem(new Item("wood_oak", "Oak Wood", "Strong oak wood", 
                ItemType.Material, ItemRarity.Common, 999, 3));

            // Quest items
            AddItem(new Item("key_ancient", "Ancient Key", "Opens ancient doors", 
                ItemType.Quest, ItemRarity.Rare, 1, 0));

            // Add multiple of the same item
            inventoryData.Items[3].AddQuantity(5); // 6 health potions
            inventoryData.Items[4].AddQuantity(2); // 3 mana potions
        }

        #endregion

        #region Navigation

        public void Next()
        {
            Item item = inventoryData.Next();
            UpdateRuntimeInfo();
            LogDebug($"→ Next: {item}");
        }

        public void Previous()
        {
            Item item = inventoryData.Previous();
            UpdateRuntimeInfo();
            LogDebug($"← Previous: {item}");
        }

        public void First()
        {
            Item item = inventoryData.First();
            UpdateRuntimeInfo();
            LogDebug($"⏮ First: {item}");
        }

        public void Last()
        {
            Item item = inventoryData.Last();
            UpdateRuntimeInfo();
            LogDebug($"⏭ Last: {item}");
        }

        public void NextOfType(ItemType type)
        {
            Item item = inventoryData.NextOfType(type);
            UpdateRuntimeInfo();
            if (item != null)
                LogDebug($"→ Next {type}: {item}");
            else
                LogDebug($"<color=orange>No more {type} items</color>");
        }

        public void NextConsumable()
        {
            Item item = inventoryData.NextConsumable();
            UpdateRuntimeInfo();
            if (item != null)
                LogDebug($"→ Next Consumable: {item}");
            else
                LogDebug("<color=orange>No consumables available</color>");
        }

        #endregion

        #region Item Actions

        public void AddItem(Item item)
        {
            if (inventoryData.Items.Count >= maxSlots)
            {
                LogDebug("<color=red>Inventory full!</color>");
                return;
            }

            // Check if item already exists and can stack
            Item existingItem = inventoryData.Items.Find(i => 
                i.ItemId == item.ItemId && i.CanStack());

            if (existingItem != null)
            {
                existingItem.AddQuantity(item.Quantity);
            }
            else
            {
                inventoryData.Add(item);
            }

            UpdateRuntimeInfo();
            OnItemAdded?.Invoke(item);
            LogDebug($"<color=green>+ Added:</color> {item}");
        }

        public void UseCurrentItem()
        {
            Item item = CurrentItem;
            if (item == null)
            {
                LogDebug("<color=red>No item selected!</color>");
                return;
            }

            if (item.IsConsumable)
            {
                item.Use();
                
                // Remove if quantity reaches 0
                if (item.Quantity <= 0)
                {
                    RemoveItem(item);
                }
                
                UpdateRuntimeInfo();
                OnItemUsed?.Invoke(item);
            }
            else
            {
                LogDebug($"<color=orange>{item.ItemName} is not usable!</color>");
            }
        }

        public void EquipCurrentItem()
        {
            Item item = CurrentItem;
            if (item == null)
            {
                LogDebug("<color=red>No item selected!</color>");
                return;
            }

            if (item.ItemType == ItemType.Weapon || item.ItemType == ItemType.Armor)
            {
                // Unequip other items of same type
                foreach (var i in inventoryData.Items)
                {
                    if (i.ItemType == item.ItemType && i.IsEquipped && i != item)
                    {
                        i.Unequip();
                    }
                }

                if (item.IsEquipped)
                {
                    item.Unequip();
                }
                else
                {
                    item.Equip();
                    OnItemEquipped?.Invoke(item);
                }
            }
            else
            {
                LogDebug($"<color=orange>Cannot equip {item.ItemName}!</color>");
            }
        }

        public void DropCurrentItem()
        {
            Item item = CurrentItem;
            if (item == null)
            {
                LogDebug("<color=red>No item selected!</color>");
                return;
            }

            if (item.ItemType == ItemType.Quest)
            {
                LogDebug("<color=red>Cannot drop quest items!</color>");
                return;
            }

            RemoveItem(item);
            LogDebug($"<color=yellow>Dropped:</color> {item.ItemName}");
        }

        private void RemoveItem(Item item)
        {
            inventoryData.Items.Remove(item);
            inventoryData.Initialize();
            UpdateRuntimeInfo();
            OnItemRemoved?.Invoke(item);
        }

        #endregion

        #region Sorting

        public void SortByName()
        {
            inventoryData.SortByName();
            UpdateRuntimeInfo();
            LogDebug("Sorted by Name");
        }

        public void SortByType()
        {
            inventoryData.SortByType();
            UpdateRuntimeInfo();
            LogDebug("Sorted by Type");
        }

        public void SortByRarity()
        {
            inventoryData.SortByRarity();
            UpdateRuntimeInfo();
            LogDebug("Sorted by Rarity");
        }

        public void SortByValue()
        {
            inventoryData.SortByValue();
            UpdateRuntimeInfo();
            LogDebug("Sorted by Value");
        }

        #endregion

        #region Info

        private void UpdateRuntimeInfo()
        {
            currentIndex = inventoryData.CurrentIndex;
            totalIterations = inventoryData.Items.Count;
            totalItems = inventoryData.Items.Count;
            totalValue = inventoryData.GetTotalValue();
        }

        public void ShowInventoryInfo()
        {
            Debug.Log("\n<color=cyan>═══════════════════════════════════════</color>");
            Debug.Log($"<color=yellow>📦 {inventoryName} 📦</color>");
            Debug.Log("<color=cyan>═══════════════════════════════════════</color>");
            Debug.Log($"Items: {totalItems}/{maxSlots}");
            Debug.Log($"Total Value: {totalValue} Gold");
            Debug.Log($"Iterations: {totalIterations}");
            Debug.Log($"Position: {currentIndex + 1}/{totalItems}");
            
            if (CurrentItem != null)
            {
                Debug.Log($"\n<color=yellow>Current:</color> {CurrentItem}");
            }
            
            Debug.Log("<color=cyan>═══════════════════════════════════════</color>\n");
        }

        private void LogDebug(string message)
        {
            if (debugMode)
            {
                Debug.Log($"<color=cyan>[{inventoryName}]</color> {message}");
            }
        }

        #endregion
    }
}
