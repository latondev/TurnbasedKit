using System;
using UnityEngine;
using GameSystems.Pet;

namespace GameSystems.Common
{
    /// <summary>
    /// Calculator để tổng hợp stats từ Equipment + Formation + Pet + GameState
    /// </summary>
    public class PlayerStatsCalculator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameSystems.Equipment.EquipmentController equipment;
        [SerializeField] private GameSystems.Formation.FormationController formation;
        [SerializeField] private GameSystems.Pet.PetManager pet;
        [SerializeField] private GameStateManager gameState;

        // Singleton pattern
        public static PlayerStatsCalculator Instance { get; private set; }

        void Awake()
        {
            if (Instance == null) Instance = this;
        }

        /// <summary>
        /// Tính tổng stats từ tất cả sources
        /// </summary>
        public PlayerStats CalculateTotalStats()
        {
            var result = new PlayerStats();

            // 1. Lấy base stats từ Equipment (flat values)
            if (equipment != null)
            {
                var equipStats = equipment.GetTotalStatsWithSetBonuses();
                result.BaseAttack = equipStats.Attack;
                result.BaseDefense = equipStats.Defense;
                result.BaseHealth = equipStats.Health;
                result.BaseMana = equipStats.Mana;
                result.BaseCritRate = equipStats.CritRate;
                result.BaseCritDamage = equipStats.CritDamage;
                result.BaseSpeed = equipStats.Speed;
            }

            // 1.5. Lấy bonus stats từ GameState (level ups)
            if (gameState != null)
            {
                result.BaseAttack += gameState.BonusAttack;
                result.BaseDefense += gameState.BonusDefense;
                result.BaseHealth += gameState.BonusHealth;
                result.BaseSpeed += gameState.BonusSpeed;
            }

            // 2. Lấy multipliers từ Formation (%)
            float atkMult = 1f;
            float defMult = 1f;
            float hpMult = 1f;
            float spdMult = 1f;
            float critMult = 1f;

            if (formation != null && formation.CurrentFormation != null)
            {
                var occupiedSlots = formation.CurrentFormation.GetOccupiedSlots();
                foreach (var slot in occupiedSlots)
                {
                    // Slot bonuses là percentage (0.2f = +20%)
                    atkMult += slot.AttackBonus;
                    defMult += slot.DefenseBonus;
                    // Formation không có HP multiplier trực tiếp
                    spdMult += slot.SpeedBonus;
                    critMult += slot.CritBonus;
                }
            }

            result.AttackMultiplier = atkMult;
            result.DefenseMultiplier = defMult;
            result.HealthMultiplier = hpMult;
            result.SpeedMultiplier = spdMult;
            result.CritRateMultiplier = critMult;

            // 3. Lấy multipliers từ Pet (%)
            if (pet != null)
            {
                var petBuffs = pet.GetActivePetBuffs();
                // Pet buffs là % (vd: 10f = 10%)
                result.AttackMultiplier *= (1f + petBuffs.atkBonus / 100f);
                result.HealthMultiplier *= (1f + petBuffs.hpBonus / 100f);
                // Không có SPD, CRIT bonus từ pet trong PetStats
            }

            return result;
        }

        /// <summary>
        /// Tính stats cho một unit cụ thể (áp dụng formation slot của unit đó)
        /// </summary>
        public PlayerStats CalculateStatsForUnit(string unitId, string unitType)
        {
            var result = CalculateTotalStats();

            // Nếu là player unit, có thể áp dụng slot-specific bonuses
            // TODO: Implement nếu cần thiết

            return result;
        }

        /// <summary>
        /// Debug - hiển thị tất cả stats breakdown
        /// </summary>
        [ContextMenu("Debug Stats Breakdown")]
        public void DebugStatsBreakdown()
        {
            var stats = CalculateTotalStats();

            Debug.Log("<color=cyan>═══════════ Player Stats Breakdown ═══════════</color>");

            // Equipment
            if (equipment != null)
            {
                var equipStats = equipment.GetTotalStatsWithSetBonuses();
                Debug.Log($"<color=yellow>📦 Equipment:</color> ATK:{equipStats.Attack} DEF:{equipStats.Defense} HP:{equipStats.Health}");
            }

            // GameState (Level bonuses)
            if (gameState != null)
            {
                Debug.Log($"<color=yellow>⭐ GameState:</color> Lv.{gameState.PlayerLevel}");
                Debug.Log($"  Bonus: ATK:+{gameState.BonusAttack} DEF:+{gameState.BonusDefense} HP:+{gameState.BonusHealth} SPD:+{gameState.BonusSpeed}");
            }

            // Formation
            if (formation != null && formation.CurrentFormation != null)
            {
                var slots = formation.CurrentFormation.GetOccupiedSlots();
                Debug.Log($"<color=yellow>📍 Formation:</color> {slots.Count} slots occupied");
                Debug.Log($"  Multipliers: ATK x{stats.AttackMultiplier:F2} | DEF x{stats.DefenseMultiplier:F2} | SPD x{stats.SpeedMultiplier:F2}");
            }

            // Pet
            if (pet != null && pet.ActivePet != null)
            {
                var petBuffs = pet.GetActivePetBuffs();
                Debug.Log($"<color=yellow>🐾 Pet:</color> {pet.ActivePet.petId}");
                Debug.Log($"  Buffs: ATK +{petBuffs.atkBonus}% | HP +{petBuffs.hpBonus}%");
            }

            // Final
            Debug.Log($"<color=green>✅ Final Stats:</color> {stats}");

            Debug.Log("<color=cyan>═════════════════════════════════════════════</color>");
        }
    }
}
