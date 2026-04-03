using System.Collections;

namespace GameSystems.Battle
{
    public sealed class MoveBackStepHandler : ISkillViewStepHandler
    {
        public SkillViewStepType StepType => SkillViewStepType.MoveBack;

        public IEnumerator Execute(ActionSequenceRunner runner, SkillViewStep step)
        {
            if (runner == null || step == null)
            {
                return null;
            }

            runner.PlaySequenceAnimation(step, 2);
            return runner.MoveBackStep(step.Duration);
        }
    }
}
