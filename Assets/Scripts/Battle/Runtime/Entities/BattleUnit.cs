using System;
using System.Collections.Generic;
using UnityEngine;
using GameSystems.Skills;
using GameSystems.Common;
using GameSystems.Stats;
using GameSystems.Battle;

namespace GameSystems.AutoBattle
{
    /// <summary>
    /// Represents a unit in battle with calculated stats
    /// Uses StatsSystem package (UnitStatController)
    /// </summary>
    [Serializable]
    public class BattleUnit : IDisposable
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
        [SerializeField] private string skillName;

        [Header("Stats System - Using StatsSystem Package")]
        [SerializeField] private UnitStatController statController;

        [Header("Battle Info")]
        [SerializeField] private List<string> actionsLog;

        private readonly BattleUnitRuntimeService runtimeService;
        private readonly BattleUnitCombatService combatService;
        private readonly BattleUnitLogService logService;
        private readonly BattleUnitEventBridgeService eventBridgeService;

        // Properties using StatsSystem
        public string UnitId => unitId;
        public string UnitName => unitName;
        public UnitType Type => unitType;
        public AttackRange Range => attackRange;
        public bool IsAlive => isAlive;

        // Stats from UnitStatController
        public int CurrentHP => (int)(statController?.GetStatValue(StatType.Health) ?? 0f);
        public int MaxHP => (int)(statController?.GetStatMaxValue(StatType.Health) ?? 0f);
        public int FinalAttack => (int)(statController?.GetStatValue(StatType.Attack) ?? 0f);
        public int FinalDefense => (int)(statController?.GetStatValue(StatType.Defense) ?? 0f);
        public int FinalSpeed => (int)(statController?.GetStatValue(StatType.Speed) ?? 0f);
        public float CritRate => statController?.GetStatValue(StatType.CriticalRate) ?? 0f;
        public float CritDamage => statController?.GetStatValue(StatType.CriticalDamage) ?? 0f;

        public int DamageDealt => combatService?.DamageDealt ?? 0;
        public int DamageTaken => combatService?.DamageTaken ?? 0;
        public int TurnsTaken => runtimeService?.TurnsTaken ?? 0;
        public List<string> ActionsLog => logService?.Entries ?? (actionsLog ??= new List<string>());
        public string SkillName => skillName;
        public float SkillDamageMultiplier => skillDamageMultiplier <= 0 ? 1f : skillDamageMultiplier;
        public bool IsSkillReady => runtimeService?.IsSkillReady ?? true;
        public int CurrentCooldown => runtimeService?.CurrentCooldown ?? 0;
        public SkillData EquippedSkill => equippedSkill;
        public int SkillCooldown => skillCooldown;

        // Mana properties
        public int CurrentMana => (int)(statController?.GetStatValue(StatType.Mana) ?? 0f);
        public int MaxMana => (int)(statController?.GetStatMaxValue(StatType.Mana) ?? 0f);
        public bool HasMana => CurrentMana > 0;

        public UnitStatController StatController => statController;
        public BattleUnitRuntimeService RuntimeService => runtimeService;

        public event Action<BattleUnit, Stat> OnStatChanged;
        public event Action<BattleUnit, StatusEffect> OnStatusApplied;
        public event Action<BattleUnit, StatusEffect> OnStatusRemoved;
        public event Action<BattleUnit> OnTurnStarted;
        public event Action<BattleUnit> OnTurnEnded;
        public event Action<BattleUnit> OnDefeated;
        public event Action<BattleUnit> OnReset;
        public event Action<BattleUnit, int> OnCooldownChanged;

        /// <summary>
        /// Create BattleUnit from CharacterDataSO - direct integration with StatsSystem
        /// </summary>
        public BattleUnit(CharacterDataSO characterData, UnitType unitType, AttackRange attackRange)
        {
            if (characterData == null)
            {
                Debug.LogError("BattleUnit: characterData is null!");
                this.runtimeService = null;
                this.combatService = null;
                this.logService = null;
                this.eventBridgeService = null;
                return;
            }

            this.unitId = characterData.id.ToString();
            this.unitName = characterData.nameHero;
            this.unitType = unitType;
            this.attackRange = attackRange;

            // Create UnitStatController for this unit
            var go = new GameObject($"StatController_{unitId}");
            this.statController = go.AddComponent<UnitStatController>();
            this.runtimeService = new BattleUnitRuntimeService(this);
            this.combatService = new BattleUnitCombatService(this);
            this.logService = new BattleUnitLogService(actionsLog);
            this.eventBridgeService = new BattleUnitEventBridgeService(this);
            //statController.Level = characterData.level;

            // Setup stats directly from CharacterDataSO.stats (StatsSystem)
            SetupStatsFromCharacterData(characterData);
            runtimeService.Initialize(statController, go.AddComponent<StatusController>());
            eventBridgeService.Initialize(runtimeService);

            // Setup skills
            this.equippedSkill = characterData.skillBasic;
            this.skillName = characterData.skillBasic?.SkillName ?? "Basic Attack";
            this.skillCooldown = characterData.skillBasic != null ? Mathf.RoundToInt(characterData.skillBasic.BaseCooldown) : 0;

            this.actionsLog = logService.Entries;
            this.isAlive = true;
        }

        /// <summary>
        /// Setup stats directly from CharacterDataSO using List<StatData> - no manual conversion needed
        /// </summary>
        private void SetupStatsFromCharacterData(CharacterDataSO data)
        {
            if (statController == null)
            {
                Debug.LogError($"BattleUnit: statController missing for {unitName}");
                return;
            }

            var stats = statController.Stats;
            stats.ClearStats();

            // Use ToStatsList() to get runtime Stat objects directly from StatData
            var statList = data.ToStatsList();
            foreach (var stat in statList)
            {
                stats.AddStat(stat);
            }

            Debug.Log($"BattleUnit: Created {unitName} with stats from CharacterDataSO");
        }

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
            this.runtimeService = new BattleUnitRuntimeService(this);
            this.combatService = new BattleUnitCombatService(this);
            this.logService = new BattleUnitLogService(actionsLog);
            this.eventBridgeService = new BattleUnitEventBridgeService(this);

            // Setup stats using StatsSystem
            SetupStats(hp, atk, def, spd);
            runtimeService.Initialize(statController, go.AddComponent<StatusController>());
            eventBridgeService.Initialize(runtimeService);

            this.skillName = skillName;
            this.skillDamageMultiplier = skillDmgMult;
            this.skillCooldown = skillCd;

            this.actionsLog = logService.Entries;

            this.isAlive = true;
        }

        /// <summary>
        /// Setup stats using StatsSystem package
        /// </summary>
        private void SetupStats(int hp, int atk, int def, int spd)
        {
            if (statController == null)
            {
                Debug.LogError($"BattleUnit: statController missing for {unitName}");
                return;
            }

            var stats = statController.Stats;
            stats.ClearStats();

            // Vital stats
            stats.AddStat(new Stat(StatType.Health, hp, hp, true, 0f));
            stats.AddStat(new Stat(StatType.Mana, 100, 100, true, 5f));

            // Combat stats
            stats.AddStat(new Stat(StatType.Attack, atk));
            stats.AddStat(new Stat(StatType.Defense, def));
            stats.AddStat(new Stat(StatType.Speed, spd));

            // Critical stats
            stats.AddStat(new Stat(StatType.CriticalRate, 5f));
            stats.AddStat(new Stat(StatType.CriticalDamage, 150f));
        }

        /// <summary>
        /// Applies equipment bonuses using StatsSystem modifiers
        /// </summary>
        public void ApplyEquipmentBonuses(int hp, int atk, int def, int spd)
        {
            if (statController == null) return;

            var hpStat = statController.GetStat(StatType.Health);
            var atkStat = statController.GetStat(StatType.Attack);
            var defStat = statController.GetStat(StatType.Defense);
            var spdStat = statController.GetStat(StatType.Speed);

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
            if (statController == null) return;

            var atkStat = statController.GetStat(StatType.Attack);
            var defStat = statController.GetStat(StatType.Defense);
            var critRateStat = statController.GetStat(StatType.CriticalRate);
            var critDmgStat = statController.GetStat(StatType.CriticalDamage);

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
            if (!isAlive) return false;

            // Need a skill equipped
            if (equippedSkill == null) return false;

            if (runtimeService != null && !runtimeService.CanCastSkill()) return false;

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
            return combatService?.CastSkill(target) ?? 0;
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
            combatService?.SetMana(current, max);
        }

        /// <summary>
        /// Attacks another unit (normal attack)
        /// </summary>
        public int Attack(BattleUnit target)
        {
            return combatService?.Attack(target) ?? 0;
        }

        /// <summary>
        /// Takes damage - uses StatsSystem
        /// </summary>
        public int TakeDamage(int damage)
        {
            return combatService?.TakeDamage(damage) ?? 0;
        }

        /// <summary>
        /// Heals the unit - uses StatsSystem
        /// </summary>
        public int Heal(int amount)
        {
            return combatService?.Heal(amount) ?? 0;
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
            if (statController == null) return;

            isAlive = true;
            runtimeService?.ResetRuntime();
            combatService?.ResetCombat();
            if (logService != null)
            {
                logService.Clear();
            }
            else
            {
                actionsLog?.Clear();
            }

            LogAction($"{unitName} [{attackRange}] ready for battle! HP:{CurrentHP}/{MaxHP} MP:{CurrentMana}/{MaxMana}");
        }

        #region Buff/Debuff Methods (using StatsSystem)

        /// <summary>
        /// Apply attack buff to this unit
        /// </summary>
        public void ApplyAttackBuff(float percentageBonus, int durationTurns)
        {
            runtimeService?.ApplyTimedModifier(
                StatType.Attack,
                (IModifier<float>)GameSystems.Stats.TurnBasedModifierFactory.Times(1f + percentageBonus, durationTurns, priority: 0, name: "attack_buff"),
                durationTurns,
                StatusEffectType.AttackBuff,
                percentageBonus);

            LogAction($"Applied Attack Buff: +{percentageBonus*100}% for {durationTurns} turns");
        }

        /// <summary>
        /// Apply defense buff to this unit
        /// </summary>
        public void ApplyDefenseBuff(float percentageBonus, int durationTurns)
        {
            runtimeService?.ApplyTimedModifier(
                StatType.Defense,
                (IModifier<float>)GameSystems.Stats.TurnBasedModifierFactory.Times(1f + percentageBonus, durationTurns, priority: 0, name: "defense_buff"),
                durationTurns,
                StatusEffectType.DefenseBuff,
                percentageBonus);

            LogAction($"Applied Defense Buff: +{percentageBonus*100}% for {durationTurns} turns");
        }

        /// <summary>
        /// Apply attack debuff to this unit
        /// </summary>
        public void ApplyAttackDebuff(float percentagePenalty, int durationTurns)
        {
            runtimeService?.ApplyTimedModifier(
                StatType.Attack,
                (IModifier<float>)GameSystems.Stats.TurnBasedModifierFactory.Times(1f - percentagePenalty, durationTurns, priority: 0, name: "attack_debuff"),
                durationTurns,
                StatusEffectType.Weakness,
                percentagePenalty);

            LogAction($"Applied Attack Debuff: -{percentagePenalty*100}% for {durationTurns} turns");
        }

        public void ApplySpeedBuff(float percentageBonus, int durationTurns)
        {
            runtimeService?.ApplyTimedModifier(
                StatType.Speed,
                (IModifier<float>)GameSystems.Stats.TurnBasedModifierFactory.Times(1f + percentageBonus, durationTurns, priority: 0, name: "speed_buff"),
                durationTurns,
                StatusEffectType.SpeedBuff,
                percentageBonus);

            LogAction($"Applied Speed Buff: +{percentageBonus*100}% for {durationTurns} turns");
        }

        public void ApplySpeedDebuff(float percentagePenalty, int durationTurns)
        {
            runtimeService?.ApplyTimedModifier(
                StatType.Speed,
                (IModifier<float>)GameSystems.Stats.TurnBasedModifierFactory.Times(1f - percentagePenalty, durationTurns, priority: 0, name: "speed_debuff"),
                durationTurns,
                StatusEffectType.Slow,
                percentagePenalty);

            LogAction($"Applied Speed Debuff: -{percentagePenalty*100}% for {durationTurns} turns");
        }

        public StatusEffect ApplyStatusEffect(StatusEffectType type, int durationTurns, float value = 0f, int stacks = 1)
        {
            if (runtimeService == null || durationTurns <= 0)
            {
                return null;
            }

            var effect = runtimeService.ApplyStatusEffect(type, durationTurns, value, stacks);
            if (effect != null)
            {
                LogAction($"Applied status {type} for {durationTurns} turns");
            }

            return effect;
        }

        public void ApplyPoison(float damagePerTurn, int durationTurns)
        {
            ApplyStatusEffect(StatusEffectType.Poison, durationTurns, damagePerTurn);
        }

        public void ApplyBurn(float damagePerTurn, int durationTurns)
        {
            ApplyStatusEffect(StatusEffectType.Burn, durationTurns, damagePerTurn);
        }

        public void ApplyStun(int durationTurns)
        {
            ApplyStatusEffect(StatusEffectType.Stun, durationTurns);
        }

        public void ApplySilence(int durationTurns)
        {
            ApplyStatusEffect(StatusEffectType.Silence, durationTurns);
        }

        public bool HasStatus(StatusEffectType type)
        {
            return runtimeService != null && runtimeService.HasStatus(type);
        }

        public bool BeginTurn()
        {
            if (!isAlive)
            {
                return false;
            }

            return runtimeService?.BeginTurn() ?? true;
        }

        public void EndTurn()
        {
            if (!isAlive)
            {
                return;
            }

            runtimeService?.EndTurn();
        }

        /// <summary>
        /// Clear all buffs/debuffs
        /// </summary>
        public void ClearAllBuffs()
        {
            if (statController == null) return;

            runtimeService?.ClearAllEffects();
            LogAction("Cleared all buffs/debuffs");
        }

        #endregion

        public void Dispose()
        {
            isAlive = false;
            eventBridgeService?.Dispose();
            runtimeService?.Dispose();
            logService?.Dispose();
            OnStatChanged = null;
            OnStatusApplied = null;
            OnStatusRemoved = null;
            OnTurnStarted = null;
            OnTurnEnded = null;
            OnDefeated = null;
            OnReset = null;
            OnCooldownChanged = null;

            if (statController != null)
            {
                var statControllerGo = statController.gameObject;
                statController = null;

                if (statControllerGo != null)
                {
                    if (Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(statControllerGo);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(statControllerGo);
                    }
                }
            }
        }

        private void LogAction(string action)
        {
            if (logService != null)
            {
                logService.Add(action);
                return;
            }

            actionsLog ??= new List<string>();
            actionsLog.Add(action);
        }

        internal void LogBattleAction(string action)
        {
            LogAction(action);
        }

        internal void RaiseStatChanged(Stat stat) => OnStatChanged?.Invoke(this, stat);
        internal void RaiseStatusApplied(StatusEffect status) => OnStatusApplied?.Invoke(this, status);
        internal void RaiseStatusRemoved(StatusEffect status) => OnStatusRemoved?.Invoke(this, status);
        internal void RaiseTurnStarted() => OnTurnStarted?.Invoke(this);
        internal void RaiseTurnEnded() => OnTurnEnded?.Invoke(this);
        internal void RaiseDefeated()
        {
            isAlive = false;
            LogAction($"<color=red>💀 {unitName} has been defeated!</color>");
            OnDefeated?.Invoke(this);
        }
        internal void RaiseReset() => OnReset?.Invoke(this);
        internal void RaiseCooldownChanged(int cooldown) => OnCooldownChanged?.Invoke(this, cooldown);

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
