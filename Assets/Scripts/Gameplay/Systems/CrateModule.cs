using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;
using Echo.Gameplay.Components;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Grab/carry crates (mechanic #3). A character holding Grab attaches the nearest free crate in front
    /// of it; the crate then follows until released, when it falls and re-solidifies. Free crates obey
    /// gravity and collide with tiles, characters and each other. Fully deterministic and input-driven, so
    /// an Echo carries a crate on replay exactly as the live run did.
    /// </summary>
    public sealed class CrateModule : ILevelModule
    {
        private readonly List<SimEntity> _crates = new List<SimEntity>();
        private readonly List<Carriable> _carry = new List<Carriable>();
        private readonly List<SimEntity> _crateSolids = new List<SimEntity>(8); // reused scratch
        private int _crateId = SimEntityFactory.IdRange.Crates;

        private static readonly Fix64 GrabReach = Fix64.FromFloat(1.1f);
        private static readonly Fix64 CarryOffsetX = Fix64.FromFloat(0.7f);
        private static readonly Fix64 CarryOffsetY = Fix64.FromFloat(0.2f);
        private static readonly Fix64 Gravity = Fix64.FromInt(-55);
        private static readonly Fix64 Terminal = Fix64.FromInt(-24);
        // Momentum Bank (#10): releasing while moving THROWS the crate; releasing at rest just drops it.
        private static readonly Fix64 ThrowSpeed = Fix64.FromInt(6);
        private static readonly Fix64 ThrowUp = Fix64.FromInt(8);
        private static readonly Fix64 MoveThreshold = Fix64.FromInt(2);
        private static readonly Fix64 GroundFriction = Fix64.FromFloat(0.97f); // landing skid decay (#38 Throw-and-Ride)
        private static readonly Fix64 RideEps = Fix64.FromFloat(0.06f);        // "resting exactly on top" tolerance

        public IReadOnlyList<SimEntity> Crates => _crates;

        public SimEntity AddCrate(Fix64Vec2 pos, Fix64Vec2 half)
        {
            var body = SimEntityFactory.CreateStaticBody(_crateId++, pos, half);
            var carry = new Carriable { Home = pos };
            body.Add(carry);
            _crates.Add(body);
            _carry.Add(carry);
            return body;
        }

        public void CollectDynamicBodies(List<SimEntity> into)
        {
            for (int i = 0; i < _crates.Count; i++) if (_crates[i].Active) into.Add(_crates[i]);
        }

        public void Tick(IReadOnlyList<SimEntity> allBodies) { /* crates are not sensors */ }

        public void OnCharacterStep(SimEntity character, in InputCommand cmd)
        {
            bool grab = cmd.Has(InputButtons.Grab);
            Carriable held = FindCarriedBy(character.Id);

            if (!grab)
            {
                if (held != null) Release(held, character);
            }
            else
            {
                if (held == null) held = TryGrabNearest(character);
                if (held != null) FollowCarrier(held, character);
            }
        }

        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids)
        {
            for (int i = 0; i < _crates.Count; i++)
            {
                SimEntity c = _crates[i];
                if (!c.Active || _carry[i].IsCarried) continue; // carried crates are positioned by their carrier

                // Build this crate's solid set = external solids + the OTHER free crates.
                _crateSolids.Clear();
                for (int s = 0; s < solids.Count; s++) _crateSolids.Add(solids[s]);
                for (int j = 0; j < _crates.Count; j++)
                    if (j != i && _crates[j].Active && !_carry[j].IsCarried) _crateSolids.Add(_crates[j]);

                Fix64 preX = c.Position.X; // for Throw-and-Ride (#38): the crate's own delta this tick
                Fix64 preMinX = c.MinX, preMaxX = c.MaxX, preMaxY = c.MaxY; // pre-move top, for rider detection

                Fix64 vx = c.Velocity.X; // preserve horizontal throw momentum during flight
                Fix64 vy = c.Velocity.Y + Gravity * CharacterMotor.Dt;
                if (vy < Terminal) vy = Terminal;
                c.Velocity = new Fix64Vec2(vx, vy);
                CollisionFlags f = KinematicSolver.MoveAndCollide(c, c.Velocity * CharacterMotor.Dt, world, _crateSolids);
                // Landing skids to a stop over a few tiles (Throw-and-Ride #38) rather than snapping to zero —
                // a "gently dropped" crate already has vx==0 on release, so that case is unaffected.
                if (f.Down) { vx = vx * GroundFriction; vy = Fix64.Zero; }
                if (f.Up) vy = Fix64.Zero;
                if (f.Left || f.Right) vx = Fix64.Zero;
                c.Velocity = new Fix64Vec2(vx, vy);

                // Throw-and-Ride (#38): a body that was resting exactly on this crate's pre-move top gets
                // carried along by the crate's own horizontal delta — the crate acts as a moving platform.
                // Vertical delta is deliberately NOT carried (avoids double-gravity fights with the rider's
                // own fall/land resolution); horizontal-only transport is exactly the "ride it across a gap" case.
                Fix64 dx = c.Position.X - preX;
                if (dx != Fix64.Zero)
                {
                    for (int r = 0; r < solids.Count; r++)
                    {
                        SimEntity rider = solids[r];
                        if (rider == c || !rider.Active || _crates.Contains(rider)) continue;
                        bool wasOnTop = Fix64.Abs(rider.MinY - preMaxY) < RideEps
                                        && rider.MaxX > preMinX && rider.MinX < preMaxX;
                        if (wasOnTop) rider.Position = new Fix64Vec2(rider.Position.X + dx, rider.Position.Y);
                    }
                }
            }
        }

        public void CollectSolids(List<SimEntity> into)
        {
            for (int i = 0; i < _crates.Count; i++)
                if (_crates[i].Active && _crates[i].SolidToEntities) into.Add(_crates[i]);
        }

        public void ContributeHash(ref StateHash h)
        {
            for (int i = 0; i < _carry.Count; i++) _carry[i].ContributeHash(ref h);
        }

        public void ResetModule()
        {
            for (int i = 0; i < _crates.Count; i++)
            {
                _crates[i].Active = true;
                _crates[i].Position = _carry[i].Home;
                _crates[i].Velocity = Fix64Vec2.Zero;
                _crates[i].SolidToEntities = true;
                _carry[i].CarrierId = -1;
            }
        }

        // ---- helpers ----
        private Carriable FindCarriedBy(int carrierId)
        {
            for (int i = 0; i < _carry.Count; i++) if (_carry[i].CarrierId == carrierId) return _carry[i];
            return null;
        }

        private Carriable TryGrabNearest(SimEntity character)
        {
            Carriable best = null; Fix64 bestDist = GrabReach;
            Fix64 frontX = character.Position.X + Fix64.FromInt(character.FacingSign) * CarryOffsetX;
            for (int i = 0; i < _crates.Count; i++)
            {
                if (!_crates[i].Active || _carry[i].IsCarried) continue;
                Fix64 dx = Fix64.Abs(_crates[i].Position.X - frontX);
                Fix64 dy = Fix64.Abs(_crates[i].Position.Y - character.Position.Y);
                if (dx <= bestDist && dy <= GrabReach) { best = _carry[i]; bestDist = dx; }
            }
            if (best != null)
            {
                best.CarrierId = character.Id;
                best.Owner.SolidToEntities = false; // non-solid while carried (no self-collision)
                best.Owner.Velocity = Fix64Vec2.Zero;
            }
            return best;
        }

        private void Release(Carriable c, SimEntity carrier)
        {
            c.CarrierId = -1;
            c.Owner.SolidToEntities = true;
            // Throw if the carrier is moving; otherwise a gentle drop. Deterministic (uses carrier state).
            if (Fix64.Abs(carrier.Velocity.X) > MoveThreshold)
                c.Owner.Velocity = new Fix64Vec2(
                    carrier.Velocity.X + Fix64.FromInt(carrier.FacingSign) * ThrowSpeed, ThrowUp);
            else
                c.Owner.Velocity = Fix64Vec2.Zero;
        }

        private void FollowCarrier(Carriable c, SimEntity carrier)
        {
            Fix64 x = carrier.Position.X + Fix64.FromInt(carrier.FacingSign) * CarryOffsetX;
            Fix64 y = carrier.Position.Y + CarryOffsetY;
            c.Owner.Position = new Fix64Vec2(x, y);
        }
    }
}
