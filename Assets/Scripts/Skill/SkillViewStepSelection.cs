using System;
using GameSystems.Battle;
using UnityEngine;

namespace GameSystems.Skills
{
    /// <summary>
    /// Serialized reference to one step inside one SkillViewSequence asset.
    /// </summary>
    [Serializable]
    public class SkillViewStepSelection
    {
        [SerializeField] private SkillViewSequence sequence;
        [SerializeField] private int stepIndex = -1;

        public SkillViewSequence Sequence => sequence;
        public int StepIndex => stepIndex;

        public SkillViewStep Step
        {
            get
            {
                if (sequence == null || sequence.Steps == null || stepIndex < 0 || stepIndex >= sequence.Steps.Count)
                {
                    return null;
                }

                return sequence.Steps[stepIndex];
            }
        }

        public bool IsValid => Step != null;

        public void SetSelection(SkillViewSequence nextSequence, int nextStepIndex)
        {
            sequence = nextSequence;
            stepIndex = nextSequence == null ? -1 : Mathf.Max(-1, nextStepIndex);
        }

        public void Clear()
        {
            sequence = null;
            stepIndex = -1;
        }

        public SkillViewStep CloneStep()
        {
            return Step?.Clone();
        }

        public string GetDisplayName()
        {
            if (sequence == null)
            {
                return "<None>";
            }

            string sequenceLabel = !string.IsNullOrWhiteSpace(sequence.SequenceId) ? sequence.SequenceId : sequence.name;
            if (stepIndex < 0 || sequence.Steps == null || stepIndex >= sequence.Steps.Count)
            {
                return $"{sequenceLabel} / Missing step #{stepIndex}";
            }

            var step = sequence.Steps[stepIndex];
            if (step == null)
            {
                return $"{sequenceLabel} / Null step #{stepIndex}";
            }

            string animationLabel = string.IsNullOrWhiteSpace(step.AnimationName) ? string.Empty : $" [{step.AnimationName}]";
            return $"{sequenceLabel} / #{stepIndex} {step.StepType}{animationLabel}";
        }

        public override string ToString()
        {
            return GetDisplayName();
        }
    }
}
