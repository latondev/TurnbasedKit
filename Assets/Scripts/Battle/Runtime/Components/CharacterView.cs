using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameSystems.Battle
{
    /// <summary>
    /// Character View - handles visual representation in battle
    /// </summary>
    public class CharacterView : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private AttackBehavior attackBehavior;
        [SerializeField] private BehitBehavior behitBehavior;
        [SerializeField] private SkillBehavior skillBehavior;
        [SerializeField] private StatusView statusView;
        [SerializeField] private AnimationController animationController;

        [Header("Settings")]
        [SerializeField] private ActionType actionType;
        [SerializeField] private bool isExecuteAction;

        [Header("Animations")]
        [SerializeField] private string appearAnimation = "appear";
        [SerializeField] private string winAnimation = "win";

        // Events
        public event Action OnEndAtk;
        public event Action<int, bool> OnStepAtk;

        private void Awake()
        {
            TryGetComponent(out attackBehavior);
            TryGetComponent(out behitBehavior);
            TryGetComponent(out skillBehavior);
            TryGetComponent(out statusView);
            TryGetComponent(out animationController);

            if (attackBehavior != null)
            {
                attackBehavior.OnEndStepAction += OnEndStepAtk;
                attackBehavior.OnEndAction += OnEndActionAtk;
            }

            if (skillBehavior != null)
            {
                skillBehavior.OnEndStepAction += OnEndStepAtk;
                skillBehavior.OnEndAction += OnEndActionAtk;
            }
        }

        public void Init(float maxHealth, float maxMp)
        {
            if (behitBehavior != null)
            {
                behitBehavior.Init(maxHealth, maxMp);
            }

            if (animationController != null && !string.IsNullOrEmpty(appearAnimation))
            {
                animationController.PlayAnimation(appearAnimation, 0.1f, 1, false);
                animationController.SetSortingOrder(2 - (int)transform.position.y);
            }
        }

        private void OnEndStepAtk(int count, bool isHitEffect)
        {
            Debug.Log($"OnEndStepAtk: count={count}, isHitEffect={isHitEffect}");
            OnStepAtk?.Invoke(count, isHitEffect);
        }

        private void OnEndActionAtk()
        {
            Debug.Log("OnEndActionAtk");
            if (behitBehavior != null)
            {
                behitBehavior.ShowUI();
            }
            OnEndAtk?.Invoke();
        }

        #region Public Methods

        public void Behit(int dmg, bool isHitEffect = false)
        {
            if (behitBehavior != null)
            {
                behitBehavior.Behit(dmg, isHitEffect);
            }
        }

        public void Die()
        {
            if (behitBehavior != null)
            {
                behitBehavior.Die();
            }
        }

        public void ResetView()
        {
            if (animationController != null)
            {
                animationController.ResetSortingOrder();
            }
        }

        public void ActiveFadeView()
        {
            if (animationController != null)
            {
                animationController.SetSortingOrder(20, "Fade");
            }
        }

        public void Attack(Vector3 targetPosition)
        {
            if (behitBehavior != null)
            {
                behitBehavior.HideUI();
            }

            if (attackBehavior != null)
            {
                attackBehavior.Attack(targetPosition);
            }
        }

        public void Skill(List<Vector3> targetPositions, Vector3 skillPosition)
        {
            if (behitBehavior != null)
            {
                behitBehavior.HideUI();
            }

            if (skillBehavior != null)
            {
                skillBehavior.Skill(targetPositions, skillPosition);
            }
            else if (attackBehavior != null)
            {
                attackBehavior.Attack(targetPositions.Count > 0 ? targetPositions[0] : Vector3.zero);
            }
        }

        public void AddMana(int mana)
        {
            if (behitBehavior != null)
            {
                behitBehavior.ChangeMana(mana);
            }
        }

        public void AnimateWin()
        {
            if (animationController != null && !string.IsNullOrEmpty(winAnimation))
            {
                animationController.PlayAnimation(winAnimation, 0.1f, 2, true, true);
            }
        }

        public void SetFlipY(TeamType teamType)
        {
            if (animationController != null)
            {
                animationController.SetFlipX(teamType == TeamType.DEF);
            }

            if (attackBehavior != null)
            {
                attackBehavior.dirType = teamType == TeamType.DEF ? 2.5f : 0;
            }
        }

        public void ChangeSpeed(float speed)
        {
            if (animationController != null)
            {
                animationController.SetSpeed(speed);
            }

            if (attackBehavior != null)
            {
                attackBehavior.SetSpeed(speed);
            }
        }

        #endregion
    }

    public enum ActionType
    {
        Attack,
        Skill
    }
}
