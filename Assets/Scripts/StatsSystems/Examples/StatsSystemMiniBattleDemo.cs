using System;
using System.Collections;
using System.Collections.Generic;
using GameSystems.Stats;
using UnityEngine;

namespace StatsSystems.Examples
{
	/// <summary>
	/// Small 1v1 battle demo built on top of UnitStatController.
	/// Two runtime units fight with damage, heal, buffs, debuffs and turn tracking.
	/// </summary>
	public class StatsSystemMiniBattleDemo : MonoBehaviour
	{
		[Header("References")]
		[SerializeField] private StatsSystemLiveOverlay overlay;

		[Header("Demo")]
		[SerializeField] private bool autoStart = true;
		[SerializeField, Range(0.1f, 2f)] private float turnDelay = 0.75f;
		[SerializeField, Range(1f, 3f)] private float battleSpeed = 1f;
		[SerializeField, Min(1)] private int maxRounds = 8;
		[SerializeField, Min(1)] private int heroLevel = 3;
		[SerializeField, Min(1)] private int enemyLevel = 3;

		[Header("Battle Tuning")]
		[SerializeField] private float baseSkillDamageBonus = 8f;
		[SerializeField] private float skillDamageMultiplier = 1.35f;
		[SerializeField] private float critFallbackMultiplier = 1.5f;
		[SerializeField] private float defenseMitigation = 0.5f;
		[SerializeField] private float hitBaseChance = 0.75f;

		private UnitStatController heroUnit;
		private UnitStatController enemyUnit;
		private TurnTracker turnTracker;
		private readonly List<IDisposable> temporaryHandles = new();
		private Coroutine battleRoutine;
		private bool battleReady;

		public UnitStatController HeroUnit => heroUnit;
		public UnitStatController EnemyUnit => enemyUnit;
		public bool IsBattleReady => battleReady;
		public bool IsBattleRunning => battleRoutine != null;
		public float BattleSpeed => battleSpeed;

		private void Awake()
		{
			if (overlay == null)
			{
				overlay = GetComponent<StatsSystemLiveOverlay>();
			}
		}

		private IEnumerator Start()
		{
			yield return null;
			SetupBattle();

			if (autoStart)
			{
				StartBattle();
			}
		}

		[ContextMenu("Setup Battle")]
		public void SetupBattle()
		{
			CleanupBattle();

			turnTracker ??= new TurnTracker();
			turnTracker.Reset();

			heroUnit = CreateUnit("Hero", heroLevel, true);
			enemyUnit = CreateUnit("Goblin", enemyLevel, false);

			ApplyOpeningEffects();

			overlay?.ClearTrackedUnits();
			overlay?.SetTrackedUnits(new[] { heroUnit, enemyUnit });
			overlay?.SetActiveUnit(null);
			overlay?.Log("Battle setup complete.");
			overlay?.Log("Hero opens with a rage buff and extra vitality.");
			overlay?.Log("Goblin starts with armor break and weakness.");

			battleReady = true;
			overlay?.MarkDirty();
		}

		[ContextMenu("Start Battle")]
		public void StartBattle()
		{
			if (IsBattleRunning)
			{
				overlay?.Log("Battle is already running.");
				return;
			}

			if (!battleReady)
			{
				SetupBattle();
			}

			battleRoutine = StartCoroutine(BattleLoop());
		}

		[ContextMenu("Start Battle (Legacy)")]
		public void StartDemoBattle()
		{
			StartBattle();
		}

		[ContextMenu("Stop Battle")]
		public void StopBattle()
		{
			if (battleRoutine == null)
			{
				return;
			}

			StopCoroutine(battleRoutine);
			battleRoutine = null;
			overlay?.Log("Battle stopped.");
			overlay?.MarkDirty();
		}

		[ContextMenu("Reset Battle")]
		public void ResetBattle()
		{
			StopBattle();
			SetupBattle();
		}

		public void ToggleSpeed()
		{
			battleSpeed = Mathf.Approximately(battleSpeed, 1f) ? 2f : 1f;
			overlay?.Log($"Battle speed set to {battleSpeed:0.#}x.");
			overlay?.MarkDirty();
		}

		public void SetBattleSpeed(float speed)
		{
			battleSpeed = Mathf.Clamp(speed, 0.1f, 4f);
			overlay?.MarkDirty();
		}

		private UnitStatController CreateUnit(string unitName, int level, bool isHero)
		{
			var go = new GameObject(unitName);
			go.transform.SetParent(transform, false);

			var controller = go.AddComponent<UnitStatController>();
			controller.EnableRegen = false;
			controller.Initialize(CreateBattleStats(level, isHero));
			return controller;
		}

		private UnitStats CreateBattleStats(int level, bool isHero)
		{
			var stats = new UnitStats(level);

			if (isHero)
			{
				stats.AddStat(new Stat(StatType.Health, 240f, 240f, true, 4f));
				stats.AddStat(new Stat(StatType.Mana, 80f, 80f, true, 5f));
				stats.AddStat(new Stat(StatType.Stamina, 100f, 100f, true, 7f));
				stats.AddStat(new Stat(StatType.Attack, 38f));
				stats.AddStat(new Stat(StatType.Defense, 18f));
				stats.AddStat(new Stat(StatType.Speed, 16f));
				stats.AddStat(new Stat(StatType.CriticalRate, 12f));
				stats.AddStat(new Stat(StatType.CriticalDamage, 160f));
				stats.AddStat(new Stat(StatType.Accuracy, 96f));
				stats.AddStat(new Stat(StatType.Evasion, 10f));
			}
			else
			{
				stats.AddStat(new Stat(StatType.Health, 210f, 210f, true, 2f));
				stats.AddStat(new Stat(StatType.Mana, 60f, 60f, true, 4f));
				stats.AddStat(new Stat(StatType.Stamina, 90f, 90f, true, 6f));
				stats.AddStat(new Stat(StatType.Attack, 32f));
				stats.AddStat(new Stat(StatType.Defense, 14f));
				stats.AddStat(new Stat(StatType.Speed, 13f));
				stats.AddStat(new Stat(StatType.CriticalRate, 8f));
				stats.AddStat(new Stat(StatType.CriticalDamage, 145f));
				stats.AddStat(new Stat(StatType.Accuracy, 90f));
				stats.AddStat(new Stat(StatType.Evasion, 8f));
			}

			return stats;
		}

		private void ApplyOpeningEffects()
		{
			var heroRage = Modifier.Times(1.2f, priority: 100, name: "Hero Rage");
			var heroVitality = Modifier.Plus(20f, priority: 0, name: "Hero Vitality");
			var enemyArmorBreak = Modifier.Minus(4f, priority: 0, name: "Armor Break");
			var enemyWeakness = Modifier.Minus(3f, priority: 0, name: "Weakness");

			heroUnit.AddModifier(StatType.Attack, heroRage);
			heroUnit.AddMaxModifier(StatType.Health, heroVitality);
			heroUnit.Heal(20f);

			enemyUnit.AddModifier(StatType.Defense, enemyArmorBreak);
			enemyUnit.AddModifier(StatType.Attack, enemyWeakness);

			temporaryHandles.Add(heroRage.DisableAfterTurns(3, turnTracker));
			temporaryHandles.Add(heroVitality.DisableAfter(TimeSpan.FromSeconds(6f)));
			temporaryHandles.Add(enemyArmorBreak.DisableAfterTurns(2, turnTracker));
			temporaryHandles.Add(enemyWeakness.DisableAfterTurns(2, turnTracker));
		}

		private IEnumerator BattleLoop()
		{
			overlay?.Log("Battle started.");

			for (int round = 1; round <= maxRounds; round++)
			{
				if (IsBattleOver())
				{
					break;
				}

				turnTracker.NextTurn();
				overlay?.Log($"Round {round}");

				var first = GetFirstActor();
				var second = first == heroUnit ? enemyUnit : heroUnit;

				yield return ExecuteAction(first, second, round);
				if (IsBattleOver())
				{
					break;
				}

				yield return ExecuteAction(second, first, round);
				if (IsBattleOver())
				{
					break;
				}

				heroUnit.ProcessRegen(1f);
				enemyUnit.ProcessRegen(1f);
				overlay?.MarkDirty();

				yield return WaitScaled(turnDelay);
			}

			if (!IsBattleOver())
			{
				overlay?.Log("Battle ended by round limit.");
			}

			overlay?.Log(GetBattleResult());
			battleRoutine = null;
			overlay?.MarkDirty();
		}

		private IEnumerator ExecuteAction(UnitStatController actor, UnitStatController target, int round)
		{
			if (actor == null || target == null || actor.IsDead() || target.IsDead())
			{
				yield break;
			}

			overlay?.SetActiveUnit(actor);

			var staminaCost = 6f;
			var manaCost = 0f;
			var useSkill = round % 3 == 0;
			var useHeal = actor.GetHpPercentage() <= 0.4f && actor.GetStatValue(StatType.Mana) >= 10f;

			if (useHeal)
			{
				manaCost = 10f;
				if (!actor.Consume(StatType.Mana, manaCost))
				{
					overlay?.Log($"{actor.UnitName} wanted to heal but had no mana.");
					yield break;
				}

				if (!actor.Consume(StatType.Stamina, staminaCost))
				{
					overlay?.Log($"{actor.UnitName} is too tired to heal.");
					yield break;
				}

				var healAmount = actor.Heal(22f + actor.GetStatValue(StatType.Attack) * 0.25f);
				overlay?.Log($"{actor.UnitName} uses First Aid and heals {healAmount:F0} HP.");
				overlay?.MarkDirty();
				yield return WaitScaled(turnDelay * 0.5f);
				yield break;
			}

			if (useSkill)
			{
				manaCost = 12f;
				if (!actor.Consume(StatType.Mana, manaCost))
				{
					useSkill = false;
				}
			}

			if (!actor.Consume(StatType.Stamina, staminaCost))
			{
				overlay?.Log($"{actor.UnitName} is too tired to act.");
				yield break;
			}

			var attack = actor.GetStatValue(StatType.Attack);
			var defense = target.GetStatValue(StatType.Defense);
			var accuracy = actor.GetStatValue(StatType.Accuracy);
			var evasion = target.GetStatValue(StatType.Evasion);
			var critRate = actor.GetStatValue(StatType.CriticalRate) / 100f;
			var critDamage = actor.GetStatValue(StatType.CriticalDamage) / 100f;

			var hitChance = Mathf.Clamp01(hitBaseChance + ((accuracy - evasion) / 200f));
			if (UnityEngine.Random.value > hitChance)
			{
				overlay?.Log($"{actor.UnitName} attacks {target.UnitName} but misses.");
				overlay?.MarkDirty();
				yield return WaitScaled(turnDelay * 0.5f);
				yield break;
			}

			var damage = Mathf.Max(1f, attack - defense * defenseMitigation);
			if (useSkill)
			{
				damage = damage * skillDamageMultiplier + baseSkillDamageBonus;
			}

			var isCritical = UnityEngine.Random.value < critRate;
			if (isCritical)
			{
				damage *= Mathf.Max(critFallbackMultiplier, critDamage);
			}

			var dealt = target.TakeDamage(damage);
			var tag = useSkill ? "Skill" : "Attack";
			var critTag = isCritical ? " CRIT" : string.Empty;
			overlay?.Log($"{actor.UnitName} uses {tag} on {target.UnitName} for {dealt:F0} damage{critTag}.");

			if (target.IsDead())
			{
				overlay?.Log($"{target.UnitName} is defeated.");
			}

			overlay?.MarkDirty();
			yield return WaitScaled(turnDelay * 0.5f);
		}

		private WaitForSeconds WaitScaled(float seconds)
		{
			var speed = Mathf.Max(0.1f, battleSpeed);
			return new WaitForSeconds(Mathf.Max(0f, seconds) / speed);
		}

		private UnitStatController GetFirstActor()
		{
			var heroSpeed = heroUnit.GetStatValue(StatType.Speed);
			var enemySpeed = enemyUnit.GetStatValue(StatType.Speed);
			return heroSpeed >= enemySpeed ? heroUnit : enemyUnit;
		}

		private bool IsBattleOver()
		{
			return heroUnit == null || enemyUnit == null || heroUnit.IsDead() || enemyUnit.IsDead();
		}

		private string GetBattleResult()
		{
			if (heroUnit == null || enemyUnit == null)
			{
				return "Battle result unavailable.";
			}

			if (heroUnit.IsDead() && enemyUnit.IsDead())
			{
				return "Battle result: draw.";
			}

			if (enemyUnit.IsDead())
			{
				return "Battle result: hero wins.";
			}

			if (heroUnit.IsDead())
			{
				return "Battle result: enemy wins.";
			}

			return "Battle result: no winner.";
		}

		private void CleanupBattle()
		{
			foreach (var handle in temporaryHandles)
			{
				handle?.Dispose();
			}

			temporaryHandles.Clear();

			if (overlay != null)
			{
				overlay.ClearTrackedUnits();
				overlay.SetActiveUnit(null);
			}

			if (heroUnit != null)
			{
				Destroy(heroUnit.gameObject);
				heroUnit = null;
			}

			if (enemyUnit != null)
			{
				Destroy(enemyUnit.gameObject);
				enemyUnit = null;
			}

			battleReady = false;
		}

		private void OnDestroy()
		{
			if (battleRoutine != null)
			{
				StopCoroutine(battleRoutine);
				battleRoutine = null;
			}

			CleanupBattle();
		}
	}
}
