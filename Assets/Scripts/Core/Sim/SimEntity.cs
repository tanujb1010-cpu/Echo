using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Infra;

namespace Echo.Core.Sim
{
    /// <summary>
    /// A simulated body in the deterministic world. Entities are *composed* of capability components
    /// (Grabbable, Pressable, …) rather than subclassed (docs/06 §1). Pooled — never allocated in the
    /// hot loop. Position/velocity are Fix64 so every body is bit-reproducible across platforms.
    /// </summary>
    public sealed class SimEntity : IPoolable
    {
        public int Id;
        public bool Active;

        public Fix64Vec2 Position;
        public Fix64Vec2 Velocity;
        public Fix64Vec2 HalfExtents;   // AABB half-size in tile units

        public bool Grounded;
        public int FacingSign = 1;       // -1 left, +1 right (for grab direction, rendering)

        /// <summary>If true, other entities can stand on / collide with this body (Echoes are solid).</summary>
        public bool SolidToEntities;

        // Lightweight component bag (composition over inheritance). Kept tiny + cache-friendly.
        private readonly List<ISimComponent> _components = new List<ISimComponent>(4);

        public void Add(ISimComponent c) { c.Owner = this; _components.Add(c); }
        public T Get<T>() where T : class, ISimComponent
        {
            for (int i = 0; i < _components.Count; i++)
                if (_components[i] is T t) return t;
            return null;
        }
        public IReadOnlyList<ISimComponent> Components => _components;

        public Fix64 MinX => Position.X - HalfExtents.X;
        public Fix64 MaxX => Position.X + HalfExtents.X;
        public Fix64 MinY => Position.Y - HalfExtents.Y;
        public Fix64 MaxY => Position.Y + HalfExtents.Y;

        public bool OverlapsAabb(SimEntity o)
            => MinX < o.MaxX && MaxX > o.MinX && MinY < o.MaxY && MaxY > o.MinY;

        /// <summary>Feed deterministic state into the world hash (DesyncGuard / keyframes).</summary>
        public void ContributeHash(ref StateHash h)
        {
            h.Add(Id);
            h.Add(Position);
            h.Add(Velocity);
            h.Add(Grounded);
            h.Add(FacingSign);
            for (int i = 0; i < _components.Count; i++)
                _components[i].ContributeHash(ref h);
        }

        public void OnSpawned() { Active = true; }
        public void OnDespawned()
        {
            Active = false;
            Velocity = Fix64Vec2.Zero;
            Grounded = false;
            _components.Clear();
        }
    }

    /// <summary>Capability component attached to a SimEntity. Deterministic, hashable.</summary>
    public interface ISimComponent
    {
        SimEntity Owner { get; set; }
        void ContributeHash(ref StateHash h);
    }
}
