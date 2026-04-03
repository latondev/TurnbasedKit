using System.Collections;
using UnityEngine;

namespace GameSystems.Battle
{
    public sealed class PlayAnimationStepHandler : ISkillViewStepHandler
    {
        public SkillViewStepType StepType => SkillViewStepType.PlayAnimation;

        public IEnumerator Execute(ActionSequenceRunner runner, SkillViewStep step)
        {
            if (runner == null || step == null)
            {
                return null;
            }

            runner.PlaySequenceAnimation(step, 1);
            if (step.WaitForAnimationEnd && step.Duration > 0f)
            {
                float wait = runner.GetScaledDuration(step.Duration);
                if (wait > 0f)
                {
                    return WaitRoutine(wait);
                }
            }

            return null;
        }

        private static IEnumerator WaitRoutine(float duration)
        {
            yield return new WaitForSeconds(duration);
        }
    }
}
