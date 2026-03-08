using System;
using System.Collections;
using System.Collections.Generic;
using Spine;
using Spine.Unity;
using UnityEngine;
using Event = Spine.Event;

namespace MythydRpg
{
    public enum ActionType
    {
        Attack,
        Skill
    }

    [RequireComponent(typeof(AnimationController))]
    [RequireComponent(typeof(AttackBehavior))]
    [RequireComponent(typeof(BehitBehavior))]
    [RequireComponent(typeof(StatusView))]
    public class CharacterView : MonoBehaviour
    {
        public Action OnEndAtk;
        public Action<int,bool> OnStepAtk;

        [SerializeField] AttackBehavior attackBehavior;
        [SerializeField] BehitBehavior behitBehavior;
        [SerializeField] SkillBehavior skillBehavior;
        [SerializeField] StatusView statusView;

        [SerializeField] ActionType actionType;
        [SerializeField] bool isExecuteAction;
        [SerializeField] AnimationController animationController;
        [SerializeField, SpineAnimation] string appearAnimation;

        [SerializeField, SpineAnimation] string winAnimation;
        
        

        private void Awake()
        {
            
            attackBehavior.OnEndStepAction += OnEndStepAtk;
            attackBehavior.OnEndAction += OnEndActionAtk;
            if (skillBehavior != null)
            {
                skillBehavior.OnEndStepAction += OnEndStepAtk;
                skillBehavior.OnEndAction += OnEndActionAtk;
            }

        }

        public void Init(float maxHealth, float maxMp)
        {
            behitBehavior.Init(maxHealth, maxMp);
            animationController.PlayAnimation(appearAnimation, 0.1f, 1, false);
            animationController.SetSortingOrder(2 - (int)transform.position.y);

        }


        void Start()
        {
        }

        private void OnEndStepAtk(int count,bool isHitEffect)
        {
            Debug.Log("OnEndStepAtk");
            OnStepAtk?.Invoke(count,isHitEffect);
        }

        private void OnEndActionAtk()
        {
            Debug.Log("OnEndActionAtk");
            behitBehavior.ShowUI();
            OnEndAtk?.Invoke();
        }

        private void OnValidate()
        {
            appearAnimation = "appear";
            TryGetComponent(out attackBehavior);
            TryGetComponent(out behitBehavior);
            TryGetComponent(out skillBehavior);
            TryGetComponent(out statusView);
            animationController = GetComponent<AnimationController>();
        }

        // Update is called once per frame
        void Update()
        {
        }


        public void Behit(int dmg,bool isHitEffect = false)
        {
            behitBehavior.Behit(dmg,isHitEffect);
        }

        public void Die()
        {
            behitBehavior.Die();
        }

        public void ResetView()
        {
            animationController.ResetSortingOrder();
        }

        public void ActiveFadeView()
        {
            animationController.SetSortingOrder(20, "Fade");
        }

        public void Attack(Vector3 transformPosition)
        {
            behitBehavior.HideUI();
            attackBehavior.Attack(transformPosition);
        }

        public void Skill(List<Vector3> targetPostions,Vector3 transformPosition)
        {
            behitBehavior.HideUI();
            if (skillBehavior != null)
            {
                skillBehavior.Skill(targetPostions,transformPosition);
            }
            else
            {
                attackBehavior.Attack(transformPosition);
            }
        }

        public void AddMana(int mana)
        {
            behitBehavior.ChangeMana(mana);
        }

        public void AnimateWin()
        {
            animationController?.PlayAnimation(winAnimation, 0.1f, 2, true,true);
        }

        public void SetFlipY(TeamType teamType)
        {
            animationController.SetFlipX(teamType == TeamType.DEF);
            attackBehavior.dirType = teamType == TeamType.DEF ? 2.5f : 0;
        }

        public void ChangeSpeed(float speed)
        {
            animationController.SetSpeed(speed);
            attackBehavior.SetSpeed(speed);
            
        }
    }
}