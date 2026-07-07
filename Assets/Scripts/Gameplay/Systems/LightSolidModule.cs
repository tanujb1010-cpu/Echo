using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Light-Solid (#40): a platform that is only solid while some body — live self or Echo — is within
    /// its carried lantern's radius, actively holding Lantern. "Lit" takes effect with the standard
    /// one-tick actuation latency: module solids are snapshotted ONCE per tick, before any character
    /// moves (docs/05 §1) — so <see cref="CollectSolids"/> always reflects whoever lit a platform during
    /// the PREVIOUS tick's <see cref="OnCharacterStep"/> passes. Clearing "lit" therefore can't happen in
    /// <see cref="Tick"/> (that runs before CollectSolids and would erase last tick's result before it's
    /// ever read) — instead the first OnCharacterStep call of each tick clears every platform once, and
    /// every character's pass that tick then only adds light, never wipes a sibling's contribution.
    /// </summary>
    public sealed class LightSolidModule : ILevelModule
    {
        private readonly List<SimEntity> _platforms = new List<SimEntity>();
        private int _platformId = SimEntityFactory.IdRange.LightPlatforms;
        private static readonly Fix64 LightRadiusSqr = Fix64.FromInt(9); // radius 3 tiles
        private bool _clearedThisTick;

        public IReadOnlyList<SimEntity> Platforms => _platforms;

        public SimEntity AddPlatform(Fix64Vec2 center, Fix64Vec2 half)
        {
            var body = SimEntityFactory.CreateStaticBody(_platformId++, center, half, solidToEntities: false);
            _platforms.Add(body);
            return body;
        }

        public void Tick(IReadOnlyList<SimEntity> allBodies) => _clearedThisTick = false;

        public void OnCharacterStep(SimEntity character, in InputCommand cmd)
        {
            if (!_clearedThisTick)
            {
                for (int i = 0; i < _platforms.Count; i++) _platforms[i].SolidToEntities = false;
                _clearedThisTick = true;
            }
            if (!cmd.Has(InputButtons.Lantern)) return;
            for (int i = 0; i < _platforms.Count; i++)
            {
                SimEntity p = _platforms[i];
                Fix64 dx = character.Position.X - p.Position.X;
                Fix64 dy = character.Position.Y - p.Position.Y;
                if (dx * dx + dy * dy <= LightRadiusSqr) p.SolidToEntities = true;
            }
        }

        public void CollectSolids(List<SimEntity> into)
        {
            for (int i = 0; i < _platforms.Count; i++)
                if (_platforms[i].SolidToEntities) into.Add(_platforms[i]);
        }

        public void ContributeHash(ref StateHash h)
        {
            for (int i = 0; i < _platforms.Count; i++) h.Add(_platforms[i].SolidToEntities);
        }

        public void ResetModule()
        {
            _clearedThisTick = false;
            for (int i = 0; i < _platforms.Count; i++) _platforms[i].SolidToEntities = false;
        }

        public void CollectDynamicBodies(List<SimEntity> into) { }
        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids) { }
    }
}
