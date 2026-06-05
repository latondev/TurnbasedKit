using System.Collections;
using System.Collections.Generic;

namespace GameSystems.Battle
{
    public sealed class SkillViewMoveHandlerRegistry
    {
        private readonly Dictionary<SkillViewMoveMode, ISkillViewMoveHandler> handlers =
            new Dictionary<SkillViewMoveMode, ISkillViewMoveHandler>();

        public static readonly SkillViewMoveHandlerRegistry Default = CreateDefault();

        public static SkillViewMoveHandlerRegistry CreateDefault()
        {
            var registry = new SkillViewMoveHandlerRegistry();
            registry.Register(new LinearSkillViewMoveHandler(SkillViewMoveMode.Direct));
            registry.Register(new LinearSkillViewMoveHandler(SkillViewMoveMode.ThroughTarget));
            registry.Register(new LinearSkillViewMoveHandler(SkillViewMoveMode.OffsetFromTarget));
            return registry;
        }

        public void Register(ISkillViewMoveHandler handler)
        {
            if (handler == null)
            {
                return;
            }

            handlers[handler.MoveMode] = handler;
        }

        public IEnumerator Execute(ActionSequenceRunner runner, SkillViewStep step)
        {
            if (runner == null || step == null)
            {
                return null;
            }

            if (handlers.TryGetValue(step.MoveMode, out var handler))
            {
                return handler.Move(runner, step);
            }

            return handlers.TryGetValue(SkillViewMoveMode.Direct, out var fallback)
                ? fallback.Move(runner, step)
                : null;
        }
    }
}
