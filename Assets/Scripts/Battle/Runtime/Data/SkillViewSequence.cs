using System;
using System.Collections.Generic;
using GameSystems.AutoBattle;
using UnityEngine;

namespace GameSystems.Battle
{
    [CreateAssetMenu(menuName = "Battle/Skill View Sequence", fileName = "SkillViewSequence")]
    public class SkillViewSequence : ScriptableObject
    {
        [SerializeField] private string sequenceId;
        [SerializeField] private string animationName = "skill";
        [SerializeField] private string fallbackAnimationName = "skill";
        [SerializeField] private string hitEventName = "hit";
        [SerializeField] private string falldownEventName = "falldown";
        [SerializeField] private string idleAnimationName = "idle";
        [SerializeField] private List<SkillViewStep> steps = new List<SkillViewStep>();

        public string SequenceId => sequenceId;
        public string AnimationName => animationName;
        public string FallbackAnimationName => fallbackAnimationName;
        public string HitEventName => hitEventName;
        public string FalldownEventName => falldownEventName;
        public string IdleAnimationName => idleAnimationName;
        public IReadOnlyList<SkillViewStep> Steps => steps;

        public void SetSequenceId(string value)
        {
            sequenceId = value;
        }

        public void SetRuntimeSteps(IEnumerable<SkillViewStep> runtimeSteps)
        {
            steps.Clear();
            if (runtimeSteps == null)
            {
                return;
            }

            steps.AddRange(runtimeSteps);
        }

        public void SetMetadata(
            string animationName,
            string fallbackAnimationName,
            string hitEventName,
            string falldownEventName,
            string idleAnimationName)
        {
            this.animationName = string.IsNullOrWhiteSpace(animationName) ? this.animationName : animationName;
            this.fallbackAnimationName = string.IsNullOrWhiteSpace(fallbackAnimationName) ? this.fallbackAnimationName : fallbackAnimationName;
            this.hitEventName = string.IsNullOrWhiteSpace(hitEventName) ? this.hitEventName : hitEventName;
            this.falldownEventName = string.IsNullOrWhiteSpace(falldownEventName) ? this.falldownEventName : falldownEventName;
            this.idleAnimationName = string.IsNullOrWhiteSpace(idleAnimationName) ? this.idleAnimationName : idleAnimationName;
        }

        public static SkillViewSequence CreateRuntimeSequence(string sequenceId, params SkillViewStep[] runtimeSteps)
        {
            var sequence = CreateInstance<SkillViewSequence>();
            sequence.sequenceId = sequenceId;
            sequence.SetRuntimeSteps(runtimeSteps);
            return sequence;
        }

        public static SkillViewSequence CreateBasicStrike(string sequenceId = "basic_strike", string attackAnimation = "skill")
        {
            var sequence = CreateRuntimeSequence(sequenceId, BuildBasicStrikeSteps(attackAnimation));
            sequence.SetMetadata(attackAnimation, attackAnimation, "hit", "falldown", "idle");
            return sequence;
        }

        public static SkillViewSequence CreateDashThroughStrike(string sequenceId = "dash_through_strike", string attackAnimation = "skill")
        {
            var sequence = CreateRuntimeSequence(sequenceId, BuildDashThroughStrikeSteps(attackAnimation));
            sequence.SetMetadata(attackAnimation, attackAnimation, "hit", "falldown", "idle");
            return sequence;
        }

        public static SkillViewSequence CreateStationaryCast(string sequenceId = "stationary_cast", string castAnimation = "skill")
        {
            var sequence = CreateRuntimeSequence(sequenceId, BuildStationaryCastSteps(castAnimation));
            sequence.SetMetadata(castAnimation, castAnimation, "hit", "falldown", "idle");
            return sequence;
        }

        public static SkillViewSequence CreateAreaBurst(string sequenceId = "area_burst", string castAnimation = "skill")
        {
            var sequence = CreateRuntimeSequence(sequenceId, BuildAreaBurstSteps(castAnimation));
            sequence.SetMetadata(castAnimation, castAnimation, "hit", "falldown", "idle");
            return sequence;
        }

        public static SkillViewSequence CreateJumpBehindStrike(string sequenceId = "jump_behind_strike", string attackAnimation = "skill")
        {
            var sequence = CreateRuntimeSequence(sequenceId, BuildJumpBehindStrikeSteps(attackAnimation));
            sequence.SetMetadata(attackAnimation, attackAnimation, "hit", "falldown", "idle");
            return sequence;
        }

        public void ApplyBasicStrikePreset(string attackAnimation = "skill")
        {
            sequenceId = string.IsNullOrEmpty(sequenceId) ? "basic_strike" : sequenceId;
            SetRuntimeSteps(BuildBasicStrikeSteps(attackAnimation));
            SetMetadata(attackAnimation, attackAnimation, "hit", "falldown", "idle");
        }

        public void ApplyDashThroughStrikePreset(string attackAnimation = "skill")
        {
            sequenceId = string.IsNullOrEmpty(sequenceId) ? "dash_through_strike" : sequenceId;
            SetRuntimeSteps(BuildDashThroughStrikeSteps(attackAnimation));
            SetMetadata(attackAnimation, attackAnimation, "hit", "falldown", "idle");
        }

        public void ApplyStationaryCastPreset(string castAnimation = "skill")
        {
            sequenceId = string.IsNullOrEmpty(sequenceId) ? "stationary_cast" : sequenceId;
            SetRuntimeSteps(BuildStationaryCastSteps(castAnimation));
            SetMetadata(castAnimation, castAnimation, "hit", "falldown", "idle");
        }

        public void ApplyAreaBurstPreset(string castAnimation = "skill")
        {
            sequenceId = string.IsNullOrEmpty(sequenceId) ? "area_burst" : sequenceId;
            SetRuntimeSteps(BuildAreaBurstSteps(castAnimation));
            SetMetadata(castAnimation, castAnimation, "hit", "falldown", "idle");
        }

        public void ApplyJumpBehindStrikePreset(string attackAnimation = "skill")
        {
            sequenceId = string.IsNullOrEmpty(sequenceId) ? "jump_behind_strike" : sequenceId;
            SetRuntimeSteps(BuildJumpBehindStrikeSteps(attackAnimation));
            SetMetadata(attackAnimation, attackAnimation, "hit", "falldown", "idle");
        }

        private static SkillViewStep[] BuildBasicStrikeSteps(string attackAnimation)
        {
            return new[]
            {
                new SkillViewStep(SkillViewStepType.MoveToTarget, SkillViewTargetType.PrimaryTarget, attackAnimation, attackAnimation, false, 0.25f, 0f, 1f, SkillViewMoveMode.Direct),
                new SkillViewStep(SkillViewStepType.PlayAnimation, SkillViewTargetType.PrimaryTarget, attackAnimation, "skill", false, 0.2f, 0f, 1f, SkillViewMoveMode.Direct, true, false, 1),
                new SkillViewStep(SkillViewStepType.TriggerHit, SkillViewTargetType.PrimaryTarget, attackAnimation, attackAnimation, false, 0f, 0f, 1f, SkillViewMoveMode.Direct, false, true, 1),
                new SkillViewStep(SkillViewStepType.MoveBack, SkillViewTargetType.Actor, attackAnimation, attackAnimation, false, 0.25f, 0f, 1f, SkillViewMoveMode.Direct),
                new SkillViewStep(SkillViewStepType.SetIdleAnimation, SkillViewTargetType.Actor, "idle", "idle", true, 0.1f)
            };
        }

        private static SkillViewStep[] BuildDashThroughStrikeSteps(string attackAnimation)
        {
            return new[]
            {
                new SkillViewStep(SkillViewStepType.MoveToTarget, SkillViewTargetType.PrimaryTarget, attackAnimation, attackAnimation, false, 0.3f, 0f, 1.2f, SkillViewMoveMode.ThroughTarget),
                new SkillViewStep(SkillViewStepType.PlayAnimation, SkillViewTargetType.PrimaryTarget, attackAnimation, attackAnimation, false, 0.16f),
                new SkillViewStep(SkillViewStepType.TriggerHit, SkillViewTargetType.PrimaryTarget, attackAnimation, attackAnimation, false, 0f, 0f, 1f, SkillViewMoveMode.Direct, false, true, 1),
                new SkillViewStep(SkillViewStepType.PlayAnimation, SkillViewTargetType.PrimaryTarget, attackAnimation, attackAnimation, false, 0.16f),
                new SkillViewStep(SkillViewStepType.TriggerHit, SkillViewTargetType.PrimaryTarget, attackAnimation, attackAnimation, false, 0f, 0f, 1f, SkillViewMoveMode.Direct, false, true, 1),
                new SkillViewStep(SkillViewStepType.MoveBack, SkillViewTargetType.Actor, attackAnimation, attackAnimation, false, 0.28f),
                new SkillViewStep(SkillViewStepType.SetIdleAnimation, SkillViewTargetType.Actor, "idle", "idle", true, 0.1f)
            };
        }

        private static SkillViewStep[] BuildStationaryCastSteps(string castAnimation)
        {
            return new[]
            {
                new SkillViewStep(SkillViewStepType.PlayAnimation, SkillViewTargetType.Actor, castAnimation, castAnimation, false, 0.2f),
                new SkillViewStep(SkillViewStepType.SpawnVfx, SkillViewTargetType.PrimaryTarget, castAnimation, castAnimation, false, 0.15f, 0f, 1f, SkillViewMoveMode.Direct, true, false, 1),
                new SkillViewStep(SkillViewStepType.TriggerHit, SkillViewTargetType.PrimaryTarget, castAnimation, castAnimation, false, 0f, 0f, 1f, SkillViewMoveMode.Direct, false, true, 1),
                new SkillViewStep(SkillViewStepType.SetIdleAnimation, SkillViewTargetType.Actor, "idle", "idle", true, 0.1f)
            };
        }

        private static SkillViewStep[] BuildAreaBurstSteps(string castAnimation)
        {
            return new[]
            {
                new SkillViewStep(SkillViewStepType.PlayAnimation, SkillViewTargetType.Actor, castAnimation, castAnimation, false, 0.2f),
                new SkillViewStep(SkillViewStepType.SpawnVfx, SkillViewTargetType.AllTargets, castAnimation, castAnimation, false, 0.2f, 0f, 1f, SkillViewMoveMode.Direct, true, false, 1),
                new SkillViewStep(SkillViewStepType.TriggerHit, SkillViewTargetType.AllTargets, castAnimation, castAnimation, false, 0f, 0f, 1f, SkillViewMoveMode.Direct, false, true, 1),
                new SkillViewStep(SkillViewStepType.SetIdleAnimation, SkillViewTargetType.Actor, "idle", "idle", true, 0.1f)
            };
        }

        private static SkillViewStep[] BuildJumpBehindStrikeSteps(string attackAnimation)
        {
            return new[]
            {
                new SkillViewStep(SkillViewStepType.MoveToTarget, SkillViewTargetType.PrimaryTarget, attackAnimation, attackAnimation, false, 0.28f, 0f, 1.05f, SkillViewMoveMode.ThroughTarget),
                new SkillViewStep(SkillViewStepType.PlayAnimation, SkillViewTargetType.PrimaryTarget, attackAnimation, attackAnimation, false, 0.12f),
                new SkillViewStep(SkillViewStepType.SetFlipX, SkillViewTargetType.Actor, attackAnimation, attackAnimation, false, 0f, 0f, -1f, SkillViewMoveMode.Direct, false, false, 1, 0, true),
                new SkillViewStep(SkillViewStepType.TriggerHit, SkillViewTargetType.PrimaryTarget, attackAnimation, attackAnimation, false, 0f, 0f, 1f, SkillViewMoveMode.Direct, false, true, 1),
                new SkillViewStep(SkillViewStepType.MoveBack, SkillViewTargetType.Actor, attackAnimation, attackAnimation, false, 0.24f),
                new SkillViewStep(SkillViewStepType.SetFlipX, SkillViewTargetType.Actor, attackAnimation, attackAnimation, false, 0f, 0f, 1f, SkillViewMoveMode.Direct, false, false, 1, 0, false),
                new SkillViewStep(SkillViewStepType.SetIdleAnimation, SkillViewTargetType.Actor, "idle", "idle", true, 0.1f)
            };
        }
    }

    [Serializable]
    public class SkillViewStep
    {
        [SerializeField] private SkillViewStepType stepType = SkillViewStepType.PlayAnimation;
        [SerializeField] private SkillViewTargetType targetType = SkillViewTargetType.PrimaryTarget;
        [SerializeField] private SkillViewMoveMode moveMode = SkillViewMoveMode.Direct;
        [SerializeField] private string animationName = "skill";
        [SerializeField] private string fallbackAnimationName = "skill";
        [SerializeField] private bool loop;
        [SerializeField] private float delay;
        [SerializeField] private float duration = 0.25f;
        [SerializeField] private float moveDistance = 1f;
        [SerializeField] private int sortingOrder;
        [SerializeField] private bool flipX;
        [SerializeField] private Vector3 worldPosition;
        [SerializeField] private Vector3 offset;
        [SerializeField] private ParticleSystem vfxPrefab;
        [SerializeField] private bool waitForAnimationEnd = true;
        [SerializeField] private bool triggerHitEffect;
        [SerializeField] private int hitCount = 1;

        public SkillViewStepType StepType => stepType;
        public SkillViewTargetType TargetType => targetType;
        public SkillViewMoveMode MoveMode => moveMode;
        public string AnimationName => animationName;
        public string FallbackAnimationName => fallbackAnimationName;
        public bool Loop => loop;
        public float Delay => delay;
        public float Duration => duration;
        public float MoveDistance => moveDistance;
        public int SortingOrder => sortingOrder;
        public bool FlipX => flipX;
        public Vector3 WorldPosition => worldPosition;
        public Vector3 Offset => offset;
        public ParticleSystem VfxPrefab => vfxPrefab;
        public bool WaitForAnimationEnd => waitForAnimationEnd;
        public bool TriggerHitEffect => triggerHitEffect;
        public int HitCount => hitCount;

        public SkillViewStep()
        {
        }

        public SkillViewStep Clone()
        {
            return new SkillViewStep(
                stepType,
                targetType,
                animationName,
                fallbackAnimationName,
                loop,
                duration,
                delay,
                moveDistance,
                moveMode,
                waitForAnimationEnd,
                triggerHitEffect,
                hitCount,
                sortingOrder,
                flipX,
                worldPosition,
                offset,
                vfxPrefab);
        }

        public SkillViewStep(
            SkillViewStepType stepType,
            SkillViewTargetType targetType = SkillViewTargetType.PrimaryTarget,
            string animationName = "skill",
            string fallbackAnimationName = "skill",
            bool loop = false,
            float duration = 0.25f,
            float delay = 0f,
            float moveDistance = 1f,
            SkillViewMoveMode moveMode = SkillViewMoveMode.Direct,
            bool waitForAnimationEnd = true,
            bool triggerHitEffect = false,
            int hitCount = 1,
            int sortingOrder = 0,
            bool flipX = false,
            Vector3? worldPosition = null,
            Vector3? offset = null,
            ParticleSystem vfxPrefab = null)
        {
            this.stepType = stepType;
            this.targetType = targetType;
            this.animationName = animationName;
            this.fallbackAnimationName = fallbackAnimationName;
            this.loop = loop;
            this.duration = duration;
            this.delay = delay;
            this.moveDistance = moveDistance;
            this.moveMode = moveMode;
            this.waitForAnimationEnd = waitForAnimationEnd;
            this.triggerHitEffect = triggerHitEffect;
            this.hitCount = hitCount;
            this.sortingOrder = sortingOrder;
            this.flipX = flipX;
            this.worldPosition = worldPosition ?? Vector3.zero;
            this.offset = offset ?? Vector3.zero;
            this.vfxPrefab = vfxPrefab;
        }
    }

    public enum SkillViewStepType
    {
        MoveToTarget,
        MoveBack,
        PlayAnimation,
        Wait,
        SpawnVfx,
        TriggerHit,
        SetFlipX,
        ResetSortingOrder,
        SetSortingOrder,
        SetIdleAnimation,
    }

    public enum SkillViewTargetType
    {
        PrimaryTarget,
        AllTargets,
        Actor,
        WorldPosition,
    }

    public enum SkillViewMoveMode
    {
        Direct,
        ThroughTarget,
        OffsetFromTarget,
    }

    public sealed class SkillViewContext
    {
        public SkillViewContext(
            BattleUnit actor,
            BattleUnit target,
            Vector3 actorStartPosition,
            Vector3 primaryTargetPosition,
            List<Vector3> targetPositions,
            BattleAction action = null)
        {
            Actor = actor;
            Target = target;
            ActorStartPosition = actorStartPosition;
            PrimaryTargetPosition = primaryTargetPosition;
            TargetPositions = targetPositions ?? new List<Vector3>();
            Action = action;
        }

        public BattleUnit Actor { get; }
        public BattleUnit Target { get; }
        public BattleAction Action { get; }
        public Vector3 ActorStartPosition { get; }
        public Vector3 PrimaryTargetPosition { get; }
        public List<Vector3> TargetPositions { get; }
        public Vector3 DirectionToTarget
        {
            get
            {
                var delta = PrimaryTargetPosition - ActorStartPosition;
                if (delta.sqrMagnitude <= 0.0001f)
                {
                    return Vector3.right;
                }

                return delta.normalized;
            }
        }

        public int HitCount => Mathf.Max(1, TargetPositions.Count);
    }
}
