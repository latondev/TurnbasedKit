using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Spine.Unity;
using Unity.VisualScripting;
using UnityEngine;

namespace MythydRpg
{
    public abstract class ActionBase : MonoBehaviour
    {
        public float dirType = 0; // 1 atk -1 def

        public Action<int, bool> OnEndStepAction;
        public Action OnEndAction;
        [SerializeField] protected AnimationController animationHandle;
        [SerializeField, SpineAnimation] protected string moveGo;
        [SerializeField, SpineAnimation] protected string moveBack;

        [SerializeField] protected AttackRange attackRange;

        [SerializeField] protected Vector2 offsetExcute;
        [SerializeField] protected Vector2 targetPosition;
        [SerializeField] protected float speed = 1;
        [SerializeField] protected Vector2 posOrigin;
        [SerializeField] protected ParticleSystem fxHit;
        [SerializeField] protected bool isHitEffect = false;

        const float duration = 0.4f;

        protected virtual void OnValidate()
        {
            animationHandle = GetComponent<AnimationController>();
            //moveGo
        }

        public virtual void SetSpeed(float speed)
        {
            this.speed = speed;
            //var main = fxHit.main;
          //  main.startSpeedMultiplier = speed;
        }

        protected virtual void Start()
        {
            posOrigin = transform.position;
        }

        protected void MoveToRoot(Action @callback = null)
        {
            transform.DOLocalMove(Vector2.zero, duration / speed).SetEase(Ease.Linear).OnComplete(() => { @callback?.Invoke(); });
        }

        public void MoveToTarget(Vector2 targetPosition, Action @callback = null)
        {
            Vector2 target = targetPosition + offsetExcute;
            transform.DOMove(target, duration / speed).SetEase(Ease.Linear).OnComplete(() => { @callback?.Invoke(); });
        }

        public void SetTargetPosition(Vector2 targetPosition)
        {
            this.targetPosition = targetPosition;
        }

        public void ChangeSpeed(float speed)
        {
            this.speed = speed;
        }

        protected Vector2 GetTargetPosFinal()
        {
            Vector2 finalPosition = transform.position.x > targetPosition.x ? offsetExcute : -offsetExcute;
            targetPosition += finalPosition;
            return targetPosition;
        }


        public void ExcuteAction()
        {
            switch (attackRange)
            {
                case AttackRange.Melee:
                    OnMeleeAttack();
                    break;
                case AttackRange.Ranged:
                    OnRangedAttack();
                    break;
            }
        }

        public abstract void OnMeleeAttack();
        public abstract void OnRangedAttack();
    }
}