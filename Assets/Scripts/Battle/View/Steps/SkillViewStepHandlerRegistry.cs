using System.Collections.Generic;

namespace GameSystems.Battle
{
    public sealed class SkillViewStepHandlerRegistry
    {
        private readonly Dictionary<SkillViewStepType, ISkillViewStepHandler> handlers =
            new Dictionary<SkillViewStepType, ISkillViewStepHandler>();

        public static SkillViewStepHandlerRegistry CreateDefault()
        {
            var registry = new SkillViewStepHandlerRegistry();
            registry.Register(new MoveToTargetStepHandler());
            registry.Register(new MoveBackStepHandler());
            registry.Register(new PlayAnimationStepHandler());
            registry.Register(new WaitStepHandler());
            registry.Register(new ResetSortingOrderStepHandler());
            registry.Register(new SetSortingOrderStepHandler());
            registry.Register(new SetFlipXStepHandler());
            registry.Register(new SetIdleAnimationStepHandler());
            return registry;
        }

        public void Register(ISkillViewStepHandler handler)
        {
            if (handler == null)
            {
                return;
            }

            handlers[handler.StepType] = handler;
        }

        public bool TryGetValue(SkillViewStepType stepType, out ISkillViewStepHandler handler)
        {
            return handlers.TryGetValue(stepType, out handler);
        }
    }
}
