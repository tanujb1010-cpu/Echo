using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Wind Toggle (#33): a switch, while held by anyone (live self or Echo), activates a linked wind zone
    /// that pushes every body inside it along a fixed direction — mirrors Held Plate (#1)'s "active while
    /// held" wiring, applied to a force field instead of a door. Since <see cref="CharacterMotor"/> derives
    /// horizontal velocity purely from input every tick (never accumulates external X velocity) and only
    /// integrates Y across ticks, a push can't be expressed as a velocity nudge the way Bounce Pad does for
    /// Y alone — so wind instead directly nudges position after the motor has already moved the body this
    /// tick, the same technique <see cref="TimeFieldModule"/> uses for its scaled displacement.
    /// </summary>
    public sealed class WindModule : ILevelModule
    {
        private struct Zone { public int LinkId; public Fix64Vec2 Min, Max; public Fix64Vec2 Force; public bool Active; }
        private struct Switch { public int LinkId; public Fix64Vec2 Min, Max; }

        private readonly List<Zone> _zones = new List<Zone>();
        private readonly List<Switch> _switches = new List<Switch>();

        public void AddZone(int linkId, Fix64Vec2 min, Fix64Vec2 max, Fix64Vec2 force)
            => _zones.Add(new Zone { LinkId = linkId, Min = min, Max = max, Force = force, Active = false });

        public void AddSwitch(int linkId, Fix64Vec2 min, Fix64Vec2 max)
            => _switches.Add(new Switch { LinkId = linkId, Min = min, Max = max });

        public bool IsZoneActive(int linkId)
        {
            for (int z = 0; z < _zones.Count; z++) if (_zones[z].LinkId == linkId) return _zones[z].Active;
            return false;
        }

        public void Tick(IReadOnlyList<SimEntity> allBodies)
        {
            for (int z = 0; z < _zones.Count; z++)
            {
                Zone zone = _zones[z];
                bool held = false;
                for (int s = 0; s < _switches.Count && !held; s++)
                    if (_switches[s].LinkId == zone.LinkId && AnyBodyIn(allBodies, _switches[s])) held = true;
                zone.Active = held;
                _zones[z] = zone;
            }
        }

        public void OnCharacterStep(SimEntity character, in InputCommand cmd)
        {
            for (int z = 0; z < _zones.Count; z++)
            {
                Zone zone = _zones[z];
                if (!zone.Active) continue;
                if (character.MaxX > zone.Min.X && character.MinX < zone.Max.X
                    && character.MaxY > zone.Min.Y && character.MinY < zone.Max.Y)
                {
                    character.Position += zone.Force * CharacterMotor.Dt;
                }
            }
        }

        private static bool AnyBodyIn(IReadOnlyList<SimEntity> bodies, Switch sw)
        {
            for (int i = 0; i < bodies.Count; i++)
            {
                SimEntity b = bodies[i];
                if (b.Active && b.MaxX > sw.Min.X && b.MinX < sw.Max.X && b.MaxY > sw.Min.Y && b.MinY < sw.Max.Y) return true;
            }
            return false;
        }

        public void CollectDynamicBodies(List<SimEntity> into) { }
        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids) { }
        public void CollectSolids(List<SimEntity> into) { }

        public void ContributeHash(ref StateHash h)
        {
            for (int z = 0; z < _zones.Count; z++) h.Add(_zones[z].Active);
        }

        public void ResetModule()
        {
            for (int z = 0; z < _zones.Count; z++)
            {
                Zone zone = _zones[z]; zone.Active = false; _zones[z] = zone;
            }
        }
    }
}
