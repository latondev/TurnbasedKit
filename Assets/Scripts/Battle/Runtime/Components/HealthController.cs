using System;
using UnityEngine;
using GameSystems.Stats;

namespace GameSystems.Battle
{
    /// <summary>
    /// Health Controller - manages HP, MP, Shield using StatsSystem package
    /// </summary>
    public class HealthController : MonoBehaviour
    {
        public Action OnDie;

        [Header("Stats System")]
        [SerializeField] private UnitStatController statController;

        // Properties - delegate to StatsSystem
        public float Health => statController?.GetStatValue(StatType.Health) ?? 0;
        public float Shield => statController?.GetStatValue(StatType.Shield) ?? 0;
        public int Mana => (int)(statController?.GetStatValue(StatType.Mana) ?? 0);
        public bool IsDead => statController?.IsDead() ?? false;
        public float MaxHealth => statController?.GetStatMaxValue(StatType.Health) ?? 0;
        public int MaxMana => (int)(statController?.GetStatMaxValue(StatType.Mana) ?? 0);
        public float MaxShield => statController?.GetStatMaxValue(StatType.Shield) ?? 0;

        public UnitStatController StatController => statController;

        private void Awake()
        {
            if (statController == null)
            {
                var go = new GameObject("HealthStats");
                go.transform.SetParent(transform);
                statController = go.AddComponent<UnitStatController>();
            }
        }

        public void AddMana(int value)
        {
            statController?.ModifyStat(StatType.Mana, value);
        }

        public void ChangeHealth(float value)
        {
            if (statController == null) return;

            // Shield absorbs damage first
            if (value < 0)
            {
                float shield = statController.GetStatValue(StatType.Shield);
                if (shield > 0)
                {
                    float remainingDamage = Mathf.Abs(value);
                    var shieldStat = statController.GetStat(StatType.Shield);

                    if (shield >= remainingDamage)
                    {
                        shieldStat.SetCurrent(shield - remainingDamage);
                        return;
                    }
                    else
                    {
                        shieldStat.SetCurrent(0);
                        remainingDamage -= shield;
                        value = -remainingDamage;
                    }
                }
            }

            statController.ModifyStat(StatType.Health, value);

            if (IsDead)
            {
                OnDie?.Invoke();
            }
        }

        public void ResetMana()
        {
            var mpStat = statController?.GetStat(StatType.Mana);
            mpStat?.SetCurrent(0);
        }

        public bool CanSkill()
        {
            return Mana >= MaxMana;
        }

        public void Init(int statHp, int statMp)
        {
            SetupStats(statHp, statMp);
        }

        /// <summary>
        /// Initialize with full stats
        /// </summary>
        public void InitFull(int hp, int mp, float atk, float def, float speed, int shield = 0)
        {
            SetupStatsFull(hp, mp, atk, def, speed, shield);
        }

        /// <summary>
        /// Setup stats using StatsSystem - basic
        /// </summary>
        public void SetupStats(int hp, int mp, int shield = 0)
        {
            if (statController == null) Awake();

            var stats = statController.Stats;
            stats.ClearStats();

            stats.AddStat(new Stat(StatType.Health, hp, hp, true, 0f));
            stats.AddStat(new Stat(StatType.Mana, 0, mp, true, 5f));
            stats.AddStat(new Stat(StatType.Shield, shield, shield, false, 0f));
        }

        /// <summary>
        /// Setup full stats using StatsSystem - includes combat stats
        /// </summary>
        public void SetupStatsFull(int hp, int mp, float atk, float def, float speed, int shield = 0)
        {
            if (statController == null) Awake();

            var stats = statController.Stats;
            stats.ClearStats();

            stats.AddStat(new Stat(StatType.Health, hp, hp, true, 0f));
            stats.AddStat(new Stat(StatType.Mana, 0, mp, true, 5f));
            stats.AddStat(new Stat(StatType.Shield, shield, shield, false, 0f));

            stats.AddStat(new Stat(StatType.Attack, (int)atk));
            stats.AddStat(new Stat(StatType.Defense, (int)def));
            stats.AddStat(new Stat(StatType.Speed, (int)speed));
        }

        public void SetMaxShield(float shield)
        {
            var shieldStat = statController?.GetStat(StatType.Shield);
            if (shieldStat != null)
            {
                shieldStat.IncreaseMax((int)(shield - MaxShield));
                shieldStat.SetCurrent(shieldStat.MaxValue);
            }
        }

        public void AddShield(float amount)
        {
            var shieldStat = statController?.GetStat(StatType.Shield);
            shieldStat?.Add(amount);
        }

        public void FullHeal()
        {
            statController?.RestoreAll();
        }

        public float GetHealthPercentage()
        {
            if (MaxHealth <= 0) return 0;
            return Health / MaxHealth;
        }

        public float GetManaPercentage()
        {
            if (MaxMana <= 0) return 0;
            return (float)Mana / MaxMana;
        }

        #region Buff/Debuff System using StatsSystem Modifiers

        public void ApplyBuff(StatType statType, float percentageBonus, int durationTurns)
        {
            var stat = statController?.GetStat(statType);
            if (stat == null) return;

            var modifier = Modifier.Times(1f + percentageBonus, 0, $"{statType}_buff");
            stat.AddModifier(modifier);
        }

        public void ApplyDebuff(StatType statType, float percentagePenalty, int durationTurns)
        {
            ApplyBuff(statType, -percentagePenalty, durationTurns);
        }

        public void ApplyFlatBuff(StatType statType, int flatBonus, int durationTurns)
        {
            var stat = statController?.GetStat(statType);
            if (stat == null) return;

            var modifier = Modifier.Plus((float)flatBonus, 0, $"{statType}_flat_buff");
            stat.AddModifier(modifier);
        }

        public void ClearBuffs(StatType statType)
        {
            var stat = statController?.GetStat(statType);
            stat?.ClearModifiers();
        }

        public void ClearAllBuffs()
        {
            statController?.ClearAllModifiers();
        }

        #endregion
    }
}
