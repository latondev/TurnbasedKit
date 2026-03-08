using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameSystems.Battle
{
    /// <summary>
    /// Status Controller - manages buffs/debuffs on character
    /// </summary>
    public class StatusController : MonoBehaviour
    {
        [SerializeField] private List<StatusEffect> activeStatuses = new List<StatusEffect>();

        void Start()
        {
        }

        void Update()
        {
            UpdateStatuses();
        }

        public void AddStatus(StatusEffect status)
        {
            var existing = activeStatuses.Find(s => s.Type == status.Type);
            if (existing != null)
            {
                existing.Duration = status.Duration;
                existing.StackCount++;
            }
            else
            {
                activeStatuses.Add(status);
            }
        }

        public void RemoveStatus(StatusEffectType type)
        {
            activeStatuses.RemoveAll(s => s.Type == type);
        }

        public bool HasStatus(StatusEffectType type)
        {
            return activeStatuses.Exists(s => s.Type == type);
        }

        private void UpdateStatuses()
        {
            float deltaTime = Time.deltaTime;
            List<StatusEffect> toRemove = new List<StatusEffect>();

            foreach (var status in activeStatuses)
            {
                status.Duration -= deltaTime;
                if (status.Duration <= 0)
                {
                    toRemove.Add(status);
                }
            }

            foreach (var status in toRemove)
            {
                activeStatuses.Remove(status);
            }
        }

        public List<StatusEffect> GetActiveStatuses()
        {
            return new List<StatusEffect>(activeStatuses);
        }

        public void ClearAllStatuses()
        {
            activeStatuses.Clear();
        }
    }

    #region Status Effect Classes

    [System.Serializable]
    public class StatusEffect
    {
        public StatusEffectType Type;
        public float Duration;
        public int StackCount;
        public float Value;

        public StatusEffect(StatusEffectType type, float duration, float value = 0)
        {
            Type = type;
            Duration = duration;
            StackCount = 1;
            Value = value;
        }
    }

    public enum StatusEffectType
    {
        Stun,
        Slow,
        Burn,
        Freeze,
        Poison,
        Shield,
        AttackBuff,
        DefenseBuff,
        SpeedBuff,
        Regeneration
    }

    #endregion
}
