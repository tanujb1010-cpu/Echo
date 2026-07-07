using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Polarity (#20): magnet emitters pull or push "metal" bodies within their radius. A positive
    /// strength attracts, negative repels — the same field type handles both flavors so a level can flip
    /// polarity by sign alone. Metal bodies fall under gravity like crates but also accumulate magnetic
    /// acceleration each tick before moving, so a body can be walked to a ledge by a well-placed magnet
    /// or slung across a gap. Fully Fix64-deterministic (uses <see cref="Fix64Vec2.Normalized"/>, itself
    /// built on the deterministic Newton-iteration <see cref="Fix64.Sqrt"/>).
    /// </summary>
    public sealed class MagnetModule : ILevelModule
    {
        private struct Magnet { public Fix64Vec2 Position; public Fix64 Radius; public Fix64 Strength; } // +attract, -repel

        private readonly List<Magnet> _magnets = new List<Magnet>();
        private readonly List<SimEntity> _metals = new List<SimEntity>();
        private readonly List<Fix64Vec2> _homes = new List<Fix64Vec2>();
        private readonly List<SimEntity> _metalSolids = new List<SimEntity>(8); // reused scratch
        private int _metalId = SimEntityFactory.IdRange.Metals;

        private static readonly Fix64 Gravity = Fix64.FromInt(-55);
        private static readonly Fix64 Terminal = Fix64.FromInt(-24);
        private static readonly Fix64 Damping = Fix64.FromFloat(0.90f);

        public IReadOnlyList<SimEntity> Metals => _metals;

        public void AddMagnet(Fix64Vec2 position, Fix64 radius, Fix64 strength)
            => _magnets.Add(new Magnet { Position = position, Radius = radius, Strength = strength });

        public SimEntity AddMetal(Fix64Vec2 pos, Fix64Vec2 half)
        {
            var body = SimEntityFactory.CreateStaticBody(_metalId++, pos, half);
            _metals.Add(body);
            _homes.Add(pos);
            return body;
        }

        public void CollectDynamicBodies(List<SimEntity> into)
        {
            for (int i = 0; i < _metals.Count; i++) if (_metals[i].Active) into.Add(_metals[i]);
        }

        public void Tick(IReadOnlyList<SimEntity> allBodies) { /* magnets act in StepDynamics, after characters move */ }
        public void OnCharacterStep(SimEntity character, in InputCommand cmd) { /* magnets don't affect characters directly */ }

        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids)
        {
            for (int i = 0; i < _metals.Count; i++)
            {
                SimEntity m = _metals[i];
                if (!m.Active) continue;

                Fix64Vec2 accel = Fix64Vec2.Zero;
                for (int f = 0; f < _magnets.Count; f++)
                {
                    Magnet mag = _magnets[f];
                    Fix64Vec2 toMagnet = mag.Position - m.Position;
                    Fix64 dist = toMagnet.Magnitude;
                    if (dist >= mag.Radius || dist == Fix64.Zero) continue;
                    // Linear falloff: full strength at the magnet's center, zero at the radius edge.
                    Fix64 falloff = Fix64.One - dist / mag.Radius;
                    accel += toMagnet.Normalized * (mag.Strength * falloff);
                }

                // Horizontal drag lets a pulled body settle near the magnet instead of oscillating forever
                // (an undamped inverse-linear attractor conserves energy and never comes to rest).
                Fix64 vx = (m.Velocity.X + accel.X * CharacterMotor.Dt) * Damping;
                Fix64 vy = m.Velocity.Y + (Gravity + accel.Y) * CharacterMotor.Dt;
                if (vy < Terminal) vy = Terminal;
                m.Velocity = new Fix64Vec2(vx, vy);

                _metalSolids.Clear();
                for (int s = 0; s < solids.Count; s++) _metalSolids.Add(solids[s]);
                for (int j = 0; j < _metals.Count; j++)
                    if (j != i && _metals[j].Active) _metalSolids.Add(_metals[j]);

                CollisionFlags flags = KinematicSolver.MoveAndCollide(m, m.Velocity * CharacterMotor.Dt, world, _metalSolids);
                vx = m.Velocity.X; vy = m.Velocity.Y;
                if (flags.Down || flags.Up) vy = Fix64.Zero;
                if (flags.Left || flags.Right) vx = Fix64.Zero;
                m.Velocity = new Fix64Vec2(vx, vy);
            }
        }

        public void CollectSolids(List<SimEntity> into)
        {
            for (int i = 0; i < _metals.Count; i++)
                if (_metals[i].Active && _metals[i].SolidToEntities) into.Add(_metals[i]);
        }

        public void ContributeHash(ref StateHash h)
        {
            for (int i = 0; i < _metals.Count; i++) _metals[i].ContributeHash(ref h);
        }

        public void ResetModule()
        {
            for (int i = 0; i < _metals.Count; i++)
            {
                _metals[i].Active = true;
                _metals[i].Position = _homes[i];
                _metals[i].Velocity = Fix64Vec2.Zero;
                _metals[i].SolidToEntities = true;
            }
        }
    }
}
