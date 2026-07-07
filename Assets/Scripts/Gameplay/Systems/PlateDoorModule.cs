using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;
using Echo.Gameplay.Components;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Bundles the "Held Plate" (#1), "Quorum Door" (#8) and crate-on-plate mechanics: pressure plates and
    /// occupancy zones that open linked doors. A door opens if ANY linked plate is pressed OR any linked
    /// quorum zone meets its threshold. Lives in Gameplay (depends on Core, never the reverse).
    /// </summary>
    public sealed class PlateDoorModule : ILevelModule
    {
        private struct Quorum { public int LinkId; public Fix64Vec2 Min, Max; public int Threshold; public int Count; }

        private readonly List<PressurePlate> _plates = new List<PressurePlate>();
        private readonly List<Door> _doors = new List<Door>();
        private readonly List<Quorum> _quorums = new List<Quorum>();
        private int _doorBodyId = SimEntityFactory.IdRange.PlateDoors;

        public IReadOnlyList<PressurePlate> Plates => _plates;
        public IReadOnlyList<Door> Doors => _doors;

        public PressurePlate AddPlate(int linkId, Fix64Vec2 min, Fix64Vec2 max)
        {
            var plate = new PressurePlate(linkId, min, max);
            _plates.Add(plate);
            return plate;
        }

        /// <summary>Quorum Door (#8): the linked door opens while ≥ threshold bodies occupy the zone.</summary>
        public void AddQuorum(int linkId, Fix64Vec2 min, Fix64Vec2 max, int threshold)
            => _quorums.Add(new Quorum { LinkId = linkId, Min = min, Max = max, Threshold = threshold });

        public Door AddDoor(int linkId, Fix64Vec2 center, Fix64Vec2 halfExtents, bool invert = false)
        {
            var body = SimEntityFactory.CreateStaticBody(_doorBodyId++, center, halfExtents);
            var door = new Door(linkId, body, invert);
            _doors.Add(door);
            return door;
        }

        public void CollectDynamicBodies(List<SimEntity> into) { /* no dynamic bodies */ }
        public void OnCharacterStep(SimEntity character, in InputCommand cmd) { /* none */ }
        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids) { /* none */ }

        public void Tick(IReadOnlyList<SimEntity> allBodies)
        {
            for (int i = 0; i < _plates.Count; i++) _plates[i].Evaluate(allBodies);

            for (int q = 0; q < _quorums.Count; q++)
            {
                Quorum z = _quorums[q];
                int count = 0;
                for (int b = 0; b < allBodies.Count; b++)
                {
                    SimEntity body = allBodies[b];
                    if (body.Active && body.MaxX > z.Min.X && body.MinX < z.Max.X && body.MaxY > z.Min.Y && body.MinY < z.Max.Y)
                        count++;
                }
                z.Count = count;
                _quorums[q] = z;
            }

            for (int d = 0; d < _doors.Count; d++)
            {
                Door door = _doors[d];
                bool open = false;
                for (int p = 0; p < _plates.Count && !open; p++)
                    if (_plates[p].LinkId == door.LinkId && _plates[p].Pressed) open = true;
                for (int q = 0; q < _quorums.Count && !open; q++)
                    if (_quorums[q].LinkId == door.LinkId && _quorums[q].Count >= _quorums[q].Threshold) open = true;
                door.SetActivated(open);
            }
        }

        public void CollectSolids(List<SimEntity> into)
        {
            for (int i = 0; i < _doors.Count; i++)
                if (_doors[i].Body.SolidToEntities) into.Add(_doors[i].Body);
        }

        public void ContributeHash(ref StateHash h)
        {
            for (int i = 0; i < _plates.Count; i++) _plates[i].ContributeHash(ref h);
            for (int i = 0; i < _doors.Count; i++) _doors[i].ContributeHash(ref h);
            for (int i = 0; i < _quorums.Count; i++) h.Add(_quorums[i].Count);
        }

        public void ResetModule()
        {
            for (int i = 0; i < _doors.Count; i++) _doors[i].SetActivated(false);
        }
    }
}
