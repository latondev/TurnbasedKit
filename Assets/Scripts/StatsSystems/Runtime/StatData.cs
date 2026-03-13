using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameSystems.Stats
{
    /// <summary>
    /// Plain data class for configuring stats in Unity Inspector (ScriptableObject).
    /// This is serializable and can be edited directly in the Unity Editor.
    /// Convert to Stat at runtime for full functionality.
    /// </summary>
    [Serializable]
    public class StatData : ISerializationCallbackReceiver
    {
        [Header("Basic Info")]
        [SerializeField] private string statId;
        [SerializeField] private string statName;
        [SerializeField] private StatType statType;

        [Header("Values")]
        [SerializeField] private float baseValue;
        [SerializeField] private float maxValue;
        [SerializeField] private float currentValue;

        [Header("Regeneration")]
        [SerializeField] private bool canRegenerate;
        [SerializeField] private float regenRate;

        // Runtime-only fields (not serialized)
        [NonSerialized] private Stat _runtimeStat;

        public string StatId => statId;
        public string StatName => statName;
        public StatType StatType => statType;
        public float BaseValue
        {
            get => baseValue;
            set => baseValue = value;
        }
        public float MaxValue
        {
            get => maxValue;
            set => maxValue = value;
        }
        public float CurrentValue => currentValue;
        public bool CanRegenerate => canRegenerate;
        public float RegenRate => regenRate;

        public Stat RuntimeStat => _runtimeStat;

        public StatData() { }

        public StatData(string id, string name, StatType type, float value, float maxValue = -1, bool canRegen = false, float regenRate = 0f)
        {
            this.statId = id;
            this.statName = name;
            this.statType = type;
            this.baseValue = value;
            this.maxValue = maxValue > 0 ? maxValue : value;
            this.currentValue = this.maxValue;
            this.canRegenerate = canRegen;
            this.regenRate = regenRate;
        }

        /// <summary>
        /// Convert to runtime Stat object with full functionality
        /// </summary>
        public Stat ToStat()
        {
            if (_runtimeStat == null)
            {
                _runtimeStat = new Stat(statId, statName, statType, baseValue, maxValue, canRegenerate, regenRate);
                _runtimeStat.SetCurrent(currentValue);
            }
            return _runtimeStat;
        }

        /// <summary>
        /// Create a copy as runtime Stat (fresh instance)
        /// </summary>
        public Stat ToStatClone()
        {
            return new Stat(statId, statName, statType, baseValue, maxValue, canRegenerate, regenRate);
        }

        #region ISerializationCallbackReceiver

        public void OnBeforeSerialize()
        {
            // Save runtime values back to serialized fields
            if (_runtimeStat != null)
            {
                currentValue = _runtimeStat.CurrentValue;
                baseValue = _runtimeStat.BaseValue;
                maxValue = _runtimeStat.MaxValue;
            }
        }

        public void OnAfterDeserialize()
        {
            // Reset runtime stat when deserialized
            _runtimeStat = null;
            // Ensure currentValue doesn't exceed maxValue
            if (currentValue > maxValue || maxValue <= 0)
            {
                currentValue = maxValue > 0 ? maxValue : baseValue;
            }
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Create default stats for a character
        /// </summary>
        public static List<StatData> CreateDefaultStats()
        {
            var stats = new List<StatData>
            {
                new StatData("hp", "Health", StatType.Health, 1000f, 1000f, true, 0f),
                new StatData("mp", "Mana", StatType.Mana, 100f, 100f, true, 5f),
                new StatData("attack", "Attack", StatType.Attack, 100f),
                new StatData("defense", "Defense", StatType.Defense, 50f),
                new StatData("speed", "Speed", StatType.Speed, 50f),
                new StatData("critical_rate", "Critical Rate", StatType.CriticalRate, 5f),
                new StatData("critical_damage", "Critical Damage", StatType.CriticalDamage, 150f),
                new StatData("accuracy", "Accuracy", StatType.Accuracy, 100f),
                new StatData("evasion", "Evasion", StatType.Evasion, 5f),
            };
            return stats;
        }

        #endregion

        public override string ToString()
        {
            return $"{statName}: {currentValue}/{maxValue}";
        }
    }
}
