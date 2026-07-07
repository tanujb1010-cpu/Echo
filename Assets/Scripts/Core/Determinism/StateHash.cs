using System.Runtime.CompilerServices;

namespace Echo.Core.Determinism
{
    /// <summary>
    /// Streaming FNV-1a 64-bit hash of deterministic world state. Used by the DesyncGuard
    /// (docs/05 §1.3): a replay must reproduce the same StateHash at each keyframe, or we have a
    /// determinism bug. In tests this is the crown-jewel assertion; in release a mismatch falls
    /// back to the stored keyframe gracefully.
    /// </summary>
    public struct StateHash
    {
        private const ulong FnvOffset = 14695981039346656037UL;
        private const ulong FnvPrime = 1099511628211UL;

        private ulong _hash;

        public static StateHash New() => new StateHash { _hash = FnvOffset };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(long value)
        {
            // Feed all 8 bytes so the full fixed-point precision affects the hash.
            for (int i = 0; i < 8; i++)
            {
                _hash ^= (byte)(value >> (i * 8));
                _hash *= FnvPrime;
            }
        }

        public void Add(int value) => Add((long)value);
        public void Add(bool value) => Add(value ? 1L : 0L);
        public void Add(Fix64 value) => Add(value.Raw);
        public void Add(Fix64Vec2 value) { Add(value.X.Raw); Add(value.Y.Raw); }

        public ulong Value => _hash;
    }
}
