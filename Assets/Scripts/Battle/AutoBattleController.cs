using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        private static readonly string[] MeleeSkills = { "Whirlwind Slash", "Shield Bash", "Power Strike", "Dragon Fist", "Thunder Cleave" };
        private static readonly string[] RangedSkills = { "Fire Arrow", "Ice Blast", "Lightning Bolt", "Meteor Shot", "Poison Cloud" };

        private void GenerateRandomTeam(List<BattleUnit> team, UnitType type, string prefix)
        {
            int count = UnityEngine.Random.Range(1, 6); // 1-5 units
            for (int i = 0; i < count; i++)
            {
                bool isMelee = UnityEngine.Random.value > 0.4f; // 60% melee, 40% ranged
                AttackRange range = isMelee ? AttackRange.Melee : AttackRange.Ranged;
                
                string[] namePool = isMelee ? MeleeNames : RangedNames;
                string[] skillPool = isMelee ? MeleeSkills : RangedSkills;
                string unitName = namePool[UnityEngine.Random.Range(0, namePool.Length)];
                string skillName = skillPool[UnityEngine.Random.Range(0, skillPool.Length)];
                
                int hp = UnityEngine.Random.Range(1500, 4000);
                int atk = isMelee ? UnityEngine.Random.Range(100, 200) : UnityEngine.Random.Range(120, 250);
                int def = isMelee ? UnityEngine.Random.Range(40, 80) : UnityEngine.Random.Range(10, 40);
                int spd = isMelee ? UnityEngine.Random.Range(50, 90) : UnityEngine.Random.Range(70, 120);
                int skillMult = isMelee ? UnityEngine.Random.Range(2, 4) : UnityEngine.Random.Range(2, 5);
                int skillCd = UnityEngine.Random.Range(2, 5);
                
                var unit = new BattleUnit(
                    $"{prefix.ToLower()}_{i}",
                    $"{unitName}",
                    type, range,
                    hp, atk, def, spd,
                    skillName, skillMult, skillCd
                );
                team.Add(unit);
                
                LogDebug($"  [{prefix}] {unitName} ({range}) HP:{hp} ATK:{atk} DEF:{def} SPD:{spd} Skill:{skillName}");
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
        /// Initializes battle
        /// </summary>
        private void InitializeBattle()
        {
            // Reset all units
            foreach (var unit in playerUnits)
            {
                unit.Reset();
                unit.CalculateFinalStats();
            }

            foreach (var unit in enemyUnits)
            {
                unit.Reset();
                unit.CalculateFinalStats();
            }

            // Setup turn order based on speed
            turnQueue = new Queue<BattleUnit>(GetTurnOrder());
            
            currentTurn = 0;
            turnHistory.Clear();
            totalBattles++;

            LogDebug("<color=cyan>═══════ Battle Started ═══════</color>");
            LogDebug($"Player Units: {playerUnits.Count}");
            LogDebug($"Enemy Units: {enemyUnits.Count}");
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
        /// AI decision making
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

            // Use skill if ready (30% chance to use even if ready, to mix it up)
            if (unit.IsSkillReady && UnityEngine.Random.value < 0.6f)
            {
                return new BattleAction(unit, target, ActionType.Skill);
            }

            return new BattleAction(unit, target, ActionType.Attack);
        }

        /// <summary>
        /// Executes battle action
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
                    int skillDamage = action.actor.SkillAttack(action.target);
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

            LogDebug($"  {action.description}");
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
