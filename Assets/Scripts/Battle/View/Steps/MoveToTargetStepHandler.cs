using System.Collections;

namespace GameSystems.Battle
{
    public sealed class MoveToTargetStepHandler : ISkillViewStepHandler
    {
        public SkillViewStepType StepType => SkillViewStepType.MoveToTarget;

        public IEnumerator Execute(ActionSequenceRunner runner, SkillViewStep step)
        {
            if (runner == null || step == null)
            {
                return null;
            }

            runner.PlaySequenceAnimation(step, 1);
            return runner.MoveToTargetStep(step);
        }
    }
}
