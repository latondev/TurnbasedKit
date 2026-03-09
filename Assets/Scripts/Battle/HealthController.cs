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
        public float Health => statController?.GetStatValue("hp") ?? 0;
        public float Shield => statController?.GetStatValue("shield") ?? 0;
        public int Mana => (int)(statController?.GetStatValue("mp") ?? 0);
        public bool IsDead => statController?.IsDead() ?? false;
        public float MaxHealth => statController?.GetStatMaxValue("hp") ?? 0;
        public int MaxMana => (int)(statController?.GetStatMaxValue("mp") ?? 0);
        public float MaxShield => statController?.GetStatMaxValue("shield") ?? 0;

        public UnitStatController StatController => statController;

        private void Awake()
        {
            if (statController == null)
            {
                var go = new GameObject("HealthStats");
                go.transform.SetParent(transform);
                statController = go.AddComponent<UnitStatController>();
                statController.UnitName = transform.name;
            }
        }

        public void AddMana(int value)
        {
            statController?.ModifyStat("mp", value);
        }

        public void ChangeHealth(float value)
        {
            if (statController == null) return;

            // Shield absorbs damage first
            if (value < 0)
            {
                float shield = statController.GetStatValue("shield");
                if (shield > 0)
                {
                    float remainingDamage = Mathf.Abs(value);
                    var shieldStat = statController.GetStat("shield");

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

            statController.ModifyStat("hp", value);

            if (IsDead)
            {
                OnDie?.Invoke();
            }
        }

        public void ResetMana()
        {
            var mpStat = statController?.GetStat("mp");
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

            stats.AddStat(new Stat("hp", "Health", StatType.Health, hp, hp, true, 0f));
            stats.AddStat(new Stat("mp", "Mana", StatType.Mana, 0, mp, true, 5f));
            stats.AddStat(new Stat("shield", "Shield", StatType.Health, shield, shield, false, 0f));
        }

        /// <summary>
        /// Setup full stats using StatsSystem - includes combat stats
        /// </summary>
        public void SetupStatsFull(int hp, int mp, float atk, float def, float speed, int shield = 0)
        {
            if (statController == null) Awake();

            var stats = statController.Stats;
            stats.ClearStats();

            stats.AddStat(new Stat("hp", "Health", StatType.Health, hp, hp, true, 0f));
            stats.AddStat(new Stat("mp", "Mana", StatType.Mana, 0, mp, true, 5f));
            stats.AddStat(new Stat("shield", "Shield", StatType.Health, shield, shield, false, 0f));

            stats.AddStat(new Stat("attack", "Attack", StatType.Attack, (int)atk));
            stats.AddStat(new Stat("defense", "Defense", StatType.Defense, (int)def));
            stats.AddStat(new Stat("speed", "Speed", StatType.Speed, (int)speed));
        }

        public void SetMaxShield(float shield)
        {
            var shieldStat = statController?.GetStat("shield");
            if (shieldStat != null)
            {
                shieldStat.IncreaseMax((int)(shield - MaxShield));
                shieldStat.SetCurrent(shieldStat.MaxValue);
            }
        }

        public void AddShield(float amount)
        {
            var shieldStat = statController?.GetStat("shield");
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

        public void ApplyBuff(string statId, float percentageBonus, int durationTurns)
        {
            var stat = statController?.GetStat(statId);
            if (stat == null) return;

            var modifier = Modifier.Times(1f + percentageBonus, 0, $"{statId}_buff");
            stat.Modifiers.Add(modifier);
        }

        public void ApplyDebuff(string statId, float percentagePenalty, int durationTurns)
        {
            ApplyBuff(statId, -percentagePenalty, durationTurns);
        }

        public void ApplyFlatBuff(string statId, int flatBonus, int durationTurns)
        {
            var stat = statController?.GetStat(statId);
            if (stat == null) return;

            var modifier = Modifier.Plus((float)flatBonus, 0, $"{statId}_flat_buff");
            stat.Modifiers.Add(modifier);
        }

        public void ClearBuffs(string statId)
        {
            var stat = statController?.GetStat(statId);
            stat?.Modifiers.Clear();
        }

        public void ClearAllBuffs()
        {
            statController?.ClearAllModifiers();
        }

        #endregion
    }
}
