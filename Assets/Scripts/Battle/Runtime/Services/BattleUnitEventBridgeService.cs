using System;
using GameSystems.Battle;
using GameSystems.Stats;

namespace GameSystems.AutoBattle
{
    /// <summary>
    /// Bridges runtime service events back to BattleUnit public events.
    /// </summary>
    public sealed class BattleUnitEventBridgeService : IDisposable
    {
        private readonly BattleUnit _owner;
        private BattleUnitRuntimeService _runtimeService;
        private bool _isDisposed;
        private bool _isBound;

        public BattleUnitEventBridgeService(BattleUnit owner)
        {
            _owner = owner;
        }

        public void Initialize(BattleUnitRuntimeService runtimeService)
        {
            Unbind();
            _runtimeService = runtimeService;
            Bind();
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            Unbind();
            _runtimeService = null;
        }

        private void Bind()
        {
            if (_isBound || _runtimeService == null)
            {
                return;
            }

            _runtimeService.OnStatChanged += HandleStatChanged;
            _runtimeService.OnStatusApplied += HandleStatusApplied;
            _runtimeService.OnStatusRemoved += HandleStatusRemoved;
            _runtimeService.OnTurnStarted += HandleTurnStarted;
            _runtimeService.OnTurnEnded += HandleTurnEnded;
            _runtimeService.OnDefeated += HandleDefeated;
            _runtimeService.OnReset += HandleReset;
            _runtimeService.OnCooldownChanged += HandleCooldownChanged;
            _isBound = true;
        }

        private void Unbind()
        {
            if (!_isBound || _runtimeService == null)
            {
                _isBound = false;
                return;
            }

            _runtimeService.OnStatChanged -= HandleStatChanged;
            _runtimeService.OnStatusApplied -= HandleStatusApplied;
            _runtimeService.OnStatusRemoved -= HandleStatusRemoved;
            _runtimeService.OnTurnStarted -= HandleTurnStarted;
            _runtimeService.OnTurnEnded -= HandleTurnEnded;
            _runtimeService.OnDefeated -= HandleDefeated;
            _runtimeService.OnReset -= HandleReset;
            _runtimeService.OnCooldownChanged -= HandleCooldownChanged;
            _isBound = false;
        }

        private void HandleStatChanged(Stat stat)
        {
            if (stat != null)
            {
                _owner?.RaiseStatChanged(stat);
            }
        }

        private void HandleStatusApplied(StatusEffect status)
        {
            if (status != null)
            {
                _owner?.RaiseStatusApplied(status);
            }
        }

        private void HandleStatusRemoved(StatusEffect status)
        {
            if (status != null)
            {
                _owner?.RaiseStatusRemoved(status);
            }
        }

        private void HandleTurnStarted()
        {
            _owner?.RaiseTurnStarted();
        }

        private void HandleTurnEnded()
        {
            _owner?.RaiseTurnEnded();
        }

        private void HandleDefeated()
        {
            _owner?.RaiseDefeated();
        }

        private void HandleReset()
        {
            _owner?.RaiseReset();
        }

        private void HandleCooldownChanged(int cooldown)
        {
            _owner?.RaiseCooldownChanged(cooldown);
        }
    }
}
