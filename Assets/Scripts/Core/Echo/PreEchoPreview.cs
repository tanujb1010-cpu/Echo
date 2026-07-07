using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;

namespace Echo.Core.Echo
{
    /// <summary>
    /// Pre-Echo (assist preview): a read-only, non-authoritative projection of where a banked run's Echo
    /// will go over its next N ticks. Meant to be rendered as a translucent "ghost of the immediate future"
    /// so the player can see what committing to a restart will produce before they commit — pure Layer B
    /// (docs/04 §1: expression only, can never influence the sim). Simulates a disposable body through the
    /// exact same deterministic CharacterMotor/KinematicSolver pipeline the real Echo will use, so the
    /// projection is faithful whenever nothing else would block that Echo (typically true for the newest
    /// banked run, since it always replays with the LOWEST id and so collides with nothing before it in the
    /// braid). It only tests against static world geometry — it does not know about other Echoes/hazards/
    /// crates, so once several Echoes already share the level the preview becomes an approximation, not a
    /// guarantee. Never touches pooled sim state, so it's safe to call from the UI layer at any time,
    /// including mid-run, without perturbing the live simulation.
    /// </summary>
    public static class PreEchoPreview
    {
        private static readonly List<SimEntity> EmptySolids = new List<SimEntity>(0);

        /// <summary>Project <paramref name="ticks"/> ticks of <paramref name="timeline"/>'s recorded motion,
        /// starting at <paramref name="fromTick"/>, against <paramref name="world"/> only. Deterministic and
        /// side-effect-free: allocates one throwaway body, touches nothing pooled or live.</summary>
        public static Fix64Vec2[] Project(Timeline timeline, ICollisionWorld world, MotorTuning tuning,
            Fix64Vec2 startPosition, int fromTick, int ticks)
        {
            var positions = new Fix64Vec2[ticks];
            var body = SimEntityFactory.CreateCharacterBody(id: -1, startPosition, solidToEntities: false);
            var loco = new LocomotionState();
            body.Add(loco);

            for (int i = 0; i < ticks; i++)
            {
                InputCommand input = timeline.InputAt(fromTick + i);
                CharacterMotor.Step(body, loco, input, tuning, world, EmptySolids);
                positions[i] = body.Position;
            }
            return positions;
        }
    }
}
