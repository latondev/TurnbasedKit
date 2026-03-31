using System.Collections.Generic;
using UnityEngine;
using GameSystems.AutoBattle;
using GameSystems.Battle;

namespace GameSystems.Battle.Demo
{
    /// <summary>
    /// Cầu nối giữa AutoBattleController (logic) và visual (Spine prefabs).
    /// Spawn prefabs, dàn trận, xử lý animation flow khi events fire.
    /// </summary>
    public class BattleVisualManager : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private BattlePrefabConfig _config;

        [Header("References")]
        [SerializeField] private AutoBattleController _battleController;
        [SerializeField] private Transform _battleField;

        [Header("Debug")]
        [SerializeField] private bool _debugLog = true;
        [SerializeField] private float _defaultPlayerBaseX = -3.5f;
        [SerializeField] private float _defaultEnemyBaseX = 3.5f;
        [SerializeField] private float _defaultYSpacing = 1.5f;
        [SerializeField] private float _defaultXStagger = 0.5f;

        // ─── Runtime ───
        private Dictionary<string, UnitView> _unitViews = new Dictionary<string, UnitView>();
        private List<UnitView> _playerViews = new List<UnitView>();
        private List<UnitView> _enemyViews = new List<UnitView>();
        private bool _warnedAboutMissingConfig;

        // ─── Properties ───
        public Dictionary<string, UnitView> unitViews => _unitViews;

        private void Awake()
        {
            if (_battleController == null)
                _battleController = FindFirstObjectByType<AutoBattleController>();

            if (_battleField == null)
            {
                var bf = new GameObject("BattleField");
                _battleField = bf.transform;
            }
        }

        /// <summary>
        /// Spawn visual prefabs cho tất cả units và dàn trận
        /// </summary>
        public void SpawnVisuals(List<BattleUnit> playerUnits, List<BattleUnit> enemyUnits)
        {
            playerUnits ??= new List<BattleUnit>();
            enemyUnits ??= new List<BattleUnit>();

            // Xóa visuals cũ
            ClearVisuals();

            if (_config == null && !_warnedAboutMissingConfig)
            {
                Log("No BattlePrefabConfig assigned. Falling back to placeholder formation.");
                _warnedAboutMissingConfig = true;
            }

            // Spawn player team
            for (int i = 0; i < playerUnits.Count; i++)
            {
                var unit = playerUnits[i];
                var prefab = ResolvePlayerPrefab(i);
                Vector2 pos = ResolveFormationPosition(i, playerUnits.Count, true);

                var view = SpawnUnitPrefab(unit, prefab, pos, true);
                if (view != null)
                {
                    _playerViews.Add(view);
                    _unitViews[unit.UnitId] = view;
                }
            }

            // Spawn enemy team
            for (int i = 0; i < enemyUnits.Count; i++)
            {
                var unit = enemyUnits[i];
                var prefab = ResolveEnemyPrefab(i);
                Vector2 pos = ResolveFormationPosition(i, enemyUnits.Count, false);

                var view = SpawnUnitPrefab(unit, prefab, pos, false);
                if (view != null)
                {
                    _enemyViews.Add(view);
                    _unitViews[unit.UnitId] = view;
                }
            }

            Log($"Spawned {_playerViews.Count} player visuals + {_enemyViews.Count} enemy visuals");
        }

        /// <summary>
        /// Đăng ký events từ AutoBattleController
        /// </summary>
        public void RegisterEvents()
        {
            if (_battleController == null) return;
            _battleController.OnActionExecuted += OnActionExecuted;
            _battleController.OnBattleEnded += OnBattleEnded;
        }

        /// <summary>
        /// Hủy đăng ký events
        /// </summary>
        public void UnregisterEvents()
        {
            if (_battleController == null) return;
            _battleController.OnActionExecuted -= OnActionExecuted;
            _battleController.OnBattleEnded -= OnBattleEnded;
        }

        /// <summary>
        /// Set tốc độ animation cho tất cả units
        /// </summary>
        public void SetSpeed(float speed)
        {
            foreach (var view in _unitViews.Values)
            {
                view.SetSpeed(speed);
            }
        }

        /// <summary>
        /// Lấy UnitView từ BattleUnit ID
        /// </summary>
        public UnitView GetView(string unitId)
        {
            _unitViews.TryGetValue(unitId, out var view);
            return view;
        }

        // ─────────────────────────────── Event Handlers ─────────────────────────────

        private void OnActionExecuted(BattleAction action)
        {
            if (action == null) return;

            var actorView = GetView(action.actor.UnitId);
            var targetView = GetView(action.target.UnitId);

            if (actorView == null || targetView == null)
            {
                Log($"⚠️ Missing view for actor={action.actor.UnitId} or target={action.target.UnitId}");
                return;
            }

            // Set waiting flag — AutoBattleController will wait
            _battleController.IsWaitingForVisuals = true;

            // Actor: play attack/skill
            actorView.OnActionComplete += OnVisualActionComplete;

            bool damageApplied = false;
            void ApplyVisualHit(int hitCount, bool isHitEffect)
            {
                if (damageApplied)
                {
                    return;
                }

                damageApplied = true;
                targetView.PlayBeHit(action.value, isHitEffect);

                if (!action.target.IsAlive)
                {
                    targetView.PlayDie();
                }
            }

            if (action.type == GameSystems.AutoBattle.ActionType.Skill)
            {
                var attackBehavior = actorView.GetComponentInChildren<AttackBehavior>();
                var skillBehavior = actorView.GetComponentInChildren<SkillBehavior>();
                var sequence = action.actor.EquippedSkill?.ViewSequence;
                bool hasTriggerStep = SequenceHasTriggerStep(sequence);

                if (skillBehavior != null)
                {
                    skillBehavior.OnEndStepAction = ApplyVisualHit;
                }

                if (attackBehavior != null)
                {
                    attackBehavior.OnEndStepAction = ApplyVisualHit;
                }

                if (sequence != null)
                {
                    var context = new SkillViewContext(
                        action.actor,
                        action.target,
                        actorView.transform.position,
                        targetView.transform.position,
                        new List<Vector3> { targetView.transform.position },
                        action);

                    actorView.PlaySkill(sequence, context);
                }
                else
                {
                    actorView.PlaySkill(targetView.transform.position);
                }

                if ((skillBehavior == null && attackBehavior == null) ||
                    (sequence != null && skillBehavior != null && !hasTriggerStep))
                {
                    ApplyVisualHit(1, false);

                    if (skillBehavior == null && attackBehavior == null)
                    {
                        OnVisualActionComplete();
                    }
                }
            }
            else
            {
                var attackBehavior = actorView.GetComponentInChildren<AttackBehavior>();
                if (attackBehavior != null)
                {
                    attackBehavior.OnEndStepAction = ApplyVisualHit;
                }

                actorView.PlayAttack(targetView.transform.position);
                
                if (attackBehavior == null)
                {
                    ApplyVisualHit(1, false);
                    OnVisualActionComplete();
                }
            }
        }

        private void OnVisualActionComplete()
        {
            // Unsubscribe all
            foreach (var view in _unitViews.Values)
            {
                view.OnActionComplete -= OnVisualActionComplete;
            }

            // Release waiting flag
            if (_battleController != null)
            {
                _battleController.IsWaitingForVisuals = false;
            }
        }

        private void OnBattleEnded(BattleResult result)
        {
            // Team thắng play win animation
            var winningViews = result.outcome == BattleOutcome.Victory ? _playerViews : _enemyViews;
            foreach (var view in winningViews)
            {
                if (view != null && view.isAlive)
                {
                    view.PlayWin();
                }
            }

            Log($"Battle ended: {result.outcome}");
        }

        // ─────────────────────────────── Spawning ───────────────────────────────────

        private UnitView SpawnUnitPrefab(BattleUnit unit, GameObject prefab, Vector2 pos, bool isPlayer)
        {
            if (prefab == null)
            {
                Log($"⚠️ No prefab assigned for {unit.UnitName}. Creating placeholder.");
                return CreatePlaceholder(unit, pos, isPlayer);
            }

            var go = Instantiate(prefab, _battleField);
            go.name = $"{(isPlayer ? "P" : "E")}_{unit.UnitName}";

            // Add UnitView nếu chưa có
            var view = go.GetComponent<UnitView>();
            if (view == null)
                view = go.AddComponent<UnitView>();

            view.Init(unit, pos, isPlayer);

            Log($"Spawned {go.name} at {pos}");
            return view;
        }

        /// <summary>
        /// Tạo placeholder visual khi không tìm thấy prefab
        /// </summary>
        private UnitView CreatePlaceholder(BattleUnit unit, Vector2 pos, bool isPlayer)
        {
            var go = new GameObject($"{(isPlayer ? "P" : "E")}_{unit.UnitName}_Placeholder");
            go.transform.SetParent(_battleField);

            // Simple sprite placeholder
            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = isPlayer ? new Color(0.3f, 0.6f, 1f, 0.7f) : new Color(1f, 0.3f, 0.3f, 0.7f);
            sr.sortingOrder = 2 - (int)pos.y;

            // Scale to approximate character size
            go.transform.localScale = new Vector3(0.8f, 1.2f, 1f);

            var view = go.AddComponent<UnitView>();
            view.Init(unit, pos, isPlayer);

            return view;
        }

        // ─────────────────────────────── Utilities ──────────────────────────────────


        private void ClearVisuals()
        {
            foreach (var view in _unitViews.Values)
            {
                if (view != null)
                    Destroy(view.gameObject);
            }
            _unitViews.Clear();
            _playerViews.Clear();
            _enemyViews.Clear();
        }

        private void OnDestroy()
        {
            UnregisterEvents();
            if (_battleController != null)
            {
                _battleController.IsWaitingForVisuals = false;
            }
        }

        private void Log(string msg)
        {
            if (_debugLog)
                Debug.Log($"[BattleVisualManager] {msg}");
        }

        private GameObject ResolvePlayerPrefab(int index)
        {
            return _config != null ? _config.GetPlayerPrefab(index) : null;
        }

        private GameObject ResolveEnemyPrefab(int index)
        {
            return _config != null ? _config.GetEnemyPrefab(index) : null;
        }

        private Vector2 ResolveFormationPosition(int index, int teamSize, bool isPlayer)
        {
            if (_config != null)
            {
                return _config.GetFormationPosition(index, teamSize, isPlayer);
            }

            float baseX = isPlayer ? _defaultPlayerBaseX : _defaultEnemyBaseX;
            float stagger = (index % 2 == 0) ? 0f : _defaultXStagger;
            float x = isPlayer ? baseX + stagger : baseX - stagger;
            float totalHeight = (teamSize - 1) * _defaultYSpacing;
            float y = (totalHeight / 2f) - (index * _defaultYSpacing);
            return new Vector2(x, y);
        }

        private static bool SequenceHasTriggerStep(SkillViewSequence skillSequence)
        {
            if (skillSequence == null || skillSequence.Steps == null)
            {
                return false;
            }

            for (int i = 0; i < skillSequence.Steps.Count; i++)
            {
                var step = skillSequence.Steps[i];
                if (step != null && step.StepType == SkillViewStepType.TriggerHit)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
