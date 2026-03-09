using System;
using System.Collections.Generic;
using UnityEngine;
using GameSystems.Skills;
using GameSystems.Common;
using GameSystems.Stats;

namespace GameSystems.AutoBattle
{
    /// <summary>
    /// Represents a unit in battle with calculated stats
    /// Uses StatsSystem package (UnitStatController)
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

        [Header("Stats System - Using StatsSystem Package")]
        [SerializeField] private UnitStatController statController;

        [Header("Battle Info")]
        [SerializeField] private int damageDealt;
        [SerializeField] private int damageTaken;
        [SerializeField] private int turnsTaken;
        [SerializeField] private List<string> actionsLog;

        // Properties using StatsSystem
        public string UnitId => unitId;
        public string UnitName => unitName;
        public UnitType Type => unitType;
        public AttackRange Range => attackRange;
        public bool IsAlive => isAlive;

        // Stats from UnitStatController
        public int CurrentHP => (int)statController.GetStatValue("hp");
        public int MaxHP => (int)statController.GetStatMaxValue("hp");
        public int FinalAttack => (int)statController.GetStatValue("attack");
        public int FinalDefense => (int)statController.GetStatValue("defense");
        public int FinalSpeed => (int)statController.GetStatValue("speed");
        public float CritRate => statController.GetStatValue("critical_rate");
        public float CritDamage => statController.GetStatValue("critical_damage");

        public int DamageDealt => damageDealt;
        public int DamageTaken => damageTaken;
        public int TurnsTaken => turnsTaken;
        public List<string> ActionsLog => actionsLog;
        public string SkillName => skillName;
        public bool IsSkillReady => currentCooldown <= 0;
        public int CurrentCooldown => currentCooldown;
        public SkillData EquippedSkill => equippedSkill;

        // Mana properties
        public int CurrentMana => (int)statController.GetStatValue("mp");
        public int MaxMana => (int)statController.GetStatMaxValue("mp");
        public bool HasMana => CurrentMana > 0;

        public UnitStatController StatController => statController;

        public BattleUnit(string id, string name, UnitType type, AttackRange range, int hp, int atk, int def, int spd,
            string skillName = "Power Strike", int skillDmgMult = 2, int skillCd = 3)
        {
            this.unitId = id;
            this.unitName = name;
            this.unitType = type;
            this.attackRange = range;

            // Create UnitStatController for this unit
            var go = new GameObject($"StatController_{id}");
            this.statController = go.AddComponent<UnitStatController>();
            statController.UnitName = name;

            // Setup stats using StatsSystem
            SetupStats(hp, atk, def, spd);

            this.skillName = skillName;
            this.skillDamageMultiplier = skillDmgMult;
            this.skillCooldown = skillCd;
            this.currentCooldown = 0;

            this.actionsLog = new List<string>();

            this.isAlive = true;
        }

        /// <summary>
        /// Setup stats using StatsSystem package
        /// </summary>
        private void SetupStats(int hp, int atk, int def, int spd)
        {
            var stats = statController.Stats;
            stats.ClearStats();

            // Vital stats
            stats.AddStat(new Stat("hp", "Health", StatType.Health, hp, hp, true, 0f));
            stats.AddStat(new Stat("mp", "Mana", StatType.Mana, 100, 100, true, 5f));

            // Combat stats
            stats.AddStat(new Stat("attack", "Attack", StatType.Attack, atk));
            stats.AddStat(new Stat("defense", "Defense", StatType.Defense, def));
            stats.AddStat(new Stat("speed", "Speed", StatType.Speed, spd));

            // Critical stats
            stats.AddStat(new Stat("critical_rate", "Critical Rate", StatType.CriticalRate, 5f));
            stats.AddStat(new Stat("critical_damage", "Critical Damage", StatType.CriticalDamage, 150f));
        }

        /// <summary>
        /// Applies equipment bonuses using StatsSystem modifiers
        /// </summary>
        public void ApplyEquipmentBonuses(int hp, int atk, int def, int spd)
        {
            var hpStat = statController.GetStat("hp");
            var atkStat = statController.GetStat("attack");
            var defStat = statController.GetStat("defense");
            var spdStat = statController.GetStat("speed");

            if (hpStat != null) hpStat.IncreaseMax(hp);
            if (atkStat != null) atkStat.ModifiableValue.InitialValue += atk;
            if (defStat != null) defStat.ModifiableValue.InitialValue += def;
            if (spdStat != null) spdStat.ModifiableValue.InitialValue += spd;

            LogAction($"Applied equipment bonuses: +{hp}HP +{atk}ATK +{def}DEF +{spd}SPD");
        }

        /// <summary>
        /// Applies skill bonuses using StatsSystem modifiers
        /// </summary>
        public void ApplySkillBonuses(int atkBonus, int defBonus, float crit, float critDmg)
        {
            var atkStat = statController.GetStat("attack");
            var defStat = statController.GetStat("defense");
            var critRateStat = statController.GetStat("critical_rate");
            var critDmgStat = statController.GetStat("critical_damage");

            if (atkStat != null) atkStat.ModifiableValue.InitialValue += atkBonus;
            if (defStat != null) defStat.ModifiableValue.InitialValue += defBonus;
            if (critRateStat != null) critRateStat.ModifiableValue.InitialValue += crit * 100f;
            if (critDmgStat != null) critDmgStat.ModifiableValue.InitialValue += (critDmg - 1f) * 100f;

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
            if (CurrentMana < manaCost) return false;

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
            if (CurrentMana < manaCost)
            {
                LogAction($"<color=red>Not enough mana! Need {manaCost}, have {CurrentMana}</color>");
                return 0;
            }

            // Check cooldown
            if (currentCooldown > 0)
            {
                LogAction($"<color=orange>Skill on cooldown: {currentCooldown} turns</color>");
                return 0;
            }

            // Deduct mana
            statController.ModifyStat("mp", -manaCost);

            // Use skill - set cooldown
            turnsTaken++;
            currentCooldown = skillCooldown;

            // Calculate damage using SkillData
            float skillDamage = equippedSkill.GetTotalDamage();
            int baseDamage = Mathf.Max(1, Mathf.RoundToInt(skillDamage) - (target.FinalDefense / 2));

            // Skills have higher crit chance
            bool isCrit = UnityEngine.Random.value < (CritRate * 2f);
            int finalDamage = isCrit ? Mathf.RoundToInt(baseDamage * CritDamage) : baseDamage;

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
            // Mana regenerates automatically via UnitStatController's regen timer
        }

        /// <summary>
        /// Sets mana values directly
        /// </summary>
        public void SetMana(int current, int max)
        {
            var mpStat = statController.GetStat("mp");
            if (mpStat != null)
            {
                mpStat.IncreaseMax(max - MaxMana);
                mpStat.SetCurrent(Mathf.Min(current, MaxMana));
            }
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
            int baseDamage = Mathf.Max(1, FinalAttack - target.FinalDefense);

            // Check for critical hit
            bool isCrit = UnityEngine.Random.value < CritRate;
            int finalDamage = isCrit ? Mathf.RoundToInt(baseDamage * CritDamage) : baseDamage;

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
            int baseDamage = Mathf.Max(1, (FinalAttack * skillDamageMultiplier) - (target.FinalDefense / 2));

            // Skills always have higher crit chance
            bool isCrit = UnityEngine.Random.value < (CritRate * 2f);
            int finalDamage = isCrit ? Mathf.RoundToInt(baseDamage * CritDamage) : baseDamage;

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
        /// Takes damage - uses StatsSystem
        /// </summary>
        public int TakeDamage(int damage)
        {
            if (!isAlive) return 0;

            int actualDamage = Mathf.Min(damage, CurrentHP);
            statController.ModifyStat("hp", -actualDamage);
            damageTaken += actualDamage;

            if (CurrentHP <= 0)
            {
                var hpStat = statController.GetStat("hp");
                if (hpStat != null) hpStat.SetCurrent(0);
                isAlive = false;
                LogAction($"<color=red>💀 {unitName} has been defeated!</color>");
            }

            return actualDamage;
        }

        /// <summary>
        /// Heals the unit - uses StatsSystem
        /// </summary>
        public int Heal(int amount)
        {
            if (!isAlive) return 0;

            int actualHeal = Mathf.Min(amount, MaxHP - CurrentHP);
            statController.ModifyStat("hp", amount);

            LogAction($"<color=green>Healed for {actualHeal} HP</color>");
            return actualHeal;
        }

        /// <summary>
        /// Gets HP percentage
        /// </summary>
        public float GetHPPercentage()
        {
            if (MaxHP <= 0) return 0f;
            return (float)CurrentHP / MaxHP;
        }

        /// <summary>
        /// Resets unit for new battle - uses StatsSystem
        /// </summary>
        public void Reset()
        {
            statController.RestoreAll();
            statController.ClearAllModifiers(); // Clear all buffs/debuffs
            isAlive = true;
            damageDealt = 0;
            damageTaken = 0;
            turnsTaken = 0;
            currentCooldown = 0;
            actionsLog.Clear();

            LogAction($"{unitName} [{attackRange}] ready for battle! HP:{CurrentHP}/{MaxHP} MP:{CurrentMana}/{MaxMana}");
        }

        #region Buff/Debuff Methods (using StatsSystem)

        /// <summary>
        /// Apply attack buff to this unit
        /// </summary>
        public void ApplyAttackBuff(float percentageBonus, int durationTurns)
        {
            var modifier = GameSystems.Stats.Modifier.Times(1f + percentageBonus, 0, "attack_buff");
            statController.AddModifier("attack", modifier);
            LogAction($"Applied Attack Buff: +{percentageBonus*100}% for {durationTurns} turns");
        }

        /// <summary>
        /// Apply defense buff to this unit
        /// </summary>
        public void ApplyDefenseBuff(float percentageBonus, int durationTurns)
        {
            var modifier = GameSystems.Stats.Modifier.Times(1f + percentageBonus, 0, "defense_buff");
            statController.AddModifier("defense", modifier);
            LogAction($"Applied Defense Buff: +{percentageBonus*100}% for {durationTurns} turns");
        }

        /// <summary>
        /// Apply attack debuff to this unit
        /// </summary>
        public void ApplyAttackDebuff(float percentagePenalty, int durationTurns)
        {
            var modifier = GameSystems.Stats.Modifier.Times(1f - percentagePenalty, 0, "attack_debuff");
            statController.AddModifier("attack", modifier);
            LogAction($"Applied Attack Debuff: -{percentagePenalty*100}% for {durationTurns} turns");
        }

        /// <summary>
        /// Clear all buffs/debuffs
        /// </summary>
        public void ClearAllBuffs()
        {
            statController.ClearAllModifiers();
            LogAction("Cleared all buffs/debuffs");
        }

        #endregion

        private void LogAction(string action)
        {
            actionsLog.Add(action);
        }

        public override string ToString()
        {
            string status = IsAlive ? $"HP: {CurrentHP}/{MaxHP}" : "💀 Defeated";
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
