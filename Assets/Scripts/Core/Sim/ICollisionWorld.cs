using Echo.Core.Determinism;

namespace Echo.Core.Sim
{
    /// <summary>Static level geometry queried by the kinematic solver. Tiles are 1 Fix64 unit square.</summary>
    public interface ICollisionWorld
    {
        bool IsSolid(int tileX, int tileY);
    }

    /// <summary>
    /// Simple bit-grid collision world for the deterministic sim. Real levels author this from a
    /// tilemap at load time; tests construct it directly. No floats, fully reproducible.
    /// </summary>
    public sealed class TileCollisionWorld : ICollisionWorld
    {
        private readonly bool[] _solid;
        public int Width { get; }
        public int Height { get; }

        public TileCollisionWorld(int width, int height)
        {
            Width = width; Height = height;
            _solid = new bool[width * height];
        }

        public void SetSolid(int x, int y, bool solid)
        {
            if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) return;
            _solid[y * Width + x] = solid;
        }

        public bool IsSolid(int tileX, int tileY)
        {
            // Out-of-bounds below/sides treated as solid walls; above the level is open sky.
            if (tileX < 0 || tileX >= Width || tileY < 0) return true;
            if (tileY >= Height) return false;
            return _solid[tileY * Width + tileX];
        }

        /// <summary>Convenience: fill the bottom row as a floor (used by the demo/tests).</summary>
        public void FillFloor(int row)
        {
            for (int x = 0; x < Width; x++) SetSolid(x, row, true);
        }
    }
}
