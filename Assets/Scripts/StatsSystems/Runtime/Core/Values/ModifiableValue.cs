using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace GameSystems.Stats
{
	[Serializable]
	public class ModifiableValue<T> : IModifiableValue<T>, IDisposable
	{
		private T _initialValue;
		private T _cachedValue;
		private bool _isDirty = true;
		private bool _isDisposed;
		private readonly ModifierCollection<T> _modifiers;
		private readonly IOperator<T> _op;

		public T InitialValue
		{
			get => _initialValue;
			set
			{
				if (EqualityComparer<T>.Default.Equals(_initialValue, value)) return;
				_initialValue = value;
				MarkDirty();
				OnPropertyChanged(nameof(InitialValue));
			}
		}

		public T Value
		{
			get
			{
				if (_isDirty)
				{
					_cachedValue = CalculateValue();
					_isDirty = false;
				}
				return _cachedValue;
			}
		}

		/// <summary>
		/// Collection wrapper — tự động subscribe/unsubscribe PropertyChanged khi Add/Remove.
		/// Dùng Modifiers.Add() hay AddModifier() đều an toàn.
		/// </summary>
		public ICollection<IModifier<T>> Modifiers => _modifiers;

		public event PropertyChangedEventHandler PropertyChanged;

		public ModifiableValue(T initialValue)
		{
			_initialValue = initialValue;
			_cachedValue = initialValue;
			_modifiers = new ModifierCollection<T>(OnModifierAdded, OnModifierRemoved);
			_op = Operator<T>.Instance;
		}

		public ModifiableValue(T initialValue, params IModifier<T>[] modifiers) : this(initialValue)
		{
			foreach (var modifier in modifiers)
			{
				AddModifier(modifier);
			}
		}

		public void AddModifier(IModifier<T> modifier)
		{
			if (modifier == null) return;
			_modifiers.Add(modifier);
		}

		public void RemoveModifier(IModifier<T> modifier)
		{
			if (modifier == null) return;
			_modifiers.Remove(modifier);
		}

		public void ClearModifiers()
		{
			_modifiers.Clear();
		}

		public void AddModifiers(IEnumerable<IModifier<T>> modifiers)
		{
			foreach (var modifier in modifiers)
			{
				AddModifier(modifier);
			}
		}

		private T CalculateValue()
		{
			T value = _initialValue;

			var sortedModifiers = _modifiers.OrderByPriority();
			foreach (var modifier in sortedModifiers)
			{
				if (modifier.Enabled)
				{
					value = modifier.Modify(value);
				}
			}

			return value;
		}

		private void OnModifierAdded(IModifier<T> modifier)
		{
			modifier.PropertyChanged += OnModifierChanged;
			MarkDirty();
		}

		private void OnModifierRemoved(IModifier<T> modifier)
		{
			modifier.PropertyChanged -= OnModifierChanged;
			MarkDirty();
		}

		private void OnModifierChanged(object sender, PropertyChangedEventArgs e)
		{
			MarkDirty();
		}

		private void MarkDirty()
		{
			_isDirty = true;
			OnPropertyChanged(nameof(Value));
		}

		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public virtual void Dispose()
		{
			if (_isDisposed) return;
			_isDisposed = true;

			foreach (var modifier in _modifiers)
			{
				modifier.PropertyChanged -= OnModifierChanged;

				if (modifier is IDisposable disposable)
				{
					disposable.Dispose();
				}
			}
			_modifiers.ClearInternal();
		}
	}

	/// <summary>
	/// Collection wrapper cho danh sách modifier.
	/// Tự động gọi callback khi Add/Remove để subscribe/unsubscribe event.
	/// Đảm bảo dù dùng Modifiers.Add() hay AddModifier() đều an toàn.
	/// </summary>
	internal class ModifierCollection<T> : ICollection<IModifier<T>>
	{
		private readonly List<IModifier<T>> _list = new();
		private readonly Action<IModifier<T>> _onAdded;
		private readonly Action<IModifier<T>> _onRemoved;

		public ModifierCollection(Action<IModifier<T>> onAdded, Action<IModifier<T>> onRemoved)
		{
			_onAdded = onAdded;
			_onRemoved = onRemoved;
		}

		public int Count => _list.Count;
		public bool IsReadOnly => false;

		public void Add(IModifier<T> item)
		{
			if (item == null) return;
			_list.Add(item);
			_onAdded?.Invoke(item);
		}

		public bool Remove(IModifier<T> item)
		{
			if (item == null) return false;
			if (!_list.Remove(item)) return false;
			_onRemoved?.Invoke(item);
			return true;
		}

		public void Clear()
		{
			// Unsubscribe từng modifier trước khi clear
			var snapshot = new List<IModifier<T>>(_list);
			_list.Clear();
			foreach (var item in snapshot)
			{
				_onRemoved?.Invoke(item);
			}
		}

		/// <summary>
		/// Clear nội bộ không gọi callback — dùng trong Dispose()
		/// </summary>
		internal void ClearInternal()
		{
			_list.Clear();
		}

		public bool Contains(IModifier<T> item) => _list.Contains(item);
		public void CopyTo(IModifier<T>[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
		public IEnumerator<IModifier<T>> GetEnumerator() => _list.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		internal IOrderedEnumerable<IModifier<T>> OrderByPriority() => _list.OrderBy(m => m.Priority);
	}
}
