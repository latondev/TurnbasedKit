using System;
using System.ComponentModel;

namespace GameSystems.Stats
{
	public class TurnBasedModifierWrapper<T> : IModifier<T>, ITurnBasedModifier, IDisposable
	{
		private readonly IModifier<T> _wrappedModifier;
		private PropertyChangedEventHandler _propertyChangedHandler;
		private PropertyChangedEventHandler _wrappedHandler;
		private bool _isDisposed;

		public bool Enabled
		{
			get => _wrappedModifier.Enabled;
			set => _wrappedModifier.Enabled = value;
		}

		public int Priority
		{
			get => _wrappedModifier.Priority;
			set => _wrappedModifier.Priority = value;
		}

		public int RemainingTurns { get; set; }
		public int TotalTurns { get; private set; }

		public string Name => $"{_wrappedModifier.Name ?? "Unknown"} ({RemainingTurns}/{TotalTurns} turns)";

		public event PropertyChangedEventHandler PropertyChanged
		{
			add => _propertyChangedHandler += value;
			remove => _propertyChangedHandler -= value;
		}

		public TurnBasedModifierWrapper(IModifier<T> wrappedModifier, int totalTurns)
		{
			_wrappedModifier = wrappedModifier;
			TotalTurns = totalTurns;
			RemainingTurns = totalTurns;

			_wrappedHandler = (s, e) => _propertyChangedHandler?.Invoke(this, e);
			_wrappedModifier.PropertyChanged += _wrappedHandler;
		}

		public T Modify(T given)
		{
			if (RemainingTurns <= 0)
			{
				Enabled = false;
				return given;
			}

			return _wrappedModifier.Modify(given);
		}

		public bool IsActiveOnTurn(int turn)
		{
			return RemainingTurns > 0;
		}

		public void DecrementTurns()
		{
			if (RemainingTurns > 0)
			{
				RemainingTurns--;
				_propertyChangedHandler?.Invoke(this, new PropertyChangedEventArgs(nameof(RemainingTurns)));

				if (RemainingTurns <= 0)
				{
					Enabled = false;
					_propertyChangedHandler?.Invoke(this, new PropertyChangedEventArgs(nameof(Enabled)));
				}
			}
		}

		public void ResetTurns()
		{
			RemainingTurns = TotalTurns;
			Enabled = true;
			_propertyChangedHandler?.Invoke(this, new PropertyChangedEventArgs(nameof(RemainingTurns)));
			_propertyChangedHandler?.Invoke(this, new PropertyChangedEventArgs(nameof(Enabled)));
		}

		public void Dispose()
		{
			if (_isDisposed) return;
			_isDisposed = true;

			if (_wrappedHandler != null)
			{
				_wrappedModifier.PropertyChanged -= _wrappedHandler;
				_wrappedHandler = null;
			}

			if (_wrappedModifier is IDisposable disposable)
			{
				disposable.Dispose();
			}
		}
	}

	public static class TurnBasedModifierFactory
	{
		public static ITurnBasedModifier CreateTurnBased<T>(IModifier<T> modifier, int turns)
		{
			return new TurnBasedModifierWrapper<T>(modifier, turns);
		}

		public static ITurnBasedModifier Plus<T>(T value, int turns, int priority = 0, string name = null)
		{
			return CreateTurnBased(Modifier.Plus(value, priority, name), turns);
		}

		public static ITurnBasedModifier Minus<T>(T value, int turns, int priority = 0, string name = null)
		{
			return CreateTurnBased(Modifier.Minus(value, priority, name), turns);
		}

		public static ITurnBasedModifier Times<T>(T value, int turns, int priority = 0, string name = null)
		{
			return CreateTurnBased(Modifier.Times(value, priority, name), turns);
		}

		public static ITurnBasedModifier Divide<T>(T value, int turns, int priority = 0, string name = null)
		{
			return CreateTurnBased(Modifier.Divide(value, priority, name), turns);
		}

		public static ITurnBasedModifier Substitute<T>(T value, int turns, int priority = 0, string name = null)
		{
			return CreateTurnBased(Modifier.Substitute(value, priority, name), turns);
		}
	}
}
