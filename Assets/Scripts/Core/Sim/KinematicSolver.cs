using System.Collections.Generic;
using Echo.Core.Determinism;

namespace Echo.Core.Sim
{
    public struct CollisionFlags
    {
        public bool Left, Right, Up, Down;
        public bool Grounded => Down;
    }

    /// <summary>
    /// Deterministic axis-separated AABB collision against static tiles and solid entities (Echoes).
    /// Resolves X then Y. Per-tick displacement is small (sub-tile) at gameplay speeds, so simple
    /// snap-out resolution is robust without continuous sweeping. All math is Fix64 → reproducible.
    ///
    /// Echoes are treated as immovable solids: the live player (and movable crates) collide against
    /// them, which is exactly what lets you stand on / be blocked by your past selves.
    ///
    /// SPAWN-OVERLAP RULE: a solid entity only blocks you on an axis if you were NOT already
    /// overlapping it at the start of the tick. On restart the whole braid spawns stacked on the same
    /// entrance tile; without this rule the bodies would violently shove each other (a 1-tick "pop").
    /// Allowing already-overlapping bodies to pass through until they separate — then re-solidifying —
    /// is the standard, deterministic fix and also handles mid-level overlaps gracefully.
    /// </summary>
    public static class KinematicSolver
    {
        // Small skin so a body resting exactly on a tile boundary doesn't false-trigger the adjacent tile.
        private static readonly Fix64 Eps = Fix64.FromRaw(64); // ~0.001 units

        public static int FloorToInt(Fix64 v) => (int)(v.Raw >> Fix64.FractionalBits);

        /// <summary>AABB bounds snapshot (tick-start), used to detect pre-existing overlaps.</summary>
        private readonly struct Bounds
        {
            public readonly Fix64 MinX, MaxX, MinY, MaxY;
            public Bounds(SimEntity e) { MinX = e.MinX; MaxX = e.MaxX; MinY = e.MinY; MaxY = e.MaxY; }
            public bool Overlaps(SimEntity o)
                => MinX < o.MaxX && MaxX > o.MinX && MinY < o.MaxY && MaxY > o.MinY;
        }

        public static CollisionFlags MoveAndCollide(
            SimEntity e, Fix64Vec2 displacement, ICollisionWorld world, IReadOnlyList<SimEntity> solids)
        {
            var flags = new CollisionFlags();
            var start = new Bounds(e); // capture BEFORE moving, for the spawn-overlap rule

            // --- X axis ---
            e.Position = new Fix64Vec2(e.Position.X + displacement.X, e.Position.Y);
            if (displacement.X > Fix64.Zero) ResolveRight(e, world, solids, start, ref flags);
            else if (displacement.X < Fix64.Zero) ResolveLeft(e, world, solids, start, ref flags);

            // --- Y axis ---
            e.Position = new Fix64Vec2(e.Position.X, e.Position.Y + displacement.Y);
            if (displacement.Y > Fix64.Zero) ResolveUp(e, world, solids, start, ref flags);
            else if (displacement.Y < Fix64.Zero) ResolveDown(e, world, solids, start, ref flags);

            return flags;
        }

        // ---------- tile + entity resolution per direction ----------

        private static void ResolveRight(SimEntity e, ICollisionWorld w, IReadOnlyList<SimEntity> solids, in Bounds start, ref CollisionFlags f)
        {
            int col = FloorToInt(e.MaxX - Eps);
            int yA = FloorToInt(e.MinY + Eps), yB = FloorToInt(e.MaxY - Eps);
            for (int ty = yA; ty <= yB; ty++)
                if (w.IsSolid(col, ty)) { SnapMaxX(e, Fix64.FromInt(col)); f.Right = true; return; }
            ResolveEntityX(e, solids, start, ref f, movingRight: true);
        }

        private static void ResolveLeft(SimEntity e, ICollisionWorld w, IReadOnlyList<SimEntity> solids, in Bounds start, ref CollisionFlags f)
        {
            int col = FloorToInt(e.MinX + Eps);
            int yA = FloorToInt(e.MinY + Eps), yB = FloorToInt(e.MaxY - Eps);
            for (int ty = yA; ty <= yB; ty++)
                if (w.IsSolid(col, ty)) { SnapMinX(e, Fix64.FromInt(col + 1)); f.Left = true; return; }
            ResolveEntityX(e, solids, start, ref f, movingRight: false);
        }

        private static void ResolveUp(SimEntity e, ICollisionWorld w, IReadOnlyList<SimEntity> solids, in Bounds start, ref CollisionFlags f)
        {
            int row = FloorToInt(e.MaxY - Eps);
            int xA = FloorToInt(e.MinX + Eps), xB = FloorToInt(e.MaxX - Eps);
            for (int tx = xA; tx <= xB; tx++)
                if (w.IsSolid(tx, row)) { SnapMaxY(e, Fix64.FromInt(row)); f.Up = true; return; }
            ResolveEntityY(e, solids, start, ref f, movingDown: false);
        }

        private static void ResolveDown(SimEntity e, ICollisionWorld w, IReadOnlyList<SimEntity> solids, in Bounds start, ref CollisionFlags f)
        {
            int row = FloorToInt(e.MinY + Eps);
            int xA = FloorToInt(e.MinX + Eps), xB = FloorToInt(e.MaxX - Eps);
            for (int tx = xA; tx <= xB; tx++)
                if (w.IsSolid(tx, row)) { SnapMinY(e, Fix64.FromInt(row + 1)); f.Down = true; return; }
            ResolveEntityY(e, solids, start, ref f, movingDown: true);
        }

        // ---------- entity-vs-entity (immovable AABBs, with spawn-overlap rule) ----------
        // Picks the *most blocking* candidate (nearest contact) so stacking is stable.

        private static void ResolveEntityX(SimEntity e, IReadOnlyList<SimEntity> solids, in Bounds start, ref CollisionFlags f, bool movingRight)
        {
            SimEntity best = null;
            for (int i = 0; i < solids.Count; i++)
            {
                SimEntity o = solids[i];
                if (!IsBlocker(e, o) || start.Overlaps(o) || !e.OverlapsAabb(o)) continue;
                if (best == null) { best = o; continue; }
                // most-blocking = the one whose edge pushes us back furthest
                best = movingRight ? (o.MinX < best.MinX ? o : best) : (o.MaxX > best.MaxX ? o : best);
            }
            if (best == null) return;
            if (movingRight) SnapMaxX(e, best.MinX); else SnapMinX(e, best.MaxX);
            if (movingRight) f.Right = true; else f.Left = true;
        }

        private static void ResolveEntityY(SimEntity e, IReadOnlyList<SimEntity> solids, in Bounds start, ref CollisionFlags f, bool movingDown)
        {
            SimEntity best = null;
            for (int i = 0; i < solids.Count; i++)
            {
                SimEntity o = solids[i];
                if (!IsBlocker(e, o) || start.Overlaps(o) || !e.OverlapsAabb(o)) continue;
                if (best == null) { best = o; continue; }
                best = movingDown ? (o.MaxY > best.MaxY ? o : best)   // rest on the highest head
                                  : (o.MinY < best.MinY ? o : best);  // bonk the lowest ceiling
            }
            if (best == null) return;
            if (movingDown) { SnapMinY(e, best.MaxY); f.Down = true; }
            else { SnapMaxY(e, best.MinY); f.Up = true; }
        }

        private static bool IsBlocker(SimEntity self, SimEntity other)
            => other != null && other.Active && other.SolidToEntities && other.Id != self.Id;

        // ---------- snap helpers (place an AABB edge exactly on a boundary) ----------
        private static void SnapMaxX(SimEntity e, Fix64 boundary) => e.Position = new Fix64Vec2(boundary - e.HalfExtents.X, e.Position.Y);
        private static void SnapMinX(SimEntity e, Fix64 boundary) => e.Position = new Fix64Vec2(boundary + e.HalfExtents.X, e.Position.Y);
        private static void SnapMaxY(SimEntity e, Fix64 boundary) => e.Position = new Fix64Vec2(e.Position.X, boundary - e.HalfExtents.Y);
        private static void SnapMinY(SimEntity e, Fix64 boundary) => e.Position = new Fix64Vec2(e.Position.X, boundary + e.HalfExtents.Y);
    }
}
