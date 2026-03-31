using System;
using System.Collections.Generic;

namespace GameSystems.AutoBattle
{
    /// <summary>
    /// Owns the battle log/history for a BattleUnit.
    /// Keeps the mutation logic out of BattleUnit while preserving the existing list API.
    /// </summary>
    public sealed class BattleUnitLogService : IDisposable
    {
        private List<string> _entries;
        private bool _isDisposed;

        public BattleUnitLogService(List<string> entries = null)
        {
            _entries = entries ?? new List<string>();
        }

        public List<string> Entries => _entries ??= new List<string>();

        public void Add(string message)
        {
            if (_isDisposed || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            Entries.Add(message);
        }

        public void Clear()
        {
            if (_isDisposed)
            {
                return;
            }

            Entries.Clear();
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _entries = null;
        }
    }
}
