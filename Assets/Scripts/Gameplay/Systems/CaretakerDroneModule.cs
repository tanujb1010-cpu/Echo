using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Caretaker Drones (Content Bible D19): a roaming hazard that hunts whichever ACTIVE ECHO currently
    /// has the highest Salience — never the live player, who this drone can't even perceive — deleting
    /// (killing) it on contact. A high-salience Echo (one you've relied on, or that's diverged/asserted
    /// itself) draws the drone's attention away from lower-salience ones, an inverted-priority cousin of
    /// Decoy Self (#9), which chases whichever body is merely nearest. Reads each Echo's Salience via
    /// <see cref="LevelSimulation.SalienceForBodyId"/> (see <see cref="ISimAware"/>) rather than owning any
    /// coupling to <see cref="Echo.Core.Echo.EchoBrain"/> itself.
    /// </summary>
    public sealed class CaretakerDroneModule : ILevelModule, ISimAware
    {
        private const int PlayerIdBase = 100000;

        private sealed class Drone { public SimEntity Body; public Fix64Vec2 Start; public Fix64 ChaseSpeed; }

        private LevelSimulation _sim;
        private readonly List<Drone> _drones = new List<Drone>();
        private readonly List<SimEntity> _bodies = new List<SimEntity>();
        private int _nextId = 1_160_000;

        public int KillsThisRun { get; private set; }
        public IReadOnlyList<SimEntity> Drones => _bodies;

        public void SetSimulation(LevelSimulation sim) => _sim = sim;

        public SimEntity AddDrone(Fix64Vec2 startPosition, Fix64Vec2 halfExtents, Fix64 chaseSpeed)
        {
            var body = SimEntityFactory.CreateStaticBody(_nextId++, startPosition, halfExtents);
            _drones.Add(new Drone { Body = body, Start = startPosition, ChaseSpeed = chaseSpeed });
            _bodies.Add(body);
            return body;
        }

        public void Tick(IReadOnlyList<SimEntity> allBodies)
        {
            for (int i = 0; i < _drones.Count; i++)
            {
                Drone d = _drones[i];
                SimEntity dr = d.Body;

                SimEntity target = null;
                Fix64 bestSalience = Fix64.Zero;
                for (int b = 0; b < allBodies.Count; b++)
                {
                    SimEntity cand = allBodies[b];
                    if (!cand.Active || cand.Id >= PlayerIdBase) continue; // only Echoes are ever a target
                    Fix64 salience = _sim != null ? _sim.SalienceForBodyId(cand.Id) : Fix64.Zero;
                    if (target == null || salience > bestSalience) { target = cand; bestSalience = salience; }
                }

                if (target != null)
                {
                    Fix64 x = dr.Position.X;
                    Fix64 y = dr.Position.Y;

                    Fix64 dx = target.Position.X - x;
                    if (Fix64.Abs(dx) <= d.ChaseSpeed) x = target.Position.X;
                    else if (dx > Fix64.Zero) x = x + d.ChaseSpeed;
                    else x = x - d.ChaseSpeed;

                    Fix64 dy = target.Position.Y - y;
                    if (Fix64.Abs(dy) <= d.ChaseSpeed) y = target.Position.Y;
                    else if (dy > Fix64.Zero) y = y + d.ChaseSpeed;
                    else y = y - d.ChaseSpeed;

                    dr.Position = new Fix64Vec2(x, y);
                }

                for (int b = 0; b < allBodies.Count; b++)
                {
                    SimEntity body = allBodies[b];
                    if (!body.Active || body.Id >= PlayerIdBase) continue; // the drone can't perceive/catch the live player
                    if (body.MaxX > dr.MinX && body.MinX < dr.MaxX && body.MaxY > dr.MinY && body.MinY < dr.MaxY)
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
            for (int i = 0; i < _drones.Count; i++) h.Add(_drones[i].Body.Position);
            h.Add(KillsThisRun);
        }

        public void ResetModule()
        {
            KillsThisRun = 0;
            for (int i = 0; i < _drones.Count; i++)
            {
                _drones[i].Body.Position = _drones[i].Start;
                _drones[i].Body.Active = true;
            }
        }
    }
}
