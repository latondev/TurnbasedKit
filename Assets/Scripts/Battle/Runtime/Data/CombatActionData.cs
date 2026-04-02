using System;
using System.Collections.Generic;
using GameSystems.Skills;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameSystems.Battle
{
    [Serializable]
    public class CombatActionData : ISerializationCallbackReceiver
    {
        [SerializeField] private string actionId;
        [SerializeField] private string displayName;
        [SerializeField] private CombatActionKind actionKind = CombatActionKind.Custom;
        [SerializeField] private SkillData skillRef;
        [SerializeField] private string mainAnimationName = "skill";
        [SerializeField] private string fallbackAnimationName = "skill";
        [SerializeField] private string idleAnimationName = "idle";
        [SerializeField] private string hitEventName = "hit";
        [SerializeField] private string falldownEventName = "falldown";
        [SerializeField] private List<SkillViewStepSelection> stepSelections =
            new List<SkillViewStepSelection>();
        [SerializeField, HideInInspector] private SkillViewSequence viewSequence; // legacy fallback

        [NonSerialized] private SkillViewSequence runtimeSequenceCache;

#if UNITY_EDITOR
        private static readonly List<SkillViewSequence> pendingRuntimeSequenceCacheDestructions =
            new List<SkillViewSequence>();
        private static bool pendingRuntimeSequenceCacheDestructionScheduled;
#endif

        public string ActionId => actionId;
        public string DisplayName => displayName;
        public CombatActionKind ActionKind => actionKind;
        public SkillData SkillRef => skillRef;
        public string MainAnimationName => mainAnimationName;
        public string FallbackAnimationName => fallbackAnimationName;
        public string IdleAnimationName => idleAnimationName;
        public string HitEventName => hitEventName;
        public string FalldownEventName => falldownEventName;
        public IReadOnlyList<SkillViewStepSelection> StepSelections => stepSelections;
        public bool HasHitEvent => !string.IsNullOrWhiteSpace(hitEventName);

        public SkillViewSequence ViewSequence => GetOrBuildRuntimeSequence();

        public void SetActionId(string value)
        {
            actionId = value;
        }

        public void SetDisplayName(string value)
        {
            displayName = value;
        }

        public void SetActionKind(CombatActionKind value)
        {
            actionKind = value;
        }

        public void SetSkillRef(SkillData value)
        {
            skillRef = value;
        }

        public void SetMetadata(
            string mainAnimation,
            string fallbackAnimation,
            string hitEvent,
            string falldownEvent,
            string idleAnimation
        )
        {
            mainAnimationName = mainAnimation;
            fallbackAnimationName = fallbackAnimation;
            hitEventName = hitEvent;
            falldownEventName = falldownEvent;
            idleAnimationName = idleAnimation;
            InvalidateRuntimeSequenceCache();
        }

        public void SetLegacyViewSequence(SkillViewSequence sequence)
        {
            viewSequence = sequence;
            InvalidateRuntimeSequenceCache();
        }

        public void SetStepSelectionsFrom(IReadOnlyList<SkillViewStepSelection> selections)
        {
            EnsureStepSelections();
            stepSelections.Clear();

            if (selections == null)
            {
                InvalidateRuntimeSequenceCache();
                return;
            }

            for (int i = 0; i < selections.Count; i++)
            {
                SkillViewStepSelection selection = selections[i];
                if (selection == null)
                {
                    continue;
                }

                stepSelections.Add(selection.DeepCopy());
            }

            InvalidateRuntimeSequenceCache();
        }

        public void EnsureDefaults()
        {
            if (string.IsNullOrWhiteSpace(actionId))
            {
                actionId = BuildDefaultActionId(actionKind);
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = actionKind.ToString();
            }

            if (actionKind == CombatActionKind.BasicAttack)
            {
                if (string.IsNullOrWhiteSpace(mainAnimationName))
                {
                    mainAnimationName = "attack";
                }

                if (string.IsNullOrWhiteSpace(fallbackAnimationName))
                {
                    fallbackAnimationName = "war_attack";
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(mainAnimationName))
                {
                    mainAnimationName = "skill";
                }

                if (string.IsNullOrWhiteSpace(fallbackAnimationName))
                {
                    fallbackAnimationName = "skill";
                }
            }

            if (string.IsNullOrWhiteSpace(idleAnimationName))
            {
                idleAnimationName = "idle";
            }

            if (string.IsNullOrWhiteSpace(hitEventName))
            {
                hitEventName = "hit";
            }

            if (string.IsNullOrWhiteSpace(falldownEventName))
            {
                falldownEventName = "falldown";
            }
        }

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            EnsureStepSelections();
            EnsureDefaults();
            InvalidateRuntimeSequenceCache();
        }

        private void EnsureStepSelections()
        {
            if (stepSelections == null)
            {
                stepSelections = new List<SkillViewStepSelection>();
            }
        }

        private SkillViewSequence GetOrBuildRuntimeSequence()
        {
            if (runtimeSequenceCache != null)
            {
                return runtimeSequenceCache;
            }

            EnsureStepSelections();

            var runtimeSteps = new List<SkillViewStep>();
            SkillViewSequence metadataSource = null;

            for (int i = 0; i < stepSelections.Count; i++)
            {
                SkillViewStepSelection selection = stepSelections[i];
                if (selection == null || !selection.IsValid)
                {
                    continue;
                }

                if (metadataSource == null)
                {
                    metadataSource = selection.Sequence;
                }

                SkillViewStep cloned = selection.CloneStep();
                if (cloned != null)
                {
                    runtimeSteps.Add(cloned);
                }
            }

            if (runtimeSteps.Count > 0)
            {
                runtimeSequenceCache = ScriptableObject.CreateInstance<SkillViewSequence>();
                runtimeSequenceCache.hideFlags = HideFlags.HideAndDontSave;
                runtimeSequenceCache.SetSequenceId(BuildRuntimeSequenceId());
                runtimeSequenceCache.SetRuntimeSteps(runtimeSteps);

                if (metadataSource != null)
                {
                    runtimeSequenceCache.SetMetadata(
                        metadataSource.AnimationName,
                        metadataSource.FallbackAnimationName,
                        metadataSource.HitEventName,
                        metadataSource.FalldownEventName,
                        metadataSource.IdleAnimationName
                    );
                }

                runtimeSequenceCache.SetMetadata(
                    mainAnimationName,
                    fallbackAnimationName,
                    hitEventName,
                    falldownEventName,
                    idleAnimationName
                );
                return runtimeSequenceCache;
            }

            if (viewSequence != null)
            {
                runtimeSequenceCache = viewSequence;
                return runtimeSequenceCache;
            }

            runtimeSequenceCache = CreateFallbackRuntimeSequence();
            return runtimeSequenceCache;
        }

        private SkillViewSequence CreateFallbackRuntimeSequence()
        {
            string sequenceId = BuildRuntimeSequenceId();
            string animationName = !string.IsNullOrWhiteSpace(mainAnimationName)
                ? mainAnimationName
                : (actionKind == CombatActionKind.BasicAttack ? "attack" : "skill");

            SkillViewSequence sequence =
                actionKind == CombatActionKind.BasicAttack
                    ? SkillViewSequence.CreateBasicStrike(sequenceId, animationName)
                    : SkillViewSequence.CreateStationaryCast(sequenceId, animationName);

            sequence.hideFlags = HideFlags.HideAndDontSave;
            sequence.SetMetadata(
                mainAnimationName,
                fallbackAnimationName,
                hitEventName,
                falldownEventName,
                idleAnimationName
            );
            return sequence;
        }

        private string BuildRuntimeSequenceId()
        {
            string baseId = !string.IsNullOrWhiteSpace(actionId) ? actionId : displayName;
            if (string.IsNullOrWhiteSpace(baseId))
            {
                baseId = BuildDefaultActionId(actionKind);
            }

            return $"{baseId}_runtime";
        }

        private static string BuildDefaultActionId(CombatActionKind kind)
        {
            return kind switch
            {
                CombatActionKind.BasicAttack => "basic_attack",
                CombatActionKind.SkillBasic => "skill_basic",
                CombatActionKind.SkillUltimate => "skill_ultimate",
                CombatActionKind.SkillPassive => "skill_passive",
                CombatActionKind.SkillAwaken => "skill_awaken",
                _ => "custom_action",
            };
        }

        public void InvalidateViewSequenceCache()
        {
            InvalidateRuntimeSequenceCache();
        }

        private void InvalidateRuntimeSequenceCache()
        {
            if (runtimeSequenceCache == null)
            {
                return;
            }

            SkillViewSequence cacheToDestroy = runtimeSequenceCache;
            runtimeSequenceCache = null;

            if (cacheToDestroy == viewSequence)
            {
                return;
            }

#if UNITY_EDITOR
            QueueRuntimeSequenceCacheDestruction(cacheToDestroy);
#else
            UnityEngine.Object.Destroy(cacheToDestroy);
#endif
        }

#if UNITY_EDITOR
        private static void QueueRuntimeSequenceCacheDestruction(SkillViewSequence cacheToDestroy)
        {
            if (cacheToDestroy == null)
            {
                return;
            }

            if (!pendingRuntimeSequenceCacheDestructions.Contains(cacheToDestroy))
            {
                pendingRuntimeSequenceCacheDestructions.Add(cacheToDestroy);
            }

            if (pendingRuntimeSequenceCacheDestructionScheduled)
            {
                return;
            }

            pendingRuntimeSequenceCacheDestructionScheduled = true;
            EditorApplication.delayCall += FlushPendingRuntimeSequenceCacheDestructions;
        }

        private static void FlushPendingRuntimeSequenceCacheDestructions()
        {
            EditorApplication.delayCall -= FlushPendingRuntimeSequenceCacheDestructions;
            pendingRuntimeSequenceCacheDestructionScheduled = false;

            for (int i = 0; i < pendingRuntimeSequenceCacheDestructions.Count; i++)
            {
                SkillViewSequence cache = pendingRuntimeSequenceCacheDestructions[i];
                if (cache != null)
                {
                    UnityEngine.Object.DestroyImmediate(cache);
                }
            }

            pendingRuntimeSequenceCacheDestructions.Clear();
        }
#endif
    }

    public enum CombatActionKind
    {
        BasicAttack,
        SkillBasic,
        SkillUltimate,
        SkillPassive,
        SkillAwaken,
        Custom,
    }
}
