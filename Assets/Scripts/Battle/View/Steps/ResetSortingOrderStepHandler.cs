using System.Collections;

namespace GameSystems.Battle
{
    public sealed class ResetSortingOrderStepHandler : ISkillViewStepHandler
    {
        public SkillViewStepType StepType => SkillViewStepType.ResetSortingOrder;

        public IEnumerator Execute(ActionSequenceRunner runner, SkillViewStep step)
        {
            if (runner == null)
            {
                return null;
            }

            runner.AnimationHandle?.ResetSortingOrder();
            return null;
        }
    }
}
