using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;
using Echo.Gameplay.Components;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Torch Sequence (#28): a row of beacons that must be lit, one at a time, in a fixed order, each by an
    /// explicit Interact — not just by walking past. Unlike Arrival Order (#24), which is forgiving of
    /// premature zone crossings, lighting the WRONG (not-yet-due) beacon snuffs every beacon lit so far,
    /// so the whole braid must be choreographed to reach each beacon and interact in the right sequence
    /// with no wasted or premature presses. Already-lit beacons are idempotent (re-interacting does nothing),
    /// so holding Interact near a torch you just lit is safe. Deterministic: interactions are resolved in the
    /// braid's stable per-tick order (Echoes 0..k-1, then the live player).
    /// </summary>
    public sealed class TorchSequenceModule : ILevelModule
    {
        private struct Torch { public Fix64Vec2 Position; public Fix64 RadiusSqr; }

        private readonly List<Torch> _torches = new List<Torch>();
        private readonly List<bool> _lit = new List<bool>();
        private readonly List<Door> _doors = new List<Door>();
        private int _doorBodyId = SimEntityFactory.IdRange.TorchDoors;
        private int _nextIndex;

        public int NextIndex => _nextIndex;
        public bool Solved => _torches.Count > 0 && _nextIndex >= _torches.Count;
        public IReadOnlyList<Door> Doors => _doors;
        public bool IsLit(int index) => _lit[index];

        public void AddTorch(Fix64Vec2 position, Fix64 radius)
        {
            _torches.Add(new Torch { Position = position, RadiusSqr = radius * radius });
            _lit.Add(false);
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
            for (int d = 0; d < _doors.Count; d++) _doors[d].SetActivated(Solved);
        }

        public void OnCharacterStep(SimEntity character, in InputCommand cmd)
        {
            if (Solved || !cmd.Has(InputButtons.Interact)) return;
            for (int i = 0; i < _torches.Count; i++)
            {
                if (_lit[i]) continue; // already lit → idempotent, no penalty for lingering presses
                Torch t = _torches[i];
                Fix64 dx = character.Position.X - t.Position.X;
                Fix64 dy = character.Position.Y - t.Position.Y;
                if (dx * dx + dy * dy <= t.RadiusSqr)
                {
                    if (i == _nextIndex) { _lit[i] = true; _nextIndex++; }
                    else SnuffAll(); // lit the wrong (not-yet-due) beacon → the whole sequence is undone
                    return; // one beacon interaction per character per tick
                }
            }
        }

        private void SnuffAll()
        {
            _nextIndex = 0;
            for (int i = 0; i < _lit.Count; i++) _lit[i] = false;
        }

        public void CollectSolids(List<SimEntity> into)
        {
            for (int i = 0; i < _doors.Count; i++)
                if (_doors[i].Body.SolidToEntities) into.Add(_doors[i].Body);
        }

        public void ContributeHash(ref StateHash h)
        {
            h.Add(_nextIndex);
            for (int i = 0; i < _lit.Count; i++) h.Add(_lit[i]);
            for (int i = 0; i < _doors.Count; i++) _doors[i].ContributeHash(ref h);
        }

        public void ResetModule()
        {
            SnuffAll();
            for (int i = 0; i < _doors.Count; i++) _doors[i].SetActivated(false);
        }

        public void CollectDynamicBodies(List<SimEntity> into) { }
        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids) { }
    }
}
