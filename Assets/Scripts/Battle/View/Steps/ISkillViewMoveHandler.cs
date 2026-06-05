using System.Collections;

namespace GameSystems.Battle
{
    public interface ISkillViewMoveHandler
    {
        SkillViewMoveMode MoveMode { get; }
        IEnumerator Move(ActionSequenceRunner runner, SkillViewStep step);
    }
}
