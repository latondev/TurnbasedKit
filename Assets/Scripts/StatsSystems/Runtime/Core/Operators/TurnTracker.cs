using System;

namespace GameSystems.Stats
{
	public class TurnTracker
	{
		private int _currentTurn;

		public int CurrentTurn => _currentTurn;

		public event Action<int> OnTurnStart;
		public event Action<int> OnTurnEnd;

		public TurnTracker()
		{
			_currentTurn = 0;
		}

		public void NextTurn()
		{
			_currentTurn++;
			OnTurnStart?.Invoke(_currentTurn);
			OnTurnEnd?.Invoke(_currentTurn);
		}

		public void Reset()
		{
			_currentTurn = 0;
		}
	}
}
