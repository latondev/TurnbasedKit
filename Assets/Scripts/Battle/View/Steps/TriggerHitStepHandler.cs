using System.Collections;

namespace GameSystems.Battle
{
    public sealed class TriggerHitStepHandler : ISkillViewStepHandler
    {
        public SkillViewStepType StepType => SkillViewStepType.TriggerHit;

        public IEnumerator Execute(ActionSequenceRunner runner, SkillViewStep step)
        {
            if (runner == null || step == null)
            {
                return null;
            }

            runner.TryTriggerHitFromStep(step);
            return null;
        }
    }
}
