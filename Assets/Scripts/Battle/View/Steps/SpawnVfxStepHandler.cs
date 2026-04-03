using System.Collections;

namespace GameSystems.Battle
{
    public sealed class SpawnVfxStepHandler : ISkillViewStepHandler
    {
        public SkillViewStepType StepType => SkillViewStepType.SpawnVfx;

        public IEnumerator Execute(ActionSequenceRunner runner, SkillViewStep step)
        {
            if (runner == null || step == null)
            {
                return null;
            }

            runner.SpawnStepVfx(step);
            return null;
        }
    }
}
