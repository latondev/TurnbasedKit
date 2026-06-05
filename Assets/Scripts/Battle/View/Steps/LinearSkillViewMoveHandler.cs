using System.Collections;

namespace GameSystems.Battle
{
    public sealed class LinearSkillViewMoveHandler : ISkillViewMoveHandler
    {
        public LinearSkillViewMoveHandler(SkillViewMoveMode moveMode)
        {
            MoveMode = moveMode;
        }

        public SkillViewMoveMode MoveMode { get; }

        public IEnumerator Move(ActionSequenceRunner runner, SkillViewStep step)
        {
            if (runner == null || step == null)
            {
                return null;
            }

            return runner.MoveToPosition(runner.ResolveDestination(step), step.Duration);
        }
    }
}
