using System;
using GameSystems.AutoBattle;
using GameSystems.Battle;
using UnityEngine;

namespace GameSystems.Battle.Demo
{
    /// <summary>
    /// Visual wrapper for one BattleUnit.
    /// Keeps visual concerns on the prefab side and delegates status/icon rendering to StatusView.
    /// </summary>
    public class UnitView : MonoBehaviour
    {
        [SerializeField] private int authoringUnitId;
        [SerializeField] private StatusView statusView;
        [SerializeField] private UnitSocketResolver socketResolver;

        private ActionSequenceRunner _actionRunner;
        private BehitBehavior _behitBehavior;
        private AnimationHandle _animHandle;
        private BattleUnit _unit;

        public event Action OnActionComplete;

        public BattleUnit unit => _unit;
        public string unitId => _unit?.UnitId;
        public int AuthoringUnitId => authoringUnitId;
        public bool isAlive => _unit?.IsAlive ?? false;

        private const string WAR_IDLE = "war_idle";
        private const string WAR_BE_ATTACK = "war_beAttack";
        private const string WAR_DIE = "war_die";
        private const string WAR_WIN = "war_win";
        private const string WAR_STUN = "war_stun";

        private const string IDLE = "idle";
        private const string BE_ATTACK = "beAttack";
        private const string DIE = "die";
        private const string WIN = "win";
        private const string STUN = "stun";

        public void Init(BattleUnit unit, Vector2 formationPos, bool isPlayer)
        {
            _unit = unit;
            transform.position = new Vector3(formationPos.x, formationPos.y, 0f);

            ResolveVisualComponents();

            if (_actionRunner != null)
            {
                _actionRunner.OnEndAction = () => OnActionComplete?.Invoke();
            }

            if (!isPlayer && _animHandle != null)
            {
                _animHandle.SetFlipX(true);
            }

            if (_behitBehavior != null)
            {
                _behitBehavior.Init(unit.MaxHP, unit.MaxMana);
            }

            if (socketResolver != null)
            {
                socketResolver.RefreshCache();
            }

            if (statusView != null)
            {
                statusView.Bind(unit, socketResolver);
            }

            if (_animHandle != null)
            {
                _animHandle.SetSortingOrder(2 - (int)formationPos.y);
            }

            PlayIdle();
        }

        public void PlayIdle()
        {
            PlayAnim(WAR_IDLE, IDLE, true);
        }

        public void PlayAction(CombatActionData action, SkillViewContext context)
        {
            if (_actionRunner != null && action != null)
            {
                _actionRunner.Play(action, context);
                return;
            }

            OnActionComplete?.Invoke();
        }

        public void SetActionStepCallback(Action<int, bool> callback)
        {
            if (_actionRunner != null)
            {
                _actionRunner.OnEndStepAction = callback;
            }
        }

        public void PlayBeHit(float damage, bool isHitEffect = false)
        {
            if (_behitBehavior != null)
            {
                _behitBehavior.Behit(damage, isHitEffect);
            }
            else
            {
                PlayAnim(WAR_BE_ATTACK, BE_ATTACK, false);
            }
        }

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

        public void PlayWin()
        {
            PlayAnim(WAR_WIN, WIN, false);
        }

        public void PlayStun()
        {
            PlayAnim(WAR_STUN, STUN, true);
        }

        public void RefreshHP()
        {
        }

        public void SetSpeed(float speed)
        {
            if (_actionRunner != null)
            {
                _actionRunner.SetSpeed(speed);
            }

            if (_animHandle != null)
            {
                _animHandle.SetSpeed(speed);
            }
        }

        private void ResolveVisualComponents()
        {
            socketResolver = socketResolver != null ? socketResolver : GetComponentInChildren<UnitSocketResolver>(true);
            if (socketResolver == null)
            {
                socketResolver = gameObject.AddComponent<UnitSocketResolver>();
            }

            _actionRunner = GetComponentInChildren<ActionSequenceRunner>(true);
            if (_actionRunner == null)
            {
                _actionRunner = gameObject.AddComponent<ActionSequenceRunner>();
            }

            _behitBehavior = GetComponentInChildren<BehitBehavior>(true);
            _animHandle = GetComponentInChildren<AnimationHandle>(true);

            if (_animHandle != null)
            {
                _animHandle.Initialize();
            }

            statusView = statusView != null ? statusView : GetComponentInChildren<StatusView>(true);
            if (statusView == null)
            {
                statusView = gameObject.AddComponent<StatusView>();
            }
        }

        private void PlayAnim(string warAnim, string fallback, bool loop)
        {
            if (_animHandle != null)
            {
                _animHandle.TryPlayAnimation(warAnim, fallback, 0.1f, 0, loop);
            }
        }

        private void OnDestroy()
        {
            if (statusView != null)
            {
                statusView.Unbind();
            }
        }
    }
}
