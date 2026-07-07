using System.Runtime.CompilerServices;

namespace Echo.Core.Determinism
{
    /// <summary>
    /// Deterministic PRNG (SplitMix64). The ONLY source of randomness allowed in the sim/Echo AI.
    /// UnityEngine.Random and System.Random are banned in deterministic code because they are not
    /// reproducible across platforms/runs (see docs/04_Clone_AI_Evolution_System.md §6).
    ///
    /// Gate predicates seed a *fresh* generator from (saveSeed, runId, tick, gateId) so a given
    /// decision is a pure function of observable state — reproducible and unit-testable.
    /// </summary>
    public struct DeterministicRng
    {
        private ulong _state;

        public DeterministicRng(ulong seed) => _state = seed;

        /// <summary>Build a generator whose stream is fully determined by these inputs.</summary>
        public static DeterministicRng Seeded(ulong saveSeed, int runId, int tick, int gateId)
        {
            // Mix the four inputs into one 64-bit seed via a hashing combine.
            ulong h = saveSeed;
            h = Mix(h ^ (ulong)(uint)runId);
            h = Mix(h ^ (ulong)(uint)tick);
            h = Mix(h ^ (ulong)(uint)gateId);
            return new DeterministicRng(h);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Mix(ulong z)
        {
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong NextULong()
        {
            _state += 0x9E3779B97F4A7C15UL;
            ulong z = _state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        public uint NextUInt() => (uint)(NextULong() >> 32);

        /// <summary>Uniform int in [minInclusive, maxExclusive).</summary>
        public int NextInt(int minInclusive, int maxExclusive)
        {
            uint range = (uint)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextUInt() % range);
        }

        /// <summary>Deterministic Fix64 in [0, 1).</summary>
        public Fix64 NextFix01()
        {
            // top 16 bits → fractional part of a Q16.16 value in [0,1).
            long frac = (long)(NextULong() >> 48);
            return Fix64.FromRaw(frac);
        }

        /// <summary>Returns true with the given Fix64 probability in [0,1]. Pure given the seed.</summary>
        public bool Chance(Fix64 probability) => NextFix01() < probability;
    }
}
