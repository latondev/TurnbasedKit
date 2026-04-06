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
                yield break;
            }

            runner.BeginPlayAnimationStep(step);
            runner.PlaySequenceAnimation(step, 1);
            if (step.WaitForAnimationEnd && step.Duration > 0f)
            {
                float wait = runner.GetScaledDuration(step.Duration);
                if (wait > 0f)
                {
                    yield return WaitRoutine(wait);
                }
            }

            runner.FinishPlayAnimationStep(step);
            yield break;
        }

        private static IEnumerator WaitRoutine(float duration)
        {
            yield return new WaitForSeconds(duration);
        }
    }
}
