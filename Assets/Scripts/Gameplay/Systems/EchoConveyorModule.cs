using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Echo Conveyor (#14): a line of Echoes physically passes live-you along — any character (live self
    /// or another Echo) resting exactly on top of a moving Echo is carried horizontally by that Echo's own
    /// tick delta, the same "resting on a moving solid rides it" technique <see cref="CrateModule"/> already
    /// uses for Throw-and-Ride (#38), just applied to Echo bodies instead of crates. Echoes are stepped
    /// (and thus already moved) before <see cref="OnCharacterStep"/> runs for the tick, so this module
    /// snapshots every Echo's position at the START of <see cref="Tick"/> (before anyone moves) and diffs
    /// against each Echo's current (already-moved) position when a later character's OnCharacterStep call
    /// checks whether it was resting on that Echo's PRE-MOVE top surface.
    /// </summary>
    public sealed class EchoConveyorModule : ILevelModule
    {
        private const int PlayerIdBase = 100000; // mirrors LevelSimulation.SpawnPlayer's id scheme
        private static readonly Fix64 RideEps = Fix64.FromFloat(0.06f);

        private readonly Dictionary<int, Fix64Vec2> _prevPos = new Dictionary<int, Fix64Vec2>();
        private readonly List<SimEntity> _echoesThisTick = new List<SimEntity>();

        public void Tick(IReadOnlyList<SimEntity> allBodies)
        {
            _prevPos.Clear();
            _echoesThisTick.Clear();
            for (int i = 0; i < allBodies.Count; i++)
            {
                SimEntity b = allBodies[i];
                if (b.Id < PlayerIdBase && b.Active)
                {
                    _prevPos[b.Id] = b.Position;
                    _echoesThisTick.Add(b);
                }
            }
        }

        public void OnCharacterStep(SimEntity character, in InputCommand cmd)
        {
            for (int i = 0; i < _echoesThisTick.Count; i++)
            {
                SimEntity e = _echoesThisTick[i];
                if (e == character || !e.Active) continue;
                if (!_prevPos.TryGetValue(e.Id, out Fix64Vec2 prevP)) continue;

                Fix64 dx = e.Position.X - prevP.X;
                if (dx == Fix64.Zero) continue;

                Fix64 prevTopY = prevP.Y + e.HalfExtents.Y;
                Fix64 prevMinX = prevP.X - e.HalfExtents.X;
                Fix64 prevMaxX = prevP.X + e.HalfExtents.X;
                bool wasOnTop = Fix64.Abs(character.MinY - prevTopY) < RideEps
                                && character.MaxX > prevMinX && character.MinX < prevMaxX;
                if (wasOnTop)
                    character.Position = new Fix64Vec2(character.Position.X + dx, character.Position.Y);
            }
        }

        public void CollectDynamicBodies(List<SimEntity> into) { }
        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids) { }
        public void CollectSolids(List<SimEntity> into) { }
        public void ContributeHash(ref StateHash h) { /* purely reactive; no state beyond this tick's read-only snapshot */ }
        public void ResetModule() { _prevPos.Clear(); _echoesThisTick.Clear(); }
    }
}
