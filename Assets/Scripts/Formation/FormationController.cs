using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameSystems.Formation
{
    /// <summary>
    /// Complete Formation Controller
    /// </summary>
    public class FormationController : MonoBehaviour
    {
        [SerializeField] private bool debugMode = true;
        [SerializeField] private List<FormationType> availableFormations = new List<FormationType>();
        [SerializeField] private FormationType currentFormation;
        [SerializeField] private int currentFormationIndex = 0;

        [Header("Available Units")]
        [SerializeField] private List<string> availableUnits = new List<string>();

        [Header("Runtime Info")]
        [SerializeField] private int unitsPlaced = 0;
        [SerializeField] private int totalSlots = 0;

        public FormationType CurrentFormation => currentFormation;
        public List<FormationType> AvailableFormations => availableFormations;
        public List<string> AvailableUnits => availableUnits;
        public int UnitsPlaced => unitsPlaced;
        public int TotalSlots => totalSlots;

        // Events
        public event Action<FormationType> OnFormationChanged;
        public event Action<FormationSlot, string> OnUnitPlaced;
        public event Action<FormationSlot> OnUnitRemoved;
        public event Action<FormationSlot, FormationSlot> OnUnitsSwapped;

        void Start()
        {
            if (availableFormations.Count == 0)
            {
                SetupDefaultFormations();
            }

            if (availableUnits.Count == 0)
            {
                SetupExampleUnits();
            }

            if (currentFormation == null && availableFormations.Count > 0)
            {
                SetFormation(0);
            }

            UpdateRuntimeInfo();
            LogDebug("✅ Formation system ready!");
        }

        #region Setup

        private void SetupDefaultFormations()
        {
            availableFormations.Add(new FormationType(
                "standard_3x3",
                "Standard Formation",
                "Balanced 3x3 grid formation",
                FormationLayout.Standard_3x3
            ));

            availableFormations.Add(new FormationType(
                "offensive_2x4",
                "Offensive Formation",
                "Focus on damage with more backline",
                FormationLayout.Offensive_2x4
            ));

            availableFormations.Add(new FormationType(
                "defensive_4x2",
                "Defensive Formation",
                "Focus on defense with strong frontline",
                FormationLayout.Defensive_4x2
            ));

            availableFormations.Add(new FormationType(
                "balanced_2x3",
                "Balanced Formation",
                "Good mix of offense and defense",
                FormationLayout.Balanced_2x3
            ));

            availableFormations.Add(new FormationType(
                "v_formation",
                "V Formation",
                "Tactical V-shaped formation",
                FormationLayout.VFormation
            ));

            LogDebug($"Created {availableFormations.Count} formations");
        }

        private void SetupExampleUnits()
        {
            availableUnits.Add("Hero");
            availableUnits.Add("Warrior");
            availableUnits.Add("Mage");
            availableUnits.Add("Archer");
            availableUnits.Add("Healer");
            availableUnits.Add("Tank");
            availableUnits.Add("Assassin");
            availableUnits.Add("Knight");
            availableUnits.Add("Paladin");

            LogDebug($"Created {availableUnits.Count} units");
        }

        #endregion

        #region Formation Management

        /// <summary>
        /// Sets current formation by index
        /// </summary>
        public void SetFormation(int index)
        {
            if (index < 0 || index >= availableFormations.Count)
            {
                LogDebug($"<color=red>Invalid formation index: {index}</color>");
                return;
            }

            currentFormationIndex = index;
            currentFormation = availableFormations[index];
            
            UpdateRuntimeInfo();
            OnFormationChanged?.Invoke(currentFormation);
            
            LogDebug($"<color=cyan>Formation changed to: {currentFormation.FormationName}</color>");
        }

        /// <summary>
        /// Switches to next formation
        /// </summary>
        public void NextFormation()
        {
            int nextIndex = (currentFormationIndex + 1) % availableFormations.Count;
            SetFormation(nextIndex);
        }

        /// <summary>
        /// Switches to previous formation
        /// </summary>
        public void PreviousFormation()
        {
            int prevIndex = currentFormationIndex - 1;
            if (prevIndex < 0) prevIndex = availableFormations.Count - 1;
            SetFormation(prevIndex);
        }

        #endregion

        #region Unit Placement

        /// <summary>
        /// Places unit in specific slot
        /// </summary>
        public bool PlaceUnit(int slotId, string unitId)
        {
            if (currentFormation == null)
            {
                LogDebug("<color=red>No formation selected!</color>");
                return false;
            }

            var slot = currentFormation.GetSlot(slotId);
            if (slot == null)
            {
                LogDebug($"<color=red>Slot {slotId} not found!</color>");
                return false;
            }

            if (!availableUnits.Contains(unitId))
            {
                LogDebug($"<color=red>Unit {unitId} not available!</color>");
                return false;
            }

            if (slot.PlaceUnit(unitId))
            {
                UpdateRuntimeInfo();
                OnUnitPlaced?.Invoke(slot, unitId);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes unit from slot
        /// </summary>
        public void RemoveUnit(int slotId)
        {
            if (currentFormation == null) return;

            var slot = currentFormation.GetSlot(slotId);
            if (slot == null) return;

            slot.RemoveUnit();
            UpdateRuntimeInfo();
            OnUnitRemoved?.Invoke(slot);
        }

        /// <summary>
        /// Swaps units between two slots
        /// </summary>
        public void SwapUnits(int slotId1, int slotId2)
        {
            if (currentFormation == null) return;

            var slot1 = currentFormation.GetSlot(slotId1);
            var slot2 = currentFormation.GetSlot(slotId2);

            if (slot1 == null || slot2 == null)
            {
                LogDebug("<color=red>Invalid slots for swap!</color>");
                return;
            }

            slot1.SwapWith(slot2);
            OnUnitsSwapped?.Invoke(slot1, slot2);
        }

        /// <summary>
        /// Auto-arranges units in formation
        /// </summary>
        public void AutoArrangeUnits()
        {
            if (currentFormation == null) return;

            // Clear current formation
            currentFormation.ClearAllUnits();

            // Place available units
            var emptySlots = currentFormation.GetEmptySlots();
            int unitIndex = 0;

            foreach (var slot in emptySlots)
            {
                if (unitIndex >= availableUnits.Count) break;
                
                slot.PlaceUnit(availableUnits[unitIndex]);
                unitIndex++;
            }

            UpdateRuntimeInfo();
            LogDebug($"<color=green>Auto-arranged {unitIndex} units</color>");
        }

        /// <summary>
        /// Clears all units from formation
        /// </summary>
        public void ClearFormation()
        {
            if (currentFormation == null) return;

            currentFormation.ClearAllUnits();
            UpdateRuntimeInfo();
            LogDebug("<color=yellow>Formation cleared</color>");
        }

        #endregion

        #region Info & Stats

        private void UpdateRuntimeInfo()
        {
            if (currentFormation == null)
            {
                unitsPlaced = 0;
                totalSlots = 0;
                return;
            }

            unitsPlaced = currentFormation.GetOccupiedSlots().Count;
            totalSlots = currentFormation.MaxUnits;
        }

        public void ShowFormationInfo()
        {
            if (currentFormation == null)
            {
                Debug.Log("<color=red>No formation selected!</color>");
                return;
            }

            Debug.Log("\n<color=cyan>═══════════════════════════════════════</color>");
            Debug.Log($"<color=yellow>📍 Formation: {currentFormation.FormationName}</color>");
            Debug.Log("<color=cyan>═══════════════════════════════════════</color>");
            Debug.Log($"Layout: {currentFormation.Layout}");
            Debug.Log($"Max Units: {currentFormation.MaxUnits}");
            Debug.Log($"Placed: {unitsPlaced}/{totalSlots}");
            Debug.Log($"Description: {currentFormation.Description}");
            
            Debug.Log($"\n<color=yellow>Slots:</color>");
            foreach (var slot in currentFormation.Slots)
            {
                string unit = slot.IsOccupied ? slot.OccupiedUnitId : "<empty>";
                Debug.Log($"  {slot.SlotName} ({slot.Row}): {unit}");
                if (slot.IsOccupied)
                {
                    Debug.Log($"    Bonuses: ATK+{slot.AttackBonus*100:F0}% DEF+{slot.DefenseBonus*100:F0}% SPD+{slot.SpeedBonus*100:F0}% CRIT+{slot.CritBonus*100:F0}%");
                }
            }
            
            Debug.Log("<color=cyan>═══════════════════════════════════════</color>\n");
        }

        private void LogDebug(string message)
        {
            if (debugMode)
            {
                Debug.Log($"<color=magenta>[Formation]</color> {message}");
            }
        }

        #endregion
    }
}
