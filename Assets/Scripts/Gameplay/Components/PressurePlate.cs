using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Sim;

namespace Echo.Gameplay.Components
{
    /// <summary>
    /// A floor trigger that is "pressed" while any body overlaps its region (Content Bible mechanic #1,
    /// "Held Plate"). Deterministic: pressed-state is a pure function of body AABBs this tick. A plate
    /// links to actuators (doors, etc.) by <see cref="LinkId"/>.
    /// </summary>
    public sealed class PressurePlate
    {
        public readonly int LinkId;
        public readonly Fix64Vec2 Min;   // world-space trigger AABB
        public readonly Fix64Vec2 Max;
        public bool Pressed { get; private set; }

        public PressurePlate(int linkId, Fix64Vec2 min, Fix64Vec2 max)
        {
            LinkId = linkId; Min = min; Max = max;
        }

        public bool Contains(SimEntity b)
            => b.Active && b.MaxX > Min.X && b.MinX < Max.X && b.MaxY > Min.Y && b.MinY < Max.Y;

        /// <summary>Recompute pressed-state from the current set of bodies (called by PlateDoorModule).</summary>
        public void Evaluate(IReadOnlyList<SimEntity> bodies)
        {
            Pressed = false;
            for (int i = 0; i < bodies.Count; i++)
                if (Contains(bodies[i])) { Pressed = true; return; }
        }

        public void ContributeHash(ref StateHash h) => h.Add(Pressed);
    }
}
