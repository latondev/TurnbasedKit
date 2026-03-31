using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameSystems.AutoBattle;
using GameSystems.Battle;
using GameSystems.Skills;
using GameSystems.Stats;
using AttackRange = GameSystems.AutoBattle.AttackRange;

namespace GameSystems.Battle.Demo
{
    /// <summary>
    /// Entry-point cho BattleDemo scene.
    /// Spawn 2 team BattleUnit -> gọi AutoBattleController -> gắn events -> cập nhật UI
    /// </summary>
    public class BattleSceneSetup : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AutoBattleController _battleController;
        [SerializeField] private BattleUIView _uiView;
        [SerializeField] private BattleVisualManager _visualManager;

        [Header("Team Config")]
        [SerializeField] private int _playerTeamSize = 3;
        [SerializeField] private int _enemyTeamSize = 3;
        [SerializeField] private float _battleSpeed = 1f;

        private List<BattleUnit> _playerUnits = new List<BattleUnit>();
        private List<BattleUnit> _enemyUnits = new List<BattleUnit>();

        // --- Tên pool ---
        private static readonly string[] PlayerNames = { "Knight", "Paladin", "Warrior", "Berserker", "Guardian" };
        private static readonly string[] EnemyNames  = { "Goblin", "Orc", "Skeleton", "Zombie", "Demon" };

        private void Awake()
        {
            if (_battleController == null)
                _battleController = GetComponent<AutoBattleController>();

            if (_uiView == null)
                _uiView = FindFirstObjectByType<BattleUIView>();

            if (_visualManager == null)
                _visualManager = FindFirstObjectByType<BattleVisualManager>();
        }

        private void Start()
        {
            SpawnTeams();
            RegisterEvents();
            _battleController.SetBattleSpeed(_battleSpeed);

            // Spawn visual prefabs
            _visualManager?.SpawnVisuals(_playerUnits, _enemyUnits);
            _visualManager?.RegisterEvents();
            _visualManager?.SetSpeed(_battleSpeed);

            BindUnitEvents(_playerUnits);
            BindUnitEvents(_enemyUnits);

            _uiView?.InitUI(_playerUnits, _enemyUnits);
            _uiView?.Log("<color=#FFD700>⚔️  Battle scene ready! Nhấn Start Battle.</color>");
        }

        // ───────────────────────────────── Spawn ──────────────────────────────────

        private void SpawnTeams()
        {
            DisposeTeams();

            // Player team
            for (int i = 0; i < _playerTeamSize; i++)
            {
                string name = PlayerNames[i % PlayerNames.Length];
                bool isMelee = i % 2 == 0;
                GameSystems.AutoBattle.AttackRange range = isMelee ? GameSystems.AutoBattle.AttackRange.Melee : GameSystems.AutoBattle.AttackRange.Ranged;
                int hp  = Random.Range(2000, 3500);
                int atk = isMelee ? Random.Range(120, 200) : Random.Range(140, 240);
                int def = isMelee ? Random.Range(40, 80)   : Random.Range(15, 40);
                int spd = isMelee ? Random.Range(60, 90)   : Random.Range(80, 120);

                var unit = new BattleUnit($"player_{i}", name, UnitType.Player, range,
                    hp, atk, def, spd, "Power Strike", 2, 3);
                unit.EquipSkill(CreateDemoSkill(i, false));

                _playerUnits.Add(unit);
            }

            // Enemy team
            for (int i = 0; i < _enemyTeamSize; i++)
            {
                string name = EnemyNames[i % EnemyNames.Length];
                bool isMelee = i % 2 == 0;
                GameSystems.AutoBattle.AttackRange range = isMelee ? GameSystems.AutoBattle.AttackRange.Melee : GameSystems.AutoBattle.AttackRange.Ranged;
                int hp  = Random.Range(1500, 3000);
                int atk = isMelee ? Random.Range(100, 180) : Random.Range(130, 220);
                int def = isMelee ? Random.Range(30, 60)   : Random.Range(10, 35);
                int spd = isMelee ? Random.Range(50, 80)   : Random.Range(70, 110);

                var unit = new BattleUnit($"enemy_{i}", name, UnitType.Enemy, range,
                    hp, atk, def, spd, "Dark Strike", 2, 3);
                unit.EquipSkill(CreateDemoSkill(i, true));

                _enemyUnits.Add(unit);
            }

            _battleController.SetupPveBattle(_playerUnits, _enemyUnits);
            Debug.Log($"[BattleSceneSetup] Spawned {_playerUnits.Count} players vs {_enemyUnits.Count} enemies");
        }

        private SkillData CreateDemoSkill(int index, bool isEnemy)
        {
            int variant = Mathf.Abs(index) % 3;
            SkillData skill = variant switch
            {
                0 => CreateBasicStrikeSkill(isEnemy),
                1 => CreateDashThroughSkill(isEnemy),
                _ => CreateJumpBehindSkill(isEnemy),
            };

            return skill;
        }

        private SkillData CreateBasicStrikeSkill(bool isEnemy)
        {
            string prefix = isEnemy ? "enemy" : "player";
            var skill = new SkillData($"{prefix}_power_strike", "Power Strike", "A powerful melee slash",
                SkillCategory.Active, SkillElement.Physical, 20, 3f, 120f);
            skill.SetViewSequence(SkillViewSequence.CreateBasicStrike($"{prefix}_power_strike_view", "skill"));
            return skill;
        }

        private SkillData CreateDashThroughSkill(bool isEnemy)
        {
            string prefix = isEnemy ? "enemy" : "player";
            var skill = new SkillData($"{prefix}_dash_strike", "Dash Strike", "A swift dash-through slash",
                SkillCategory.Active, SkillElement.Dark, 18, 3f, 110f);
            skill.SetViewSequence(SkillViewSequence.CreateDashThroughStrike($"{prefix}_dash_strike_view", "skill"));
            return skill;
        }

        private SkillData CreateJumpBehindSkill(bool isEnemy)
        {
            string prefix = isEnemy ? "enemy" : "player";
            var skill = new SkillData($"{prefix}_backstab", "Backstab", "Jump behind and strike",
                SkillCategory.Active, SkillElement.Physical, 22, 3f, 130f);
            skill.SetViewSequence(SkillViewSequence.CreateJumpBehindStrike($"{prefix}_backstab_view", "skill"));
            return skill;
        }

        // ───────────────────────────────── Events ─────────────────────────────────

        private void RegisterEvents()
        {
            _battleController.OnTurnStarted   += OnTurnStarted;
            _battleController.OnActionExecuted += OnActionExecuted;
            _battleController.OnTurnEnded      += OnTurnEnded;
            _battleController.OnBattleEnded    += OnBattleEnded;
        }

        private void UnregisterEvents()
        {
            if (_battleController == null) return;
            _battleController.OnTurnStarted   -= OnTurnStarted;
            _battleController.OnActionExecuted -= OnActionExecuted;
            _battleController.OnTurnEnded      -= OnTurnEnded;
            _battleController.OnBattleEnded    -= OnBattleEnded;
        }

        private void OnDestroy()
        {
            UnregisterEvents();
            UnbindUnitEvents(_playerUnits);
            UnbindUnitEvents(_enemyUnits);
            _visualManager?.UnregisterEvents();
            DisposeTeams();
        }

        // ───────────────────────────── Event Handlers ─────────────────────────────

        private void OnTurnStarted(BattleTurn turn)
        {
            string teamColor = turn.activeUnit.Type == UnitType.Player ? "#4FC3F7" : "#EF9A9A";
            _uiView?.Log($"<color={teamColor}>── Turn {turn.turnNumber}: <b>{turn.activeUnit.UnitName}</b> ──</color>");
        }

        private void OnActionExecuted(BattleAction action)
        {
            // Cập nhật toàn bộ HP bars
            _uiView?.RefreshHPBars(_playerUnits, _enemyUnits);

            // Log action
            string actorColor  = action.actor.Type == UnitType.Player ? "#81D4FA" : "#FFAB91";
            string targetColor = action.target.Type == UnitType.Player ? "#81D4FA" : "#FFAB91";
            string actionIcon  = action.type == GameSystems.AutoBattle.ActionType.Skill ? "💥" : "⚔️";
            string critText    = action.isCritical ? " <color=#FFD700>[CRIT!]</color>" : "";
            string actionTypeStr = action.type switch
            {
                GameSystems.AutoBattle.ActionType.Skill  => "used skill on",
                GameSystems.AutoBattle.ActionType.Heal   => "healed",
                GameSystems.AutoBattle.ActionType.Defend => "defended",
                _                 => "attacked"
            };

            _uiView?.Log(
                $"  {actionIcon} <color={actorColor}>{action.actor.UnitName}</color> " +
                $"{actionTypeStr} <color={targetColor}>{action.target.UnitName}</color> " +
                $"→ <b>{action.value}</b> dmg{critText}"
            );

            // HP còn lại của target
            if (!action.target.IsAlive)
            {
                _uiView?.Log($"    <color=#FF5252>💀 {action.target.UnitName} bị đánh bại!</color>");
            }
        }

        private void OnTurnEnded(BattleTurn turn)
        {
            // Có thể thêm log cooldown v.v. — để trống tránh spam
        }

        private void OnBattleEnded(BattleResult result)
        {
            _uiView?.RefreshHPBars(_playerUnits, _enemyUnits);

            string outcomeMsg = result.outcome switch
            {
                BattleOutcome.Victory => "<color=#69F0AE>🏆 VICTORY!</color>",
                BattleOutcome.Defeat  => "<color=#FF5252>💀 DEFEAT...</color>",
                _                     => "<color=#FFD740>🤝 DRAW</color>"
            };

            _uiView?.Log($"\n<b>═══════════════</b>");
            _uiView?.Log($"<b>{outcomeMsg}</b>  (Turns: {result.totalTurns})");
            _uiView?.Log($"<b>═══════════════</b>");
            _uiView?.ShowResult(result.outcome);
        }

        private void HandleUnitStatChanged(BattleUnit unit, Stat stat)
        {
            if (unit == null || stat == null)
            {
                return;
            }

            if (stat.StatType is StatType.Health or StatType.Mana or StatType.Shield)
            {
                _uiView?.RefreshHPBars(_playerUnits, _enemyUnits);
            }
        }

        private void HandleUnitStatusApplied(BattleUnit unit, StatusEffect status)
        {
            if (unit == null || status == null)
            {
                return;
            }

            _uiView?.Log($"<color=#FFD54F>{unit.UnitName} gains <b>{status.Type}</b> ({status.RemainingTurns} turns)</color>");
        }

        private void HandleUnitStatusRemoved(BattleUnit unit, StatusEffect status)
        {
            if (unit == null || status == null)
            {
                return;
            }

            _uiView?.Log($"<color=#90CAF9>{unit.UnitName} loses <b>{status.Type}</b></color>");
            _uiView?.RefreshHPBars(_playerUnits, _enemyUnits);
        }

        private void HandleUnitDefeated(BattleUnit unit)
        {
            if (unit == null)
            {
                return;
            }

            _uiView?.RefreshHPBars(_playerUnits, _enemyUnits);
        }

        // ───────────────────────────── Public API (Buttons) ───────────────────────

        /// <summary>Gọi từ UI Button "Start Battle"</summary>
        public void StartBattle()
        {
            _battleController.StartBattle();
        }

        /// <summary>Gọi từ UI Button "Reset"</summary>
        public void ResetBattle()
        {
            _battleController.StopBattle();
            UnregisterEvents();
            _visualManager?.UnregisterEvents();

            SpawnTeams();
            RegisterEvents();

            // Re-spawn visuals
            _visualManager?.SpawnVisuals(_playerUnits, _enemyUnits);
            _visualManager?.RegisterEvents();
            _visualManager?.SetSpeed(_battleSpeed);

            _uiView?.InitUI(_playerUnits, _enemyUnits);
            _uiView?.Log("<color=#FFD700>🔄 Reset xong! Nhấn Start Battle để chơi lại.</color>");
        }

        /// <summary>Gọi từ UI Button "2x Speed"</summary>
        public void ToggleSpeed()
        {
            _battleSpeed = _battleSpeed < 2f ? 2f : 1f;
            _battleController.SetBattleSpeed(_battleSpeed);
            _visualManager?.SetSpeed(_battleSpeed);
            _uiView?.Log($"<color=#B2EBF2>⏩ Tốc độ: {_battleSpeed}x</color>");
        }

        private void DisposeTeams()
        {
            UnbindUnitEvents(_playerUnits);
            UnbindUnitEvents(_enemyUnits);
            DisposeTeam(_playerUnits);
            DisposeTeam(_enemyUnits);
        }

        private void DisposeTeam(List<BattleUnit> units)
        {
            if (units == null) return;

            foreach (var unit in units)
            {
                unit?.Dispose();
            }

            units.Clear();
        }

        private void BindUnitEvents(IEnumerable<BattleUnit> units)
        {
            if (units == null)
            {
                return;
            }

            foreach (var unit in units)
            {
                if (unit == null)
                {
                    continue;
                }

                unit.OnStatChanged += HandleUnitStatChanged;
                unit.OnStatusApplied += HandleUnitStatusApplied;
                unit.OnStatusRemoved += HandleUnitStatusRemoved;
                unit.OnDefeated += HandleUnitDefeated;
            }
        }

        private void UnbindUnitEvents(IEnumerable<BattleUnit> units)
        {
            if (units == null)
            {
                return;
            }

            foreach (var unit in units)
            {
                if (unit == null)
                {
                    continue;
                }

                unit.OnStatChanged -= HandleUnitStatChanged;
                unit.OnStatusApplied -= HandleUnitStatusApplied;
                unit.OnStatusRemoved -= HandleUnitStatusRemoved;
                unit.OnDefeated -= HandleUnitDefeated;
            }
        }
    }
}
