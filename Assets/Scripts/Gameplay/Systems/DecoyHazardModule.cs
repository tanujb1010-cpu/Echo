using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Mobile lure hazard (Content Bible "Decoy Self" #9). Each tick the hazard steps a fixed distance
    /// toward whichever active body (live player or Echo) is currently nearest to it, then kills anything
    /// its AABB overlaps afterward. Nearest is found by comparing squared distances only — Fix64 has no
    /// square root, and none is needed since only the relative ordering of candidates matters, never the
    /// actual distance value. Chasing "whichever active body is nearest" is the entire trick: no separate
    /// player-vs-Echo logic is required for the hazard to peel off after a closer Echo and lure it away
    /// from the live player's own path.
    /// </summary>
    public sealed class DecoyHazardModule : ILevelModule
    {
        private sealed class Decoy { public SimEntity Body; public Fix64Vec2 Start; public Fix64 ChaseSpeed; }

        private readonly List<Decoy> _decoys = new List<Decoy>();
        private readonly List<SimEntity> _bodies = new List<SimEntity>();
        private int _nextId = 1_130_000;

        /// <summary>Bodies killed this level by any decoy hazard. Cleared on reset.</summary>
        public int KillsThisRun { get; private set; }

        public IReadOnlyList<SimEntity> Hazards => _bodies;

        public SimEntity AddDecoyHazard(Fix64Vec2 startPosition, Fix64Vec2 halfExtents, Fix64 chaseSpeed)
        {
            var body = SimEntityFactory.CreateStaticBody(_nextId++, startPosition, halfExtents);
            _decoys.Add(new Decoy { Body = body, Start = startPosition, ChaseSpeed = chaseSpeed });
            _bodies.Add(body);
            return body;
        }

        public void Tick(IReadOnlyList<SimEntity> allBodies)
        {
            for (int i = 0; i < _decoys.Count; i++)
            {
                Decoy d = _decoys[i];
                SimEntity hz = d.Body;

                SimEntity target = null;
                Fix64 bestSq = Fix64.Zero;
                for (int b = 0; b < allBodies.Count; b++)
                {
                    SimEntity cand = allBodies[b];
                    if (!cand.Active) continue;
                    Fix64 dx = cand.Position.X - hz.Position.X;
                    Fix64 dy = cand.Position.Y - hz.Position.Y;
                    Fix64 sq = dx * dx + dy * dy;
                    if (target == null || sq < bestSq) { target = cand; bestSq = sq; }
                }

                if (target != null)
                {
                    Fix64 x = hz.Position.X;
                    Fix64 y = hz.Position.Y;

                    Fix64 dx = target.Position.X - x;
                    if (Fix64.Abs(dx) <= d.ChaseSpeed) x = target.Position.X;
                    else if (dx > Fix64.Zero) x = x + d.ChaseSpeed;
                    else x = x - d.ChaseSpeed;

                    Fix64 dy = target.Position.Y - y;
                    if (Fix64.Abs(dy) <= d.ChaseSpeed) y = target.Position.Y;
                    else if (dy > Fix64.Zero) y = y + d.ChaseSpeed;
                    else y = y - d.ChaseSpeed;

                    hz.Position = new Fix64Vec2(x, y);
                }

                for (int b = 0; b < allBodies.Count; b++)
                {
                    SimEntity body = allBodies[b];
                    if (!body.Active) continue;
                    if (body.MaxX > hz.MinX && body.MinX < hz.MaxX && body.MaxY > hz.MinY && body.MinY < hz.MaxY)
                    {
                        body.Active = false;
                        KillsThisRun++;
                    }
                }
            }
        }

        public void CollectDynamicBodies(List<SimEntity> into) { }
        public void OnCharacterStep(SimEntity character, in InputCommand cmd) { }
        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids) { }
        public void CollectSolids(List<SimEntity> into) { }

        public void ContributeHash(ref StateHash h)
        {
            for (int i = 0; i < _decoys.Count; i++) h.Add(_decoys[i].Body.Position);
            h.Add(KillsThisRun);
        }

        public void ResetModule()
        {
            KillsThisRun = 0;
            for (int i = 0; i < _decoys.Count; i++)
            {
                _decoys[i].Body.Position = _decoys[i].Start;
                _decoys[i].Body.Active = true;
            }
        }
    }
}
