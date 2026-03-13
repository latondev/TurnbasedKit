using UnityEngine;
using System;

namespace GameSystems.Stats
{
	/// <summary>
	/// Unity MonoBehaviour controller for UnitStats.
	/// Handles Unity-specific concerns: lifecycle, coroutines, serialization.
	/// All game logic lives in UnitStats (pure C#).
	/// </summary>
	public class UnitStatController : MonoBehaviour
	{
		[Header("Configuration")]
		[SerializeField] private string _unitName = "Hero";
		[SerializeField] private int _level = 1;
		[SerializeField] private bool _enableRegen = true;
		[SerializeField] private float _regenInterval = 0.1f;

		private UnitStats _stats;
		private Coroutine _regenCoroutine;

		public string UnitName
		{
			get => _stats?.UnitName ?? _unitName;
			set
			{
				_unitName = value;
				if (_stats != null) _stats.UnitName = value;
			}
		}

		public int Level => _stats?.Level ?? _level;
		public UnitStats Stats => _stats;

		public bool EnableRegen
		{
			get => _enableRegen;
			set => _enableRegen = value;
		}

		// Events - forwarding from UnitStats
		public event Action<Stat> OnStatChanged
		{
			add { if (_stats != null) _stats.OnStatChanged += value; }
			remove { if (_stats != null) _stats.OnStatChanged -= value; }
		}

		public event Action<Stat> OnStatDepleted
		{
			add { if (_stats != null) _stats.OnStatDepleted += value; }
			remove { if (_stats != null) _stats.OnStatDepleted -= value; }
		}

		public event Action OnLevelUp
		{
			add { if (_stats != null) _stats.OnLevelUp += value; }
			remove { if (_stats != null) _stats.OnLevelUp -= value; }
		}

		public event Action<Stat> OnRegenComplete
		{
			add { if (_stats != null) _stats.OnRegenComplete += value; }
			remove { if (_stats != null) _stats.OnRegenComplete -= value; }
		}

		#region Unity Lifecycle

		protected virtual void Awake()
		{
			_stats = new UnitStats(GetInstanceID().ToString(), _unitName, _level);
		}

		protected virtual void Start()
		{
			if (_stats.Count == 0)
			{
				SetupDefaultStats();
			}

			if (_enableRegen)
			{
				StartRegen();
			}
		}

		protected virtual void OnEnable()
		{
			if (_enableRegen && _regenCoroutine == null && _stats != null)
			{
				StartRegen();
			}
		}

		protected virtual void OnDisable()
		{
			StopRegen();
		}

		protected virtual void OnDestroy()
		{
			StopRegen();
		}

		#endregion

		#region Stat Access

		public Stat GetStat(string statId) => _stats?.GetStat(statId);

		public Stat GetStat(StatType type) => _stats?.GetStat(type);

		public float GetStatValue(string statId) => _stats?.GetStat(statId)?.CurrentValue ?? 0f;

		public float GetStatMaxValue(string statId) => _stats?.GetStat(statId)?.MaxValue ?? 0f;

		public float GetStatPercentage(string statId) => _stats?.GetStat(statId)?.GetPercentage() ?? 0f;

		public float GetHpPercentage() => GetStatPercentage("hp");

		public bool HasStat(string statId) => _stats?.HasStat(statId) ?? false;

		public bool IsDead() => _stats?.IsDead() ?? false;

		public bool CanAct() => _stats?.CanAct() ?? false;

		#endregion

		#region Stat Modification

		public void ModifyStat(string statId, float amount) => _stats?.ModifyStat(statId, amount);

		public void ModifyStat(StatType type, float amount) => _stats?.ModifyStat(type, amount);

		public float TakeDamage(float damage) => _stats?.TakeDamage(damage) ?? 0;

		public float Heal(float amount) => _stats?.Heal(amount) ?? 0;

		public bool Consume(StatType resourceType, float amount) => _stats?.Consume(resourceType, amount) ?? false;

		public void RestoreStat(string statId) => _stats?.Restore(statId);

		public void RestoreAll() => _stats?.RestoreAll();

		#endregion

		#region Modifiers

		public void AddModifier(string statId, IModifier<float> modifier) => _stats?.AddModifier(statId, modifier);

		public void AddMaxModifier(string statId, IModifier<float> modifier) => _stats?.AddMaxModifier(statId, modifier);

		public void ClearModifiers(string statId) => _stats?.ClearModifiers(statId);

		public void ClearAllModifiers() => _stats?.ClearAllModifiers();

		#endregion

		#region Level Up

		public void LevelUp()
		{
			_stats?.LevelUp();
			_level = _stats?.Level ?? _level;
		}

		public void LevelUp(int levels)
		{
			_stats?.LevelUp(levels);
			_level = _stats?.Level ?? _level;
		}

		#endregion

		#region Regeneration

		public void StartRegen()
		{
			StopRegen();
			_regenCoroutine = StartCoroutine(RegenCoroutine());
		}

		public void StopRegen()
		{
			if (_regenCoroutine != null)
			{
				StopCoroutine(_regenCoroutine);
				_regenCoroutine = null;
			}
		}

		private System.Collections.IEnumerator RegenCoroutine()
		{
			var wait = new WaitForSeconds(_regenInterval);

			while (true)
			{
				yield return wait;

				if (_stats != null && isActiveAndEnabled)
				{
					_stats.ProcessRegen(_regenInterval);
				}
			}
		}

		/// <summary>
		/// Manual regen tick - for external manager control
		/// </summary>
		public void ProcessRegen(float deltaTime) => _stats?.ProcessRegen(deltaTime);

		#endregion

		#region Setup

		/// <summary>
		/// Initialize with external UnitStats (e.g. from database/save)
		/// </summary>
		public void Initialize(UnitStats unitStats)
		{
			_stats = unitStats ?? throw new ArgumentNullException(nameof(unitStats));
			_unitName = _stats.UnitName;
			_level = _stats.Level;
		}

		[ContextMenu("Setup Default Stats")]
		private void SetupDefaultStats()
		{
			_stats.AddStat(new Stat("hp", "Health", StatType.Health, 100f, 100f, true, 1f));
			_stats.AddStat(new Stat("mp", "Mana", StatType.Mana, 50f, 50f, true, 2f));
			_stats.AddStat(new Stat("stamina", "Stamina", StatType.Stamina, 100f, 100f, true, 5f));

			_stats.AddStat(new Stat("attack", "Attack", StatType.Attack, 20f));
			_stats.AddStat(new Stat("defense", "Defense", StatType.Defense, 10f));
			_stats.AddStat(new Stat("speed", "Speed", StatType.Speed, 15f));
		}

		#endregion

		#region Debug

		[ContextMenu("Show Stats")]
		public void ShowStats()
		{
			if (_stats == null)
			{
				Debug.LogWarning($"<color=yellow>[UnitStatController]</color> No stats initialized");
				return;
			}

			Debug.Log($"<color=cyan>═══ {_stats.UnitName} (Lv.{_stats.Level}) ═══</color>");

			foreach (var stat in _stats.Stats)
			{
				Debug.Log($"  {stat}");
			}

			Debug.Log("<color=cyan>═══════════════════════════════</color>");
		}

		#endregion
	}
}
