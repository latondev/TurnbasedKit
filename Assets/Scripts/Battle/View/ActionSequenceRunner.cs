using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameSystems.Battle
{
    /// <summary>
    /// Unified visual action runner used by both Attack and Skill actions.
    /// </summary>
    public class ActionSequenceRunner : ActionBase
    {
        [Header("Defaults")]
        [SerializeField] private string defaultAttackAnimation = "attack";
        [SerializeField] private string defaultSkillAnimation = "skill";
        [SerializeField] private string defaultIdleAnimation = "idle";
        [SerializeField] private string defaultHitEvent = "hit";
        [SerializeField] private string defaultFalldownEvent = "falldown";

        private CombatActionData currentAction;
        private SkillViewSequence currentSequence;
        private SkillViewContext currentContext;
        private Coroutine sequenceCoroutine;
        private string activeHitEvent;
        private string activeFalldownEvent;
        private bool hitSignalSent;
        private bool falldownSignalSent;

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
                animationHandle.Initialize();
                animationHandle.OnEventAnimation -= HandleEventAnimation;
                animationHandle.OnEventAnimation += HandleEventAnimation;
            }
        }

        private void OnDestroy()
        {
            if (animationHandle != null)
            {
                animationHandle.OnEventAnimation -= HandleEventAnimation;
            }
        }

        public void Play(CombatActionData action, SkillViewContext context)
        {
            currentAction = action;
            currentContext = context;
            currentSequence = action != null ? action.ViewSequence : null;
            hitSignalSent = false;
            falldownSignalSent = false;
            activeHitEvent = ResolveHitEventName();
            activeFalldownEvent = ResolveFalldownEventName();

            if (animationHandle != null)
            {
                animationHandle.Initialize();
            }

            if (sequenceCoroutine != null)
            {
                StopCoroutine(sequenceCoroutine);
                sequenceCoroutine = null;
            }

            if (
                currentSequence == null
                || currentSequence.Steps == null
                || currentSequence.Steps.Count == 0
            )
            {
                PlayIdleAnimation(null);
                TriggerFallbackHitIfNeeded();
                OnEndAction?.Invoke();
                return;
            }

            sequenceCoroutine = StartCoroutine(PlaySequenceRoutine());
        }

        private IEnumerator PlaySequenceRoutine()
        {
            for (int i = 0; i < currentSequence.Steps.Count; i++)
            {
                SkillViewStep step = currentSequence.Steps[i];
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
                        PlaySequenceAnimation(step, 1);
                        yield return MoveToTargetStep(ResolveDestination(step), step.Duration);
                        break;
                    }
                    case SkillViewStepType.MoveBack:
                    {
                        PlaySequenceAnimation(step, 2);
                        yield return MoveBackStep(step.Duration);
                        break;
                    }
                    case SkillViewStepType.PlayAnimation:
                    {
                        PlaySequenceAnimation(step, 1);
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
                        SpawnStepVfx(step);
                        break;
                    }
                    case SkillViewStepType.TriggerHit:
                    {
                        // TriggerHit is compatibility fallback; event animation is the primary timing source.
                        if (!hitSignalSent && string.IsNullOrWhiteSpace(activeHitEvent))
                        {
                            OnEndStepAction?.Invoke(ResolveHitCount(step), step.TriggerHitEffect);
                            hitSignalSent = true;
                        }
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
                        PlayIdleAnimation(step);
                        if (step.Duration > 0f)
                        {
                            yield return new WaitForSeconds(step.Duration / Mathf.Max(0.01f, speed));
                        }
                        break;
                    }
                }
            }

            if (animationHandle != null)
            {
                PlayIdleAnimation(null);
                animationHandle.ResetSortingOrder();
                animationHandle.SetSortingOrder(2 - (int)transform.position.y);
            }

            TriggerFallbackHitIfNeeded();
            sequenceCoroutine = null;
            OnEndAction?.Invoke();
        }

        private void TriggerFallbackHitIfNeeded()
        {
            if (hitSignalSent)
            {
                return;
            }

            hitSignalSent = true;
            OnEndStepAction?.Invoke(ResolveHitCount(null), false);
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

        private void PlaySequenceAnimation(SkillViewStep step, int layer)
        {
            if (animationHandle == null)
            {
                return;
            }

            string primary = ResolveAnimationName(step);
            string fallback = ResolveFallbackAnimationName(step);
            animationHandle.TryPlayAnimation(primary, fallback, 0.1f, layer, step.Loop);
        }

        private void PlayIdleAnimation(SkillViewStep step)
        {
            if (animationHandle == null)
            {
                return;
            }

            animationHandle.ClearTrack(1);
            animationHandle.ClearTrack(2);

            string primary = step != null && !string.IsNullOrWhiteSpace(step.AnimationName)
                ? step.AnimationName
                : ResolveIdleAnimationName();
            string fallback = step != null && !string.IsNullOrWhiteSpace(step.FallbackAnimationName)
                ? step.FallbackAnimationName
                : primary;

            animationHandle.TryPlayAnimation(primary, fallback, step != null ? step.Duration : 0.1f, 0, true);
        }

        private void HandleEventAnimation(string animationName, string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                return;
            }

            if (
                !hitSignalSent
                && !string.IsNullOrWhiteSpace(activeHitEvent)
                && string.Equals(eventName, activeHitEvent, StringComparison.OrdinalIgnoreCase)
            )
            {
                hitSignalSent = true;
                OnEndStepAction?.Invoke(ResolveHitCount(null), false);
                return;
            }

            if (
                !falldownSignalSent
                && !string.IsNullOrWhiteSpace(activeFalldownEvent)
                && string.Equals(eventName, activeFalldownEvent, StringComparison.OrdinalIgnoreCase)
            )
            {
                falldownSignalSent = true;
                OnEndStepAction?.Invoke(ResolveHitCount(null), true);
            }
        }

        private int ResolveHitCount(SkillViewStep step)
        {
            if (step != null && step.HitCount > 0)
            {
                return step.HitCount;
            }

            if (currentContext != null && currentContext.HitCount > 0)
            {
                return currentContext.HitCount;
            }

            return 1;
        }

        private string ResolveHitEventName()
        {
            if (currentAction != null && !string.IsNullOrWhiteSpace(currentAction.HitEventName))
            {
                return currentAction.HitEventName;
            }

            if (currentSequence != null && !string.IsNullOrWhiteSpace(currentSequence.HitEventName))
            {
                return currentSequence.HitEventName;
            }

            return defaultHitEvent;
        }

        private string ResolveFalldownEventName()
        {
            if (currentAction != null && !string.IsNullOrWhiteSpace(currentAction.FalldownEventName))
            {
                return currentAction.FalldownEventName;
            }

            if (currentSequence != null && !string.IsNullOrWhiteSpace(currentSequence.FalldownEventName))
            {
                return currentSequence.FalldownEventName;
            }

            return defaultFalldownEvent;
        }

        private string ResolveIdleAnimationName()
        {
            if (currentAction != null && !string.IsNullOrWhiteSpace(currentAction.IdleAnimationName))
            {
                return currentAction.IdleAnimationName;
            }

            if (currentSequence != null && !string.IsNullOrWhiteSpace(currentSequence.IdleAnimationName))
            {
                return currentSequence.IdleAnimationName;
            }

            return defaultIdleAnimation;
        }

        private string ResolveAnimationName(SkillViewStep step)
        {
            if (step != null && !string.IsNullOrWhiteSpace(step.AnimationName))
            {
                return step.AnimationName;
            }

            if (currentAction != null && !string.IsNullOrWhiteSpace(currentAction.MainAnimationName))
            {
                return currentAction.MainAnimationName;
            }

            if (currentSequence != null && !string.IsNullOrWhiteSpace(currentSequence.AnimationName))
            {
                return currentSequence.AnimationName;
            }

            return currentAction != null && currentAction.ActionKind == CombatActionKind.BasicAttack
                ? defaultAttackAnimation
                : defaultSkillAnimation;
        }

        private string ResolveFallbackAnimationName(SkillViewStep step)
        {
            if (step != null && !string.IsNullOrWhiteSpace(step.FallbackAnimationName))
            {
                return step.FallbackAnimationName;
            }

            if (currentAction != null && !string.IsNullOrWhiteSpace(currentAction.FallbackAnimationName))
            {
                return currentAction.FallbackAnimationName;
            }

            if (
                currentSequence != null
                && !string.IsNullOrWhiteSpace(currentSequence.FallbackAnimationName)
            )
            {
                return currentSequence.FallbackAnimationName;
            }

            return ResolveAnimationName(step);
        }

        private Vector3 ResolveDestination(SkillViewStep step)
        {
            if (currentContext == null)
            {
                return transform.position;
            }

            return step.TargetType switch
            {
                SkillViewTargetType.Actor => currentContext.ActorStartPosition + step.Offset,
                SkillViewTargetType.AllTargets => currentContext.PrimaryTargetPosition + step.Offset,
                SkillViewTargetType.WorldPosition => step.WorldPosition + step.Offset,
                _ => ResolvePrimaryTargetDestination(step),
            };
        }

        private Vector3 ResolvePrimaryTargetDestination(SkillViewStep step)
        {
            if (currentContext == null)
            {
                return transform.position;
            }

            if (step.MoveMode == SkillViewMoveMode.OffsetFromTarget)
            {
                return currentContext.PrimaryTargetPosition + step.Offset;
            }

            Vector3 direction = currentContext.DirectionToTarget;
            float signedDistance = step.MoveMode == SkillViewMoveMode.ThroughTarget
                ? -Mathf.Abs(step.MoveDistance)
                : Mathf.Abs(step.MoveDistance);

            return currentContext.PrimaryTargetPosition - (direction * signedDistance) + step.Offset;
        }

        private void SpawnStepVfx(SkillViewStep step)
        {
            if (step == null || step.VfxPrefab == null)
            {
                return;
            }

            if (
                step.TargetType == SkillViewTargetType.AllTargets
                && currentContext != null
                && currentContext.TargetPositions.Count > 0
            )
            {
                for (int i = 0; i < currentContext.TargetPositions.Count; i++)
                {
                    Vector3 targetPos = currentContext.TargetPositions[i];
                    var fx = Instantiate(step.VfxPrefab, targetPos + step.Offset, Quaternion.identity);
                    fx.Play();
                    Destroy(fx.gameObject, 5f);
                }

                return;
            }

            Vector3 spawnPosition = currentContext != null
                ? currentContext.PrimaryTargetPosition
                : transform.position;
            if (step.TargetType == SkillViewTargetType.Actor && currentContext != null)
            {
                spawnPosition = currentContext.ActorStartPosition;
            }
            else if (step.TargetType == SkillViewTargetType.WorldPosition)
            {
                spawnPosition = step.WorldPosition;
            }

            var instance = Instantiate(step.VfxPrefab, spawnPosition + step.Offset, Quaternion.identity);
            instance.Play();
            Destroy(instance.gameObject, 5f);
        }

        public override void OnMeleeAttack() { }

        public override void OnRangedAttack() { }
    }
}
