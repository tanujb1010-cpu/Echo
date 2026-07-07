using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;
using Echo.Gameplay.Components;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Color Carry (#35): doors need Echoes delivering color-matched items. Each keyhole zone only
    /// satisfies its linked door while a crate tagged with THAT SAME color is resting inside it — a
    /// wrong-colored crate in the keyhole never counts, and each door only cares about its own linkId's
    /// keyhole(s). Crate bodies are owned and moved entirely by <see cref="CrateModule"/> (grab/carry/
    /// throw/drop); this module never creates or mutates a crate body, it only holds external SimEntity
    /// references handed to it (post-CrateModule.AddCrate) plus a color tag, and checks their AABB against
    /// each keyhole zone. Same AABB-overlap zone-check and Door usage pattern as PressureBalanceModule.
    /// </summary>
    public sealed class ColorCarryModule : ILevelModule
    {
        private struct ColoredItem { public SimEntity Item; public int Color; }
        private struct Keyhole { public int LinkId; public int RequiredColor; public Fix64Vec2 Min, Max; public bool Satisfied; }

        private readonly List<ColoredItem> _items = new List<ColoredItem>();
        private readonly List<Keyhole> _keyholes = new List<Keyhole>();
        private readonly List<Door> _doors = new List<Door>();
        private int _doorBodyId = 1_040_000;

        public IReadOnlyList<Door> Doors => _doors;

        public void RegisterColoredItem(SimEntity item, int color)
            => _items.Add(new ColoredItem { Item = item, Color = color });

        public void AddKeyhole(int linkId, int requiredColor, Fix64Vec2 min, Fix64Vec2 max)
            => _keyholes.Add(new Keyhole { LinkId = linkId, RequiredColor = requiredColor, Min = min, Max = max, Satisfied = false });

        public Door AddDoor(int linkId, Fix64Vec2 center, Fix64Vec2 halfExtents)
        {
            var body = SimEntityFactory.CreateStaticBody(_doorBodyId++, center, halfExtents);
            var door = new Door(linkId, body);
            _doors.Add(door);
            return door;
        }

        public bool IsKeyholeSatisfied(int linkId)
        {
            for (int i = 0; i < _keyholes.Count; i++)
                if (_keyholes[i].LinkId == linkId)
                    return _keyholes[i].Satisfied;
            return false;
        }

        public void Tick(IReadOnlyList<SimEntity> allBodies)
        {
            for (int i = 0; i < _keyholes.Count; i++)
            {
                Keyhole k = _keyholes[i];
                k.Satisfied = AnyMatchingItemInside(k.RequiredColor, k.Min, k.Max);
                _keyholes[i] = k;
            }
            for (int d = 0; d < _doors.Count; d++)
            {
                bool satisfied = false;
                for (int i = 0; i < _keyholes.Count && !satisfied; i++)
                    satisfied = _keyholes[i].LinkId == _doors[d].LinkId && _keyholes[i].Satisfied;
                _doors[d].SetActivated(satisfied);
            }
        }

        private bool AnyMatchingItemInside(int requiredColor, Fix64Vec2 min, Fix64Vec2 max)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                ColoredItem ci = _items[i];
                if (ci.Color != requiredColor) continue;
                SimEntity b = ci.Item;
                if (b.Active && b.MaxX > min.X && b.MinX < max.X && b.MaxY > min.Y && b.MinY < max.Y) return true;
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
            for (int i = 0; i < _keyholes.Count; i++) h.Add(_keyholes[i].Satisfied);
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
