using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Counterweight Self (#4): a platform's height eases toward a target proportional to how many bodies
    /// (live self + Echoes) are standing in its counterweight zone — more occupants weigh it down further
    /// toward full rise, and it sinks back as they leave. Occupancy counting mirrors
    /// <see cref="PressureBalanceModule"/>'s zone AABB test; the rise itself is a deterministic exponential
    /// smoothing of the platform's Y toward the target, so an Echo standing on the zone reproduces the
    /// exact same analog motion on replay as it did live.
    /// </summary>
    public sealed class ElevatorModule : ILevelModule
    {
        private struct Platform
        {
            public SimEntity Body;
            public Fix64 StartY;
            public Fix64Vec2 ZoneMin, ZoneMax;
            public int MaxWeightForFullRise;
            public Fix64 MaxRise;
        }

        private static readonly Fix64 SmoothingFactor = Fix64.FromFloat(0.08f);

        private readonly List<Platform> _platforms = new List<Platform>();
        private int _platformId = 1_000_000;

        public IReadOnlyList<SimEntity> Platforms => GetBodies();

        public SimEntity AddPlatform(Fix64Vec2 startCenter, Fix64Vec2 halfExtents, Fix64Vec2 zoneMin, Fix64Vec2 zoneMax, int maxWeightForFullRise, Fix64 maxRise)
        {
            var body = SimEntityFactory.CreateStaticBody(_platformId++, startCenter, halfExtents);
            _platforms.Add(new Platform
            {
                Body = body,
                StartY = startCenter.Y,
                ZoneMin = zoneMin,
                ZoneMax = zoneMax,
                MaxWeightForFullRise = maxWeightForFullRise,
                MaxRise = maxRise,
            });
            return body;
        }

        public void Tick(IReadOnlyList<SimEntity> allBodies)
        {
            for (int i = 0; i < _platforms.Count; i++)
            {
                Platform p = _platforms[i];
                int count = CountIn(allBodies, p.ZoneMin, p.ZoneMax);
                int clamped = count < p.MaxWeightForFullRise ? count : p.MaxWeightForFullRise;
                Fix64 weightRatio = p.MaxWeightForFullRise > 0
                    ? Fix64.FromInt(clamped) / Fix64.FromInt(p.MaxWeightForFullRise)
                    : Fix64.Zero;
                Fix64 targetY = p.StartY + p.MaxRise * weightRatio;
                Fix64 currentY = p.Body.Position.Y;
                Fix64 newY = currentY + (targetY - currentY) * SmoothingFactor;
                p.Body.Position = new Fix64Vec2(p.Body.Position.X, newY);
            }
        }

        private static int CountIn(IReadOnlyList<SimEntity> bodies, Fix64Vec2 min, Fix64Vec2 max)
        {
            int count = 0;
            for (int i = 0; i < bodies.Count; i++)
            {
                SimEntity b = bodies[i];
                if (b.Active && b.MaxX > min.X && b.MinX < max.X && b.MaxY > min.Y && b.MinY < max.Y) count++;
            }
            return count;
        }

        public void CollectSolids(List<SimEntity> into)
        {
            for (int i = 0; i < _platforms.Count; i++)
                if (_platforms[i].Body.SolidToEntities) into.Add(_platforms[i].Body);
        }

        public void ContributeHash(ref StateHash h)
        {
            for (int i = 0; i < _platforms.Count; i++) h.Add(_platforms[i].Body.Position.Y);
        }

        public void ResetModule()
        {
            for (int i = 0; i < _platforms.Count; i++)
            {
                Platform p = _platforms[i];
                p.Body.Position = new Fix64Vec2(p.Body.Position.X, p.StartY);
            }
        }

        private List<SimEntity> GetBodies()
        {
            var list = new List<SimEntity>(_platforms.Count);
            for (int i = 0; i < _platforms.Count; i++) list.Add(_platforms[i].Body);
            return list;
        }

        public void CollectDynamicBodies(List<SimEntity> into) { }
        public void OnCharacterStep(SimEntity character, in InputCommand cmd) { }
        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids) { }
    }
}
