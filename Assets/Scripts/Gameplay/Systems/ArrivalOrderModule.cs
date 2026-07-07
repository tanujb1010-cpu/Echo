using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;
using Echo.Gameplay.Components;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Arrival Order (#24): a row of checkpoints that must be touched in a fixed sequence — by anyone,
    /// live player or Echo — before the linked door opens. Touching a later checkpoint before its
    /// predecessor has no effect (it neither helps nor breaks the sequence), so the puzzle is purely
    /// "get the braid to visit these marks in order," which usually means choreographing an earlier
    /// Echo to reach checkpoint 1 while a later self is still free to reach checkpoint 2, etc.
    /// Deterministic: checkpoints are scanned in stable index order against the stable body list order.
    /// </summary>
    public sealed class ArrivalOrderModule : ILevelModule
    {
        private struct Checkpoint { public Fix64Vec2 Min, Max; }

        private readonly List<Checkpoint> _checkpoints = new List<Checkpoint>();
        private readonly List<Door> _doors = new List<Door>();
        private int _doorBodyId = SimEntityFactory.IdRange.ArrivalDoors;
        private int _nextIndex;

        public int NextIndex => _nextIndex;
        public bool Solved => _checkpoints.Count > 0 && _nextIndex >= _checkpoints.Count;
        public IReadOnlyList<Door> Doors => _doors;

        public void AddCheckpoint(Fix64Vec2 min, Fix64Vec2 max) => _checkpoints.Add(new Checkpoint { Min = min, Max = max });

        public Door AddDoor(int linkId, Fix64Vec2 center, Fix64Vec2 halfExtents)
        {
            var body = SimEntityFactory.CreateStaticBody(_doorBodyId++, center, halfExtents);
            var door = new Door(linkId, body);
            _doors.Add(door);
            return door;
        }

        public void Tick(IReadOnlyList<SimEntity> allBodies)
        {
            if (_nextIndex < _checkpoints.Count)
            {
                Checkpoint cp = _checkpoints[_nextIndex];
                for (int i = 0; i < allBodies.Count; i++)
                {
                    SimEntity b = allBodies[i];
                    if (b.Active && b.MaxX > cp.Min.X && b.MinX < cp.Max.X && b.MaxY > cp.Min.Y && b.MinY < cp.Max.Y)
                    {
                        _nextIndex++;
                        break;
                    }
                }
            }
            for (int d = 0; d < _doors.Count; d++) _doors[d].SetActivated(Solved);
        }

        public void CollectSolids(List<SimEntity> into)
        {
            for (int i = 0; i < _doors.Count; i++)
                if (_doors[i].Body.SolidToEntities) into.Add(_doors[i].Body);
        }

        public void ContributeHash(ref StateHash h)
        {
            h.Add(_nextIndex);
            for (int i = 0; i < _doors.Count; i++) _doors[i].ContributeHash(ref h);
        }

        public void ResetModule()
        {
            _nextIndex = 0;
            for (int i = 0; i < _doors.Count; i++) _doors[i].SetActivated(false);
        }

        public void CollectDynamicBodies(List<SimEntity> into) { }
        public void OnCharacterStep(SimEntity character, in InputCommand cmd) { }
        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids) { }
    }
}
