using System.Collections;
using UnityEngine;

namespace GameSystems.Battle
{
    public sealed class WaitStepHandler : ISkillViewStepHandler
    {
        public SkillViewStepType StepType => SkillViewStepType.Wait;

        public IEnumerator Execute(ActionSequenceRunner runner, SkillViewStep step)
        {
            if (runner == null || step == null || step.Duration <= 0f)
            {
                return null;
            }

            float wait = runner.GetScaledDuration(step.Duration);
            if (wait <= 0f)
            {
                return null;
            }

            return WaitRoutine(wait);
        }

        private static IEnumerator WaitRoutine(float duration)
        {
            yield return new WaitForSeconds(duration);
        }
    }
}
