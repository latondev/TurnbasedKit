using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using GameSystems.AutoBattle;
using GameSystems.Battle;

namespace GameSystems.Battle.Demo
{
    /// <summary>
    /// Visual wrapper cho 1 BattleUnit.
    /// Gắn trên root của instantiated Spine prefab.
    /// Kết nối AttackBehavior/SkillBehavior/BehitBehavior/AnimationHandle đã có sẵn trong prefab.
    /// </summary>
    public class UnitView : MonoBehaviour
    {
        // ─── Refs (auto-find từ prefab children) ───
        private AttackBehavior _attackBehavior;
        private SkillBehavior _skillBehavior;
        private ActionSequenceRunner _actionRunner;
        private BehitBehavior _behitBehavior;
        private AnimationHandle _animHandle;
        private TMP_Text _statusText;

        // ─── Data ───
        private BattleUnit _unit;
        private Vector2 _formationPos;
        private bool _isPlayer;
        private bool _eventsBound;

        // ─── Events ───
        public event Action OnActionComplete;

        // ─── Properties ───
        public BattleUnit unit => _unit;
        public string unitId => _unit?.UnitId;
        public bool isAlive => _unit?.IsAlive ?? false;

        // ─── Animation names (war_ prefix cho battle) ───
        private const string WAR_IDLE = "war_idle";
        private const string WAR_ATTACK = "war_attack";
        private const string WAR_MOVE = "war_move";
        private const string WAR_MOVE_BACK = "war_moveBack";
        private const string WAR_BE_ATTACK = "war_beAttack";
        private const string WAR_DIE = "war_die";
        private const string WAR_WIN = "war_win";
        private const string WAR_STUNT = "war_stunt";
        private const string WAR_STUN = "war_stun";

        // Fallback (không có war_ prefix)
        private const string IDLE = "idle";
        private const string ATTACK = "attack";
        private const string MOVE = "move";
        private const string MOVE_BACK = "moveBack";
        private const string BE_ATTACK = "beAttack";
        private const string DIE = "die";
        private const string WIN = "win";
        private const string STUN = "stun";
        private const int MAX_STATUS_VISIBLE = 4;

        /// <summary>
        /// Khởi tạo UnitView với BattleUnit data
        /// </summary>
        public void Init(BattleUnit unit, Vector2 formationPos, bool isPlayer)
        {
            UnbindUnitEvents();
            _unit = unit;
            _formationPos = formationPos;
            _isPlayer = isPlayer;

            // Set position
            transform.position = new Vector3(formationPos.x, formationPos.y, 0);

            // Auto-find components trong prefab children
            _attackBehavior = GetComponentInChildren<AttackBehavior>();
            _skillBehavior = GetComponentInChildren<SkillBehavior>();
            _actionRunner = GetComponentInChildren<ActionSequenceRunner>();
            _behitBehavior = GetComponentInChildren<BehitBehavior>();
            _animHandle = GetComponentInChildren<AnimationHandle>();
            if (_animHandle != null)
            {
                _animHandle.Initialize();
            }

            if (_actionRunner == null)
            {
                _actionRunner = gameObject.AddComponent<ActionSequenceRunner>();
            }

            // Setup attack behavior
            if (_attackBehavior != null)
            {
                _attackBehavior.dirType = isPlayer ? -1f : 1f;
                _attackBehavior.OnEndAction = () => OnActionComplete?.Invoke();
            }

            if (_skillBehavior != null)
            {
                _skillBehavior.dirType = isPlayer ? -1f : 1f;
                _skillBehavior.OnEndAction = () => OnActionComplete?.Invoke();
            }

            if (_actionRunner != null)
            {
                _actionRunner.dirType = isPlayer ? -1f : 1f;
                _actionRunner.OnEndAction = () => OnActionComplete?.Invoke();
            }

            // Flip enemy để quay mặt về phía player
            if (!isPlayer && _animHandle != null)
            {
                _animHandle.SetFlipX(true);
            }

            // Init HP bar
            if (_behitBehavior != null)
            {
                _behitBehavior.Init(unit.MaxHP, unit.MaxMana);
            }

            BuildStatusOverlay();
            BindUnitEvents();
            RefreshStatusOverlay();

            // Set sorting order dựa trên Y
            if (_animHandle != null)
            {
                _animHandle.SetSortingOrder(2 - (int)formationPos.y);
            }

            // Play idle animation
            PlayIdle();
        }

        /// <summary>
        /// Play idle animation (war_idle ưu tiên, fallback idle)
        /// </summary>
        public void PlayIdle()
        {
            PlayAnim(WAR_IDLE, IDLE, true);
        }

        /// <summary>
        /// Thực hiện attack animation flow: move → attack → hit event → moveBack → idle
        /// </summary>
        public void PlayAttack(Vector3 targetPos)
        {
            if (_attackBehavior != null)
            {
                _attackBehavior.Attack(targetPos);
            }
            else
            {
                // Fallback: không có AttackBehavior → chỉ play anim rồi complete
                PlayAnim(WAR_ATTACK, ATTACK, false);
                OnActionComplete?.Invoke();
            }
        }

        /// <summary>
        /// Thực hiện skill animation flow
        /// </summary>
        public void PlaySkill(Vector3 targetPos, System.Collections.Generic.List<Vector3> allTargetPositions = null)
        {
            if (_skillBehavior != null)
            {
                _skillBehavior.Skill(
                    allTargetPositions ?? new System.Collections.Generic.List<Vector3> { targetPos },
                    targetPos
                );
            }
            else
            {
                PlayAttack(targetPos);
            }
        }

        /// <summary>
        /// Play a data-driven skill view sequence
        /// </summary>
        public void PlaySkill(SkillViewSequence sequence, SkillViewContext context)
        {
            if (_skillBehavior != null)
            {
                _skillBehavior.Play(sequence, context);
                return;
            }

            if (context != null)
            {
                PlaySkill(context.PrimaryTargetPosition, context.TargetPositions);
            }
        }

        public void PlayAction(CombatActionData action, SkillViewContext context)
        {
            if (_actionRunner != null)
            {
                _actionRunner.Play(action, context);
                return;
            }

            if (action != null && action.ActionKind == CombatActionKind.BasicAttack)
            {
                PlayAttack(context != null ? context.PrimaryTargetPosition : transform.position);
                return;
            }

            PlaySkill(action != null ? action.ViewSequence : null, context);
        }

        public void SetActionStepCallback(Action<int, bool> callback)
        {
            if (_actionRunner != null)
            {
                _actionRunner.OnEndStepAction = callback;
            }

            if (_skillBehavior != null)
            {
                _skillBehavior.OnEndStepAction = callback;
            }

            if (_attackBehavior != null)
            {
                _attackBehavior.OnEndStepAction = callback;
            }
        }

        /// <summary>
        /// Nhận damage — play beAttack anim + floating text + HP bar
        /// </summary>
        public void PlayBeHit(float damage, bool isHitEffect = false)
        {
            if (_behitBehavior != null)
            {
                _behitBehavior.Behit(damage, isHitEffect);
            }
            else
            {
                // Fallback animation
                PlayAnim(WAR_BE_ATTACK, BE_ATTACK, false);
            }
        }

        /// <summary>
        /// Play die animation
        /// </summary>
        public void PlayDie()
        {
            if (_behitBehavior != null)
            {
                _behitBehavior.Die();
            }
            else
            {
                PlayAnim(WAR_DIE, DIE, false);
            }
        }

        /// <summary>
        /// Play win animation
        /// </summary>
        public void PlayWin()
        {
            PlayAnim(WAR_WIN, WIN, false);
        }

        /// <summary>
        /// Play stun animation
        /// </summary>
        public void PlayStun()
        {
            PlayAnim(WAR_STUN, STUN, true);
        }

        /// <summary>
        /// Cập nhật HP bar (gọi khi HP thay đổi từ bên ngoài)
        /// </summary>
        public void RefreshHP()
        {
            // BehitBehavior tự quản lý HP bar nội bộ
            // Method này dùng khi cần force refresh
        }

        /// <summary>
        /// Set tốc độ animation (cho battle speed control)
        /// </summary>
        public void SetSpeed(float speed)
        {
            if (_attackBehavior != null) _attackBehavior.SetSpeed(speed);
            if (_skillBehavior != null) _skillBehavior.SetSpeed(speed);
            if (_actionRunner != null) _actionRunner.SetSpeed(speed);
            if (_animHandle != null) _animHandle.SetSpeed(speed);
        }

        // ─── Internal ───

        private void PlayAnim(string warAnim, string fallback, bool loop)
        {
            if (_animHandle != null)
            {
                _animHandle.TryPlayAnimation(warAnim, fallback, 0.1f, 0, loop);
            }
        }

        private void BuildStatusOverlay()
        {
            if (_statusText != null)
            {
                return;
            }

            var canvas = GetComponentInChildren<Canvas>(true);
            if (canvas == null)
            {
                return;
            }

            var statusGo = new GameObject("StatusOverlay", typeof(RectTransform));
            statusGo.transform.SetParent(canvas.transform, false);

            var rt = statusGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(220f, 28f);
            rt.anchoredPosition = new Vector2(0f, -6f);

            _statusText = statusGo.AddComponent<TextMeshProUGUI>();
            _statusText.alignment = TextAlignmentOptions.Center;
            _statusText.fontSize = 12f;
            _statusText.color = Color.white;
            _statusText.enableWordWrapping = false;
            _statusText.raycastTarget = false;
            _statusText.richText = true;
            _statusText.text = string.Empty;
        }

        private void BindUnitEvents()
        {
            if (_unit == null || _eventsBound)
            {
                return;
            }

            _unit.OnStatusApplied += HandleStatusApplied;
            _unit.OnStatusRemoved += HandleStatusRemoved;
            _unit.OnTurnStarted += HandleTurnChanged;
            _unit.OnTurnEnded += HandleTurnChanged;
            _unit.OnReset += HandleUnitReset;
            _unit.OnDefeated += HandleUnitDefeated;
            _eventsBound = true;
        }

        private void UnbindUnitEvents()
        {
            if (_unit != null && _eventsBound)
            {
                _unit.OnStatusApplied -= HandleStatusApplied;
                _unit.OnStatusRemoved -= HandleStatusRemoved;
                _unit.OnTurnStarted -= HandleTurnChanged;
                _unit.OnTurnEnded -= HandleTurnChanged;
                _unit.OnReset -= HandleUnitReset;
                _unit.OnDefeated -= HandleUnitDefeated;
            }

            _eventsBound = false;
        }

        private void HandleStatusApplied(BattleUnit unit, StatusEffect status)
        {
            if (unit != null && unit == _unit)
            {
                RefreshStatusOverlay();
            }
        }

        private void HandleStatusRemoved(BattleUnit unit, StatusEffect status)
        {
            if (unit != null && unit == _unit)
            {
                RefreshStatusOverlay();
            }
        }

        private void HandleTurnChanged(BattleUnit unit)
        {
            if (unit != null && unit == _unit)
            {
                RefreshStatusOverlay();
            }
        }

        private void HandleUnitReset(BattleUnit unit)
        {
            if (unit != null && unit == _unit)
            {
                RefreshStatusOverlay();
            }
        }

        private void HandleUnitDefeated(BattleUnit unit)
        {
            if (unit != null && unit == _unit)
            {
                RefreshStatusOverlay();
            }
        }

        private void RefreshStatusOverlay()
        {
            if (_statusText == null)
            {
                return;
            }

            var statuses = _unit?.RuntimeService?.StatusController?.GetActiveStatuses();
            if (statuses == null || statuses.Count == 0)
            {
                _statusText.text = string.Empty;
                _statusText.gameObject.SetActive(false);
                return;
            }

            _statusText.gameObject.SetActive(true);

            var visibleCount = Mathf.Min(MAX_STATUS_VISIBLE, statuses.Count);
            var parts = new List<string>(visibleCount + 1);

            for (int i = 0; i < visibleCount; i++)
            {
                parts.Add(FormatStatusChip(statuses[i]));
            }

            if (statuses.Count > visibleCount)
            {
                parts.Add($"+{statuses.Count - visibleCount}");
            }

            _statusText.text = string.Join("  ", parts);
        }

        private string FormatStatusChip(StatusEffect status)
        {
            if (status == null)
            {
                return string.Empty;
            }

            string label = GetStatusShortName(status.Type);

            if (status.Value > 0f)
            {
                if (status.IsDamageOverTime || status.IsHealOverTime)
                {
                    label += $"{status.Value:F0}";
                }
                else if (status.Type == StatusEffectType.AttackBuff ||
                         status.Type == StatusEffectType.DefenseBuff ||
                         status.Type == StatusEffectType.SpeedBuff ||
                         status.Type == StatusEffectType.Weakness ||
                         status.Type == StatusEffectType.Slow)
                {
                    label += $"{status.Value * 100f:F0}%";
                }
            }

            if (status.StackCount > 1)
            {
                label += $"x{status.StackCount}";
            }

            if (status.RemainingTurns > 0)
            {
                label += $"({status.RemainingTurns})";
            }

            return $"<color={GetStatusColor(status.Type)}><b>{label}</b></color>";
        }

        private string GetStatusShortName(StatusEffectType type)
        {
            return type switch
            {
                StatusEffectType.Stun => "STN",
                StatusEffectType.Freeze => "FRZ",
                StatusEffectType.Silence => "SIL",
                StatusEffectType.Poison => "PSN",
                StatusEffectType.Burn => "BRN",
                StatusEffectType.Regeneration => "REG",
                StatusEffectType.AttackBuff => "ATK+",
                StatusEffectType.DefenseBuff => "DEF+",
                StatusEffectType.SpeedBuff => "SPD+",
                StatusEffectType.Weakness => "ATK-",
                StatusEffectType.Slow => "SPD-",
                _ => type.ToString().ToUpperInvariant()
            };
        }

        private string GetStatusColor(StatusEffectType type)
        {
            return type switch
            {
                StatusEffectType.Stun => "#FF6B6B",
                StatusEffectType.Freeze => "#66D9FF",
                StatusEffectType.Silence => "#D18BFF",
                StatusEffectType.Poison => "#7CFF7A",
                StatusEffectType.Burn => "#FF9A3C",
                StatusEffectType.Regeneration => "#88FFB0",
                StatusEffectType.AttackBuff => "#7DCFFF",
                StatusEffectType.DefenseBuff => "#9AD0FF",
                StatusEffectType.SpeedBuff => "#FFD166",
                StatusEffectType.Weakness => "#FF9FB3",
                StatusEffectType.Slow => "#B9B9B9",
                _ => "#FFFFFF"
            };
        }

        private void OnDestroy()
        {
            UnbindUnitEvents();
        }
    }
}
