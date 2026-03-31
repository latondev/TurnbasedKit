using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GameSystems.Stats
{
	/// <summary>
	/// Helper IDisposable — chạy action khi Dispose.
	/// </summary>
	public sealed class DisposableAction : IDisposable
	{
		private Action _disposeAction;
		private bool _isDisposed;

		public DisposableAction(Action disposeAction)
		{
			_disposeAction = disposeAction;
		}

		public void Dispose()
		{
			if (_isDisposed) return;
			_isDisposed = true;
			_disposeAction?.Invoke();
			_disposeAction = null;
		}
	}

	/// <summary>
	/// Gom nhiều IDisposable thành 1 — dispose tất cả khi Dispose.
	/// </summary>
	public sealed class CompositeDisposable : IDisposable
	{
		private readonly List<IDisposable> _disposables = new();
		private bool _isDisposed;

		public void Add(IDisposable disposable)
		{
			if (_isDisposed)
			{
				disposable?.Dispose();
				return;
			}
			if (disposable != null)
				_disposables.Add(disposable);
		}

		public void Dispose()
		{
			if (_isDisposed) return;
			_isDisposed = true;
			foreach (var d in _disposables)
				d?.Dispose();
			_disposables.Clear();
		}
	}

	public static class ValueExtensions
	{
		public static IReadOnlyValue<TResult> Select<TSource, TResult>(this IValue<TSource> source, Func<TSource, TResult> selector)
		{
			return new ComputedValue<TSource, TResult>(source, selector);
		}

		public static IReadOnlyValue<TResult> Zip<TFirst, TSecond, TResult>(
			this IValue<TFirst> first,
			IValue<TSecond> second,
			Func<TFirst, TSecond, TResult> resultSelector)
		{
			return new ZippedValue<TFirst, TSecond, TResult>(first, second, resultSelector);
		}

		/// <summary>
		/// Enable modifier sau khoảng delay. Trả về IDisposable để hủy timer.
		/// Sử dụng SynchronizationContext để đảm bảo callback chạy trên main thread (Unity).
		/// </summary>
		public static IDisposable EnableAfter<T>(this IModifier<T> modifier, TimeSpan delay)
		{
			modifier.Enabled = false;
			var context = SynchronizationContext.Current;
			var timer = new System.Timers.Timer(delay.TotalMilliseconds);
			timer.AutoReset = false;
			timer.Elapsed += (s, e) =>
			{
				if (context != null)
					context.Post(_ => modifier.Enabled = true, null);
				else
					modifier.Enabled = true;
				timer.Dispose();
			};
			timer.Start();

			return new DisposableAction(() =>
			{
				timer.Stop();
				timer.Dispose();
			});
		}

		/// <summary>
		/// Disable modifier sau khoảng delay. Trả về IDisposable để hủy timer.
		/// Sử dụng SynchronizationContext để đảm bảo callback chạy trên main thread (Unity).
		/// </summary>
		public static IDisposable DisableAfter<T>(this IModifier<T> modifier, TimeSpan delay)
		{
			var context = SynchronizationContext.Current;
			var timer = new System.Timers.Timer(delay.TotalMilliseconds);
			timer.AutoReset = false;
			timer.Elapsed += (s, e) =>
			{
				if (context != null)
					context.Post(_ => modifier.Enabled = false, null);
				else
					modifier.Enabled = false;
				timer.Dispose();
			};
			timer.Start();

			return new DisposableAction(() =>
			{
				timer.Stop();
				timer.Dispose();
			});
		}

		/// <summary>
		/// Disable modifier sau N lượt. Trả về IDisposable để hủy đăng ký.
		/// Tự động unsubscribe khi hết lượt hoặc khi Dispose.
		/// </summary>
		public static IDisposable DisableAfterTurns<T>(this IModifier<T> modifier, int turns, TurnTracker turnTracker)
		{
			if (turnTracker == null || turns <= 0) return new DisposableAction(null);

			int startTurn = turnTracker.CurrentTurn;
			Action<int> handler = null;

			handler = (currentTurn) =>
			{
				if (currentTurn - startTurn >= turns)
				{
					modifier.Enabled = false;
					turnTracker.OnTurnEnd -= handler; // Tự hủy đăng ký
				}
			};

			turnTracker.OnTurnEnd += handler;

			return new DisposableAction(() =>
			{
				turnTracker.OnTurnEnd -= handler;
			});
		}

		/// <summary>
		/// Enable modifier sau N lượt. Trả về IDisposable để hủy đăng ký.
		/// Tự động unsubscribe khi đủ lượt hoặc khi Dispose.
		/// </summary>
		public static IDisposable EnableAfterTurns<T>(this IModifier<T> modifier, int turns, TurnTracker turnTracker)
		{
			if (turnTracker == null || turns <= 0) return new DisposableAction(null);

			modifier.Enabled = false;
			int startTurn = turnTracker.CurrentTurn;
			Action<int> handler = null;

			handler = (currentTurn) =>
			{
				if (currentTurn - startTurn >= turns)
				{
					modifier.Enabled = true;
					turnTracker.OnTurnEnd -= handler; // Tự hủy đăng ký
				}
			};

			turnTracker.OnTurnEnd += handler;

			return new DisposableAction(() =>
			{
				turnTracker.OnTurnEnd -= handler;
			});
		}

		/// <summary>
		/// Tự giảm số lượt còn lại mỗi turn. Trả về IDisposable để hủy.
		/// </summary>
		public static IDisposable DisableAfterTurns(this ITurnBasedModifier modifier, TurnTracker turnTracker)
		{
			if (turnTracker == null) return new DisposableAction(null);

			int startTurn = turnTracker.CurrentTurn;
			Action<int> handler = null;

			handler = (currentTurn) =>
			{
				if (currentTurn <= startTurn)
				{
					return;
				}

				modifier.DecrementTurns();

				if (modifier.RemainingTurns <= 0)
				{
					turnTracker.OnTurnEnd -= handler;
				}
			};

			turnTracker.OnTurnEnd += handler;

			return new DisposableAction(() =>
			{
				turnTracker.OnTurnEnd -= handler;
			});
		}

		/// <summary>
		/// Enable modifier trong N lượt rồi tự disable. Trả về IDisposable để hủy.
		/// </summary>
		public static IDisposable EnableForTurns(this ITurnBasedModifier modifier, int totalTurns, TurnTracker turnTracker)
		{
			if (turnTracker == null || totalTurns <= 0) return new DisposableAction(null);

			modifier.ResetTurns();
			modifier.RemainingTurns = totalTurns;

			return DisableAfterTurns(modifier, turnTracker);
		}
	}

	internal class ComputedValue<TSource, TResult> : IReadOnlyValue<TResult>, IDisposable
	{
		private readonly IValue<TSource> _source;
		private readonly Func<TSource, TResult> _selector;
		private TResult _cachedValue;
		private System.ComponentModel.PropertyChangedEventHandler _handler;
		private bool _isDisposed;

		public TResult Value
		{
			get
			{
				_cachedValue = _selector(_source.Value);
				return _cachedValue;
			}
		}

		public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

		public ComputedValue(IValue<TSource> source, Func<TSource, TResult> selector)
		{
			_source = source;
			_selector = selector;

			if (_source is System.ComponentModel.INotifyPropertyChanged notify)
			{
				_handler = (s, e) =>
				{
					_cachedValue = _selector(_source.Value);
					PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Value)));
				};
				notify.PropertyChanged += _handler;
			}
		}

		public void Dispose()
		{
			if (_isDisposed) return;
			_isDisposed = true;

			if (_handler != null && _source is System.ComponentModel.INotifyPropertyChanged notify)
			{
				notify.PropertyChanged -= _handler;
				_handler = null;
			}
		}
	}

	internal class ZippedValue<TFirst, TSecond, TResult> : IReadOnlyValue<TResult>, IDisposable
	{
		private readonly IValue<TFirst> _first;
		private readonly IValue<TSecond> _second;
		private readonly Func<TFirst, TSecond, TResult> _selector;
		private TResult _cachedValue;
		private System.ComponentModel.PropertyChangedEventHandler _firstHandler;
		private System.ComponentModel.PropertyChangedEventHandler _secondHandler;
		private bool _isDisposed;

		public TResult Value
		{
			get
			{
				_cachedValue = _selector(_first.Value, _second.Value);
				return _cachedValue;
			}
		}

		public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

		public ZippedValue(IValue<TFirst> first, IValue<TSecond> second, Func<TFirst, TSecond, TResult> selector)
		{
			_first = first;
			_second = second;
			_selector = selector;

			_firstHandler = (s, e) =>
			{
				_cachedValue = _selector(_first.Value, _second.Value);
				PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Value)));
			};

			_secondHandler = (s, e) =>
			{
				_cachedValue = _selector(_first.Value, _second.Value);
				PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Value)));
			};

			if (_first is System.ComponentModel.INotifyPropertyChanged notify1)
			{
				notify1.PropertyChanged += _firstHandler;
			}

			if (_second is System.ComponentModel.INotifyPropertyChanged notify2)
			{
				notify2.PropertyChanged += _secondHandler;
			}
		}

		public void Dispose()
		{
			if (_isDisposed) return;
			_isDisposed = true;

			if (_firstHandler != null && _first is System.ComponentModel.INotifyPropertyChanged notify1)
			{
				notify1.PropertyChanged -= _firstHandler;
				_firstHandler = null;
			}

			if (_secondHandler != null && _second is System.ComponentModel.INotifyPropertyChanged notify2)
			{
				notify2.PropertyChanged -= _secondHandler;
				_secondHandler = null;
			}
		}
	}
}
