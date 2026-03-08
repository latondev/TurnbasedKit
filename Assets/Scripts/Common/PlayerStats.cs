using System;
using UnityEngine;

namespace GameSystems.Common
{
    /// <summary>
    /// Unified Player Stats - kết hợp từ Equipment, Formation, và Pet
    /// </summary>
    [Serializable]
    public class PlayerStats
    {
        // Base stats (từ Equipment - flat values)
        public int BaseAttack;
        public int BaseDefense;
        public int BaseHealth;
        public int BaseMana;
        public float BaseCritRate;
        public float BaseCritDamage;
        public int BaseSpeed;

        // Multipliers (từ Formation + Pet - percentage)
        // Giá trị 1f = 100% (không nhân)
        // Giá trị 1.2f = 120% (+20%)
        public float AttackMultiplier = 1f;
        public float DefenseMultiplier = 1f;
        public float HealthMultiplier = 1f;
        public float ManaMultiplier = 1f;
        public float SpeedMultiplier = 1f;
        public float CritRateMultiplier = 1f;
        public float CritDamageMultiplier = 1f;

        // Constructor mặc định
        public PlayerStats()
        {
            SetDefaults();
        }

        public void SetDefaults()
        {
            BaseAttack = 0;
            BaseDefense = 0;
            BaseHealth = 0;
            BaseMana = 0;
            BaseCritRate = 0.05f;
            BaseCritDamage = 1.5f;
            BaseSpeed = 50;

            AttackMultiplier = 1f;
            DefenseMultiplier = 1f;
            HealthMultiplier = 1f;
            ManaMultiplier = 1f;
            SpeedMultiplier = 1f;
            CritRateMultiplier = 1f;
            CritDamageMultiplier = 1f;
        }

        // Tính final stats sau khi áp dụng multipliers
        public int FinalAttack => Mathf.RoundToInt(BaseAttack * AttackMultiplier);
        public int FinalDefense => Mathf.RoundToInt(BaseDefense * DefenseMultiplier);
        public int FinalHealth => Mathf.RoundToInt(BaseHealth * HealthMultiplier);
        public int FinalMana => Mathf.RoundToInt(BaseMana * ManaMultiplier);
        public int FinalSpeed => Mathf.RoundToInt(BaseSpeed * SpeedMultiplier);
        public float FinalCritRate => BaseCritRate * CritRateMultiplier;
        public float FinalCritDamage => BaseCritDamage * CritDamageMultiplier;

        // Debug
        public override string ToString()
        {
            return $"[PlayerStats] ATK: {FinalAttack} ({BaseAttack} x {AttackMultiplier:F2}) | " +
                   $"DEF: {FinalDefense} ({BaseDefense} x {DefenseMultiplier:F2}) | " +
                   $"HP: {FinalHealth} ({BaseHealth} x {HealthMultiplier:F2}) | " +
                   $"SPD: {FinalSpeed} ({BaseSpeed} x {SpeedMultiplier:F2}) | " +
                   $"CRIT: {FinalCritRate*100:F1}% ({BaseCritRate*100:F1}% x {CritRateMultiplier:F2})";
        }
    }
}
