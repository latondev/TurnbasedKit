using System;
using UnityEngine;

namespace GameSystems.Formation
{
    /// <summary>
    /// Represents a slot in formation
    /// </summary>
    [Serializable]
    public class FormationSlot
    {
        [SerializeField] private int slotId;
        [SerializeField] private string slotName;
        [SerializeField] private SlotPosition position;
        [SerializeField] private SlotRow row;
        [SerializeField] private Vector2 gridPosition; // Grid coordinates
        [SerializeField] private bool isOccupied;
        [SerializeField] private string occupiedUnitId;
        
        [Header("Position Bonuses")]
        [SerializeField] private float attackBonus;
        [SerializeField] private float defenseBonus;
        [SerializeField] private float speedBonus;
        [SerializeField] private float critBonus;

        public int SlotId => slotId;
        public string SlotName => slotName;
        public SlotPosition Position => position;
        public SlotRow Row => row;
        public Vector2 GridPosition => gridPosition;
        public bool IsOccupied => isOccupied;
        public string OccupiedUnitId => occupiedUnitId;
        public float AttackBonus => attackBonus;
        public float DefenseBonus => defenseBonus;
        public float SpeedBonus => speedBonus;
        public float CritBonus => critBonus;

        public FormationSlot(int id, string name, SlotPosition position, SlotRow row, Vector2 gridPos)
        {
            this.slotId = id;
            this.slotName = name;
            this.position = position;
            this.row = row;
            this.gridPosition = gridPos;
            this.isOccupied = false;
            this.occupiedUnitId = "";
            
            ApplyPositionBonuses();
        }

        /// <summary>
        /// Applies bonuses based on position
        /// </summary>
        private void ApplyPositionBonuses()
        {
            // Front row = more defense, less damage
            // Back row = more damage, less defense
            
            switch (row)
            {
                case SlotRow.Front:
                    attackBonus = 0f;
                    defenseBonus = 0.2f; // +20% defense
                    speedBonus = -0.1f; // -10% speed
                    critBonus = 0f;
                    break;
                    
                case SlotRow.Middle:
                    attackBonus = 0.1f; // +10% attack
                    defenseBonus = 0.1f; // +10% defense
                    speedBonus = 0f;
                    critBonus = 0.05f; // +5% crit
                    break;
                    
                case SlotRow.Back:
                    attackBonus = 0.2f; // +20% attack
                    defenseBonus = 0f;
                    speedBonus = 0.1f; // +10% speed
                    critBonus = 0.1f; // +10% crit
                    break;
            }
        }

        /// <summary>
        /// Places unit in this slot
        /// </summary>
        public bool PlaceUnit(string unitId)
        {
            if (isOccupied)
            {
                Debug.LogWarning($"Slot {slotName} is already occupied!");
                return false;
            }

            occupiedUnitId = unitId;
            isOccupied = true;
            Debug.Log($"<color=green>Placed unit {unitId} in slot {slotName}</color>");
            return true;
        }

        /// <summary>
        /// Removes unit from slot
        /// </summary>
        public void RemoveUnit()
        {
            if (!isOccupied)
            {
                Debug.LogWarning($"Slot {slotName} is empty!");
                return;
            }

            Debug.Log($"<color=yellow>Removed unit {occupiedUnitId} from slot {slotName}</color>");
            occupiedUnitId = "";
            isOccupied = false;
        }

        /// <summary>
        /// Swaps unit with another slot
        /// </summary>
        public void SwapWith(FormationSlot other)
        {
            string tempId = occupiedUnitId;
            bool tempOccupied = isOccupied;

            occupiedUnitId = other.occupiedUnitId;
            isOccupied = other.isOccupied;

            other.occupiedUnitId = tempId;
            other.isOccupied = tempOccupied;

            Debug.Log($"<color=cyan>Swapped {slotName} ↔ {other.slotName}</color>");
        }

        public override string ToString()
        {
            string unit = isOccupied ? occupiedUnitId : "Empty";
            return $"{slotName} ({row}, {position}) - {unit}";
        }

        public Color GetRowColor()
        {
            return row switch
            {
                SlotRow.Front => new Color(1f, 0.5f, 0.5f),
                SlotRow.Middle => new Color(1f, 0.9f, 0.5f),
                SlotRow.Back => new Color(0.5f, 0.9f, 1f),
                _ => Color.white
            };
        }

        public string GetPositionIcon()
        {
            return position switch
            {
                SlotPosition.Left => "◀",
                SlotPosition.Center => "●",
                SlotPosition.Right => "▶",
                _ => "○"
            };
        }
    }

    public enum SlotPosition
    {
        Left,
        Center,
        Right
    }

    public enum SlotRow
    {
        Front,
        Middle,
        Back
    }
}
