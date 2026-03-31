using System;
using System.Collections.Generic;
using UnityEngine;
using GameSystems.AutoBattle;

namespace GameSystems.Battle
{
    /// <summary>
    /// Status Controller - manages buffs/debuffs on character
    /// </summary>
    public class StatusController : MonoBehaviour
    {
        [SerializeField] private List<StatusEffect> activeStatuses = new List<StatusEffect>();

        public event Action<StatusEffect> OnStatusAdded;
        public event Action<StatusEffect> OnStatusRefreshed;
        public event Action<StatusEffect> OnStatusRemoved;
        public event Action<StatusEffect, int> OnStatusTicked;
        public event Action<StatusEffect> OnStatusExpired;

        public StatusEffect AddStatus(StatusEffect status)
        {
            if (status == null)
            {
                return null;
            }

            var existing = activeStatuses.Find(s => s.Type == status.Type);
            if (existing != null)
            {
                existing.StackCount += Mathf.Max(1, status.StackCount);
                if (status.RefreshDurationOnReapply)
                {
                    existing.RemainingTurns = Mathf.Max(existing.RemainingTurns, status.RemainingTurns);
                }

                if (status.Value > 0f)
                {
                    existing.Value = Mathf.Max(existing.Value, status.Value);
                }

                OnStatusRefreshed?.Invoke(existing);
                return existing;
            }

            activeStatuses.Add(status);
            OnStatusAdded?.Invoke(status);
            return status;
        }

        public StatusEffect AddStatus(StatusEffectType type, int durationTurns, float value = 0f, int stackCount = 1, bool refreshDurationOnReapply = true)
        {
            var status = new StatusEffect(type, durationTurns, value)
            {
                StackCount = Mathf.Max(1, stackCount),
                RefreshDurationOnReapply = refreshDurationOnReapply
            };

            return AddStatus(status);
        }

        public bool RemoveStatus(StatusEffectType type)
        {
            var removed = activeStatuses.FindAll(s => s.Type == type);
            if (removed.Count == 0)
            {
                return false;
            }

            foreach (var status in removed)
            {
                activeStatuses.Remove(status);
                OnStatusRemoved?.Invoke(status);
            }

            return true;
        }

        public bool HasStatus(StatusEffectType type)
        {
            return activeStatuses.Exists(s => s.Type == type);
        }

        public bool CanAct()
        {
            return !HasStatus(StatusEffectType.Stun) && !HasStatus(StatusEffectType.Freeze);
        }

        public bool CanCastSkill()
        {
            return !HasStatus(StatusEffectType.Silence);
        }

        public IReadOnlyList<StatusEffect> GetActiveStatuses()
        {
            return new List<StatusEffect>(activeStatuses);
        }

        public StatusTurnResult TickTurn(BattleUnit owner = null)
        {
            var result = new StatusTurnResult
            {
                CanAct = CanAct(),
                CanCastSkill = CanCastSkill()
            };

            if (activeStatuses.Count == 0)
            {
                return result;
            }

            var toRemove = new List<StatusEffect>();
            var snapshot = new List<StatusEffect>(activeStatuses);

            foreach (var status in snapshot)
            {
                if (status == null || !activeStatuses.Contains(status))
                {
                    continue;
                }

                int tickAmount = Mathf.RoundToInt(status.Value * Mathf.Max(1, status.StackCount));

                if (owner != null && tickAmount > 0)
                {
                    if (status.IsDamageOverTime)
                    {
                        int damage = owner.TakeDamage(tickAmount);
                        result.DamageApplied += damage;
                        OnStatusTicked?.Invoke(status, damage);
                    }
                    else if (status.IsHealOverTime)
                    {
                        int heal = owner.Heal(tickAmount);
                        result.HealApplied += heal;
                        OnStatusTicked?.Invoke(status, heal);
                    }

                    if (!owner.IsAlive)
                    {
                        break;
                    }
                }

                if (status.RemainingTurns > 0)
                {
                    status.RemainingTurns--;
                }

                if (status.RemainingTurns <= 0)
                {
                    toRemove.Add(status);
                }
            }

            if (owner != null && !owner.IsAlive)
            {
                return result;
            }

            foreach (var status in toRemove)
            {
                activeStatuses.Remove(status);
                OnStatusExpired?.Invoke(status);
                OnStatusRemoved?.Invoke(status);
                result.ExpiredCount++;
            }

            return result;
        }

        public void ClearAllStatuses()
        {
            if (activeStatuses.Count == 0)
            {
                return;
            }

            foreach (var status in activeStatuses)
            {
                OnStatusRemoved?.Invoke(status);
            }

            activeStatuses.Clear();
        }
    }

    #region Status Effect Classes

    [System.Serializable]
    public class StatusEffect
    {
        public StatusEffectType Type;
        [SerializeField] private int remainingTurns;
        public int StackCount;
        public float Value;
        public bool RefreshDurationOnReapply = true;

        public int RemainingTurns
        {
            get => remainingTurns;
            set => remainingTurns = Mathf.Max(0, value);
        }

        [Obsolete("Use RemainingTurns instead.")]
        public float Duration
        {
            get => RemainingTurns;
            set => RemainingTurns = Mathf.RoundToInt(value);
        }

        public bool BlocksAction => Type == StatusEffectType.Stun || Type == StatusEffectType.Freeze;
        public bool BlocksSkill => Type == StatusEffectType.Silence;
        public bool IsDamageOverTime => Type == StatusEffectType.Burn || Type == StatusEffectType.Poison;
        public bool IsHealOverTime => Type == StatusEffectType.Regeneration;

        public StatusEffect(StatusEffectType type, int durationTurns, float value = 0)
        {
            Type = type;
            RemainingTurns = durationTurns;
            StackCount = 1;
            Value = value;
        }
    }

    [System.Serializable]
    public struct StatusTurnResult
    {
        public int DamageApplied;
        public int HealApplied;
        public int ExpiredCount;
        public bool CanAct;
        public bool CanCastSkill;
    }

    public enum StatusEffectType
    {
        Stun,
        Slow,
        Burn,
        Freeze,
        Poison,
        Silence,
        Weakness,
        Shield,
        AttackBuff,
        DefenseBuff,
        SpeedBuff,
        Regeneration
    }

    #endregion
}
