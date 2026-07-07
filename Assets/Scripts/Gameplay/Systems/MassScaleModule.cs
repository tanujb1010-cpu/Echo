using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;
using Echo.Gameplay.Components;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Mass Scale (#18): a linked door opens only while a scale zone's occupant count hits an EXACT
    /// target — not "at least" (Quorum Door's threshold) and not overloaded past it either. One self
    /// too many closes it just like one self too few. Deterministic occupancy counting, same style as
    /// Quorum Door (#8) and Pressure Balance (#29), but comparing a single zone's count against a fixed
    /// target with strict equality instead of a `>=` threshold or a cross-zone comparison.
    /// </summary>
    public sealed class MassScaleModule : ILevelModule
    {
        private struct Scale { public int LinkId; public Fix64Vec2 Min, Max; public int Target; public int Count; }

        private readonly List<Scale> _scales = new List<Scale>();
        private readonly List<Door> _doors = new List<Door>();
        private int _doorBodyId = 1_010_000;

        public IReadOnlyList<Door> Doors => _doors;

        public void AddScale(int linkId, Fix64Vec2 min, Fix64Vec2 max, int target)
            => _scales.Add(new Scale { LinkId = linkId, Min = min, Max = max, Target = target });

        public Door AddDoor(int linkId, Fix64Vec2 center, Fix64Vec2 halfExtents)
        {
            var body = SimEntityFactory.CreateStaticBody(_doorBodyId++, center, halfExtents);
            var door = new Door(linkId, body);
            _doors.Add(door);
            return door;
        }

        public bool IsAtTarget(int linkId)
        {
            for (int i = 0; i < _scales.Count; i++)
                if (_scales[i].LinkId == linkId)
                    return _scales[i].Count == _scales[i].Target;
            return false;
        }

        public void Tick(IReadOnlyList<SimEntity> allBodies)
        {
            for (int i = 0; i < _scales.Count; i++)
            {
                Scale s = _scales[i];
                s.Count = CountIn(allBodies, s.Min, s.Max);
                _scales[i] = s;
            }
            for (int d = 0; d < _doors.Count; d++)
            {
                bool atTarget = false;
                for (int i = 0; i < _scales.Count && !atTarget; i++)
                    atTarget = _scales[i].LinkId == _doors[d].LinkId && _scales[i].Count == _scales[i].Target;
                _doors[d].SetActivated(atTarget);
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
            for (int i = 0; i < _scales.Count; i++) h.Add(_scales[i].Count);
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
