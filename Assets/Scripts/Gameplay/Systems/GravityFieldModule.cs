using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Gravity Memory (#37): recorded gravity-flip zones replayed by Echoes. Any body standing inside a
    /// zone experiences reversed gravity — live player and Echoes alike, since both run through the same
    /// <see cref="CharacterMotor"/> code. The motor already integrates one tick of downward gravity into
    /// <see cref="SimEntity.Velocity"/>.Y before <see cref="OnCharacterStep"/> runs, so flipping is done by
    /// subtracting 2*Gravity*Dt (a positive amount, since Gravity is negative): this cancels the tick's
    /// downward pull and applies an equal upward one instead, purely in Fix64 so replay stays bit-identical.
    /// </summary>
    public sealed class GravityFieldModule : ILevelModule
    {
        private struct Zone { public Fix64Vec2 Min, Max; }

        private readonly List<Zone> _zones = new List<Zone>();

        private static readonly Fix64 Gravity = Fix64.FromInt(-55);
        private static readonly Fix64 TwoGravityDt = Fix64.FromInt(2) * Gravity * CharacterMotor.Dt;

        public void AddZone(Fix64Vec2 min, Fix64Vec2 max)
            => _zones.Add(new Zone { Min = min, Max = max });

        public void OnCharacterStep(SimEntity character, in InputCommand cmd)
        {
            for (int z = 0; z < _zones.Count; z++)
            {
                Zone zone = _zones[z];
                if (character.MaxX > zone.Min.X && character.MinX < zone.Max.X
                    && character.MaxY > zone.Min.Y && character.MinY < zone.Max.Y)
                {
                    character.Velocity = new Fix64Vec2(character.Velocity.X, character.Velocity.Y - TwoGravityDt);
                }
            }
        }

        public void ResetModule() { /* zones are fixed at level-build time */ }
        public void CollectDynamicBodies(List<SimEntity> into) { }
        public void Tick(IReadOnlyList<SimEntity> allBodies) { }
        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids) { }
        public void CollectSolids(List<SimEntity> into) { }
        public void ContributeHash(ref StateHash h) { /* zones are static geometry */ }
    }
}
