using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;
using Echo.Gameplay.Components;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Memory-Lock Door (#43): "Opens only if an Echo re-walks the exact path that first opened it."
    /// Unlike Arrival Order (#24) — where any body may touch any checkpoint and a single shared index
    /// advances regardless of who triggers it — Memory-Lock tracks progress PER BODY (keyed by
    /// <see cref="SimEntity.Id"/>). Each body has its own independent cursor into the checkpoint
    /// sequence, and only a body that has already visited checkpoints 0..k-1 itself, in order, may
    /// advance to checkpoint k. The door opens once a single body has walked the entire sequence solo,
    /// start to finish; other bodies' partial progress never combines with it.
    /// Deterministic: checkpoints are scanned in stable index order against the stable body list order,
    /// and per-body progress is hashed in bodyId-sorted order since dictionary enumeration order is not
    /// guaranteed stable.
    /// </summary>
    public sealed class MemoryLockModule : ILevelModule
    {
        private struct Checkpoint { public Fix64Vec2 Min, Max; }

        private const int DoorBodyIdStart = 1_050_000;

        private readonly List<Checkpoint> _checkpoints = new List<Checkpoint>();
        private readonly List<Door> _doors = new List<Door>();
        private readonly Dictionary<int, int> _progress = new Dictionary<int, int>();
        private int _doorBodyId = DoorBodyIdStart;
        private bool _solved;

        public bool Solved => _solved;
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
            if (!_solved && _checkpoints.Count > 0)
            {
                for (int i = 0; i < allBodies.Count; i++)
                {
                    SimEntity b = allBodies[i];
                    if (!b.Active) continue;

                    _progress.TryGetValue(b.Id, out int index);
                    if (index >= _checkpoints.Count) continue;

                    Checkpoint cp = _checkpoints[index];
                    if (b.MaxX > cp.Min.X && b.MinX < cp.Max.X && b.MaxY > cp.Min.Y && b.MinY < cp.Max.Y)
                    {
                        index++;
                        _progress[b.Id] = index;
                        if (index >= _checkpoints.Count)
                        {
                            _solved = true;
                        }
                    }
                }
            }
            for (int d = 0; d < _doors.Count; d++) _doors[d].SetActivated(_solved);
        }

        public void CollectSolids(List<SimEntity> into)
        {
            for (int i = 0; i < _doors.Count; i++)
                if (_doors[i].Body.SolidToEntities) into.Add(_doors[i].Body);
        }

        public void ContributeHash(ref StateHash h)
        {
            h.Add(_solved);

            var ids = new List<int>(_progress.Keys);
            ids.Sort();
            for (int i = 0; i < ids.Count; i++)
            {
                int id = ids[i];
                h.Add(id);
                h.Add(_progress[id]);
            }

            for (int i = 0; i < _doors.Count; i++) _doors[i].ContributeHash(ref h);
        }

        public void ResetModule()
        {
            _progress.Clear();
            _solved = false;
            for (int i = 0; i < _doors.Count; i++) _doors[i].SetActivated(false);
        }

        public void CollectDynamicBodies(List<SimEntity> into) { }
        public void OnCharacterStep(SimEntity character, in InputCommand cmd) { }
        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids) { }
    }
}
