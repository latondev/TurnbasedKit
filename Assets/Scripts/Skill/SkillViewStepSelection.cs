using System;
using GameSystems.Battle;
using UnityEngine;

namespace GameSystems.Skills
{
    /// <summary>
    /// Serialized reference to one step inside one SkillViewSequence asset.
    /// </summary>
    [Serializable]
    public class SkillViewStepSelection : ISerializationCallbackReceiver
    {
        [SerializeField] private SkillViewSequence sequence;
        [SerializeField] private int stepIndex = -1;
        [SerializeField] private bool useLocalOverride;
        [SerializeField] private SkillViewStep localOverrideStep;

        public SkillViewSequence Sequence => sequence;
        public int StepIndex => stepIndex;
        public SkillViewStep SourceStep => GetSourceStep();
        public bool HasLocalOverride => useLocalOverride && localOverrideStep != null;

        public SkillViewStep Step
        {
            get
            {
                return HasLocalOverride ? localOverrideStep : SourceStep;
            }
        }

        public bool IsValid => Step != null;

        public void SetSelection(SkillViewSequence nextSequence, int nextStepIndex)
        {
            bool changed = sequence != nextSequence || stepIndex != (nextSequence == null ? -1 : Mathf.Max(-1, nextStepIndex));
            sequence = nextSequence;
            stepIndex = nextSequence == null ? -1 : Mathf.Max(-1, nextStepIndex);
            if (changed)
            {
                RevertLocalOverride();
            }
        }

        public void Clear()
        {
            sequence = null;
            stepIndex = -1;
            RevertLocalOverride();
        }

        public SkillViewStep CloneStep()
        {
            return Step?.Clone();
        }

        public SkillViewStepSelection DeepCopy()
        {
            var copied = new SkillViewStepSelection
            {
                sequence = sequence,
                stepIndex = stepIndex,
                useLocalOverride = useLocalOverride,
                localOverrideStep = localOverrideStep != null ? localOverrideStep.Clone() : null
            };

            if (copied.useLocalOverride && copied.localOverrideStep == null)
            {
                copied.localOverrideStep = copied.SourceStep != null ? copied.SourceStep.Clone() : null;
                if (copied.localOverrideStep == null)
                {
                    copied.useLocalOverride = false;
                }
            }

            return copied;
        }

        public bool TryActivateLocalOverride()
        {
            var sourceStep = SourceStep;
            if (sourceStep == null)
            {
                return false;
            }

            if (!HasLocalOverride || localOverrideStep == null)
            {
                localOverrideStep = sourceStep.Clone();
            }

            useLocalOverride = true;
            return true;
        }

        public void RevertLocalOverride()
        {
            useLocalOverride = false;
            localOverrideStep = null;
        }

        public string GetDisplayName()
        {
            var step = Step;
            if (step == null)
            {
                return sequence == null ? "<None>" : $"{(!string.IsNullOrWhiteSpace(sequence.SequenceId) ? sequence.SequenceId : sequence.name)} / Missing step #{stepIndex}";
            }

            string sequenceLabel = sequence == null
                ? "Local Override"
                : (!string.IsNullOrWhiteSpace(sequence.SequenceId) ? sequence.SequenceId : sequence.name);

            string animationLabel = string.IsNullOrWhiteSpace(step.AnimationName) ? string.Empty : $" [{step.AnimationName}]";
            string overrideSuffix = HasLocalOverride ? " [override]" : string.Empty;
            return $"{sequenceLabel} / #{stepIndex} {step.StepType}{animationLabel}{overrideSuffix}";
        }

        public override string ToString()
        {
            return GetDisplayName();
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            if (useLocalOverride && localOverrideStep == null)
            {
                localOverrideStep = SourceStep != null ? SourceStep.Clone() : new SkillViewStep();
            }
        }

        private SkillViewStep GetSourceStep()
        {
            if (sequence == null || sequence.Steps == null || stepIndex < 0 || stepIndex >= sequence.Steps.Count)
            {
                return null;
            }

            return sequence.Steps[stepIndex];
        }
    }
}
