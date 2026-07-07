using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;
using Echo.Gameplay.Components;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Mirror Relay (#6): an Echo holds a mirror, routing a beam through frozen optics. Since light travels
    /// instantly, the beam only completes its path while EVERY mirror point along the chain is
    /// simultaneously occupied by a body — a single Echo standing still only holds one mirror, so a
    /// multi-bend relay needs one frozen self per bend, all present at once. Deterministic occupancy
    /// checking, same style as Pressure Balance (#29), but requiring every zone in a list to be
    /// simultaneously satisfied rather than comparing two counts.
    /// </summary>
    public sealed class MirrorRelayModule : ILevelModule
    {
        private struct MirrorPoint { public Fix64Vec2 Min, Max; }

        private readonly List<MirrorPoint> _mirrors = new List<MirrorPoint>();
        private readonly List<Door> _doors = new List<Door>();
        private int _doorBodyId = 1_140_000;

        public bool BeamComplete { get; private set; }
        public IReadOnlyList<Door> Doors => _doors;

        public void AddMirrorPoint(Fix64Vec2 min, Fix64Vec2 max) => _mirrors.Add(new MirrorPoint { Min = min, Max = max });

        public Door AddDoor(int linkId, Fix64Vec2 center, Fix64Vec2 halfExtents)
        {
            var body = SimEntityFactory.CreateStaticBody(_doorBodyId++, center, halfExtents);
            var door = new Door(linkId, body);
            _doors.Add(door);
            return door;
        }

        public void Tick(IReadOnlyList<SimEntity> allBodies)
        {
            bool complete = _mirrors.Count > 0;
            for (int i = 0; i < _mirrors.Count && complete; i++)
                complete = AnyBodyIn(allBodies, _mirrors[i]);
            BeamComplete = complete;

            for (int d = 0; d < _doors.Count; d++) _doors[d].SetActivated(BeamComplete);
        }

        private static bool AnyBodyIn(IReadOnlyList<SimEntity> bodies, MirrorPoint m)
        {
            for (int i = 0; i < bodies.Count; i++)
            {
                SimEntity b = bodies[i];
                if (b.Active && b.MaxX > m.Min.X && b.MinX < m.Max.X && b.MaxY > m.Min.Y && b.MinY < m.Max.Y) return true;
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
            h.Add(BeamComplete);
            for (int i = 0; i < _doors.Count; i++) _doors[i].ContributeHash(ref h);
        }

        public void ResetModule()
        {
            BeamComplete = false;
            for (int i = 0; i < _doors.Count; i++) _doors[i].SetActivated(false);
        }

        public void CollectDynamicBodies(List<SimEntity> into) { }
        public void OnCharacterStep(SimEntity character, in InputCommand cmd) { }
        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids) { }
    }
}
