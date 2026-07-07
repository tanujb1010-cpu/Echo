using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Weighted Pendulum (#47): a platform swings back and forth between two X extremes on a fixed
    /// period, flinging anyone riding it across a chasm. This codebase's <see cref="Fix64"/> has no
    /// trig functions, so the swing is driven by a triangle wave (linear ping-pong between the two
    /// extremes) rather than a sine wave — it produces the same "swings back and forth, fastest in the
    /// middle" feel using only addition, subtraction, and comparisons, the same way
    /// <see cref="ElevatorModule"/> drives its analog motion without trig. Riding the bob uses the exact
    /// same pre-move-top-surface delta-carry technique as <see cref="EchoConveyorModule"/> — a
    /// character resting on the bob's top surface before it moves this tick is shifted by the bob's own
    /// horizontal tick delta — just for a single module-owned body instead of a dynamic set of Echoes,
    /// so only one previous position needs to be remembered per pendulum.
    /// </summary>
    public sealed class PendulumModule : ILevelModule
    {
        private sealed class Pendulum
        {
            public SimEntity Body;
            public Fix64 CenterX;
            public Fix64 CenterY;
            public Fix64 SwingHalfWidth;
            public int PeriodTicks;
            public int TickCounter;
            public Fix64Vec2 PrevPosition;
        }

        private static readonly Fix64 RideEps = Fix64.FromFloat(0.06f);

        private readonly List<Pendulum> _pendulums = new List<Pendulum>();
        private int _nextId = 1_120_000;

        public IReadOnlyList<SimEntity> Pendulums => GetBodies();

        public SimEntity AddPendulum(Fix64Vec2 centerPosition, Fix64Vec2 halfExtents, Fix64 swingHalfWidth, int periodTicks)
        {
            // The triangle-wave formula's phase=0 corresponds to the LEFT extreme (CenterX - SwingHalfWidth),
            // not the center — so the body must start there too, or the very first Tick() call snaps it
            // several tiles sideways to reconcile the mismatch (a real, jarring teleport bug, not a feature).
            Fix64Vec2 startPosition = new Fix64Vec2(centerPosition.X - swingHalfWidth, centerPosition.Y);
            var body = SimEntityFactory.CreateStaticBody(_nextId++, startPosition, halfExtents);
            _pendulums.Add(new Pendulum
            {
                Body = body,
                CenterX = centerPosition.X,
                CenterY = centerPosition.Y,
                SwingHalfWidth = swingHalfWidth,
                PeriodTicks = periodTicks,
                TickCounter = 0,
                PrevPosition = body.Position,
            });
            return body;
        }

        public void Tick(IReadOnlyList<SimEntity> allBodies)
        {
            for (int i = 0; i < _pendulums.Count; i++)
            {
                Pendulum p = _pendulums[i];
                p.PrevPosition = p.Body.Position;

                p.TickCounter++;
                int half = p.PeriodTicks / 2;
                int phase = p.TickCounter % p.PeriodTicks;

                Fix64 span = Fix64.FromInt(2) * p.SwingHalfWidth;
                Fix64 newX;
                if (phase < half)
                {
                    Fix64 frac = Fix64.FromInt(phase) / Fix64.FromInt(half);
                    newX = p.CenterX - p.SwingHalfWidth + span * frac;
                }
                else
                {
                    Fix64 frac = Fix64.FromInt(phase - half) / Fix64.FromInt(half);
                    newX = p.CenterX + p.SwingHalfWidth - span * frac;
                }

                p.Body.Position = new Fix64Vec2(newX, p.CenterY);
            }
        }

        public void OnCharacterStep(SimEntity character, in InputCommand cmd)
        {
            for (int i = 0; i < _pendulums.Count; i++)
            {
                Pendulum p = _pendulums[i];
                Fix64Vec2 prevP = p.PrevPosition;

                Fix64 dx = p.Body.Position.X - prevP.X;
                if (dx == Fix64.Zero) continue;

                Fix64 prevTopY = prevP.Y + p.Body.HalfExtents.Y;
                Fix64 prevMinX = prevP.X - p.Body.HalfExtents.X;
                Fix64 prevMaxX = prevP.X + p.Body.HalfExtents.X;
                bool wasOnTop = Fix64.Abs(character.MinY - prevTopY) < RideEps
                                && character.MaxX > prevMinX && character.MinX < prevMaxX;
                if (wasOnTop)
                    character.Position = new Fix64Vec2(character.Position.X + dx, character.Position.Y);
            }
        }

        public void CollectSolids(List<SimEntity> into)
        {
            for (int i = 0; i < _pendulums.Count; i++)
                if (_pendulums[i].Body.SolidToEntities) into.Add(_pendulums[i].Body);
        }

        public void ContributeHash(ref StateHash h)
        {
            for (int i = 0; i < _pendulums.Count; i++)
            {
                h.Add(_pendulums[i].Body.Position.X);
                h.Add(_pendulums[i].TickCounter);
            }
        }

        public void ResetModule()
        {
            for (int i = 0; i < _pendulums.Count; i++)
            {
                Pendulum p = _pendulums[i];
                p.TickCounter = 0;
                p.Body.Position = new Fix64Vec2(p.CenterX - p.SwingHalfWidth, p.CenterY); // matches phase=0
                p.PrevPosition = p.Body.Position;
            }
        }

        private List<SimEntity> GetBodies()
        {
            var list = new List<SimEntity>(_pendulums.Count);
            for (int i = 0; i < _pendulums.Count; i++) list.Add(_pendulums[i].Body);
            return list;
        }

        public void CollectDynamicBodies(List<SimEntity> into) { }
        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids) { }
    }
}
