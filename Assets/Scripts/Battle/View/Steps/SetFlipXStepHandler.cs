using System.Collections;

namespace GameSystems.Battle
{
    public sealed class SetFlipXStepHandler : ISkillViewStepHandler
    {
        public SkillViewStepType StepType => SkillViewStepType.SetFlipX;

        public IEnumerator Execute(ActionSequenceRunner runner, SkillViewStep step)
        {
            if (runner?.AnimationHandle == null || step == null)
            {
                return null;
            }

            runner.AnimationHandle.SetFlipX(step.FlipX);
            return null;
        }
    }
}
