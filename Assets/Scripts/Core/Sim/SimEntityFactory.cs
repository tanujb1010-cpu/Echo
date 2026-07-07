using Echo.Core.Determinism;

namespace Echo.Core.Sim
{
    /// <summary>
    /// Centralizes SimEntity construction so body shape and entity-id-range constants live in one place
    /// instead of being hand-copied (and risking a silent collision) across every module. Pure
    /// assembly — no behavior beyond what call sites already did by hand.
    /// </summary>
    public static class SimEntityFactory
    {
        /// <summary>Standard character AABB half-size — the player and every Echo share one body shape.</summary>
        public static readonly Fix64Vec2 CharacterHalfExtents =
            new Fix64Vec2(Fix64.FromFloat(0.4f), Fix64.FromFloat(0.45f));

        /// <summary>
        /// Non-overlapping entity-id ranges, one 10,000-wide block per module, so two modules can never
        /// accidentally collide on Id. Add a new named constant (and bump <see cref="NextFree"/>) rather
        /// than picking an arbitrary number when a new module needs one.
        /// </summary>
        public static class IdRange
        {
            public const int Crates = 800000;
            public const int PlateDoors = 900000;
            public const int SwitchDoors = 910000;
            public const int BouncePads = 920000;
            public const int Metals = 930000;
            public const int ArrivalDoors = 940000;
            public const int TorchDoors = 950000;
            public const int LightPlatforms = 960000;
            public const int PressureDoors = 970000;
            public const int NextFree = 980000;
        }

        /// <summary>A moving character body (player or Echo). Caller attaches its own LocomotionState.</summary>
        public static SimEntity CreateCharacterBody(int id, Fix64Vec2 position, bool solidToEntities)
            => new SimEntity
            {
                Id = id,
                Active = true,
                Position = position,
                HalfExtents = CharacterHalfExtents,
                SolidToEntities = solidToEntities,
            };

        /// <summary>A static/gated body (door, pad, plate anchor, metal crate) with an explicit shape.</summary>
        public static SimEntity CreateStaticBody(int id, Fix64Vec2 center, Fix64Vec2 halfExtents, bool solidToEntities = true)
            => new SimEntity
            {
                Id = id,
                Active = true,
                Position = center,
                HalfExtents = halfExtents,
                SolidToEntities = solidToEntities,
            };
    }
}
