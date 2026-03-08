using System;
using System.Collections;
using UnityEngine;

namespace GameSystems.Battle
{
    /// <summary>
    /// Attack Behavior - handles attack animation
    /// </summary>
    public class AttackBehavior : ActionBase
    {
        [Header("Animations")]
        [SerializeField] private string attackAnimation = "attack";
        [SerializeField] private string idleAnimation = "idle";
        [SerializeField] private string eventHit = "hit";

        private void Awake()
        {
            if (animationHandle == null)
            {
                TryGetComponent(out animationHandle);
            }

            if (animationHandle != null)
            {
                animationHandle.OnEventAnimation += OnEventAnimation;
                animationHandle.OnEndAnimation += EndAnimation;
            }
        }

        protected override void Start()
        {
            base.Start();
        }

        protected void EndAnimation(string trackentry)
        {
            if (trackentry == attackAnimation)
            {
                if (animationHandle != null)
                {
                    animationHandle.ResetSortingOrder();
                    animationHandle.PlayAnimation(moveBack, 0.1f, 2, false);
                }

                MoveToRoot(() =>
                {
                    if (animationHandle != null)
                    {
                        animationHandle.SetSortingOrder(2 - (int)transform.position.y);
                        animationHandle.PlayAnimation(idleAnimation, 0.1f, 0, true);
                    }
                    OnEndAction?.Invoke();
                });
            }
        }

        private void OnEventAnimation(string nameAni, string ev)
        {
            if (nameAni == attackAnimation && ev == eventHit)
            {
                Debug.Log($"AttackBehavior Event: {nameAni} - {ev}");

                if (fxHit != null)
                {
                    var fx = Instantiate(fxHit, targetPosition, Quaternion.identity);
                    fx.Play();
                    Destroy(fx.gameObject, 5f);
                }

                OnEndStepAction?.Invoke(1, isHitEffect);
            }
        }

        public override void OnMeleeAttack()
        {
            if (animationHandle != null)
            {
                animationHandle.PlayAnimation(moveGo, 0.1f, 1, false);
            }

            int sortingOrder = 1000 + (int)transform.position.y;
            if (animationHandle != null)
            {
                animationHandle.SetSortingOrder(Mathf.Abs(sortingOrder));
            }

            Vector2 target = new Vector2(targetPosition.x + dirType, targetPosition.y);
            MoveToTarget(target, () =>
            {
                if (animationHandle != null)
                {
                    animationHandle.PlayAnimation(attackAnimation, 0.1f, 1, false);
                }
            });
        }

        public override void OnRangedAttack()
        {
            // Ranged attack logic - can implement arrow/projectile
        }

        public void Attack(Vector3 transformPosition)
        {
            SetTargetPosition(transformPosition);
            ExcuteAction();
        }
    }
}
