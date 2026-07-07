using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;
using Echo.Gameplay.Components;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Generator Crank (#31): a linked door stays open ONLY while some body — live self or Echo — is
    /// standing in its crank zone AND actively holding Interact that exact tick; unlike Held Plate (#1),
    /// mere presence is never enough, and there is no held-open memory/latch. Module solids are
    /// snapshotted ONCE per tick, before any character moves (docs/05 §1), so <see cref="CollectSolids"/>
    /// always reflects whoever cranked during the PREVIOUS tick's <see cref="OnCharacterStep"/> passes.
    /// Closing every door therefore can't happen in <see cref="Tick"/> (that runs before CollectSolids and
    /// would erase last tick's result before it's ever read) — instead the first OnCharacterStep call of
    /// each tick closes every door once, and every character's pass that tick then only opens doors, never
    /// wipes a sibling's contribution.
    /// </summary>
    public sealed class GeneratorCrankModule : ILevelModule
    {
        private struct Zone { public int LinkId; public Fix64Vec2 Min, Max; }

        private readonly List<Zone> _zones = new List<Zone>();
        private readonly List<Door> _doors = new List<Door>();
        private int _doorBodyId = 1_030_000;
        private bool _clearedThisTick;

        public IReadOnlyList<Door> Doors => _doors;

        public void AddZone(int linkId, Fix64Vec2 min, Fix64Vec2 max)
            => _zones.Add(new Zone { LinkId = linkId, Min = min, Max = max });

        public Door AddDoor(int linkId, Fix64Vec2 center, Fix64Vec2 halfExtents)
        {
            var body = SimEntityFactory.CreateStaticBody(_doorBodyId++, center, halfExtents);
            var door = new Door(linkId, body);
            _doors.Add(door);
            return door;
        }

        public void Tick(IReadOnlyList<SimEntity> allBodies) => _clearedThisTick = false;

        public void OnCharacterStep(SimEntity character, in InputCommand cmd)
        {
            if (!_clearedThisTick)
            {
                for (int i = 0; i < _doors.Count; i++) _doors[i].SetActivated(false);
                _clearedThisTick = true;
            }
            if (!cmd.Has(InputButtons.Interact)) return;
            for (int z = 0; z < _zones.Count; z++)
            {
                Zone zone = _zones[z];
                if (character.MaxX > zone.Min.X && character.MinX < zone.Max.X
                    && character.MaxY > zone.Min.Y && character.MinY < zone.Max.Y)
                {
                    for (int d = 0; d < _doors.Count; d++)
                        if (_doors[d].LinkId == zone.LinkId) _doors[d].SetActivated(true);
                }
            }
        }

        public void CollectSolids(List<SimEntity> into)
        {
            for (int i = 0; i < _doors.Count; i++)
                if (_doors[i].Body.SolidToEntities) into.Add(_doors[i].Body);
        }

        public void ContributeHash(ref StateHash h)
        {
            for (int i = 0; i < _doors.Count; i++) _doors[i].ContributeHash(ref h);
        }

        public void ResetModule()
        {
            _clearedThisTick = false;
            for (int i = 0; i < _doors.Count; i++) _doors[i].SetActivated(false);
        }

        public void CollectDynamicBodies(List<SimEntity> into) { }
        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids) { }
    }
}
