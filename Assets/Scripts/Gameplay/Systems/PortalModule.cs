using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Anchored Portals (#34): linked portal mouths. A body entering region A is teleported to point B and
    /// vice-versa, with a short per-body cooldown so it can't ping-pong. Deterministic (positions are
    /// hashed via the bodies themselves; teleports happen in the sensor phase before motion).
    /// An exit point may be a fixed location or bound to a body (an Echo) so the destination follows a
    /// past self — the "anchored to Echo positions" flavor.
    /// </summary>
    public sealed class PortalModule : ILevelModule
    {
        private struct Pair
        {
            public Fix64Vec2 AMin, AMax, APoint;
            public Fix64Vec2 BMin, BMax, BPoint;
            public int AAnchorBodyId, BAnchorBodyId; // -1 = use fixed point
        }

        private readonly List<Pair> _pairs = new List<Pair>();
        private readonly Dictionary<int, int> _cooldown = new Dictionary<int, int>();
        private const int CooldownTicks = 10;

        public void AddPair(Fix64Vec2 aMin, Fix64Vec2 aMax, Fix64Vec2 aPoint,
                            Fix64Vec2 bMin, Fix64Vec2 bMax, Fix64Vec2 bPoint,
                            int aAnchorBodyId = -1, int bAnchorBodyId = -1)
            => _pairs.Add(new Pair { AMin = aMin, AMax = aMax, APoint = aPoint, BMin = bMin, BMax = bMax, BPoint = bPoint, AAnchorBodyId = aAnchorBodyId, BAnchorBodyId = bAnchorBodyId });

        public void Tick(IReadOnlyList<SimEntity> allBodies)
        {
            // Decrement cooldowns deterministically (bodies are visited in their stable list order).
            for (int i = 0; i < allBodies.Count; i++)
            {
                int id = allBodies[i].Id;
                if (_cooldown.TryGetValue(id, out int cd) && cd > 0) _cooldown[id] = cd - 1;
            }

            for (int i = 0; i < allBodies.Count; i++)
            {
                SimEntity body = allBodies[i];
                if (!body.Active) continue;
                if (_cooldown.TryGetValue(body.Id, out int cd) && cd > 0) continue;

                for (int p = 0; p < _pairs.Count; p++)
                {
                    Pair pair = _pairs[p];
                    if (In(body, pair.AMin, pair.AMax))
                    {
                        body.Position = Resolve(pair.BPoint, pair.BAnchorBodyId, allBodies);
                        _cooldown[body.Id] = CooldownTicks; break;
                    }
                    if (In(body, pair.BMin, pair.BMax))
                    {
                        body.Position = Resolve(pair.APoint, pair.AAnchorBodyId, allBodies);
                        _cooldown[body.Id] = CooldownTicks; break;
                    }
                }
            }
        }

        private static Fix64Vec2 Resolve(Fix64Vec2 fixedPoint, int anchorId, IReadOnlyList<SimEntity> bodies)
        {
            if (anchorId < 0) return fixedPoint;
            for (int i = 0; i < bodies.Count; i++)
                if (bodies[i].Id == anchorId && bodies[i].Active) return bodies[i].Position;
            return fixedPoint; // anchor missing/dead → fall back
        }

        private static bool In(SimEntity b, Fix64Vec2 min, Fix64Vec2 max)
            => b.MaxX > min.X && b.MinX < max.X && b.MaxY > min.Y && b.MinY < max.Y;

        public void ResetModule() => _cooldown.Clear();
        public void CollectDynamicBodies(List<SimEntity> into) { }
        public void OnCharacterStep(SimEntity character, in InputCommand cmd) { }
        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids) { }
        public void CollectSolids(List<SimEntity> into) { }
        public void ContributeHash(ref StateHash h) { /* state is reflected in body positions, which are hashed */ }
    }
}
