using System;
using System.ComponentModel;

namespace GameSystems.Stats
{
	/// <summary>
	/// Base class cho tất cả modifier — đảm bảo Enabled/Priority fire PropertyChanged
	/// để ModifiableValue tự động recalculate khi modifier thay đổi trạng thái.
	/// </summary>
	public abstract class ModifierBase<T> : IModifier<T>
	{
		private bool _enabled = true;
		private int _priority;

		public bool Enabled
		{
			get => _enabled;
			set
			{
				if (_enabled == value) return;
				_enabled = value;
				OnPropertyChanged(nameof(Enabled));
			}
		}

		public int Priority
		{
			get => _priority;
			set
			{
				if (_priority == value) return;
				_priority = value;
				OnPropertyChanged(nameof(Priority));
			}
		}

		public string Name { get; }

		public event PropertyChangedEventHandler PropertyChanged;

		protected ModifierBase(string name, int priority)
		{
			Name = name;
			_priority = priority;
		}

		public abstract T Modify(T given);

		protected void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public static class Modifier
	{
		public static IModifier<T> Plus<T>(T value, int priority = 0, string name = null)
		{
			return new AddModifier<T>(value, name, priority);
		}

		public static IModifier<T> Minus<T>(T value, int priority = 0, string name = null)
		{
			return new SubtractModifier<T>(value, name, priority);
		}

		public static IModifier<T> Times<T>(T value, int priority = 0, string name = null)
		{
			return new MultiplyModifier<T>(value, name, priority);
		}

		public static IModifier<T> Divide<T>(T value, int priority = 0, string name = null)
		{
			return new DivideModifier<T>(value, name, priority);
		}

		public static IModifier<T> Substitute<T>(T value, int priority = 0, string name = null)
		{
			return new SubstituteModifier<T>(value, name, priority);
		}

		public static IModifier<T> Create<T>(Func<T, T> modifyFunc, int priority = 0, string name = null)
		{
			return new FuncModifier<T>(modifyFunc, name, priority);
		}

		public static IModifier<T> Create<T>(IReadOnlyValue<T> value, int priority = 0, string name = null)
		{
			return new ValueModifier<T>(value, name, priority);
		}
	}

	internal class AddModifier<T> : ModifierBase<T>
	{
		private readonly T _value;
		private readonly IOperator<T> _op;

		public AddModifier(T value, string name, int priority) : base(name ?? $"+{value}", priority)
		{
			_value = value;
			_op = Operator<T>.Instance;
		}

		public override T Modify(T given)
		{
			if (!Enabled) return given;
			return _op.Add(given, _value);
		}
	}

	internal class SubtractModifier<T> : ModifierBase<T>
	{
		private readonly T _value;
		private readonly IOperator<T> _op;

		public SubtractModifier(T value, string name, int priority) : base(name ?? $"-{value}", priority)
		{
			_value = value;
			_op = Operator<T>.Instance;
		}

		public override T Modify(T given)
		{
			if (!Enabled) return given;
			return _op.Subtract(given, _value);
		}
	}

	internal class MultiplyModifier<T> : ModifierBase<T>
	{
		private readonly T _value;
		private readonly IOperator<T> _op;

		public MultiplyModifier(T value, string name, int priority) : base(name ?? $"×{value}", priority)
		{
			_value = value;
			_op = Operator<T>.Instance;
		}

		public override T Modify(T given)
		{
			if (!Enabled) return given;
			return _op.Multiply(given, _value);
		}
	}

	internal class DivideModifier<T> : ModifierBase<T>
	{
		private readonly T _value;
		private readonly IOperator<T> _op;

		public DivideModifier(T value, string name, int priority) : base(name ?? $"/{value}", priority)
		{
			_value = value;
			_op = Operator<T>.Instance;
		}

		public override T Modify(T given)
		{
			if (!Enabled) return given;
			return _op.Divide(given, _value);
		}
	}

	internal class SubstituteModifier<T> : ModifierBase<T>
	{
		private readonly T _value;

		public SubstituteModifier(T value, string name, int priority) : base(name ?? $"={value}", priority)
		{
			_value = value;
		}

		public override T Modify(T given)
		{
			if (!Enabled) return given;
			return _value;
		}
	}

	internal class FuncModifier<T> : ModifierBase<T>
	{
		private readonly Func<T, T> _func;

		public FuncModifier(Func<T, T> func, string name, int priority) : base(name ?? "Custom", priority)
		{
			_func = func;
		}

		public override T Modify(T given)
		{
			if (!Enabled) return given;
			return _func(given);
		}
	}

	internal class ValueModifier<T> : ModifierBase<T>, IDisposable
	{
		private readonly IReadOnlyValue<T> _value;
		private PropertyChangedEventHandler _valueChangedHandler;
		private bool _isDisposed;

		public ValueModifier(IReadOnlyValue<T> value, string name, int priority)
			: base(name ?? $"Value({value.Value})", priority)
		{
			_value = value;

			if (_value is INotifyPropertyChanged notify)
			{
				_valueChangedHandler = (s, e) => OnPropertyChanged("Value");
				notify.PropertyChanged += _valueChangedHandler;
			}
		}

		public override T Modify(T given)
		{
			if (!Enabled) return given;
			return _value.Value;
		}

		public void Dispose()
		{
			if (_isDisposed) return;
			_isDisposed = true;

			if (_valueChangedHandler != null && _value is INotifyPropertyChanged notify)
			{
				notify.PropertyChanged -= _valueChangedHandler;
				_valueChangedHandler = null;
			}
		}
	}
}
