using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;
using Echo.Gameplay.Components;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Same-Tick Switch (#5): a group of momentary switches whose linked door LATCHES open only when every
    /// switch is occupied on the SAME tick. One body can't hold two distant switches at once — so you must
    /// have a past self stand on one while you hit the other simultaneously. Deterministic occupancy test.
    /// </summary>
    public sealed class SameTickSwitchModule : ILevelModule
    {
        private struct Region { public Fix64Vec2 Min, Max; }
        private sealed class Group { public int LinkId; public readonly List<Region> Switches = new List<Region>(); public bool Latched; }

        private readonly List<Group> _groups = new List<Group>();
        private readonly List<Door> _doors = new List<Door>();
        private int _doorBodyId = SimEntityFactory.IdRange.SwitchDoors;

        public IReadOnlyList<Door> Doors => _doors;
        public bool IsLatched(int linkId) { var g = Find(linkId); return g != null && g.Latched; }

        public void AddSwitch(int linkId, Fix64Vec2 min, Fix64Vec2 max)
        {
            Group g = Find(linkId);
            if (g == null) { g = new Group { LinkId = linkId }; _groups.Add(g); }
            g.Switches.Add(new Region { Min = min, Max = max });
        }

        public Door AddDoor(int linkId, Fix64Vec2 center, Fix64Vec2 halfExtents)
        {
            var body = SimEntityFactory.CreateStaticBody(_doorBodyId++, center, halfExtents);
            var door = new Door(linkId, body);
            _doors.Add(door);
            return door;
        }

        public void Tick(IReadOnlyList<SimEntity> allBodies)
        {
            for (int gi = 0; gi < _groups.Count; gi++)
            {
                Group g = _groups[gi];
                if (g.Latched || g.Switches.Count == 0) continue;
                bool allOccupied = true;
                for (int si = 0; si < g.Switches.Count && allOccupied; si++)
                    allOccupied = AnyBodyIn(allBodies, g.Switches[si]);
                if (allOccupied) g.Latched = true; // fired simultaneously → latch open
            }
            for (int d = 0; d < _doors.Count; d++)
                _doors[d].SetActivated(IsLatched(_doors[d].LinkId));
        }

        private static bool AnyBodyIn(IReadOnlyList<SimEntity> bodies, Region r)
        {
            for (int i = 0; i < bodies.Count; i++)
            {
                SimEntity b = bodies[i];
                if (b.Active && b.MaxX > r.Min.X && b.MinX < r.Max.X && b.MaxY > r.Min.Y && b.MinY < r.Max.Y) return true;
            }
            return false;
        }

        public void CollectSolids(List<SimEntity> into)
        {
            for (int i = 0; i < _doors.Count; i++)
                if (_doors[i].Body.SolidToEntities) into.Add(_doors[i].Body);
        }

        public void ContributeHash(ref StateHash h)
        {
            for (int i = 0; i < _groups.Count; i++) h.Add(_groups[i].Latched);
            for (int i = 0; i < _doors.Count; i++) _doors[i].ContributeHash(ref h);
        }

        public void ResetModule()
        {
            for (int i = 0; i < _groups.Count; i++) _groups[i].Latched = false;
            for (int i = 0; i < _doors.Count; i++) _doors[i].SetActivated(false);
        }

        public void CollectDynamicBodies(List<SimEntity> into) { }
        public void OnCharacterStep(SimEntity character, in InputCommand cmd) { }
        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids) { }

        private Group Find(int linkId)
        {
            for (int i = 0; i < _groups.Count; i++) if (_groups[i].LinkId == linkId) return _groups[i];
            return null;
        }
    }
}
