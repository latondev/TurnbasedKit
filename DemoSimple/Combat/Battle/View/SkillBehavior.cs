using System.Collections;
using System.Collections.Generic;
using PathologicalGames;
using Spine.Unity;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace MythydRpg
{
    public class SkillBehavior : ActionBase
    {
        [SpineAnimation, SerializeField] string skillAnimation;
        [SpineAnimation, SerializeField] string idleAnimation;

        [SerializeField, SpineEvent] string eventHit;
        [SerializeField, SpineEvent] string eventFalldown;

        [SerializeField, SpineAnimation] List<string> skillAnimations;
        [SerializeField] private int totalHit;
        private List<Vector3> _targetPostions;

        protected override void OnValidate()
        {
            // totalHit = animationHandle.GetTotalEventByAnimation(
            //     animationHandle.SkeletonAnimation,
            //     eventHit,
            //     skillAnimation
            // );
        }

        private void Awake()
        {
            animationHandle.OnEventAnimation += OnEventAnimation;
            animationHandle.OnEndAnimation += EndAnimation;
        }

        void Start()
        {
        }

        void PlaySkillAnimation()
        {
            string animationName = skillAnimations[Random.Range(0, skillAnimations.Count)];
        }

        protected void EndAnimation(string trackentry)
        {
            if (trackentry == skillAnimation)
            {
                animationHandle.PlayAnimation(moveBack, 0.1f, 2, false);


                MoveToRoot(() =>
                {
                    animationHandle.PlayAnimation(idleAnimation, 0.1f, 0, true);
                    animationHandle.ResetSortingOrder();
                    animationHandle.SetSortingOrder(2 - (int)transform.position.y);

                    OnEndAction?.Invoke();
                });
            }
        }

        private void OnEventAnimation(string nameAni, string ev)
        {
            if (nameAni == skillAnimation && ev == eventHit)
            {
                Debug.Log("Event Animation " + nameAni + " " + ev);
                foreach (var item in _targetPostions)
                {
                    ParticleSystem fxHitGame = PoolManager.Pools[PoolName.poolFx].Spawn(base.fxHit.gameObject, item, Quaternion.identity).GetComponent<ParticleSystem>();
                    fxHitGame.SpeedUpFx(speed);

                    fxHitGame.Play();
                    this.Wait(5f, () => { PoolManager.Pools[PoolName.poolFx].Despawn(fxHitGame.transform); });
                }


                OnEndStepAction?.Invoke(totalHit, false);
            }
            else if (nameAni == skillAnimation && ev == eventFalldown)
            {
                OnEndStepAction?.Invoke(totalHit, true);

            }
        }

        public override void OnMeleeAttack()
        {
            animationHandle.PlayAnimation(moveGo, 0.1f, 1, false);
            int sortingOrder = 2 + (int)transform.position.y;
            //animationHandle.SetSortingOrder(Mathf.Abs(sortingOrder));
            MoveToTarget(new Vector2(targetPosition.x + dirType, targetPosition.y), () =>
            {
                animationHandle.PlayAnimation(skillAnimation, 0.1f, 1, false);
                GetComponent<SkillHandle>().Excute(speed);
            });
        }

        public override void OnRangedAttack()
        {
            animationHandle.PlayAnimation(skillAnimation, 0.1f, 1, false);
        }

        public void Skill(List<Vector3> targetPostions, Vector3 transformPosition)
        {
            _targetPostions = targetPostions;
            SetTargetPosition(transformPosition);
            ExcuteAction();
        }
    }
}