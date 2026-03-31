using System;
using System.Collections.Generic;
using UnityEngine;
using GameSystems.Battle;
using GameSystems.Stats;

namespace GameSystems.AutoBattle
{
    /// <summary>
    /// Owns the runtime lifecycle for a BattleUnit:
    /// turn tracking, cooldown ticking, status effects, and stat event forwarding.
    /// </summary>
    public sealed class BattleUnitRuntimeService : IDisposable
    {
        private readonly BattleUnit _owner;
        private UnitStatController _statController;
        private GameSystems.Battle.StatusController _statusController;
        private TurnTracker _turnTracker;
        private readonly List<IDisposable> _temporaryEffectHandles = new List<IDisposable>();
        private bool _isBound;
        private bool _isDisposed;
        private bool _defeatNotified;
        private int _currentCooldown;

        public BattleUnitRuntimeService(BattleUnit owner)
        {
            _owner = owner;
        }

        public UnitStatController StatController => _statController;
        public GameSystems.Battle.StatusController StatusController => _statusController;
        public int TurnsTaken { get; private set; }
        public int CurrentCooldown => _currentCooldown;
        public bool IsSkillReady => _currentCooldown <= 0;

        public event Action<Stat> OnStatChanged;
        public event Action<StatusEffect> OnStatusApplied;
        public event Action<StatusEffect> OnStatusRemoved;
        public event Action OnTurnStarted;
        public event Action OnTurnEnded;
        public event Action OnDefeated;
        public event Action OnReset;
        public event Action<int> OnCooldownChanged;

        public void Initialize(UnitStatController statController, GameSystems.Battle.StatusController statusController)
        {
            Unbind();

            _statController = statController;
            _statusController = statusController;
            _turnTracker ??= new TurnTracker();
            _currentCooldown = 0;
            TurnsTaken = 0;
            _defeatNotified = false;

            Bind();
        }

        public bool CanAct()
        {
            return _owner != null && _owner.IsAlive && (_statusController == null || _statusController.CanAct());
        }

        public bool CanCastSkill()
        {
            return _owner != null && _owner.IsAlive &&
                   (_statusController == null || _statusController.CanCastSkill()) &&
                   IsSkillReady;
        }

        public void SetCooldown(int cooldown)
        {
            _currentCooldown = Mathf.Max(0, cooldown);
            OnCooldownChanged?.Invoke(_currentCooldown);
        }

        public bool BeginTurn()
        {
            if (!CanAct())
            {
                OnTurnStarted?.Invoke();
                TurnsTaken++;
                return false;
            }

            _turnTracker ??= new TurnTracker();
            TurnsTaken++;
            OnTurnStarted?.Invoke();
            return true;
        }

        public void EndTurn()
        {
            if (_owner == null || !_owner.IsAlive)
            {
                return;
            }

            if (_currentCooldown > 0)
            {
                _currentCooldown = Mathf.Max(0, _currentCooldown - 1);
                OnCooldownChanged?.Invoke(_currentCooldown);
            }

            _statusController?.TickTurn(_owner);
            _turnTracker?.NextTurn();
            OnTurnEnded?.Invoke();
        }

        public StatusEffect ApplyStatusEffect(StatusEffectType type, int durationTurns, float value = 0f, int stacks = 1)
        {
            if (_statusController == null || durationTurns <= 0)
            {
                return null;
            }

            var effect = _statusController.AddStatus(type, durationTurns, value, stacks);
            if (effect != null)
            {
                OnStatusApplied?.Invoke(effect);
            }

            return effect;
        }

        public bool HasStatus(StatusEffectType type)
        {
            return _statusController != null && _statusController.HasStatus(type);
        }

        public IDisposable ApplyTimedModifier(StatType statType, IModifier<float> modifier, int durationTurns, StatusEffectType statusType, float value)
        {
            if (_statController == null || modifier == null)
            {
                return null;
            }

            _turnTracker ??= new TurnTracker();
            _statController.AddModifier(statType, modifier);

            IDisposable handle = null;
            if (durationTurns > 0 && modifier is ITurnBasedModifier turnBasedModifier)
            {
                handle = turnBasedModifier.DisableAfterTurns(_turnTracker);
                if (handle != null)
                {
                    _temporaryEffectHandles.Add(handle);
                }

                ApplyStatusEffect(statusType, durationTurns, value);
            }

            return handle;
        }

        public void ResetRuntime()
        {
            ClearAllEffects();
            _statController?.RestoreAll();
            _turnTracker?.Reset();
            _currentCooldown = 0;
            TurnsTaken = 0;
            _defeatNotified = false;

            OnCooldownChanged?.Invoke(_currentCooldown);
            OnReset?.Invoke();
        }

        public void ClearAllEffects()
        {
            DisposeTemporaryEffectHandles();
            _statController?.ClearAllModifiers();
            _statusController?.ClearAllStatuses();
        }

        public void NotifyDefeat()
        {
            if (_defeatNotified)
            {
                return;
            }

            _defeatNotified = true;
            ClearAllEffects();
            OnDefeated?.Invoke();
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            Unbind();
            DisposeTemporaryEffectHandles();

            OnStatChanged = null;
            OnStatusApplied = null;
            OnStatusRemoved = null;
            OnTurnStarted = null;
            OnTurnEnded = null;
            OnDefeated = null;
            OnReset = null;
            OnCooldownChanged = null;

            _statController = null;
            _statusController = null;
            _turnTracker = null;
        }

        private void Bind()
        {
            if (_isBound)
            {
                return;
            }

            if (_statController != null)
            {
                _statController.OnStatChanged += HandleStatChanged;
                _statController.OnStatDepleted += HandleStatDepleted;
            }

            if (_statusController != null)
            {
                _statusController.OnStatusAdded += HandleStatusAdded;
                _statusController.OnStatusRefreshed += HandleStatusRefreshed;
                _statusController.OnStatusRemoved += HandleStatusRemoved;
                _statusController.OnStatusExpired += HandleStatusExpired;
                _statusController.OnStatusTicked += HandleStatusTicked;
            }

            _isBound = true;
        }

        private void Unbind()
        {
            if (!_isBound)
            {
                return;
            }

            if (_statController != null)
            {
                _statController.OnStatChanged -= HandleStatChanged;
                _statController.OnStatDepleted -= HandleStatDepleted;
            }

            if (_statusController != null)
            {
                _statusController.OnStatusAdded -= HandleStatusAdded;
                _statusController.OnStatusRefreshed -= HandleStatusRefreshed;
                _statusController.OnStatusRemoved -= HandleStatusRemoved;
                _statusController.OnStatusExpired -= HandleStatusExpired;
                _statusController.OnStatusTicked -= HandleStatusTicked;
            }

            _isBound = false;
        }

        private void HandleStatChanged(Stat stat)
        {
            if (stat != null)
            {
                OnStatChanged?.Invoke(stat);
            }
        }

        private void HandleStatDepleted(Stat stat)
        {
            if (stat != null)
            {
                OnStatChanged?.Invoke(stat);
            }

            if (stat != null && stat.StatType == StatType.Health)
            {
                NotifyDefeat();
            }
        }

        private void HandleStatusAdded(StatusEffect status)
        {
            if (status != null)
            {
                OnStatusApplied?.Invoke(status);
            }
        }

        private void HandleStatusRefreshed(StatusEffect status)
        {
            if (status != null)
            {
                OnStatusApplied?.Invoke(status);
            }
        }

        private void HandleStatusRemoved(StatusEffect status)
        {
            if (status != null)
            {
                OnStatusRemoved?.Invoke(status);
            }
        }

        private void HandleStatusExpired(StatusEffect status)
        {
            if (status != null)
            {
                OnStatusRemoved?.Invoke(status);
            }
        }

        private void HandleStatusTicked(StatusEffect status, int amount)
        {
            // Intentionally no-op: stat change events already flow from the underlying stat controller.
        }

        private void DisposeTemporaryEffectHandles()
        {
            if (_temporaryEffectHandles.Count == 0)
            {
                return;
            }

            foreach (var handle in _temporaryEffectHandles)
            {
                handle?.Dispose();
            }

            _temporaryEffectHandles.Clear();
        }
    }
}
