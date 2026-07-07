using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;
using Echo.Gameplay.Components;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Self-Negotiation (#50): a plate that only counts as held while the Echo standing on it has EARNED
    /// enough Trust — a defiant Echo won't cooperate (hold the door) until the player has done it a favor
    /// in an earlier run (raising that specific Echo's own Attachment drive via reliance, the same Trust
    /// system Trust/Reliance already wires into live gameplay). Reads each occupying body's trust via
    /// <see cref="LevelSimulation.TrustForBodyId"/> rather than owning any relationship state itself — this
    /// module only decides what counts as "held," it doesn't grant or track trust. Only Echoes are ever
    /// eligible (the live player standing on the plate itself doesn't "negotiate" — the relationship being
    /// tested is with a specific past self, using the id-range convention already established elsewhere:
    /// the live player's id starts at 100,000, every Echo's is its small runId+1).
    /// </summary>
    public sealed class SelfNegotiationModule : ILevelModule, ISimAware
    {
        private const int PlayerIdBase = 100000;

        private struct Plate { public int LinkId; public Fix64Vec2 Min, Max; public Fix64 TrustThreshold; public bool Pressed; }

        private LevelSimulation _sim;
        private readonly List<Plate> _plates = new List<Plate>();
        private readonly List<Door> _doors = new List<Door>();
        private int _doorBodyId = 1_100_000;

        /// <summary>Set once by the caller after both this module and the LevelSimulation exist (see <see cref="ISimAware"/>).</summary>
        public void SetSimulation(LevelSimulation sim) => _sim = sim;

        public IReadOnlyList<Door> Doors => _doors;

        public void AddPlate(int linkId, Fix64Vec2 min, Fix64Vec2 max, Fix64 trustThreshold)
            => _plates.Add(new Plate { LinkId = linkId, Min = min, Max = max, TrustThreshold = trustThreshold, Pressed = false });

        public Door AddDoor(int linkId, Fix64Vec2 center, Fix64Vec2 halfExtents)
        {
            var body = SimEntityFactory.CreateStaticBody(_doorBodyId++, center, halfExtents);
            var door = new Door(linkId, body);
            _doors.Add(door);
            return door;
        }

        public bool IsPressed(int linkId)
        {
            for (int i = 0; i < _plates.Count; i++)
                if (_plates[i].LinkId == linkId) return _plates[i].Pressed;
            return false;
        }

        public void Tick(IReadOnlyList<SimEntity> allBodies)
        {
            for (int i = 0; i < _plates.Count; i++)
            {
                Plate p = _plates[i];
                p.Pressed = AnyCooperatingEchoIn(allBodies, p.Min, p.Max, p.TrustThreshold);
                _plates[i] = p;
            }
            for (int d = 0; d < _doors.Count; d++)
            {
                bool held = false;
                for (int i = 0; i < _plates.Count && !held; i++)
                    held = _plates[i].LinkId == _doors[d].LinkId && _plates[i].Pressed;
                _doors[d].SetActivated(held);
            }
        }

        private bool AnyCooperatingEchoIn(IReadOnlyList<SimEntity> bodies, Fix64Vec2 min, Fix64Vec2 max, Fix64 trustThreshold)
        {
            for (int i = 0; i < bodies.Count; i++)
            {
                SimEntity b = bodies[i];
                if (!b.Active || b.Id >= PlayerIdBase) continue; // only Echoes can be "asked"; the live player doesn't negotiate
                if (b.MaxX > min.X && b.MinX < max.X && b.MaxY > min.Y && b.MinY < max.Y
                    && _sim.TrustForBodyId(b.Id) >= trustThreshold)
                    return true;
            }
            return false;
        }

        public void CollectSolids(List<SimEntity> into)
        {
            for (int i = 0; i < _doors.Count; i++)
                if (_doors[i].Body.SolidToEntities) into.Add(_doors[i].Body);
        }

        public void ContributeHash(ref StateHash h)
        {
            for (int i = 0; i < _plates.Count; i++) h.Add(_plates[i].Pressed);
            for (int i = 0; i < _doors.Count; i++) _doors[i].ContributeHash(ref h);
        }

        public void ResetModule()
        {
            for (int i = 0; i < _doors.Count; i++) _doors[i].SetActivated(false);
        }

        public void CollectDynamicBodies(List<SimEntity> into) { }
        public void OnCharacterStep(SimEntity character, in InputCommand cmd) { }
        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids) { }
    }
}
