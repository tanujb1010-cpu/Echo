using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;
using Echo.Gameplay.Components;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Pressure Balance (#29): a linked door opens only while two plates hold EQUAL, nonzero occupancy —
    /// not merely "both pressed." One body piling onto a single plate never satisfies it; the braid must
    /// split evenly across both (e.g. one self per plate). Deterministic occupancy counting, same style as
    /// Quorum Door (#8), but comparing two counts against each other instead of against a fixed threshold.
    /// </summary>
    public sealed class PressureBalanceModule : ILevelModule
    {
        private struct Pair { public int LinkId; public Fix64Vec2 AMin, AMax, BMin, BMax; public int CountA, CountB; }

        private readonly List<Pair> _pairs = new List<Pair>();
        private readonly List<Door> _doors = new List<Door>();
        private int _doorBodyId = SimEntityFactory.IdRange.PressureDoors;

        public IReadOnlyList<Door> Doors => _doors;

        public void AddPair(int linkId, Fix64Vec2 aMin, Fix64Vec2 aMax, Fix64Vec2 bMin, Fix64Vec2 bMax)
            => _pairs.Add(new Pair { LinkId = linkId, AMin = aMin, AMax = aMax, BMin = bMin, BMax = bMax });

        public Door AddDoor(int linkId, Fix64Vec2 center, Fix64Vec2 halfExtents)
        {
            var body = SimEntityFactory.CreateStaticBody(_doorBodyId++, center, halfExtents);
            var door = new Door(linkId, body);
            _doors.Add(door);
            return door;
        }

        public bool IsBalanced(int linkId)
        {
            for (int i = 0; i < _pairs.Count; i++)
                if (_pairs[i].LinkId == linkId)
                    return _pairs[i].CountA == _pairs[i].CountB && _pairs[i].CountA > 0;
            return false;
        }

        public void Tick(IReadOnlyList<SimEntity> allBodies)
        {
            for (int i = 0; i < _pairs.Count; i++)
            {
                Pair p = _pairs[i];
                p.CountA = CountIn(allBodies, p.AMin, p.AMax);
                p.CountB = CountIn(allBodies, p.BMin, p.BMax);
                _pairs[i] = p;
            }
            for (int d = 0; d < _doors.Count; d++)
            {
                bool balanced = false;
                for (int i = 0; i < _pairs.Count && !balanced; i++)
                    balanced = _pairs[i].LinkId == _doors[d].LinkId && _pairs[i].CountA == _pairs[i].CountB && _pairs[i].CountA > 0;
                _doors[d].SetActivated(balanced);
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
            for (int i = 0; i < _doors.Count; i++)
                if (_doors[i].Body.SolidToEntities) into.Add(_doors[i].Body);
        }

        public void ContributeHash(ref StateHash h)
        {
            for (int i = 0; i < _pairs.Count; i++) { h.Add(_pairs[i].CountA); h.Add(_pairs[i].CountB); }
            for (int i = 0; i < _doors.Count; i++) _doors[i].ContributeHash(ref h);
        }

        public void ResetModule()
        {
            for (int i = 0; i < _doors.Count; i++) _doors[i].SetActivated(false);
        }

        public void CollectDynamicBodies(List<SimEntity> into) { }
        public void OnCharacterStep(SimEntity character, in InputCommand cmd) { }
        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids) { }
    }
}
