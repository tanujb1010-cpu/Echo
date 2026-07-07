using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;
using Echo.Gameplay.Components;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Mirrored Path (#17): a room that demands an Echo mirror your exact inverse route. Two independent
    /// ordered checkpoint sequences run side by side: the live player must complete ITS OWN sequence, while
    /// some Echo (any Echo — shared progress, same convention as Arrival Order #24) must independently
    /// complete the MIRROR sequence, authored as the spatial inverse of the player's own path. Both tracks
    /// must finish (in either order, not necessarily in lockstep) before the linked door opens. Distinguishes
    /// "the live player" from "an Echo" using the id-range convention already established elsewhere (the
    /// live player's id starts at 100,000; every Echo's is its small runId+1).
    /// </summary>
    public sealed class MirroredPathModule : ILevelModule
    {
        private const int PlayerIdBase = 100000;

        private struct Checkpoint { public Fix64Vec2 Min, Max; }

        private readonly List<Checkpoint> _playerPath = new List<Checkpoint>();
        private readonly List<Checkpoint> _mirrorPath = new List<Checkpoint>();
        private readonly List<Door> _doors = new List<Door>();
        private int _doorBodyId = 1_150_000;
        private int _playerIndex;
        private int _mirrorIndex;

        public bool Solved => _playerIndex >= _playerPath.Count && _mirrorIndex >= _mirrorPath.Count && _playerPath.Count > 0 && _mirrorPath.Count > 0;
        public int PlayerIndex => _playerIndex;
        public int MirrorIndex => _mirrorIndex;
        public IReadOnlyList<Door> Doors => _doors;

        public void AddPlayerCheckpoint(Fix64Vec2 min, Fix64Vec2 max) => _playerPath.Add(new Checkpoint { Min = min, Max = max });
        public void AddMirrorCheckpoint(Fix64Vec2 min, Fix64Vec2 max) => _mirrorPath.Add(new Checkpoint { Min = min, Max = max });

        public Door AddDoor(int linkId, Fix64Vec2 center, Fix64Vec2 halfExtents)
        {
            var body = SimEntityFactory.CreateStaticBody(_doorBodyId++, center, halfExtents);
            var door = new Door(linkId, body);
            _doors.Add(door);
            return door;
        }

        public void Tick(IReadOnlyList<SimEntity> allBodies)
        {
            if (_playerIndex < _playerPath.Count)
            {
                Checkpoint cp = _playerPath[_playerIndex];
                for (int i = 0; i < allBodies.Count; i++)
                {
                    SimEntity b = allBodies[i];
                    if (b.Active && b.Id >= PlayerIdBase && Overlaps(b, cp)) { _playerIndex++; break; }
                }
            }

            if (_mirrorIndex < _mirrorPath.Count)
            {
                Checkpoint cp = _mirrorPath[_mirrorIndex];
                for (int i = 0; i < allBodies.Count; i++)
                {
                    SimEntity b = allBodies[i];
                    if (b.Active && b.Id < PlayerIdBase && Overlaps(b, cp)) { _mirrorIndex++; break; }
                }
            }

            for (int d = 0; d < _doors.Count; d++) _doors[d].SetActivated(Solved);
        }

        private static bool Overlaps(SimEntity b, Checkpoint cp)
            => b.MaxX > cp.Min.X && b.MinX < cp.Max.X && b.MaxY > cp.Min.Y && b.MinY < cp.Max.Y;

        public void CollectSolids(List<SimEntity> into)
        {
            for (int i = 0; i < _doors.Count; i++)
                if (_doors[i].Body.SolidToEntities) into.Add(_doors[i].Body);
        }

        public void ContributeHash(ref StateHash h)
        {
            h.Add(_playerIndex);
            h.Add(_mirrorIndex);
            for (int i = 0; i < _doors.Count; i++) _doors[i].ContributeHash(ref h);
        }

        public void ResetModule()
        {
            _playerIndex = 0;
            _mirrorIndex = 0;
            for (int i = 0; i < _doors.Count; i++) _doors[i].SetActivated(false);
        }

        public void CollectDynamicBodies(List<SimEntity> into) { }
        public void OnCharacterStep(SimEntity character, in InputCommand cmd) { }
        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids) { }
    }
}
