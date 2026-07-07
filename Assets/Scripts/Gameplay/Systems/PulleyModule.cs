using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;
using Echo.Gameplay.Components;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Pulley Crew (#21): a linked door is wound open gradually — its analog <c>raisedAmount</c> (0..1)
    /// climbs while at least <c>threshold</c> bodies occupy the pull zone, and slips back down (at its own,
    /// typically slower, rate) whenever fewer are pulling. Unlike Quorum Door's instant on/off, letting go
    /// costs progress, so the crew must sustain effort. The one non-obvious rule: once a pulley reaches
    /// <see cref="Fix64.One"/> it is fully wound and STAYS that way — a maxed-out pulley no longer decays
    /// even if everyone leaves the zone, so the linked door stays open for good.
    /// </summary>
    public sealed class PulleyModule : ILevelModule
    {
        private struct Pulley { public int LinkId; public Fix64Vec2 ZoneMin, ZoneMax; public int Threshold; public Fix64 RiseRate, SlipRate; public Fix64 RaisedAmount; }

        private readonly List<Pulley> _pulleys = new List<Pulley>();
        private readonly List<Door> _doors = new List<Door>();
        private int _doorBodyId = 1_020_000;

        public IReadOnlyList<Door> Doors => _doors;

        public void AddPulley(int linkId, Fix64Vec2 zoneMin, Fix64Vec2 zoneMax, int threshold, Fix64 riseRate, Fix64 slipRate)
            => _pulleys.Add(new Pulley { LinkId = linkId, ZoneMin = zoneMin, ZoneMax = zoneMax, Threshold = threshold, RiseRate = riseRate, SlipRate = slipRate, RaisedAmount = Fix64.Zero });

        public Door AddDoor(int linkId, Fix64Vec2 center, Fix64Vec2 halfExtents)
        {
            var body = SimEntityFactory.CreateStaticBody(_doorBodyId++, center, halfExtents);
            var door = new Door(linkId, body);
            _doors.Add(door);
            return door;
        }

        public Fix64 RaisedAmount(int linkId)
        {
            for (int i = 0; i < _pulleys.Count; i++)
                if (_pulleys[i].LinkId == linkId)
                    return _pulleys[i].RaisedAmount;
            return Fix64.Zero;
        }

        public void Tick(IReadOnlyList<SimEntity> allBodies)
        {
            for (int i = 0; i < _pulleys.Count; i++)
            {
                Pulley p = _pulleys[i];
                int count = CountIn(allBodies, p.ZoneMin, p.ZoneMax);
                if (count >= p.Threshold)
                    p.RaisedAmount = Fix64.Clamp(p.RaisedAmount + p.RiseRate, Fix64.Zero, Fix64.One);
                else if (p.RaisedAmount < Fix64.One)
                    p.RaisedAmount = Fix64.Clamp(p.RaisedAmount - p.SlipRate, Fix64.Zero, Fix64.One);
                _pulleys[i] = p;
            }
            for (int d = 0; d < _doors.Count; d++)
            {
                bool wound = false;
                for (int i = 0; i < _pulleys.Count && !wound; i++)
                    wound = _pulleys[i].LinkId == _doors[d].LinkId && _pulleys[i].RaisedAmount >= Fix64.One;
                _doors[d].SetActivated(wound);
            }
        }

        private static int CountIn(IReadOnlyList<SimEntity> bodies, Fix64Vec2 min, Fix64Vec2 max)
        {
            int count = 0;
            for (int i = 0; i < bodies.Count; i++)
            {
                SimEntity b = bodies[i];
                if (b.Active && b.MaxX > min.X && b.MinX < max.X && b.MaxY > min.Y && b.MinY < max.Y) count++;
            }
            return count;
        }

        public void CollectSolids(List<SimEntity> into)
        {
            for (int i = 0; i < _doors.Count; i++)
                if (_doors[i].Body.SolidToEntities) into.Add(_doors[i].Body);
        }

        public void ContributeHash(ref StateHash h)
        {
            for (int i = 0; i < _pulleys.Count; i++) h.Add(_pulleys[i].RaisedAmount);
            for (int i = 0; i < _doors.Count; i++) _doors[i].ContributeHash(ref h);
        }

        public void ResetModule()
        {
            for (int i = 0; i < _pulleys.Count; i++)
            {
                Pulley p = _pulleys[i];
                p.RaisedAmount = Fix64.Zero;
                _pulleys[i] = p;
            }
            for (int i = 0; i < _doors.Count; i++) _doors[i].SetActivated(false);
        }

        public void CollectDynamicBodies(List<SimEntity> into) { }
        public void OnCharacterStep(SimEntity character, in InputCommand cmd) { }
        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids) { }
    }
}
