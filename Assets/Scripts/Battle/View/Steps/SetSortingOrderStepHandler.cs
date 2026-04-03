using System.Collections;

namespace GameSystems.Battle
{
    public sealed class SetSortingOrderStepHandler : ISkillViewStepHandler
    {
        public SkillViewStepType StepType => SkillViewStepType.SetSortingOrder;

        public IEnumerator Execute(ActionSequenceRunner runner, SkillViewStep step)
        {
            if (runner?.AnimationHandle == null || step == null)
            {
                return null;
            }

            runner.AnimationHandle.SetSortingOrder(step.SortingOrder, "Unit");
            return null;
        }
    }
}
