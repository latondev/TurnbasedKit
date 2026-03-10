using UnityEngine;
using System.Collections.Generic;

namespace GameSystems.Equipment
{
    /// <summary>
    /// Complete Equipment Controller with set bonuses and enhancement
    /// </summary>
    public class EquipmentController : MonoBehaviour
    {
        [SerializeField] private string controllerName = "Equipment System";
        [SerializeField] private bool debugMode = true;
        [SerializeField] private int playerLevel = 1;
        
        [SerializeField] private EquipmentIteratorData equipmentData = new EquipmentIteratorData();
        [SerializeField] private Dictionary<string, EquipmentSet> equipmentSets = new Dictionary<string, EquipmentSet>();

        [Header("Runtime Info")]
        [SerializeField] private int currentIndex = -1;
        [SerializeField] private int totalIterations = 0;
        [SerializeField] private int totalPowerScore = 0;

        public string ControllerName 
        { 
            get => controllerName; 
            set => controllerName = value; 
        }

        public EquipmentIteratorData EquipmentData => equipmentData;
        public EquipmentItem CurrentItem => equipmentData.Current;
        public int PlayerLevel => playerLevel;
        public Dictionary<string, EquipmentSet> EquipmentSets => equipmentSets;

        // Events
        public event System.Action<EquipmentItem> OnItemEquipped;
        public event System.Action<EquipmentItem> OnItemUnequipped;
        public event System.Action<EquipmentItem> OnItemEnhanced;

        void Start()
        {
            if (equipmentData.Items.Count == 0)
            {
                SetupExampleEquipment();
            }

            SetupEquipmentSets();
            equipmentData.Initialize();
            UpdateRuntimeInfo();
            LogDebug("✅ Equipment system ready!");
        }

        void Update()
        {
            HandleInput();
        }

        #region Setup

        private void SetupExampleEquipment()
        {
            // Dragon Set - Legendary
            var dragonSword = new EquipmentItem(
                "weapon_dragon_sword",
                "Dragon Blade",
                "A legendary sword forged from dragon scales",
                EquipmentSlot.Weapon,
                EquipmentRarity.Legendary,
                10
            );
            dragonSword.SetStats(atk: 100, critDmg: 0.5f);
            dragonSword.SetEquipmentSet("Dragon's Fury", 1);
            equipmentData.Add(dragonSword);

            var dragonHelmet = new EquipmentItem(
                "helmet_dragon",
                "Dragon Helm",
                "Helmet crafted from dragon bone",
                EquipmentSlot.Helmet,
                EquipmentRarity.Legendary,
                10
            );
            dragonHelmet.SetStats(def: 50, hp: 200);
            dragonHelmet.SetEquipmentSet("Dragon's Fury", 2);
            equipmentData.Add(dragonHelmet);

            var dragonArmor = new EquipmentItem(
                "armor_dragon",
                "Dragon Plate",
                "Heavy armor made from dragon scales",
                EquipmentSlot.Armor,
                EquipmentRarity.Legendary,
                10
            );
            dragonArmor.SetStats(def: 80, hp: 300);
            dragonArmor.SetEquipmentSet("Dragon's Fury", 3);
            equipmentData.Add(dragonArmor);

            // Knight Set - Epic
            var knightSword = new EquipmentItem(
                "weapon_knight_sword",
                "Knight's Longsword",
                "A noble knight's trusted blade",
                EquipmentSlot.Weapon,
                EquipmentRarity.Epic,
                7
            );
            knightSword.SetStats(atk: 60, critRate: 0.1f);
            knightSword.SetEquipmentSet("Knight's Honor", 1);
            equipmentData.Add(knightSword);

            var knightShield = new EquipmentItem(
                "armor_knight_shield",
                "Knight's Greatshield",
                "Heavy shield with the knight's crest",
                EquipmentSlot.Armor,
                EquipmentRarity.Epic,
                7
            );
            knightShield.SetStats(def: 60, hp: 150);
            knightShield.SetEquipmentSet("Knight's Honor", 2);
            equipmentData.Add(knightShield);

            // Assassin Set - Rare
            var assassinDagger = new EquipmentItem(
                "weapon_assassin_dagger",
                "Shadow Dagger",
                "A swift blade for quick strikes",
                EquipmentSlot.Weapon,
                EquipmentRarity.Rare,
                5
            );
            assassinDagger.SetStats(atk: 40, critRate: 0.25f, spd: 20);
            assassinDagger.SetEquipmentSet("Shadow Strike", 1);
            equipmentData.Add(assassinDagger);

            // Accessories
            var powerRing = new EquipmentItem(
                "ring_power",
                "Ring of Power",
                "Increases attack power",
                EquipmentSlot.Ring,
                EquipmentRarity.Epic,
                5
            );
            powerRing.SetStats(atk: 30, critRate: 0.15f);
            equipmentData.Add(powerRing);

            var lifeNecklace = new EquipmentItem(
                "necklace_life",
                "Amulet of Vitality",
                "Grants increased health",
                EquipmentSlot.Necklace,
                EquipmentRarity.Rare,
                4
            );
            lifeNecklace.SetStats(hp: 150, def: 20);
            equipmentData.Add(lifeNecklace);

            // Common items
            var ironSword = new EquipmentItem(
                "weapon_iron_sword",
                "Iron Sword",
                "A basic iron weapon",
                EquipmentSlot.Weapon,
                EquipmentRarity.Common,
                1
            );
            ironSword.SetStats(atk: 15);
            equipmentData.Add(ironSword);

            var leatherArmor = new EquipmentItem(
                "armor_leather",
                "Leather Armor",
                "Basic leather protection",
                EquipmentSlot.Armor,
                EquipmentRarity.Common,
                1
            );
            leatherArmor.SetStats(def: 10, hp: 50);
            equipmentData.Add(leatherArmor);
        }

        private void SetupEquipmentSets()
        {
            // Dragon's Fury Set
            var dragonSet = new EquipmentSet(
                "Dragon's Fury",
                "Grants incredible power from dragon essence",
                6
            );
            dragonSet.AddSetBonus(2, "Dragon's Might", atk: 50, special: "+10% Fire Damage");
            dragonSet.AddSetBonus(4, "Dragon's Resilience", def: 50, hp: 200, special: "+20% Fire Resistance");
            dragonSet.AddSetBonus(6, "Dragon's Awakening", atk: 100, def: 100, hp: 500, critRate: 0.2f, special: "Dragon Form");
            equipmentSets["Dragon's Fury"] = dragonSet;

            // Knight's Honor Set
            var knightSet = new EquipmentSet(
                "Knight's Honor",
                "The armor of noble knights",
                4
            );
            knightSet.AddSetBonus(2, "Knight's Valor", def: 30, hp: 100);
            knightSet.AddSetBonus(4, "Knight's Glory", atk: 40, def: 60, hp: 200, special: "Shield Bash");
            equipmentSets["Knight's Honor"] = knightSet;

            // Shadow Strike Set
            var assassinSet = new EquipmentSet(
                "Shadow Strike",
                "Equipment favored by assassins",
                4
            );
            assassinSet.AddSetBonus(2, "Shadow Step", critRate: 0.15f, special: "+15% Movement Speed");
            assassinSet.AddSetBonus(4, "Shadow Master", atk: 30, critRate: 0.3f, special: "Backstab x2 Damage");
            equipmentSets["Shadow Strike"] = assassinSet;
        }

        #endregion

        #region Navigation

        public void Next()
        {
            if (equipmentData.CurrentIndex < equipmentData.Items.Count - 1)
                equipmentData.CurrentIndex++;
            else
                equipmentData.CurrentIndex = 0;
            var item = equipmentData.Current;
            UpdateRuntimeInfo();
            LogDebug($"→ {item}");
        }

        public void Previous()
        {
            if (equipmentData.CurrentIndex > 0)
                equipmentData.CurrentIndex--;
            else
                equipmentData.CurrentIndex = equipmentData.Items.Count - 1;
            var item = equipmentData.Current;
            UpdateRuntimeInfo();
            LogDebug($"← {item}");
        }

        public void First()
        {
            equipmentData.CurrentIndex = 0;
            var item = equipmentData.Current;
            UpdateRuntimeInfo();
            LogDebug($"⏮ {item}");
        }

        public void Last()
        {
            equipmentData.CurrentIndex = equipmentData.Items.Count - 1;
            var item = equipmentData.Current;
            UpdateRuntimeInfo();
            LogDebug($"⏭ {item}");
        }

        #endregion

        #region Equipment Actions

        public void EquipCurrentItem()
        {
            EquipmentItem item = CurrentItem;
            if (item == null)
            {
                LogDebug("<color=red>No item selected!</color>");
                return;
            }

            if (item.RequiredLevel > playerLevel)
            {
                LogDebug($"<color=red>Level {item.RequiredLevel} required!</color>");
                return;
            }

            // Unequip current item in slot
            var currentInSlot = equipmentData.GetEquippedItemInSlot(item.Slot);
            if (currentInSlot != null && currentInSlot != item)
            {
                currentInSlot.Unequip();
                OnItemUnequipped?.Invoke(currentInSlot);
            }

            item.Equip();
            OnItemEquipped?.Invoke(item);
            UpdateRuntimeInfo();
            
            // Check set bonuses
            CheckSetBonuses();
        }

        public void UnequipCurrentItem()
        {
            EquipmentItem item = CurrentItem;
            if (item == null || !item.IsEquipped)
            {
                LogDebug("<color=red>Item not equipped!</color>");
                return;
            }

            item.Unequip();
            OnItemUnequipped?.Invoke(item);
            UpdateRuntimeInfo();
            
            CheckSetBonuses();
        }

        public void EnhanceCurrentItem()
        {
            EquipmentItem item = CurrentItem;
            if (item == null)
            {
                LogDebug("<color=red>No item selected!</color>");
                return;
            }

            if (item.Enhance())
            {
                OnItemEnhanced?.Invoke(item);
                UpdateRuntimeInfo();
            }
        }

        public void RepairCurrentItem()
        {
            EquipmentItem item = CurrentItem;
            if (item == null)
            {
                LogDebug("<color=red>No item selected!</color>");
                return;
            }

            item.Repair();
        }

        #endregion

        #region Set Bonuses

        private void CheckSetBonuses()
        {
            var setCounts = equipmentData.GetEquippedSetCounts();
            
            if (setCounts.Count == 0) return;

            Debug.Log("<color=cyan>═══ Set Bonuses ═══</color>");
            
            foreach (var kvp in setCounts)
            {
                string setName = kvp.Key;
                int equippedCount = kvp.Value;
                
                if (equipmentSets.ContainsKey(setName))
                {
                    var set = equipmentSets[setName];
                    var activeBonuses = set.GetActiveBonuses(equippedCount);
                    
                    Debug.Log($"<color=yellow>{setName}:</color> {equippedCount}/{set.TotalPieces} pieces");
                    
                    foreach (var bonus in activeBonuses)
                    {
                        Debug.Log($"  <color=green>✓</color> {bonus}");
                    }
                }
            }
        }

        public EquipmentStats GetTotalStatsWithSetBonuses()
        {
            var stats = equipmentData.GetTotalStats();
            var setCounts = equipmentData.GetEquippedSetCounts();
            
            foreach (var kvp in setCounts)
            {
                if (equipmentSets.ContainsKey(kvp.Key))
                {
                    var setBonus = equipmentSets[kvp.Key].GetTotalBonus(kvp.Value);
                    stats.Attack += setBonus.AttackBonus;
                    stats.Defense += setBonus.DefenseBonus;
                    stats.Health += setBonus.HealthBonus;
                    stats.Mana += setBonus.ManaBonus;
                    stats.CritRate += setBonus.CritRateBonus;
                }
            }
            
            return stats;
        }

        #endregion

        #region Sorting

        public void SortByPowerScore()
        {
            equipmentData.SortByPowerScore();
            UpdateRuntimeInfo();
            LogDebug("Sorted by Power Score");
        }

        public void SortByRarity()
        {
            equipmentData.SortByRarity();
            UpdateRuntimeInfo();
            LogDebug("Sorted by Rarity");
        }

        public void SortByEnhancement()
        {
            equipmentData.SortByEnhancement();
            UpdateRuntimeInfo();
            LogDebug("Sorted by Enhancement");
        }

        public void SortBySlot()
        {
            equipmentData.SortBySlot();
            UpdateRuntimeInfo();
            LogDebug("Sorted by Slot");
        }

        #endregion

        #region Input

        private void HandleInput()
        {
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
                UnequipCurrentItem();
            }
            else if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                EnhanceCurrentItem();
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                RepairCurrentItem();
            }
        }

        #endregion

        #region Info

        private void UpdateRuntimeInfo()
        {
            currentIndex = equipmentData.CurrentIndex;
            totalIterations = equipmentData.Items.Count;
            
            int totalPower = 0;
            foreach (var item in equipmentData.GetEquippedItems())
            {
                totalPower += item.GetPowerScore();
            }
            totalPowerScore = totalPower;
        }

        public void ShowEquipmentInfo()
        {
            Debug.Log("\n<color=cyan>═══════════════════════════════════════</color>");
            Debug.Log($"<color=yellow>⚔️ {controllerName} ⚔️</color>");
            Debug.Log("<color=cyan>═══════════════════════════════════════</color>");
            Debug.Log($"Player Level: {playerLevel}");
            Debug.Log($"Total Equipment: {equipmentData.Items.Count}");
            Debug.Log($"Equipped: {equipmentData.GetEquippedItems().Count}");
            Debug.Log($"Total Power Score: {totalPowerScore}");
            
            var stats = GetTotalStatsWithSetBonuses();
            Debug.Log($"\n<color=green>Total Stats:</color>");
            Debug.Log($"  {stats}");
            
            CheckSetBonuses();
            
            Debug.Log("<color=cyan>═══════════════════════════════════════</color>\n");
        }

        private void LogDebug(string message)
        {
            if (debugMode)
            {
                Debug.Log($"<color=orange>[{controllerName}]</color> {message}");
            }
        }

        #endregion
    }
}
