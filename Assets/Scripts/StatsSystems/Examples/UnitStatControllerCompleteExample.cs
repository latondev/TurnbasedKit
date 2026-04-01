using System;
using System.Collections;
using System.Collections.Generic;
using GameSystems.Stats;
using UnityEngine;

namespace StatsSystems.Examples
{
	/// <summary>
	/// Full demo for UnitStatController:
	/// - level up
	/// - permanent buffs/debuffs
	/// - turn-based effects
	/// - time-based effects
	/// - damage, heal, consume, regen, death, reset
	/// </summary>
	public class UnitStatControllerCompleteExample : MonoBehaviour
	{
		[Header("References")]
		[SerializeField] private UnitStatController unitStats;

		[Header("Demo")]
		[SerializeField, Min(1)] private int startingLevel = 1;
		[SerializeField] private bool runOnStart = true;
		[SerializeField, Min(1)] private int turnsToSimulate = 4;
		[SerializeField, Min(0.1f)] private float temporaryEffectDuration = 3f;

		private TurnTracker turnTracker;
		private readonly List<IDisposable> temporaryHandles = new();
		private bool isSetup;
		private bool eventsBound;

		private Action<Stat> onStatChangedHandler;
		private Action<Stat> onStatDepletedHandler;
		private Action onLevelUpHandler;
		private Action<Stat> onRegenCompleteHandler;

		private IEnumerator Start()
		{
			if (unitStats == null)
			{
				unitStats = GetComponent<UnitStatController>();
			}

			// Let UnitStatController finish its own Awake/Start lifecycle first.
			yield return null;

			SetupUnit();

			if (runOnStart)
			{
				yield return RunFullDemo();
			}
		}

		[ContextMenu("Setup Unit")]
		public void SetupUnit()
		{
			if (unitStats == null)
			{
				unitStats = GetComponent<UnitStatController>();
			}

			if (unitStats == null)
			{
				Debug.LogError("[UnitStatControllerCompleteExample] Missing UnitStatController reference.");
				return;
			}

			UnbindEvents();
			DisposeTemporaryHandles();

			turnTracker ??= new TurnTracker();
			turnTracker.Reset();

			unitStats.StopRegen();
			unitStats.EnableRegen = false;

			var stats = unitStats.Stats;
			if (stats == null)
			{
				unitStats.Initialize(CreateDemoUnitStats(startingLevel));
			}
			else
			{
				stats.ClearStats();
				stats.Level = startingLevel;
				AddDemoStats(stats);
			}

			BindEventsOnce();

			gameObject.name = "Hero Knight";
			isSetup = true;

			LogSnapshot("Initial setup");
		}

		[ContextMenu("Run Full Demo")]
		public void RunFullDemoFromContext()
		{
			if (!isSetup)
			{
				SetupUnit();
			}

			if (Application.isPlaying)
			{
				StartCoroutine(RunFullDemo());
			}
			else
			{
				Debug.LogWarning("[Stats Demo] Run Full Demo can only be started in Play Mode.");
			}
		}

		private IEnumerator RunFullDemo()
		{
			if (!isSetup)
			{
				yield break;
			}

			LogSection("LEVEL UP");
			unitStats.LevelUp();
			unitStats.LevelUp(2);
			LogSnapshot("After level up x3");

			yield return new WaitForSeconds(0.25f);

			LogSection("PERMANENT BUFFS");
			ApplyPermanentBuffs();
			LogSnapshot("After permanent buffs");

			yield return new WaitForSeconds(0.25f);

			LogSection("PERMANENT DEBUFFS");
			ApplyPermanentDebuffs();
			LogSnapshot("After permanent debuffs");

			yield return new WaitForSeconds(0.25f);

			LogSection("TEMPORARY EFFECTS");
			ApplyTemporaryTimeBuff();
			ApplyTemporaryTurnEffects();
			LogSnapshot("After temporary effects");

			yield return new WaitForSeconds(0.25f);

			LogSection("TURN SIMULATION");
			for (int i = 0; i < turnsToSimulate; i++)
			{
				AdvanceTurn();
				yield return new WaitForSeconds(0.25f);
			}

			yield return new WaitForSeconds(temporaryEffectDuration + 0.25f);

			LogSection("COMBAT ACTIONS");
			DemoCombatActions();
			LogSnapshot("After combat actions");

			yield return new WaitForSeconds(0.25f);

			LogSection("REGEN");
			unitStats.ProcessRegen(2f);
			LogSnapshot("After manual regen tick");

			yield return new WaitForSeconds(0.25f);

			LogSection("RESET");
			ResetDemo();
			LogSnapshot("After reset");
		}

		[ContextMenu("Advance Turn")]
		public void AdvanceTurn()
		{
			if (turnTracker == null)
			{
				turnTracker = new TurnTracker();
			}

			turnTracker.NextTurn();
			Debug.Log($"[Stats Demo] Turn {turnTracker.CurrentTurn} processed.");
			LogSnapshot($"Turn {turnTracker.CurrentTurn}");
		}

		[ContextMenu("Apply Permanent Buffs")]
		public void ApplyPermanentBuffs()
		{
			if (!EnsureReady()) return;

			// Order matters: additive bonuses first, multipliers after.
			unitStats.AddModifier(StatType.Attack, Modifier.Plus(12f, priority: 0, name: "Weapon"));
			unitStats.AddModifier(StatType.Attack, Modifier.Times(1.25f, priority: 100, name: "Rage"));

			// Max HP buff.
			unitStats.AddMaxModifier(StatType.Health, Modifier.Plus(40f, priority: 0, name: "Vitality"));
		}

		[ContextMenu("Apply Permanent Debuffs")]
		public void ApplyPermanentDebuffs()
		{
			if (!EnsureReady()) return;

			unitStats.AddModifier(StatType.Defense, Modifier.Minus(8f, priority: 0, name: "Armor Break"));
			unitStats.AddModifier(StatType.Speed, Modifier.Divide(1.25f, priority: 0, name: "Slow"));
		}

		[ContextMenu("Apply Time Buff")]
		public void ApplyTemporaryTimeBuff()
		{
			if (!EnsureReady()) return;

			// A temporary HP max boost that expires after real time.
			var shieldWall = Modifier.Plus(30f, priority: 0, name: "Shield Wall");
			unitStats.AddMaxModifier(StatType.Health, shieldWall);
			temporaryHandles.Add(shieldWall.DisableAfter(TimeSpan.FromSeconds(temporaryEffectDuration)));
		}

		[ContextMenu("Apply Turn Effects")]
		public void ApplyTemporaryTurnEffects()
		{
			if (!EnsureReady()) return;

			// Turn-based buff: attack up for 3 turns.
			var rage = TurnBasedModifierFactory.Times(1.35f, turns: 3, priority: 100, name: "Turn Rage");
			unitStats.AddModifier(StatType.Attack, (IModifier<float>)rage);
			temporaryHandles.Add(rage.DisableAfterTurns(turnTracker));

			// Turn-based debuff: accuracy down for 2 turns.
			var blind = TurnBasedModifierFactory.Minus(15f, turns: 2, priority: 0, name: "Blind");
			unitStats.AddModifier(StatType.Accuracy, (IModifier<float>)blind);
			temporaryHandles.Add(blind.DisableAfterTurns(turnTracker));
		}

		[ContextMenu("Combat Actions")]
		public void DemoCombatActions()
		{
			if (!EnsureReady()) return;

			var hp = unitStats.GetStat(StatType.Health);
			var mana = unitStats.GetStat(StatType.Mana);
			var stamina = unitStats.GetStat(StatType.Stamina);

			Debug.Log($"[Stats Demo] Before combat: HP {hp.CurrentValue:F0}, MP {mana.CurrentValue:F0}, STA {stamina.CurrentValue:F0}");

			// Standard resource usage.
			unitStats.Consume(StatType.Mana, 15f);
			unitStats.Consume(StatType.Stamina, 20f);

			// Normal damage and heal.
			var damageTaken = unitStats.TakeDamage(55f);
			Debug.Log($"[Stats Demo] Damage taken: {damageTaken:F0}");

			var healed = unitStats.Heal(25f);
			Debug.Log($"[Stats Demo] Healed: {healed:F0}");

			// Force a depleted state to show the death event path.
			var overkill = unitStats.TakeDamage(hp.CurrentValue + 999f);
			Debug.Log($"[Stats Demo] Overkill damage: {overkill:F0}");

			// Bring the unit back for the next demo step.
			unitStats.RestoreStat(StatType.Health);
			unitStats.RestoreStat(StatType.Mana);
			unitStats.RestoreStat(StatType.Stamina);
		}

		[ContextMenu("Reset Demo")]
		public void ResetDemo()
		{
			if (!EnsureReady()) return;

			DisposeTemporaryHandles();
			unitStats.ClearAllModifiers();
			unitStats.RestoreAll();
			turnTracker?.Reset();
		}

		private UnitStats CreateDemoUnitStats(int level)
		{
			var stats = new UnitStats(level);
			AddDemoStats(stats);
			return stats;
		}

		private void AddDemoStats(UnitStats stats)
		{
			// Vital stats
			stats.AddStat(new Stat(StatType.Health, 220f, 220f, true, 3f));
			stats.AddStat(new Stat(StatType.Mana, 90f, 90f, true, 5f));
			stats.AddStat(new Stat(StatType.Stamina, 120f, 120f, true, 8f));

			// Combat stats
			stats.AddStat(new Stat(StatType.Attack, 35f));
			stats.AddStat(new Stat(StatType.Defense, 20f));
			stats.AddStat(new Stat(StatType.Speed, 14f));
			stats.AddStat(new Stat(StatType.CriticalRate, 10f));
			stats.AddStat(new Stat(StatType.CriticalDamage, 150f));
			stats.AddStat(new Stat(StatType.Accuracy, 95f));
			stats.AddStat(new Stat(StatType.Evasion, 8f));
		}

		private void BindEventsOnce()
		{
			if (eventsBound)
			{
				return;
			}

			if (onStatChangedHandler == null)
			{
				onStatChangedHandler = stat =>
				{
					Debug.Log($"[Stats Demo] Changed: {FormatStatLine(stat)}");
				};
			}

			if (onStatDepletedHandler == null)
			{
				onStatDepletedHandler = stat =>
				{
					Debug.LogWarning($"[Stats Demo] Depleted: {stat.StatName}");
				};
			}

			if (onLevelUpHandler == null)
			{
				onLevelUpHandler = () =>
				{
					Debug.Log($"[Stats Demo] Level up -> Lv.{unitStats.Level}");
				};
			}

			if (onRegenCompleteHandler == null)
			{
				onRegenCompleteHandler = stat =>
				{
					Debug.Log($"[Stats Demo] Regen complete: {stat.StatName}");
				};
			}

			unitStats.OnStatChanged += onStatChangedHandler;
			unitStats.OnStatDepleted += onStatDepletedHandler;
			unitStats.OnLevelUp += onLevelUpHandler;
			unitStats.OnRegenComplete += onRegenCompleteHandler;
			eventsBound = true;
		}

		private void UnbindEvents()
		{
			if (!eventsBound || unitStats == null)
			{
				return;
			}

			if (onStatChangedHandler != null) unitStats.OnStatChanged -= onStatChangedHandler;
			if (onStatDepletedHandler != null) unitStats.OnStatDepleted -= onStatDepletedHandler;
			if (onLevelUpHandler != null) unitStats.OnLevelUp -= onLevelUpHandler;
			if (onRegenCompleteHandler != null) unitStats.OnRegenComplete -= onRegenCompleteHandler;

			eventsBound = false;
		}

		private bool EnsureReady()
		{
			if (!isSetup || unitStats == null || unitStats.Stats == null)
			{
				Debug.LogWarning("[Stats Demo] Unit is not ready yet.");
				return false;
			}

			return true;
		}

		private void DisposeTemporaryHandles()
		{
			foreach (var handle in temporaryHandles)
			{
				handle?.Dispose();
			}

			temporaryHandles.Clear();
		}

		private void LogSnapshot(string label)
		{
			if (!EnsureReady()) return;

			Debug.Log($"<color=cyan>[Stats Demo] {label} | {gameObject.name} | Lv.{unitStats.Level}</color>");

			PrintResourceStat(StatType.Health);
			PrintResourceStat(StatType.Mana);
			PrintResourceStat(StatType.Stamina);
			PrintCombatStat(StatType.Attack);
			PrintCombatStat(StatType.Defense);
			PrintCombatStat(StatType.Speed);
			PrintCombatStat(StatType.CriticalRate);
			PrintCombatStat(StatType.CriticalDamage);
			PrintCombatStat(StatType.Accuracy);
			PrintCombatStat(StatType.Evasion);
		}

		private void PrintResourceStat(StatType type)
		{
			var stat = unitStats.GetStat(type);
			if (stat == null) return;

			Debug.Log($"  {stat.GetStatIcon()} {stat.StatName}: {stat.CurrentValue:F0}/{stat.MaxValue:F0} (regen {stat.RegenRate:F1}/s)");
		}

		private void PrintCombatStat(StatType type)
		{
			var stat = unitStats.GetStat(type);
			if (stat == null) return;

			Debug.Log($"  {stat.GetStatIcon()} {stat.StatName}: base {stat.BaseValue:F1} -> final {stat.GetFinalValue():F1}");
		}

		private string FormatStatLine(Stat stat)
		{
			if (stat == null) return "null";

			if (stat.StatType is StatType.Health or StatType.Mana or StatType.Stamina)
			{
				return $"{stat.StatName} = {stat.CurrentValue:F0}/{stat.MaxValue:F0}";
			}

			return $"{stat.StatName} = base {stat.BaseValue:F1}, final {stat.GetFinalValue():F1}";
		}

		private void LogSection(string title)
		{
			Debug.Log($"\n========== {title} ==========");
		}

		private void OnDestroy()
		{
			UnbindEvents();
			DisposeTemporaryHandles();
		}
	}
}
