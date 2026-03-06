using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameSystems.Formation
{
    /// <summary>
    /// Represents a formation type/layout
    /// </summary>
    [Serializable]
    public class FormationType
    {
        [SerializeField] private string formationId;
        [SerializeField] private string formationName;
        [SerializeField] private string description;
        [SerializeField] private FormationLayout layout;
        [SerializeField] private int maxUnits;
        [SerializeField] private List<FormationSlot> slots;

        public string FormationId => formationId;
        public string FormationName => formationName;
        public string Description => description;
        public FormationLayout Layout => layout;
        public int MaxUnits => maxUnits;
        public List<FormationSlot> Slots => slots;

        public FormationType(string id, string name, string description, FormationLayout layout)
        {
            this.formationId = id;
            this.formationName = name;
            this.description = description;
            this.layout = layout;
            this.slots = new List<FormationSlot>();
            
            CreateSlots();
            this.maxUnits = slots.Count;
        }

        /// <summary>
        /// Creates slots based on layout
        /// </summary>
        private void CreateSlots()
        {
            slots.Clear();
            int slotId = 0;

            switch (layout)
            {
                case FormationLayout.Standard_3x3:
                    Create3x3Formation(ref slotId);
                    break;

                case FormationLayout.Offensive_2x4:
                    Create2x4Formation(ref slotId);
                    break;

                case FormationLayout.Defensive_4x2:
                    Create4x2Formation(ref slotId);
                    break;

                case FormationLayout.Balanced_2x3:
                    Create2x3Formation(ref slotId);
                    break;

                case FormationLayout.VFormation:
                    CreateVFormation(ref slotId);
                    break;
            }
        }

        private void Create3x3Formation(ref int slotId)
        {
            // Front row (3 slots)
            slots.Add(new FormationSlot(slotId++, "Front Left", SlotPosition.Left, SlotRow.Front, new Vector2(0, 0)));
            slots.Add(new FormationSlot(slotId++, "Front Center", SlotPosition.Center, SlotRow.Front, new Vector2(1, 0)));
            slots.Add(new FormationSlot(slotId++, "Front Right", SlotPosition.Right, SlotRow.Front, new Vector2(2, 0)));

            // Middle row (3 slots)
            slots.Add(new FormationSlot(slotId++, "Mid Left", SlotPosition.Left, SlotRow.Middle, new Vector2(0, 1)));
            slots.Add(new FormationSlot(slotId++, "Mid Center", SlotPosition.Center, SlotRow.Middle, new Vector2(1, 1)));
            slots.Add(new FormationSlot(slotId++, "Mid Right", SlotPosition.Right, SlotRow.Middle, new Vector2(2, 1)));

            // Back row (3 slots)
            slots.Add(new FormationSlot(slotId++, "Back Left", SlotPosition.Left, SlotRow.Back, new Vector2(0, 2)));
            slots.Add(new FormationSlot(slotId++, "Back Center", SlotPosition.Center, SlotRow.Back, new Vector2(1, 2)));
            slots.Add(new FormationSlot(slotId++, "Back Right", SlotPosition.Right, SlotRow.Back, new Vector2(2, 2)));
        }

        private void Create2x4Formation(ref int slotId)
        {
            // Offensive: 2 front, 4 back (more damage)
            slots.Add(new FormationSlot(slotId++, "Front Left", SlotPosition.Left, SlotRow.Front, new Vector2(0, 0)));
            slots.Add(new FormationSlot(slotId++, "Front Right", SlotPosition.Right, SlotRow.Front, new Vector2(1, 0)));

            slots.Add(new FormationSlot(slotId++, "Back 1", SlotPosition.Left, SlotRow.Back, new Vector2(0, 1)));
            slots.Add(new FormationSlot(slotId++, "Back 2", SlotPosition.Center, SlotRow.Back, new Vector2(1, 1)));
            slots.Add(new FormationSlot(slotId++, "Back 3", SlotPosition.Center, SlotRow.Back, new Vector2(0, 2)));
            slots.Add(new FormationSlot(slotId++, "Back 4", SlotPosition.Right, SlotRow.Back, new Vector2(1, 2)));
        }

        private void Create4x2Formation(ref int slotId)
        {
            // Defensive: 4 front, 2 back (more defense)
            slots.Add(new FormationSlot(slotId++, "Front 1", SlotPosition.Left, SlotRow.Front, new Vector2(0, 0)));
            slots.Add(new FormationSlot(slotId++, "Front 2", SlotPosition.Center, SlotRow.Front, new Vector2(1, 0)));
            slots.Add(new FormationSlot(slotId++, "Front 3", SlotPosition.Center, SlotRow.Front, new Vector2(0, 1)));
            slots.Add(new FormationSlot(slotId++, "Front 4", SlotPosition.Right, SlotRow.Front, new Vector2(1, 1)));

            slots.Add(new FormationSlot(slotId++, "Back Left", SlotPosition.Left, SlotRow.Back, new Vector2(0, 2)));
            slots.Add(new FormationSlot(slotId++, "Back Right", SlotPosition.Right, SlotRow.Back, new Vector2(1, 2)));
        }

        private void Create2x3Formation(ref int slotId)
        {
            // Balanced: 2 front, 3 back
            slots.Add(new FormationSlot(slotId++, "Front Left", SlotPosition.Left, SlotRow.Front, new Vector2(0, 0)));
            slots.Add(new FormationSlot(slotId++, "Front Right", SlotPosition.Right, SlotRow.Front, new Vector2(2, 0)));

            slots.Add(new FormationSlot(slotId++, "Back Left", SlotPosition.Left, SlotRow.Back, new Vector2(0, 2)));
            slots.Add(new FormationSlot(slotId++, "Back Center", SlotPosition.Center, SlotRow.Back, new Vector2(1, 2)));
            slots.Add(new FormationSlot(slotId++, "Back Right", SlotPosition.Right, SlotRow.Back, new Vector2(2, 2)));
        }

        private void CreateVFormation(ref int slotId)
        {
            // V shape: 1 front, 2 mid, 3 back
            slots.Add(new FormationSlot(slotId++, "Front Center", SlotPosition.Center, SlotRow.Front, new Vector2(1, 0)));

            slots.Add(new FormationSlot(slotId++, "Mid Left", SlotPosition.Left, SlotRow.Middle, new Vector2(0, 1)));
            slots.Add(new FormationSlot(slotId++, "Mid Right", SlotPosition.Right, SlotRow.Middle, new Vector2(2, 1)));

            slots.Add(new FormationSlot(slotId++, "Back Left", SlotPosition.Left, SlotRow.Back, new Vector2(0, 2)));
            slots.Add(new FormationSlot(slotId++, "Back Center", SlotPosition.Center, SlotRow.Back, new Vector2(1, 2)));
            slots.Add(new FormationSlot(slotId++, "Back Right", SlotPosition.Right, SlotRow.Back, new Vector2(2, 2)));
        }

        /// <summary>
        /// Gets slot by ID
        /// </summary>
        public FormationSlot GetSlot(int slotId)
        {
            return slots.Find(s => s.SlotId == slotId);
        }

        /// <summary>
        /// Gets occupied slots
        /// </summary>
        public List<FormationSlot> GetOccupiedSlots()
        {
            return slots.FindAll(s => s.IsOccupied);
        }

        /// <summary>
        /// Gets empty slots
        /// </summary>
        public List<FormationSlot> GetEmptySlots()
        {
            return slots.FindAll(s => !s.IsOccupied);
        }

        /// <summary>
        /// Clears all units from formation
        /// </summary>
        public void ClearAllUnits()
        {
            foreach (var slot in slots)
            {
                if (slot.IsOccupied)
                {
                    slot.RemoveUnit();
                }
            }
        }

        public override string ToString()
        {
            int occupied = GetOccupiedSlots().Count;
            return $"{formationName} ({layout}) - {occupied}/{maxUnits} units";
        }

        public Color GetLayoutColor()
        {
            return layout switch
            {
                FormationLayout.Standard_3x3 => new Color(0.7f, 0.7f, 0.7f),
                FormationLayout.Offensive_2x4 => new Color(1f, 0.5f, 0.5f),
                FormationLayout.Defensive_4x2 => new Color(0.5f, 0.7f, 1f),
                FormationLayout.Balanced_2x3 => new Color(0.5f, 1f, 0.7f),
                FormationLayout.VFormation => new Color(1f, 0.7f, 0.5f),
                _ => Color.white
            };
        }
    }

    public enum FormationLayout
    {
        Standard_3x3,     // 3 front, 3 mid, 3 back (9 units)
        Offensive_2x4,    // 2 front, 4 back (6 units)
        Defensive_4x2,    // 4 front, 2 back (6 units)
        Balanced_2x3,     // 2 front, 3 back (5 units)
        VFormation        // 1 front, 2 mid, 3 back (6 units)
    }
}
