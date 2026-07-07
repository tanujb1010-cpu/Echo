using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Flood Control (#32): Echoes (or the live self) standing on ALL registered drain zones keep the
    /// water rising toward its max height; leaving any drain uncovered lets it recede. Any body whose feet
    /// are below the current water surface gets a steady buoyant push upward — the same post-motor
    /// velocity-correction technique <see cref="GravityFieldModule"/> uses, since <see cref="CharacterMotor"/>
    /// only integrates Y velocity across ticks (never X), so an external vertical force must be applied
    /// there rather than as a one-shot nudge.
    /// </summary>
    public sealed class FloodModule : ILevelModule
    {
        private struct Drain { public Fix64Vec2 Min, Max; }

        private readonly List<Drain> _drains = new List<Drain>();
        private Fix64 _startY, _maxY, _waterY;
        private static readonly Fix64 RiseSmoothing = Fix64.FromFloat(0.02f);
        // Must exceed CharacterMotor's Gravity magnitude (55) for a submerged body to actually rise rather
        // than merely fall slower — this is a net upward push, not a partial counter to gravity.
        private static readonly Fix64 BuoyancyForce = Fix64.FromInt(90);

        public Fix64 WaterLevel => _waterY;

        public void Configure(Fix64 startY, Fix64 maxY)
        {
            _startY = startY;
            _maxY = maxY;
            _waterY = startY;
        }

        public void AddDrain(Fix64Vec2 min, Fix64Vec2 max) => _drains.Add(new Drain { Min = min, Max = max });

        public void Tick(IReadOnlyList<SimEntity> allBodies)
        {
            bool allBlocked = _drains.Count > 0;
            for (int d = 0; d < _drains.Count && allBlocked; d++)
                allBlocked = AnyBodyIn(allBodies, _drains[d].Min, _drains[d].Max);

            Fix64 target = allBlocked ? _maxY : _startY;
            _waterY += (target - _waterY) * RiseSmoothing;
        }

        private static bool AnyBodyIn(IReadOnlyList<SimEntity> bodies, Fix64Vec2 min, Fix64Vec2 max)
        {
            for (int i = 0; i < bodies.Count; i++)
            {
                SimEntity b = bodies[i];
                if (b.Active && b.MaxX > min.X && b.MinX < max.X && b.MaxY > min.Y && b.MinY < max.Y) return true;
            }
            return false;
        }

        public void OnCharacterStep(SimEntity character, in InputCommand cmd)
        {
            if (character.Position.Y < _waterY)
                character.Velocity = new Fix64Vec2(character.Velocity.X, character.Velocity.Y + BuoyancyForce * CharacterMotor.Dt);
        }

        public void CollectDynamicBodies(List<SimEntity> into) { }
        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids) { }
        public void CollectSolids(List<SimEntity> into) { }

        public void ContributeHash(ref StateHash h) => h.Add(_waterY);

        public void ResetModule() => _waterY = _startY;
    }
}
