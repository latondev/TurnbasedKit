using System.Collections;

namespace GameSystems.Battle
{
    public interface ISkillViewStepHandler
    {
        SkillViewStepType StepType { get; }
        IEnumerator Execute(ActionSequenceRunner runner, SkillViewStep step);
    }
}
