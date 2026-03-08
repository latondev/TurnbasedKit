using System;
using System.Collections.Generic;
using UnityEngine;
using GameSystems.Skills;
using GameSystems.Common;

namespace GameSystems.AutoBattle
{
    /// <summary>
    /// Represents a unit in battle with calculated stats
    /// </summary>
    [Serializable]
    public class BattleUnit
    {
        [SerializeField] private string unitId;
        [SerializeField] private string unitName;
        [SerializeField] private UnitType unitType;
        [SerializeField] private AttackRange attackRange;
        [SerializeField] private bool isAlive;

        [Header("Skill System")]
        [SerializeField] private SkillData equippedSkill;
        [SerializeField] private int skillDamageMultiplier;
        [SerializeField] private int skillCooldown;
        [SerializeField] private int currentCooldown;
        [SerializeField] private string skillName;

        [Header("Mana")]
        [SerializeField] private int currentMana;
        [SerializeField] private int maxMana;
        [SerializeField] private int manaRegen;

        [Header("Base Stats")]
        [SerializeField] private int baseHP;
        [SerializeField] private int baseAttack;
        [SerializeField] private int baseDefense;
        [SerializeField] private int baseSpeed;
        
        [Header("Equipment Bonuses")]
        [SerializeField] private int equipmentHP;
        [SerializeField] private int equipmentAttack;
        [SerializeField] private int equipmentDefense;
        [SerializeField] private int equipmentSpeed;
        [SerializeField] private int equipmentMana;

        [Header("Player Stats (Unified)")]
        [SerializeField] private PlayerStats playerStats;
        
        [Header("Skill Bonuses")]
        [SerializeField] private int skillAttackBonus;
        [SerializeField] private int skillDefenseBonus;
        [SerializeField] private float critRate;
        [SerializeField] private float critDamage;
        
        [Header("Final Stats")]
        [SerializeField] private int currentHP;
        [SerializeField] private int maxHP;
        [SerializeField] private int finalAttack;
        [SerializeField] private int finalDefense;
        [SerializeField] private int finalSpeed;
        
        [Header("Battle Info")]
        [SerializeField] private int damageDealt;
        [SerializeField] private int damageTaken;
        [SerializeField] private int turnsTaken;
        [SerializeField] private List<string> actionsLog;

        // Properties
        public string UnitId => unitId;
        public string UnitName => unitName;
        public UnitType Type => unitType;
        public AttackRange Range => attackRange;
        public bool IsAlive => isAlive;
        public int CurrentHP => currentHP;
        public int MaxHP => maxHP;
        public int FinalAttack => finalAttack;
        public int FinalDefense => finalDefense;
        public int FinalSpeed => finalSpeed;
        public float CritRate => critRate;
        public float CritDamage => critDamage;
        public int DamageDealt => damageDealt;
        public int DamageTaken => damageTaken;
        public int TurnsTaken => turnsTaken;
        public List<string> ActionsLog => actionsLog;
        public string SkillName => skillName;
        public bool IsSkillReady => currentCooldown <= 0;
        public int CurrentCooldown => currentCooldown;
        public SkillData EquippedSkill => equippedSkill;

        // Mana properties
        public int CurrentMana => currentMana;
        public int MaxMana => maxMana;
        public bool HasMana => currentMana > 0;
        public PlayerStats PlayerStats => playerStats;

        public BattleUnit(string id, string name, UnitType type, AttackRange range, int hp, int atk, int def, int spd,
            string skillName = "Power Strike", int skillDmgMult = 2, int skillCd = 3)
        {
            this.unitId = id;
            this.unitName = name;
            this.unitType = type;
            this.attackRange = range;
            
            this.baseHP = hp;
            this.baseAttack = atk;
            this.baseDefense = def;
            this.baseSpeed = spd;

            this.equipmentHP = 0;
            this.equipmentAttack = 0;
            this.equipmentDefense = 0;
            this.equipmentSpeed = 0;
            this.equipmentMana = 0;

            // Mana initialization
            this.maxMana = 100;
            this.currentMana = maxMana;
            this.manaRegen = 5;

            this.playerStats = new PlayerStats();

            this.skillAttackBonus = 0;
            this.skillDefenseBonus = 0;
            this.critRate = 0.05f;
            this.critDamage = 1.5f;
            
            this.skillName = skillName;
            this.skillDamageMultiplier = skillDmgMult;
            this.skillCooldown = skillCd;
            this.currentCooldown = 0;
            
            this.actionsLog = new List<string>();
            
            CalculateFinalStats();
            this.currentHP = maxHP;
            this.isAlive = true;
        }

        /// <summary>
        /// Calculates final stats from base + equipment + skills + player stats (unified)
        /// </summary>
        public void CalculateFinalStats()
        {
            // Apply PlayerStats multipliers if available
            if (playerStats != null)
            {
                maxHP = Mathf.RoundToInt((baseHP + equipmentHP) * playerStats.HealthMultiplier);
                finalAttack = Mathf.RoundToInt((baseAttack + equipmentAttack + skillAttackBonus) * playerStats.AttackMultiplier);
                finalDefense = Mathf.RoundToInt((baseDefense + equipmentDefense + skillDefenseBonus) * playerStats.DefenseMultiplier);
                finalSpeed = Mathf.RoundToInt((baseSpeed + equipmentSpeed) * playerStats.SpeedMultiplier);
                maxMana = Mathf.RoundToInt((maxMana + equipmentMana) * playerStats.ManaMultiplier);

                // Apply crit multipliers
                critRate = Mathf.Clamp01(playerStats.BaseCritRate * playerStats.CritRateMultiplier);
                critDamage = playerStats.BaseCritDamage * playerStats.CritDamageMultiplier;
            }
            else
            {
                // Fallback: original calculation without multipliers
                maxHP = baseHP + equipmentHP;
                finalAttack = baseAttack + equipmentAttack + skillAttackBonus;
                finalDefense = baseDefense + equipmentDefense + skillDefenseBonus;
                finalSpeed = baseSpeed + equipmentSpeed;
            }

            // Ensure minimum values
            maxHP = Mathf.Max(1, maxHP);
            finalAttack = Mathf.Max(1, finalAttack);
            finalDefense = Mathf.Max(0, finalDefense);
            finalSpeed = Mathf.Max(1, finalSpeed);
            maxMana = Mathf.Max(1, maxMana);

            currentHP = Mathf.Min(currentHP, maxHP);
            currentMana = Mathf.Min(currentMana, maxMana);
        }

        /// <summary>
        /// Applies unified PlayerStats from Equipment + Formation + Pet
        /// </summary>
        public void ApplyPlayerStats(PlayerStats stats)
        {
            this.playerStats = stats;
            CalculateFinalStats();
            LogAction($"Applied PlayerStats: {stats}");
        }

        /// <summary>
        /// Applies equipment bonuses
        /// </summary>
        public void ApplyEquipmentBonuses(int hp, int atk, int def, int spd)
        {
            equipmentHP = hp;
            equipmentAttack = atk;
            equipmentDefense = def;
            equipmentSpeed = spd;
            
            CalculateFinalStats();
            LogAction($"Applied equipment bonuses: +{hp}HP +{atk}ATK +{def}DEF +{spd}SPD");
        }

        /// <summary>
        /// Applies skill bonuses
        /// </summary>
        public void ApplySkillBonuses(int atkBonus, int defBonus, float crit, float critDmg)
        {
            skillAttackBonus = atkBonus;
            skillDefenseBonus = defBonus;
            critRate = crit;
            critDamage = critDmg;

            CalculateFinalStats();
            LogAction($"Applied skill bonuses: +{atkBonus}ATK +{defBonus}DEF {crit*100:F0}%CRIT");
        }

        /// <summary>
        /// Equips a skill to this unit (before battle)
        /// </summary>
        public void EquipSkill(SkillData skill)
        {
            this.equippedSkill = skill;
            if (skill != null)
            {
                this.skillName = skill.SkillName;
                this.skillCooldown = Mathf.RoundToInt(skill.BaseCooldown);
                LogAction($"Equipped skill: {skill.SkillName}");
            }
        }

        /// <summary>
        /// Checks if skill can be cast (has skill + enough mana + not on cooldown)
        /// </summary>
        public bool CanCastSkill()
        {
            // Need a skill equipped
            if (equippedSkill == null) return false;

            // Check cooldown (from SkillData or local cooldown)
            if (currentCooldown > 0) return false;

            // Check mana
            int manaCost = equippedSkill != null ? equippedSkill.GetScaledManaCost() : 0;
            if (currentMana < manaCost) return false;

            return true;
        }

        /// <summary>
        /// Casts the equipped skill on target
        /// </summary>
        public int CastSkill(BattleUnit target)
        {
            if (!isAlive) return 0;
            if (equippedSkill == null)
            {
                LogAction("<color=red>No skill equipped!</color>");
                return 0;
            }

            int manaCost = equippedSkill.GetScaledManaCost();

            // Check mana
            if (currentMana < manaCost)
            {
                LogAction($"<color=red>Not enough mana! Need {manaCost}, have {currentMana}</color>");
                return 0;
            }

            // Check cooldown
            if (currentCooldown > 0)
            {
                LogAction($"<color=orange>Skill on cooldown: {currentCooldown} turns</color>");
                return 0;
            }

            // Deduct mana
            currentMana -= manaCost;

            // Use skill - set cooldown
            turnsTaken++;
            currentCooldown = skillCooldown;

            // Calculate damage using SkillData
            float skillDamage = equippedSkill.GetTotalDamage();
            int baseDamage = Mathf.Max(1, Mathf.RoundToInt(skillDamage) - (target.finalDefense / 2));

            // Skills have higher crit chance
            bool isCrit = UnityEngine.Random.value < (critRate * 2f);
            int finalDamage = isCrit ? Mathf.RoundToInt(baseDamage * critDamage) : baseDamage;

            int actualDamage = target.TakeDamage(finalDamage);
            damageDealt += actualDamage;

            string critText = isCrit ? " [CRIT!]" : "";
            LogAction($"💥 Used [{equippedSkill.SkillName}] on {target.unitName} for {actualDamage} damage{critText}");
            target.LogAction($"Hit by [{equippedSkill.SkillName}] from {unitName} for {actualDamage}{critText}");

            return actualDamage;
        }

        /// <summary>
        /// Regenerates mana at end of turn
        /// </summary>
        public void RegenerateMana()
        {
            if (!isAlive) return;
            currentMana = Mathf.Min(currentMana + manaRegen, maxMana);
        }

        /// <summary>
        /// Sets mana values directly
        /// </summary>
        public void SetMana(int current, int max)
        {
            this.maxMana = max;
            this.currentMana = Mathf.Min(current, max);
        }

        /// <summary>
        /// Attacks another unit (normal attack)
        /// </summary>
        public int Attack(BattleUnit target)
        {
            if (!isAlive) return 0;
            
            turnsTaken++;
            ReduceCooldown();
            
            // Calculate base damage
            int baseDamage = Mathf.Max(1, finalAttack - target.finalDefense);
            
            // Check for critical hit
            bool isCrit = UnityEngine.Random.value < critRate;
            int finalDamage = isCrit ? Mathf.RoundToInt(baseDamage * critDamage) : baseDamage;
            
            // Apply damage to target
            int actualDamage = target.TakeDamage(finalDamage);
            damageDealt += actualDamage;
            
            string critText = isCrit ? " [CRIT!]" : "";
            string rangeText = attackRange == AttackRange.Ranged ? "🏹" : "⚔️";
            LogAction($"{rangeText} Attacked {target.unitName} for {actualDamage} damage{critText}");
            target.LogAction($"Took {actualDamage} damage from {unitName}{critText}");
            
            return actualDamage;
        }

        /// <summary>
        /// Uses skill attack on target (stronger, has cooldown)
        /// </summary>
        public int SkillAttack(BattleUnit target)
        {
            if (!isAlive || !IsSkillReady) return 0;
            
            turnsTaken++;
            currentCooldown = skillCooldown; // set cooldown
            
            // Skill does multiplied damage, ignores part of defense
            int baseDamage = Mathf.Max(1, (finalAttack * skillDamageMultiplier) - (target.finalDefense / 2));
            
            // Skills always have higher crit chance
            bool isCrit = UnityEngine.Random.value < (critRate * 2f);
            int finalDamage = isCrit ? Mathf.RoundToInt(baseDamage * critDamage) : baseDamage;
            
            int actualDamage = target.TakeDamage(finalDamage);
            damageDealt += actualDamage;
            
            string critText = isCrit ? " [CRIT!]" : "";
            LogAction($"💥 Used [{skillName}] on {target.unitName} for {actualDamage} damage{critText}");
            target.LogAction($"Hit by [{skillName}] from {unitName} for {actualDamage}{critText}");
            
            return actualDamage;
        }

        private void ReduceCooldown()
        {
            if (currentCooldown > 0) currentCooldown--;
        }

        /// <summary>
        /// Takes damage
        /// </summary>
        public int TakeDamage(int damage)
        {
            if (!isAlive) return 0;
            
            int actualDamage = Mathf.Min(damage, currentHP);
            currentHP -= actualDamage;
            damageTaken += actualDamage;
            
            if (currentHP <= 0)
            {
                currentHP = 0;
                isAlive = false;
                LogAction($"<color=red>💀 {unitName} has been defeated!</color>");
            }
            
            return actualDamage;
        }

        /// <summary>
        /// Heals the unit
        /// </summary>
        public int Heal(int amount)
        {
            if (!isAlive) return 0;
            
            int actualHeal = Mathf.Min(amount, maxHP - currentHP);
            currentHP += actualHeal;
            
            LogAction($"<color=green>Healed for {actualHeal} HP</color>");
            return actualHeal;
        }

        /// <summary>
        /// Gets HP percentage
        /// </summary>
        public float GetHPPercentage()
        {
            if (maxHP <= 0) return 0f;
            return (float)currentHP / maxHP;
        }

        /// <summary>
        /// Resets unit for new battle
        /// </summary>
        public void Reset()
        {
            currentHP = maxHP;
            currentMana = maxMana;
            isAlive = true;
            damageDealt = 0;
            damageTaken = 0;
            turnsTaken = 0;
            currentCooldown = 0;
            actionsLog.Clear();

            LogAction($"{unitName} [{attackRange}] ready for battle! HP:{currentHP}/{maxHP} MP:{currentMana}/{maxMana}");
        }

        private void LogAction(string action)
        {
            actionsLog.Add(action);
        }

        public override string ToString()
        {
            string status = isAlive ? $"HP: {currentHP}/{maxHP}" : "💀 Defeated";
            return $"{unitName} ({unitType}) - {status}";
        }

        public Color GetUnitColor()
        {
            return unitType switch
            {
                UnitType.Player => new Color(0.3f, 0.7f, 1f),
                UnitType.Enemy => new Color(1f, 0.3f, 0.3f),
                UnitType.Boss => new Color(0.8f, 0.2f, 0.8f),
                _ => Color.white
            };
        }
    }

    public enum UnitType
    {
        Player,
        Enemy,
        Boss,
        Ally
    }

    public enum AttackRange
    {
        Melee,
        Ranged
    }
}
