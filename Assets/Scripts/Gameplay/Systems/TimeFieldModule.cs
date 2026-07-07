using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Slow / Fast time fields (Content Bible §E #3-4). A character inside a field has its per-tick
    /// displacement scaled (0.5 = half speed, 2.0 = double). Implemented by snapshotting each body's
    /// start-of-tick position and, after it moves, pulling it part-way back toward that snapshot — so the
    /// effect needs no change to the motor or the module interface, and stays Fix64-deterministic.
    /// </summary>
    public sealed class TimeFieldModule : ILevelModule
    {
        private struct Field { public Fix64Vec2 Min, Max; public Fix64 Scale; }
        private readonly List<Field> _fields = new List<Field>();
        private readonly Dictionary<int, Fix64Vec2> _prev = new Dictionary<int, Fix64Vec2>();

        public void AddField(Fix64Vec2 min, Fix64Vec2 max, Fix64 scale)
            => _fields.Add(new Field { Min = min, Max = max, Scale = scale });

        public void Tick(IReadOnlyList<SimEntity> allBodies)
        {
            _prev.Clear();
            for (int i = 0; i < allBodies.Count; i++) _prev[allBodies[i].Id] = allBodies[i].Position; // start-of-tick
        }

        public void OnCharacterStep(SimEntity character, in InputCommand cmd)
        {
            if (!_prev.TryGetValue(character.Id, out Fix64Vec2 prev)) return;
            for (int i = 0; i < _fields.Count; i++)
            {
                Field f = _fields[i];
                if (character.MaxX > f.Min.X && character.MinX < f.Max.X && character.MaxY > f.Min.Y && character.MinY < f.Max.Y)
                {
                    // scaled displacement = prev + (curr - prev) * scale
                    character.Position = prev + (character.Position - prev) * f.Scale;
                    return;
                }
            }
        }

        public void ResetModule() => _prev.Clear();
        public void CollectDynamicBodies(List<SimEntity> into) { }
        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids) { }
        public void CollectSolids(List<SimEntity> into) { }
        public void ContributeHash(ref StateHash h) { /* reflected in body positions, which are hashed */ }
    }
}
