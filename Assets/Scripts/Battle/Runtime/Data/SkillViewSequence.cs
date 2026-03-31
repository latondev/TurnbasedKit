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
        [SerializeField] private List<SkillViewStep> steps = new List<SkillViewStep>();

        public string SequenceId => sequenceId;
        public IReadOnlyList<SkillViewStep> Steps => steps;

        public void SetRuntimeSteps(IEnumerable<SkillViewStep> runtimeSteps)
        {
            steps.Clear();
            if (runtimeSteps == null)
            {
                return;
            }

            steps.AddRange(runtimeSteps);
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
            return CreateRuntimeSequence(sequenceId, BuildBasicStrikeSteps(attackAnimation));
        }

        public static SkillViewSequence CreateDashThroughStrike(string sequenceId = "dash_through_strike", string attackAnimation = "skill")
        {
            return CreateRuntimeSequence(sequenceId, BuildDashThroughStrikeSteps(attackAnimation));
        }

        public static SkillViewSequence CreateStationaryCast(string sequenceId = "stationary_cast", string castAnimation = "skill")
        {
            return CreateRuntimeSequence(sequenceId, BuildStationaryCastSteps(castAnimation));
        }

        public static SkillViewSequence CreateAreaBurst(string sequenceId = "area_burst", string castAnimation = "skill")
        {
            return CreateRuntimeSequence(sequenceId, BuildAreaBurstSteps(castAnimation));
        }

        public static SkillViewSequence CreateJumpBehindStrike(string sequenceId = "jump_behind_strike", string attackAnimation = "skill")
        {
            return CreateRuntimeSequence(sequenceId, BuildJumpBehindStrikeSteps(attackAnimation));
        }

        public void ApplyBasicStrikePreset(string attackAnimation = "skill")
        {
            sequenceId = string.IsNullOrEmpty(sequenceId) ? "basic_strike" : sequenceId;
            SetRuntimeSteps(BuildBasicStrikeSteps(attackAnimation));
        }

        public void ApplyDashThroughStrikePreset(string attackAnimation = "skill")
        {
            sequenceId = string.IsNullOrEmpty(sequenceId) ? "dash_through_strike" : sequenceId;
            SetRuntimeSteps(BuildDashThroughStrikeSteps(attackAnimation));
        }

        public void ApplyStationaryCastPreset(string castAnimation = "skill")
        {
            sequenceId = string.IsNullOrEmpty(sequenceId) ? "stationary_cast" : sequenceId;
            SetRuntimeSteps(BuildStationaryCastSteps(castAnimation));
        }

        public void ApplyAreaBurstPreset(string castAnimation = "skill")
        {
            sequenceId = string.IsNullOrEmpty(sequenceId) ? "area_burst" : sequenceId;
            SetRuntimeSteps(BuildAreaBurstSteps(castAnimation));
        }

        public void ApplyJumpBehindStrikePreset(string attackAnimation = "skill")
        {
            sequenceId = string.IsNullOrEmpty(sequenceId) ? "jump_behind_strike" : sequenceId;
            SetRuntimeSteps(BuildJumpBehindStrikeSteps(attackAnimation));
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
