using System;
using System.Collections;
using System.Collections.Generic;
using PathologicalGames;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace MythydRpg
{
    public enum AttackRange
    {
        Melee,
        Ranged
    }

    public class AttackBehavior : ActionBase
    {
        [SpineAnimation, SerializeField] string attackAnimation;
        [SpineAnimation, SerializeField] string idleAnimation;
        [SerializeField, SpineEvent] string eventHit;

        private void Awake()
        {
            animationHandle.OnEventAnimation += OnEventAnimation;
            animationHandle.OnEndAnimation += EndAnimation;
        }


        protected override void Start()
        {
        }

        protected void EndAnimation(string trackentry)
        {
            if (trackentry == attackAnimation)
            {
                animationHandle.ResetSortingOrder();
                animationHandle.PlayAnimation(moveBack, 0.1f, 2, false);

                MoveToRoot(() =>
                {
                    animationHandle.SetSortingOrder(2- (int)transform.position.y);

                    animationHandle.PlayAnimation(idleAnimation, 0.1f, 0, true);
                    OnEndAction?.Invoke();
                });
            }
        }

        private void OnEventAnimation(string nameAni, string ev)
        {
            if (nameAni == attackAnimation && ev == eventHit)
            {
                Debug.Log("Event Animation " + nameAni + " " + ev);
                if (fxHit != null)
                {
                    ParticleSystem fxHitGame = PoolManager.Pools[PoolName.poolFx].Spawn(base.fxHit.gameObject, targetPosition, Quaternion.identity).GetComponent<ParticleSystem>();
                    fxHitGame.SpeedUpFx(speed);
                    fxHitGame.Play();
                    this.Wait(5f, () =>
                    {
                        PoolManager.Pools[PoolName.poolFx].Despawn(fxHitGame.transform);

                    });
                }
               
                OnEndStepAction.Invoke(1,isHitEffect);
            }
        }


        public override void OnMeleeAttack()
        {
            animationHandle.PlayAnimation(moveGo, 0.1f, 1, false);
            int sortingOrder = 1000 + (int)transform.position.y;
            animationHandle.SetSortingOrder(Mathf.Abs(sortingOrder));
            MoveToTarget(new Vector2(targetPosition.x + dirType, targetPosition.y), () =>
            {
                animationHandle.PlayAnimation(attackAnimation, 0.1f, 1, false);

                
            });
        }


        public override void OnRangedAttack()
        {
        }

        public void Attack(Vector3 transformPosition)
        {
            SetTargetPosition(transformPosition);
            ExcuteAction();
        }
    }
}