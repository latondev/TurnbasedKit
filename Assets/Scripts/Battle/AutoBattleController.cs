using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameSystems.Common;
using GameSystems.Skills;
using GameSystems.Stats;

namespace GameSystems.AutoBattle
{
    /// <summary>
    /// Complete Auto Battle Controller with turn-based combat
    /// </summary>
    public class AutoBattleController : MonoBehaviour
    {
        [SerializeField] private bool debugMode = true;
        [SerializeField] private float turnDelay = 1f;
        [SerializeField] private float battleSpeed = 1f;
        [SerializeField] private int maxTurns = 100;

        [Header("Battle Units")]
        [SerializeField] private List<BattleUnit> playerUnits = new List<BattleUnit>();
        [SerializeField] private List<BattleUnit> enemyUnits = new List<BattleUnit>();

        [Header("Player Stats Sources")]
        [SerializeField] private PlayerStatsCalculator statsCalculator;
        [SerializeField] private GameSystems.Skills.SkillController skillController;

        [Header("Battle State")]
        [SerializeField] private BattleState currentState = BattleState.Idle;
        [SerializeField] private int currentTurn = 0;
        [SerializeField] private BattleUnit currentActiveUnit;
        [SerializeField] private List<BattleTurn> turnHistory = new List<BattleTurn>();

        [Header("Battle Results")]
        [SerializeField] private BattleResult lastResult;
        [SerializeField] private int totalBattles = 0;
        [SerializeField] private int victoriesCount = 0;
        [SerializeField] private int defeatsCount = 0;

        [Header("Visual Sync")]
        public bool IsWaitingForVisuals = false;

        private Queue<BattleUnit> turnQueue;
        private Coroutine battleCoroutine;

        public BattleState CurrentState => currentState;
        public int CurrentTurn => currentTurn;
        public BattleUnit CurrentActiveUnit => currentActiveUnit;
        public List<BattleUnit> PlayerUnits => playerUnits;
        public List<BattleUnit> EnemyUnits => enemyUnits;
        public BattleResult LastResult => lastResult;
        public int TotalBattles => totalBattles;
        public int VictoriesCount => victoriesCount;
        public int DefeatsCount => defeatsCount;
        public PlayerStatsCalculator StatsCalculator => statsCalculator;
        public GameSystems.Skills.SkillController SkillController => skillController;

        // Events
        public event Action<BattleState> OnBattleStateChanged;
        public event Action<BattleTurn> OnTurnStarted;
        public event Action<BattleAction> OnActionExecuted;
        public event Action<BattleTurn> OnTurnEnded;
        public event Action<BattleResult> OnBattleEnded;

        void Start()
        {
            if (playerUnits.Count == 0)
            {
                SetupExampleBattle();
            }

            LogDebug("✅ Auto Battle System ready!");
        }

        #region Setup

        private void SetupExampleBattle()
        {
            GenerateRandomTeam(playerUnits, UnitType.Player, "Player");
            GenerateRandomTeam(enemyUnits, UnitType.Enemy, "Enemy");
            LogDebug("Random battle setup complete");
        }

        private static readonly string[] MeleeNames = { "Knight", "Warrior", "Berserker", "Paladin", "Samurai" };
        private static readonly string[] RangedNames = { "Archer", "Mage", "Gunner", "Sniper", "Wizard" };

        /// <summary>
        /// Gets a random skill from SkillController, falls back to default if not available
        /// </summary>
        private SkillData GetRandomSkill()
        {
            // Try to get from SkillController
            if (skillController != null && skillController.SkillData != null)
            {
                var unlockedSkills = skillController.SkillData.GetUnlockedSkills();
                if (unlockedSkills.Count > 0)
                {
                    return unlockedSkills[UnityEngine.Random.Range(0, unlockedSkills.Count)];
                }
            }

            // Fallback: create a basic skill if no controller
            return new SkillData(
                "default_attack",
                "Power Strike",
                "A basic attack skill",
                SkillCategory.Active,
                SkillElement.Physical,
                20, 3f, 100f
            );
        }

        /// <summary>
        /// Get all unlocked skills from SkillController for team setup
        /// </summary>
        public List<SkillData> GetUnlockedSkills()
        {
            if (skillController != null && skillController.SkillData != null)
            {
                return skillController.SkillData.GetUnlockedSkills();
            }
            return new List<SkillData>();
        }

        private void GenerateRandomTeam(List<BattleUnit> team, UnitType type, string prefix)
        {
            int count = UnityEngine.Random.Range(1, 6); // 1-5 units
            for (int i = 0; i < count; i++)
            {
                bool isMelee = UnityEngine.Random.value > 0.4f; // 60% melee, 40% ranged
                AttackRange range = isMelee ? AttackRange.Melee : AttackRange.Ranged;

                string[] namePool = isMelee ? MeleeNames : RangedNames;
                string unitName = namePool[UnityEngine.Random.Range(0, namePool.Length)];

                int hp = UnityEngine.Random.Range(1500, 4000);
                int atk = isMelee ? UnityEngine.Random.Range(100, 200) : UnityEngine.Random.Range(120, 250);
                int def = isMelee ? UnityEngine.Random.Range(40, 80) : UnityEngine.Random.Range(10, 40);
                int spd = isMelee ? UnityEngine.Random.Range(50, 90) : UnityEngine.Random.Range(70, 120);

                // Get skill from SkillController (or fallback)
                SkillData skill = GetRandomSkill();
                int skillCd = skill != null ? Mathf.RoundToInt(skill.BaseCooldown) : 3;

                var unit = new BattleUnit(
                    $"{prefix.ToLower()}_{i}",
                    $"{unitName}",
                    type, range,
                    hp, atk, def, spd,
                    skill?.SkillName ?? "Power Strike", 2, skillCd
                );

                // Assign SkillData if available
                if (skill != null)
                {
                    unit.EquipSkill(skill);
                }

                team.Add(unit);

                LogDebug($"  [{prefix}] {unitName} ({range}) HP:{hp} ATK:{atk} DEF:{def} SPD:{spd} Skill:{skill?.SkillName ?? "None"}");
            }
        }

        #endregion

        #region Battle Control

        /// <summary>
        /// Starts auto battle
        /// </summary>
        public void StartBattle()
        {
            if (currentState != BattleState.Idle)
            {
                LogDebug("Battle already in progress!");
                return;
            }

            if (playerUnits.Count == 0 || enemyUnits.Count == 0)
            {
                LogDebug("<color=red>Need both player and enemy units!</color>");
                return;
            }

            InitializeBattle();
            ChangeBattleState(BattleState.InProgress);

            battleCoroutine = StartCoroutine(BattleLoop());
        }

        /// <summary>
        /// Stops current battle
        /// </summary>
        public void StopBattle()
        {
            if (battleCoroutine != null)
            {
                StopCoroutine(battleCoroutine);
                battleCoroutine = null;
            }

            ChangeBattleState(BattleState.Idle);
            LogDebug("Battle stopped");
        }

        /// <summary>
        /// Initializes battle - applies player stats from Equipment + Formation + Pet using StatsSystem
        /// </summary>
        private void InitializeBattle()
        {
            // Apply unified player stats if calculator is available
            if (statsCalculator != null)
            {
                var playerStats = statsCalculator.CalculateTotalStats();
                LogDebug($"<color=green>Applied PlayerStats: {playerStats}");

                foreach (var unit in playerUnits)
                {
                    // Apply stats using new method
                    ApplyPlayerStatsToUnit(unit, playerStats);
                }

                // Enemies get a base stat bonus (optional)
                ApplyEnemyBaseStats();
            }

            // Reset all units
            foreach (var unit in playerUnits)
            {
                unit.Reset();
            }

            foreach (var unit in enemyUnits)
            {
                unit.Reset();
            }

            // Setup turn order based on speed
            turnQueue = new Queue<BattleUnit>(GetTurnOrder());

            currentTurn = 0;
            turnHistory.Clear();
            totalBattles++;

            LogDebug("<color=cyan>═══════ Battle Started ═══════</color>");
            LogDebug($"Player Units: {playerUnits.Count}");
            LogDebug($"Enemy Units: {enemyUnits.Count}");

            // Log player stats
            if (playerUnits.Count > 0)
            {
                var u = playerUnits[0];
                LogDebug($"Player stats: HP:{u.MaxHP} ATK:{u.FinalAttack} DEF:{u.FinalDefense} SPD:{u.FinalSpeed}");
            }
        }

        /// <summary>
        /// Apply PlayerStats to BattleUnit using StatsSystem
        /// </summary>
        private void ApplyPlayerStatsToUnit(BattleUnit unit, PlayerStats stats)
        {
            var statCtrl = unit.StatController;

            var hp = statCtrl.GetStat("hp");
            var atk = statCtrl.GetStat("attack");
            var def = statCtrl.GetStat("defense");
            var spd = statCtrl.GetStat("speed");
            var critRate = statCtrl.GetStat("critical_rate");
            var critDmg = statCtrl.GetStat("critical_damage");

            if (hp != null)
            {
                hp.IncreaseMax(stats.BaseHealth);
            }
            if (atk != null)
            {
                atk.ModifiableValue.InitialValue = Mathf.RoundToInt(atk.ModifiableValue.InitialValue * stats.AttackMultiplier);
            }
            if (def != null)
            {
                def.ModifiableValue.InitialValue = Mathf.RoundToInt(def.ModifiableValue.InitialValue * stats.DefenseMultiplier);
            }
            if (spd != null)
            {
                spd.ModifiableValue.InitialValue = Mathf.RoundToInt(spd.ModifiableValue.InitialValue * stats.SpeedMultiplier);
            }
            if (critRate != null)
            {
                critRate.ModifiableValue.InitialValue = stats.BaseCritRate * 100f * stats.CritRateMultiplier;
            }
            if (critDmg != null)
            {
                critDmg.ModifiableValue.InitialValue = stats.BaseCritDamage * 100f;
            }

            LogDebug($"Applied stats to {unit.UnitName}: HP:{unit.MaxHP} ATK:{unit.FinalAttack}");
        }

        /// <summary>
        /// Apply base stats to enemy units
        /// </summary>
        private void ApplyEnemyBaseStats()
        {
            foreach (var unit in enemyUnits)
            {
                var statCtrl = unit.StatController;

                var hp = statCtrl.GetStat("hp");
                var atk = statCtrl.GetStat("attack");
                var def = statCtrl.GetStat("defense");
                var spd = statCtrl.GetStat("speed");

                if (hp != null) hp.IncreaseMax(1000 - hp.BaseValue);
                if (atk != null) atk.ModifiableValue.InitialValue = 50;
                if (def != null) def.ModifiableValue.InitialValue = 20;
                if (spd != null) spd.ModifiableValue.InitialValue = 60;
            }
        }

        /// <summary>
        /// Main battle loop - Team A acts fully, then Team B acts fully
        /// </summary>
        private IEnumerator BattleLoop()
        {
            bool playerGoesFirst = true; // Players start first

            while (currentState == BattleState.InProgress)
            {
                currentTurn++;

                // Check win/lose conditions
                if (CheckBattleEnd()) yield break;
                if (currentTurn >= maxTurns) { EndBattle(BattleOutcome.Draw); yield break; }

                // Determine which team goes this turn
                List<BattleUnit> activeTeam = playerGoesFirst ? playerUnits : enemyUnits;
                string teamName = playerGoesFirst ? "PLAYER" : "ENEMY";

                LogDebug($"\n<color=cyan>══ Turn {currentTurn}: {teamName} TEAM ══</color>");

                // All alive units in the active team act (sorted by speed)
                var orderedTeam = activeTeam
                    .Where(u => u.IsAlive)
                    .OrderByDescending(u => u.FinalSpeed)
                    .ToList();

                foreach (var unit in orderedTeam)
                {
                    if (!unit.IsAlive) continue; // double check in case killed mid-turn
                    if (CheckBattleEnd()) yield break;

                    // Execute this unit's action
                    BattleTurn turn = new BattleTurn(currentTurn, unit);
                    OnTurnStarted?.Invoke(turn);

                    BattleAction action = DecideAction(unit);
                    if (action != null)
                    {
                        ExecuteAction(action);
                        turn.SetAction(action);
                        OnActionExecuted?.Invoke(action);
                    }

                    turnHistory.Add(turn);
                    OnTurnEnded?.Invoke(turn);

                    // Wait for visual animations to finish
                    while (IsWaitingForVisuals) yield return null;

                    yield return new WaitForSeconds(turnDelay / battleSpeed);
                }

                // Swap to the other team
                playerGoesFirst = !playerGoesFirst;
            }
        }

        /// <summary>
        /// Executes a single turn
        /// </summary>
        private IEnumerator ExecuteTurn(BattleUnit unit)
        {
            currentTurn++;
            
            BattleTurn turn = new BattleTurn(currentTurn, unit);
            OnTurnStarted?.Invoke(turn);

            LogDebug($"\n<color=yellow>--- Turn {currentTurn}: {unit.UnitName} ---</color>");

            // AI decides action
            BattleAction action = DecideAction(unit);
            
            if (action != null)
            {
                ExecuteAction(action);
                turn.SetAction(action);
                OnActionExecuted?.Invoke(action);
            }

            turnHistory.Add(turn);
            OnTurnEnded?.Invoke(turn);

            yield return null;
        }

        /// <summary>
        /// AI decision making - uses new skill system with mana
        /// </summary>
        private BattleAction DecideAction(BattleUnit unit)
        {
            // Get valid targets
            List<BattleUnit> targets = unit.Type == UnitType.Player || unit.Type == UnitType.Ally
                ? GetAliveUnits(enemyUnits)
                : GetAliveUnits(playerUnits);

            if (targets.Count == 0)
                return null;

            // Pick target: prioritize lowest HP
            BattleUnit target = targets.OrderBy(t => t.CurrentHP).First();

            // Try to use skill if available (with mana check)
            // Skill is prioritized - if can cast, 60% chance to cast
            if (unit.CanCastSkill() && UnityEngine.Random.value < 0.6f)
            {
                return new BattleAction(unit, target, ActionType.Skill);
            }

            // Otherwise, normal attack
            return new BattleAction(unit, target, ActionType.Attack);
        }

        /// <summary>
        /// Executes battle action - updated to use new skill system with mana
        /// </summary>
        private void ExecuteAction(BattleAction action)
        {
            switch (action.type)
            {
                case ActionType.Attack:
                    int damage = action.actor.Attack(action.target);
                    action.value = damage;
                    action.isCritical = damage > action.actor.FinalAttack;
                    break;

                case ActionType.Skill:
                    // Use new CastSkill method with mana management
                    int skillDamage = action.actor.CastSkill(action.target);
                    action.value = skillDamage;
                    action.isCritical = skillDamage > action.actor.FinalAttack * 2;
                    break;

                case ActionType.Heal:
                    int healAmount = action.actor.Heal(action.value);
                    action.value = healAmount;
                    break;

                case ActionType.Defend:
                    // Implement defend logic
                    break;
            }

            // Regenerate mana after action
            action.actor.RegenerateMana();

            LogDebug($"  {action.description}");
            LogDebug($"    [Mana: {action.actor.CurrentMana}/{action.actor.MaxMana}]");
        }

        #endregion

        #region Battle End

        /// <summary>
        /// Checks if battle should end
        /// </summary>
        private bool CheckBattleEnd()
        {
            bool allPlayersDead = GetAliveUnits(playerUnits).Count == 0;
            bool allEnemiesDead = GetAliveUnits(enemyUnits).Count == 0;

            if (allPlayersDead)
            {
                EndBattle(BattleOutcome.Defeat);
                return true;
            }

            if (allEnemiesDead)
            {
                EndBattle(BattleOutcome.Victory);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Ends battle with result
        /// </summary>
        private void EndBattle(BattleOutcome outcome)
        {
            ChangeBattleState(BattleState.Ended);

            lastResult = new BattleResult
            {
                outcome = outcome,
                totalTurns = currentTurn,
                playerUnits = new List<BattleUnit>(playerUnits),
                enemyUnits = new List<BattleUnit>(enemyUnits),
                turnHistory = new List<BattleTurn>(turnHistory)
            };

            if (outcome == BattleOutcome.Victory)
                victoriesCount++;
            else if (outcome == BattleOutcome.Defeat)
                defeatsCount++;

            LogDebug("<color=cyan>═══════ Battle Ended ═══════</color>");
            LogDebug($"<color=yellow>Result: {outcome}</color>");
            LogDebug($"Total Turns: {currentTurn}");
            
            OnBattleEnded?.Invoke(lastResult);

            if (battleCoroutine != null)
            {
                StopCoroutine(battleCoroutine);
                battleCoroutine = null;
            }
        }

        #endregion

        #region Helpers

        private List<BattleUnit> GetTurnOrder()
        {
            var allUnits = new List<BattleUnit>();
            allUnits.AddRange(GetAliveUnits(playerUnits));
            allUnits.AddRange(GetAliveUnits(enemyUnits));
            
            // Sort by speed (highest first)
            return allUnits.OrderByDescending(u => u.FinalSpeed).ToList();
        }

        private List<BattleUnit> GetAliveUnits(List<BattleUnit> units)
        {
            return units.Where(u => u.IsAlive).ToList();
        }

        private void ChangeBattleState(BattleState newState)
        {
            if (currentState != newState)
            {
                currentState = newState;
                OnBattleStateChanged?.Invoke(newState);
                LogDebug($"Battle State: {newState}");
            }
        }

        public void SetBattleSpeed(float speed)
        {
            battleSpeed = Mathf.Clamp(speed, 0.5f, 5f);
            LogDebug($"Battle speed set to {battleSpeed}x");
        }

        private void LogDebug(string message)
        {
            if (debugMode)
            {
                Debug.Log($"<color=orange>[Auto Battle]</color> {message}");
            }
        }

        public void ShowBattleInfo()
        {
            Debug.Log("\n<color=cyan>═══════════════════════════════════════</color>");
            Debug.Log($"<color=yellow>⚔️ Auto Battle Statistics</color>");
            Debug.Log("<color=cyan>═══════════════════════════════════════</color>");
            Debug.Log($"State: {currentState}");
            Debug.Log($"Current Turn: {currentTurn}");
            Debug.Log($"Total Battles: {totalBattles}");
            Debug.Log($"Victories: {victoriesCount}");
            Debug.Log($"Defeats: {defeatsCount}");
            Debug.Log($"Win Rate: {(totalBattles > 0 ? (float)victoriesCount / totalBattles * 100f : 0f):F1}%");
            Debug.Log("<color=cyan>═══════════════════════════════════════</color>\n");
        }

        #endregion
    }

    public enum BattleState
    {
        Idle,
        InProgress,
        Paused,
        Ended
    }

    public enum BattleOutcome
    {
        Victory,
        Defeat,
        Draw
    }

    [Serializable]
    public class BattleResult
    {
        public BattleOutcome outcome;
        public int totalTurns;
        public List<BattleUnit> playerUnits;
        public List<BattleUnit> enemyUnits;
        public List<BattleTurn> turnHistory;
    }
}
