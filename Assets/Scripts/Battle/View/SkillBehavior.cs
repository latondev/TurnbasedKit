using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameSystems.Battle
{
    /// <summary>
    /// Skill Behavior - runs view-only skill sequences.
    /// Legacy animation flow is preserved as fallback.
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
        private Coroutine _sequenceCoroutine;

        private void Awake()
        {
            if (animationHandle == null)
            {
                TryGetComponent(out animationHandle);
            }

            if (animationHandle == null)
            {
                animationHandle = GetComponentInChildren<AnimationHandle>(true);
            }

            if (animationHandle != null)
            {
                animationHandle.OnEventAnimation += OnEventAnimation;
                animationHandle.OnEndAnimation += EndAnimation;
            }
        }

        public void Play(SkillViewSequence sequence, SkillViewContext context)
        {
            if (sequence == null || sequence.Steps == null || sequence.Steps.Count == 0 || context == null)
            {
                Skill(
                    context?.TargetPositions != null && context.TargetPositions.Count > 0
                        ? context.TargetPositions
                        : new List<Vector3> { context?.PrimaryTargetPosition ?? transform.position },
                    context?.PrimaryTargetPosition ?? transform.position);
                return;
            }

            if (_sequenceCoroutine != null)
            {
                StopCoroutine(_sequenceCoroutine);
            }

            _sequenceCoroutine = StartCoroutine(PlaySequenceRoutine(sequence, context));
        }

        private void PlaySkillAnimation()
        {
            if (skillAnimations != null && skillAnimations.Count > 0)
            {
                skillAnimation = skillAnimations[Random.Range(0, skillAnimations.Count)];
            }
        }

        protected void EndAnimation(string trackentry)
        {
            if (trackentry == skillAnimation)
            {
                if (animationHandle != null)
                {
                    animationHandle.TryPlayAnimation(moveBack, moveBack, 0.1f, 2, false);
                }

                MoveToRoot(() =>
                {
                    if (animationHandle != null)
                    {
                        animationHandle.TryPlayAnimation(idleAnimation, idleAnimation, 0.1f, 0, true);
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
                animationHandle.TryPlayAnimation(moveGo, moveGo, 0.1f, 1, false);
            }

            MoveToTarget(new Vector2(targetPosition.x + dirType, targetPosition.y), () =>
            {
                if (animationHandle != null)
                {
                    animationHandle.TryPlayAnimation(skillAnimation, "skill", 0.1f, 1, false);
                }
            });
        }

        public override void OnRangedAttack()
        {
            if (animationHandle != null)
            {
                animationHandle.TryPlayAnimation(skillAnimation, "skill", 0.1f, 1, false);
            }
        }

        public void Skill(List<Vector3> targetPositions, Vector3 transformPosition)
        {
            _targetPositions = targetPositions;
            SetTargetPosition(transformPosition);
            ExcuteAction();
        }

        private IEnumerator PlaySequenceRoutine(SkillViewSequence sequence, SkillViewContext context)
        {
            if (animationHandle != null)
            {
                animationHandle.Initialize();
            }

            foreach (var step in sequence.Steps)
            {
                if (step == null)
                {
                    continue;
                }

                if (step.Delay > 0f)
                {
                    yield return new WaitForSeconds(step.Delay / Mathf.Max(0.01f, speed));
                }

                switch (step.StepType)
                {
                    case SkillViewStepType.MoveToTarget:
                    {
                        yield return MoveToTargetStep(ResolveDestination(step, context), step.Duration);
                        break;
                    }
                    case SkillViewStepType.MoveBack:
                    {
                        yield return MoveBackStep(step.Duration);
                        break;
                    }
                    case SkillViewStepType.PlayAnimation:
                    {
                        PlaySequenceAnimation(step);
                        if (step.WaitForAnimationEnd && step.Duration > 0f)
                        {
                            yield return new WaitForSeconds(step.Duration / Mathf.Max(0.01f, speed));
                        }
                        break;
                    }
                    case SkillViewStepType.Wait:
                    {
                        if (step.Duration > 0f)
                        {
                            yield return new WaitForSeconds(step.Duration / Mathf.Max(0.01f, speed));
                        }
                        break;
                    }
                    case SkillViewStepType.SpawnVfx:
                    {
                        SpawnStepVfx(step, context);
                        break;
                    }
                    case SkillViewStepType.TriggerHit:
                    {
                        int hitCount = step.HitCount > 0 ? step.HitCount : context.HitCount;
                        OnEndStepAction?.Invoke(hitCount, step.TriggerHitEffect);
                        break;
                    }
                    case SkillViewStepType.ResetSortingOrder:
                    {
                        animationHandle?.ResetSortingOrder();
                        break;
                    }
                    case SkillViewStepType.SetSortingOrder:
                    {
                        if (animationHandle != null)
                        {
                            animationHandle.SetSortingOrder(step.SortingOrder, "Unit");
                        }
                        break;
                    }
                    case SkillViewStepType.SetFlipX:
                    {
                        animationHandle?.SetFlipX(step.FlipX);
                        break;
                    }
                    case SkillViewStepType.SetIdleAnimation:
                    {
                        if (animationHandle != null)
                        {
                            animationHandle.TryPlayAnimation(idleAnimation, idleAnimation, 0.1f, 0, true);
                        }
                        break;
                    }
                }
            }

            if (animationHandle != null)
            {
                animationHandle.TryPlayAnimation(idleAnimation, idleAnimation, 0.1f, 0, true);
                animationHandle.ResetSortingOrder();
                animationHandle.SetSortingOrder(2 - (int)transform.position.y);
            }

            OnEndAction?.Invoke();
            _sequenceCoroutine = null;
        }

        private IEnumerator MoveToTargetStep(Vector3 destination, float desiredDuration)
        {
            float previousSpeed = speed;
            if (desiredDuration > 0f)
            {
                speed = 0.4f / desiredDuration;
            }

            bool done = false;
            MoveToTarget(destination, () => done = true);

            while (!done)
            {
                yield return null;
            }

            speed = previousSpeed;
        }

        private IEnumerator MoveBackStep(float desiredDuration)
        {
            float previousSpeed = speed;
            if (desiredDuration > 0f)
            {
                speed = 0.4f / desiredDuration;
            }

            bool done = false;
            MoveToRoot(() => done = true);

            while (!done)
            {
                yield return null;
            }

            speed = previousSpeed;
        }

        private void PlaySequenceAnimation(SkillViewStep step)
        {
            if (animationHandle == null)
            {
                return;
            }

            string primary = string.IsNullOrEmpty(step.AnimationName) ? skillAnimation : step.AnimationName;
            string fallback = string.IsNullOrEmpty(step.FallbackAnimationName) ? skillAnimation : step.FallbackAnimationName;
            animationHandle.TryPlayAnimation(primary, fallback, 0.1f, 1, step.Loop);
        }

        private Vector3 ResolveDestination(SkillViewStep step, SkillViewContext context)
        {
            if (context == null)
            {
                return transform.position;
            }

            return step.TargetType switch
            {
                SkillViewTargetType.Actor => context.ActorStartPosition + step.Offset,
                SkillViewTargetType.AllTargets => context.PrimaryTargetPosition + step.Offset,
                SkillViewTargetType.WorldPosition => step.WorldPosition + step.Offset,
                _ => ResolvePrimaryTargetDestination(step, context),
            };
        }

        private Vector3 ResolvePrimaryTargetDestination(SkillViewStep step, SkillViewContext context)
        {
            if (step.MoveMode == SkillViewMoveMode.OffsetFromTarget)
            {
                return context.PrimaryTargetPosition + step.Offset;
            }

            Vector3 direction = context.DirectionToTarget;
            float signedDistance = step.MoveMode == SkillViewMoveMode.ThroughTarget
                ? -Mathf.Abs(step.MoveDistance)
                : Mathf.Abs(step.MoveDistance);

            return context.PrimaryTargetPosition - (direction * signedDistance) + step.Offset;
        }

        private void SpawnStepVfx(SkillViewStep step, SkillViewContext context)
        {
            if (step.VfxPrefab == null)
            {
                return;
            }

            if (step.TargetType == SkillViewTargetType.AllTargets && context != null && context.TargetPositions.Count > 0)
            {
                foreach (var targetPos in context.TargetPositions)
                {
                    var fx = Instantiate(step.VfxPrefab, targetPos + step.Offset, Quaternion.identity);
                    fx.Play();
                    Destroy(fx.gameObject, 5f);
                }

                return;
            }

            Vector3 spawnPosition = context != null ? context.PrimaryTargetPosition : transform.position;
            if (step.TargetType == SkillViewTargetType.Actor && context != null)
            {
                spawnPosition = context.ActorStartPosition;
            }
            else if (step.TargetType == SkillViewTargetType.WorldPosition)
            {
                spawnPosition = step.WorldPosition;
            }

            var instance = Instantiate(step.VfxPrefab, spawnPosition + step.Offset, Quaternion.identity);
            instance.Play();
            Destroy(instance.gameObject, 5f);
        }
    }
}
