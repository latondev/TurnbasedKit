using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameSystems.Battle
{
    /// <summary>
    /// Skill Behavior - handles skill animation
    /// </summary>
    public class SkillBehavior : ActionBase
    {
        [Header("Animations")]
        [SerializeField] private string skillAnimation = "skill";
        [SerializeField] private string idleAnimation = "idle";
        [SerializeField] private string eventHit = "hit";
        [SerializeField] private string eventFalldown = "falldown";

        [Header("Settings")]
        [SerializeField] private List<string> skillAnimations = new List<string>();
        [SerializeField] private int totalHit = 1;

        private List<Vector3> _targetPositions;

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


        void PlaySkillAnimation()
        {
            if (skillAnimations != null && skillAnimations.Count > 0)
            {
                skillAnimation = skillAnimations[UnityEngine.Random.Range(0, skillAnimations.Count)];
            }
        }

        protected void EndAnimation(string trackentry)
        {
            if (trackentry == skillAnimation)
            {
                if (animationHandle != null)
                {
                    animationHandle.PlayAnimation(moveBack, 0.1f, 2, false);
                }

                MoveToRoot(() =>
                {
                    if (animationHandle != null)
                    {
                        animationHandle.PlayAnimation(idleAnimation, 0.1f, 0, true);
                        animationHandle.ResetSortingOrder();
                        animationHandle.SetSortingOrder(2 - (int)transform.position.y);
                    }
                    OnEndAction?.Invoke();
                });
            }
        }

        private void OnEventAnimation(string nameAni, string ev)
        {
            if (nameAni == skillAnimation && ev == eventHit)
            {
                Debug.Log($"SkillBehavior Event: {nameAni} - {ev}");

                if (_targetPositions != null && fxHit != null)
                {
                    foreach (var item in _targetPositions)
                    {
                        var fx = Instantiate(fxHit, item, Quaternion.identity);
                        fx.Play();
                        Destroy(fx.gameObject, 5f);
                    }
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
            if (animationHandle != null)
            {
                animationHandle.PlayAnimation(moveGo, 0.1f, 1, false);
            }

            int sortingOrder = 2 + (int)transform.position.y;
            MoveToTarget(new Vector2(targetPosition.x + dirType, targetPosition.y), () =>
            {
                if (animationHandle != null)
                {
                    animationHandle.PlayAnimation(skillAnimation, 0.1f, 1, false);
                }

                // Execute skill effect
                var skillHandle = GetComponent<SkillHandle>();
                if (skillHandle != null)
                {
                    skillHandle.Excute(speed);
                }
            });
        }

        public override void OnRangedAttack()
        {
            if (animationHandle != null)
            {
                animationHandle.PlayAnimation(skillAnimation, 0.1f, 1, false);
            }
        }

        public void Skill(List<Vector3> targetPositions, Vector3 transformPosition)
        {
            _targetPositions = targetPositions;
            SetTargetPosition(transformPosition);
            ExcuteAction();
        }
    }

    /// <summary>
    /// Skill Handle - handles skill effects
    /// </summary>
    public abstract class SkillHandle : MonoBehaviour
    {
        public abstract void Excute(float speed, Action callback = null);
    }

    /// <summary>
    /// Basic Skill - simple skill implementation
    /// </summary>
    public class BasicSkill : SkillHandle
    {
        [SerializeField] private ParticleSystem fxSkill;

        public override void Excute(float speed, Action callback = null)
        {
            if (fxSkill != null)
            {
                fxSkill.Play();
            }
            callback?.Invoke();
        }
    }

    /// <summary>
    /// Status View - displays status effects
    /// </summary>
    public class StatusView : MonoBehaviour
    {
        [SerializeField] private List<StatusIcon> activeStatusIcons = new List<StatusIcon>();

        void Start()
        {
        }

        public void AddStatus(StatusEffectType type, float duration)
        {
            // Could spawn UI icon here
            Debug.Log($"StatusView: Added {type} for {duration}s");
        }

        public void RemoveStatus(StatusEffectType type)
        {
            Debug.Log($"StatusView: Removed {type}");
        }

        public void ClearAll()
        {
            activeStatusIcons.Clear();
        }
    }

    public class StatusIcon : MonoBehaviour
    {
    }
}
