using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;
using Echo.Gameplay.Components;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Cumulative Lever (#49): a lever that only fully throws after Echoes pre-loaded it over runs. Each
    /// lever carries two pieces of state that must NOT be confused: <c>Charge</c> is the permanent,
    /// cross-restart total — it is deliberately NEVER touched by <see cref="ResetModule"/>, because
    /// surviving <see cref="ResetModule"/> is the entire point of "cumulative... over runs." Bumping it
    /// happens exactly once per run, inside <see cref="ResetModule"/> itself, as that run's activation is
    /// banked on the way out. <c>ActivatedThisRun</c> is the opposite: ordinary per-run sensor state,
    /// latched the first time any body overlaps the zone during the run and cleared every restart. Charge
    /// accrues by a fixed amount per run the zone was EVER occupied — not by how long a body lingered in
    /// one run. Once <c>Charge</c> reaches <c>Threshold</c> the linked door opens and, since <c>Charge</c>
    /// only ever grows and is never reset, it stays thrown permanently across all further restarts.
    /// </summary>
    public sealed class CumulativeLeverModule : ILevelModule
    {
        private struct Lever { public int LinkId; public Fix64Vec2 ZoneMin, ZoneMax; public Fix64 ChargePerActivation; public Fix64 Threshold; public Fix64 Charge; public bool ActivatedThisRun; }

        private readonly List<Lever> _levers = new List<Lever>();
        private readonly List<Door> _doors = new List<Door>();
        private int _doorBodyId = 1_070_000;

        public IReadOnlyList<Door> Doors => _doors;

        public void AddLever(int linkId, Fix64Vec2 zoneMin, Fix64Vec2 zoneMax, Fix64 chargePerActivation, Fix64 threshold)
            => _levers.Add(new Lever { LinkId = linkId, ZoneMin = zoneMin, ZoneMax = zoneMax, ChargePerActivation = chargePerActivation, Threshold = threshold, Charge = Fix64.Zero, ActivatedThisRun = false });

        public Door AddDoor(int linkId, Fix64Vec2 center, Fix64Vec2 halfExtents)
        {
            var body = SimEntityFactory.CreateStaticBody(_doorBodyId++, center, halfExtents);
            var door = new Door(linkId, body);
            _doors.Add(door);
            return door;
        }

        public Fix64 Charge(int linkId)
        {
            for (int i = 0; i < _levers.Count; i++)
                if (_levers[i].LinkId == linkId)
                    return _levers[i].Charge;
            return Fix64.Zero;
        }

        public bool IsThrown(int linkId)
        {
            for (int i = 0; i < _levers.Count; i++)
                if (_levers[i].LinkId == linkId)
                    return _levers[i].Charge >= _levers[i].Threshold;
            return false;
        }

        public void Tick(IReadOnlyList<SimEntity> allBodies)
        {
            for (int i = 0; i < _levers.Count; i++)
            {
                Lever l = _levers[i];
                if (!l.ActivatedThisRun && AnyIn(allBodies, l.ZoneMin, l.ZoneMax))
                    l.ActivatedThisRun = true;
                _levers[i] = l;
            }
            for (int d = 0; d < _doors.Count; d++)
            {
                bool thrown = false;
                for (int i = 0; i < _levers.Count && !thrown; i++)
                    thrown = _levers[i].LinkId == _doors[d].LinkId && _levers[i].Charge >= _levers[i].Threshold;
                _doors[d].SetActivated(thrown);
            }
        }

        private static bool AnyIn(IReadOnlyList<SimEntity> bodies, Fix64Vec2 min, Fix64Vec2 max)
        {
            for (int i = 0; i < bodies.Count; i++)
            {
                SimEntity b = bodies[i];
                if (b.Active && b.MaxX > min.X && b.MinX < max.X && b.MaxY > min.Y && b.MinY < max.Y) return true;
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
            for (int i = 0; i < _levers.Count; i++) { h.Add(_levers[i].Charge); h.Add(_levers[i].ActivatedThisRun); }
            for (int i = 0; i < _doors.Count; i++) _doors[i].ContributeHash(ref h);
        }

        public void ResetModule()
        {
            for (int i = 0; i < _levers.Count; i++)
            {
                Lever l = _levers[i];
                if (l.ActivatedThisRun) l.Charge += l.ChargePerActivation;
                l.ActivatedThisRun = false;
                _levers[i] = l;
            }
            for (int i = 0; i < _doors.Count; i++) _doors[i].SetActivated(false);
        }

        public void CollectDynamicBodies(List<SimEntity> into) { }
        public void OnCharacterStep(SimEntity character, in InputCommand cmd) { }
        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids) { }
    }
}
