using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameSystems.Equipment
{
    /// <summary>
    /// Represents an equipment set with bonuses
    /// </summary>
    [Serializable]
    public class EquipmentSet
    {
        [SerializeField] private string setName;
        [SerializeField] private string description;
        [SerializeField] private int totalPieces;
        
        // Set bonuses at different piece counts
        [SerializeField] private Dictionary<int, SetBonus> setBonuses;

        public string SetName => setName;
        public string Description => description;
        public int TotalPieces => totalPieces;

        public EquipmentSet(string name, string description, int totalPieces)
        {
            this.setName = name;
            this.description = description;
            this.totalPieces = totalPieces;
            this.setBonuses = new Dictionary<int, SetBonus>();
        }

        /// <summary>
        /// Adds a set bonus for specific piece count
        /// </summary>
        public void AddSetBonus(int pieceCount, string bonusDescription, int atk = 0, int def = 0, 
            int hp = 0, int mp = 0, float critRate = 0f, string special = "")
        {
            setBonuses[pieceCount] = new SetBonus
            {
                PieceCount = pieceCount,
                Description = bonusDescription,
                AttackBonus = atk,
                DefenseBonus = def,
                HealthBonus = hp,
                ManaBonus = mp,
                CritRateBonus = critRate,
                SpecialEffect = special
            };
        }

        /// <summary>
        /// Gets active bonuses for equipped piece count
        /// </summary>
        public List<SetBonus> GetActiveBonuses(int equippedCount)
        {
            List<SetBonus> active = new List<SetBonus>();
            
            foreach (var kvp in setBonuses)
            {
                if (equippedCount >= kvp.Key)
                {
                    active.Add(kvp.Value);
                }
            }
            
            return active;
        }

        /// <summary>
        /// Gets total stats from active bonuses
        /// </summary>
        public SetBonus GetTotalBonus(int equippedCount)
        {
            SetBonus total = new SetBonus();
            
            foreach (var kvp in setBonuses)
            {
                if (equippedCount >= kvp.Key)
                {
                    total.AttackBonus += kvp.Value.AttackBonus;
                    total.DefenseBonus += kvp.Value.DefenseBonus;
                    total.HealthBonus += kvp.Value.HealthBonus;
                    total.ManaBonus += kvp.Value.ManaBonus;
                    total.CritRateBonus += kvp.Value.CritRateBonus;
                }
            }
            
            return total;
        }
    }

    [Serializable]
    public class SetBonus
    {
        public int PieceCount;
        public string Description;
        public int AttackBonus;
        public int DefenseBonus;
        public int HealthBonus;
        public int ManaBonus;
        public float CritRateBonus;
        public string SpecialEffect;

        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append($"({PieceCount} pieces) ");
            
            if (AttackBonus > 0) sb.Append($"+{AttackBonus} ATK ");
            if (DefenseBonus > 0) sb.Append($"+{DefenseBonus} DEF ");
            if (HealthBonus > 0) sb.Append($"+{HealthBonus} HP ");
            if (ManaBonus > 0) sb.Append($"+{ManaBonus} MP ");
            if (CritRateBonus > 0) sb.Append($"+{CritRateBonus * 100:F0}% CRIT ");
            if (!string.IsNullOrEmpty(SpecialEffect)) sb.Append($"[{SpecialEffect}]");
            
            return sb.ToString().Trim();
        }
    }
}
