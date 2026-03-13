using System;
using System.Collections.Generic;
using System.Linq;

namespace GameSystems.Stats
{
	/// <summary>
	/// Pure C# class for managing unit stats - No Unity dependency.
	/// All game logic lives here, UnitStatController is just a Unity wrapper.
	/// </summary>
	[Serializable]
	public class UnitStats
	{
		private string unitId;
		private string unitName;
		private int level;
		private List<Stat> stats;

		public string UnitId => unitId;
		public string UnitName 
		{ 
			get => unitName; 
			set => unitName = value; 
		}
		public int Level 
		{ 
			get => level; 
			set => level = Math.Max(1, value); 
		}
		public IReadOnlyList<Stat> Stats => stats.AsReadOnly();
		public int Count => stats.Count;

		// Events
		public event Action<Stat> OnStatChanged;
		public event Action<Stat> OnStatDepleted;
		public event Action OnLevelUp;
		public event Action<Stat> OnRegenComplete;

		public UnitStats(string unitId = null, string unitName = "Unit", int level = 1)
		{
			this.unitId = unitId ?? Guid.NewGuid().ToString();
			this.unitName = unitName;
			this.level = Math.Max(1, level);
			this.stats = new List<Stat>();
		}

		#region Stat Management

		public void AddStat(Stat stat)
		{
			if (stat == null) return;
			stats.Add(stat);
			SubscribeToStatEvents(stat);
		}

		public void RemoveStat(Stat stat)
		{
			if (stat == null) return;
			UnsubscribeFromStatEvents(stat);
			stats.Remove(stat);
		}

		public void RemoveStat(string statId)
		{
			var stat = GetStat(statId);
			if (stat != null)
			{
				RemoveStat(stat);
			}
		}

		public Stat GetStat(string statId)
		{
			return stats.FirstOrDefault(s => s.StatId == statId);
		}

		public Stat GetStat(StatType type)
		{
			return stats.FirstOrDefault(s => s.StatType == type);
		}

		public bool HasStat(string statId)
		{
			return stats.Any(s => s.StatId == statId);
		}

		public bool HasStat(StatType type)
		{
			return stats.Any(s => s.StatType == type);
		}

		public void ClearStats()
		{
			foreach (var stat in stats)
			{
				UnsubscribeFromStatEvents(stat);
			}
			stats.Clear();
		}

		#endregion

		#region Query Methods

		public IEnumerable<Stat> GetVitalStats()
		{
			return stats.Where(s => s.StatType is StatType.Health or StatType.Mana or StatType.Stamina);
		}

		public IEnumerable<Stat> GetCombatStats()
		{
			return stats.Where(s => s.StatType is StatType.Attack or StatType.Defense or StatType.Speed 
				or StatType.CriticalRate or StatType.CriticalDamage);
		}

		public IEnumerable<Stat> GetDepletedStats()
		{
			return stats.Where(s => s.IsDepleted());
		}

		public IEnumerable<Stat> GetRegenerableStats()
		{
			return stats.Where(s => s.CanRegenerate && !s.IsAtMax());
		}

		public IEnumerable<Stat> GetStatsByType(StatType type)
		{
			return stats.Where(s => s.StatType == type);
		}

		/// <summary>
		/// Process regeneration for all regenerable stats
		/// </summary>
		/// <param name="deltaTime">Time since last update in seconds</param>
		public void ProcessRegen(float deltaTime)
		{
			foreach (var stat in stats)
			{
				if (stat.CanRegenerate && !stat.IsAtMax())
				{
					float regenAmount = stat.RegenRate * deltaTime;
					float newValue = Math.Min(stat.CurrentValue + regenAmount, stat.MaxValue);
					stat.SetCurrent(newValue);

					if (stat.IsAtMax())
					{
						OnRegenComplete?.Invoke(stat);
					}
				}
			}
		}

		/// <summary>
		/// Check if any vital stat is depleted
		/// </summary>
		public bool IsDead()
		{
			return GetVitalStats().Any(s => s.StatType == StatType.Health && s.IsDepleted());
		}

		/// <summary>
		/// Check if unit can act (has resources)
		/// </summary>
		public bool CanAct()
		{
			var vitalStats = GetVitalStats().ToList();
			
			// Need at least some stamina/mana to act
			if (vitalStats.Any(s => s.StatType == StatType.Stamina && s.IsDepleted()))
				return false;
				
			return true;
		}

		#endregion

		#region Stat Modification

		/// <summary>
		/// Modify current value of a stat
		/// </summary>
		public void ModifyStat(string statId, float amount)
		{
			var stat = GetStat(statId);
			if (stat == null) return;

			if (amount > 0)
			{
				stat.Add(amount);
			}
			else
			{
				stat.Subtract(-amount);
			}
		}

		/// <summary>
		/// Modify current value of a stat by type
		/// </summary>
		public void ModifyStat(StatType type, float amount)
		{
			var stat = GetStat(type);
			if (stat == null) return;

			if (amount > 0)
			{
				stat.Add(amount);
			}
			else
			{
				stat.Subtract(-amount);
			}
		}

		/// <summary>
		/// Take damage - reduces HP and returns actual damage taken
		/// </summary>
		public float TakeDamage(float damage)
		{
			var hp = GetStat(StatType.Health);
			if (hp == null) return 0;

			float actualDamage = Math.Min(damage, hp.CurrentValue);
			hp.Subtract(actualDamage);
			
			return actualDamage;
		}

		/// <summary>
		/// Heal - increases HP without exceeding max
		/// </summary>
		public float Heal(float amount)
		{
			var hp = GetStat(StatType.Health);
			if (hp == null) return 0;

			float healAmount = Math.Min(amount, hp.MaxValue - hp.CurrentValue);
			hp.Add(healAmount);
			
			return healAmount;
		}

		/// <summary>
		/// Consume resource (mana/stamina)
		/// </summary>
		public bool Consume(StatType resourceType, float amount)
		{
			var stat = GetStat(resourceType);
			if (stat == null || stat.CurrentValue < amount)
				return false;

			stat.Subtract(amount);
			return true;
		}

		/// <summary>
		/// Restore all stats to max
		/// </summary>
		public void RestoreAll()
		{
			foreach (var stat in stats)
			{
				stat.RestoreToMax();
			}
		}

		/// <summary>
		/// Restore a specific stat to max
		/// </summary>
		public void Restore(string statId)
		{
			var stat = GetStat(statId);
			stat?.RestoreToMax();
		}

		#endregion

		#region Modifiers

		public void AddModifier(string statId, IModifier<float> modifier)
		{
			var stat = GetStat(statId);
			stat?.Modifiers.Add(modifier);
		}

		public void AddMaxModifier(string statId, IModifier<float> modifier)
		{
			var stat = GetStat(statId);
			stat?.MaxModifiers.Add(modifier);
		}

		public void ClearModifiers(string statId)
		{
			var stat = GetStat(statId);
			if (stat != null)
			{
				stat.Modifiers.Clear();
				stat.MaxModifiers.Clear();
			}
		}

		public void ClearAllModifiers()
		{
			foreach (var stat in stats)
			{
				stat.Modifiers.Clear();
				stat.MaxModifiers.Clear();
			}
		}

		#endregion

		#region Level Up

		public void LevelUp()
		{
			Level++;

			foreach (var stat in stats)
			{
				float baseIncrease = GetStatIncrease(stat.StatType);
				float maxIncrease = GetMaxIncrease(stat.StatType);
				stat.LevelUp(baseIncrease, maxIncrease);
			}

			OnLevelUp?.Invoke();
		}

		public void LevelUp(int levels)
		{
			for (int i = 0; i < levels; i++)
			{
				LevelUp();
			}
		}

		private float GetStatIncrease(StatType type)
		{
			return type switch
			{
				StatType.Health => 10f,
				StatType.Mana => 5f,
				StatType.Stamina => 5f,
				StatType.Attack => 2f,
				StatType.Defense => 1f,
				StatType.Speed => 1f,
				StatType.CriticalRate => 0.01f,
				StatType.CriticalDamage => 0.05f,
				StatType.Accuracy => 0.01f,
				StatType.Evasion => 0.01f,
				_ => 1f
			};
		}

		private float GetMaxIncrease(StatType type)
		{
			return type switch
			{
				StatType.Health => 10f,
				StatType.Mana => 5f,
				StatType.Stamina => 5f,
				_ => 0f
			};
		}

		#endregion

		#region Cloning

		public UnitStats Clone()
		{
			var clone = new UnitStats(unitId, unitName, level);
			
			foreach (var stat in stats)
			{
				clone.AddStat(stat.Clone());
			}
			
			return clone;
		}

		#endregion

		#region Private Methods

		private void SubscribeToStatEvents(Stat stat)
		{
			if (stat == null) return;
			
			stat.OnValueChanged += OnStatValueChanged;
		}

		private void UnsubscribeFromStatEvents(Stat stat)
		{
			if (stat == null) return;
			
			stat.OnValueChanged -= OnStatValueChanged;
		}

		private void OnStatValueChanged(Stat stat, float newValue)
		{
			OnStatChanged?.Invoke(stat);

			if (stat.IsDepleted() && (stat.StatType == StatType.Health))
			{
				OnStatDepleted?.Invoke(stat);
			}
		}

		#endregion

		#region Factory Methods

		public static UnitStats CreateDefault(string unitName = "Hero", int level = 1)
		{
			var unit = new UnitStats(Guid.NewGuid().ToString(), unitName, level);

			unit.AddStat(new Stat("hp", "Health", StatType.Health, 100f, 100f, true, 1f));
			unit.AddStat(new Stat("mp", "Mana", StatType.Mana, 50f, 50f, true, 2f));
			unit.AddStat(new Stat("stamina", "Stamina", StatType.Stamina, 100f, 100f, true, 5f));

			unit.AddStat(new Stat("attack", "Attack", StatType.Attack, 20f));
			unit.AddStat(new Stat("defense", "Defense", StatType.Defense, 10f));
			unit.AddStat(new Stat("speed", "Speed", StatType.Speed, 15f));

			return unit;
		}

		#endregion
	}
}

