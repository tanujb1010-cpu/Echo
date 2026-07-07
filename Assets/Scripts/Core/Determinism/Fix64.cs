using System;
using System.Runtime.CompilerServices;

namespace Echo.Core.Determinism
{
    /// <summary>
    /// Q16.16 fixed-point number (long-backed) for the deterministic gameplay simulation.
    ///
    /// WHY: cross-platform float math is not bit-identical, which would desync ghost replays
    /// (see docs/05_Technical_Design_Document.md §1). All sim-critical math uses Fix64 so a run
    /// recorded on a PC replays bit-identically on a phone. Rendering may freely use float.
    ///
    /// Range/precision: ~±32,767 with 1/65536 (~1.5e-5) precision. Coordinates are in *tile units*,
    /// so this comfortably covers level space. The multiply intermediate (raw*raw) stays within a
    /// 64-bit long for the documented value range. Do not exceed it in sim math.
    /// </summary>
    public readonly struct Fix64 : IEquatable<Fix64>, IComparable<Fix64>
    {
        public const int FractionalBits = 16;
        private const long One_Raw = 1L << FractionalBits;       // 65536
        private const long Half_Raw = One_Raw >> 1;

        /// <summary>Underlying fixed-point value. Public for serialization/hashing only.</summary>
        public readonly long Raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Fix64(long raw) => Raw = raw;

        public static readonly Fix64 Zero = new Fix64(0);
        public static readonly Fix64 One = new Fix64(One_Raw);
        public static readonly Fix64 Half = new Fix64(Half_Raw);
        public static readonly Fix64 MinusOne = new Fix64(-One_Raw);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fix64 FromRaw(long raw) => new Fix64(raw);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fix64 FromInt(int value) => new Fix64((long)value << FractionalBits);

        /// <summary>
        /// Authoring-time only (level data, tuning). Deterministic once stored, but never call
        /// this inside the per-tick sim loop with runtime floats — convert at load time.
        /// </summary>
        public static Fix64 FromFloat(float value) => new Fix64((long)Math.Round(value * One_Raw));

        public float ToFloat() => (float)Raw / One_Raw;   // for rendering/interpolation only
        public int ToInt() => (int)(Raw >> FractionalBits);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fix64 operator +(Fix64 a, Fix64 b) => new Fix64(a.Raw + b.Raw);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fix64 operator -(Fix64 a, Fix64 b) => new Fix64(a.Raw - b.Raw);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fix64 operator -(Fix64 a) => new Fix64(-a.Raw);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fix64 operator *(Fix64 a, Fix64 b) => new Fix64((a.Raw * b.Raw) >> FractionalBits);

        public static Fix64 operator /(Fix64 a, Fix64 b)
        {
            if (b.Raw == 0) throw new DivideByZeroException("Fix64 division by zero");
            return new Fix64((a.Raw << FractionalBits) / b.Raw);
        }

        public static bool operator ==(Fix64 a, Fix64 b) => a.Raw == b.Raw;
        public static bool operator !=(Fix64 a, Fix64 b) => a.Raw != b.Raw;
        public static bool operator <(Fix64 a, Fix64 b) => a.Raw < b.Raw;
        public static bool operator >(Fix64 a, Fix64 b) => a.Raw > b.Raw;
        public static bool operator <=(Fix64 a, Fix64 b) => a.Raw <= b.Raw;
        public static bool operator >=(Fix64 a, Fix64 b) => a.Raw >= b.Raw;

        public static Fix64 Abs(Fix64 v) => new Fix64(v.Raw < 0 ? -v.Raw : v.Raw);
        public static Fix64 Min(Fix64 a, Fix64 b) => a.Raw <= b.Raw ? a : b;
        public static Fix64 Max(Fix64 a, Fix64 b) => a.Raw >= b.Raw ? a : b;
        public static int Sign(Fix64 v) => v.Raw > 0 ? 1 : (v.Raw < 0 ? -1 : 0);
        public static Fix64 Floor(Fix64 v) => new Fix64(v.Raw & ~(One_Raw - 1));

        public static Fix64 Clamp(Fix64 v, Fix64 lo, Fix64 hi)
            => v.Raw < lo.Raw ? lo : (v.Raw > hi.Raw ? hi : v);

        /// <summary>Deterministic integer-based square root (Newton on the raw value).</summary>
        public static Fix64 Sqrt(Fix64 v)
        {
            if (v.Raw < 0) throw new ArgumentException("Sqrt of negative Fix64");
            if (v.Raw == 0) return Zero;
            // sqrt(v) in Q16.16 == isqrt(raw << 16)
            ulong n = (ulong)v.Raw << FractionalBits;
            ulong x = n, c = 0, d = 1UL << 62;
            while (d > n) d >>= 2;
            while (d != 0)
            {
                if (x >= c + d) { x -= c + d; c = (c >> 1) + d; }
                else c >>= 1;
                d >>= 2;
            }
            return new Fix64((long)c);
        }

        public int CompareTo(Fix64 other) => Raw.CompareTo(other.Raw);
        public bool Equals(Fix64 other) => Raw == other.Raw;
        public override bool Equals(object obj) => obj is Fix64 f && f.Raw == Raw;
        public override int GetHashCode() => Raw.GetHashCode();
        public override string ToString() => ToFloat().ToString("0.#####");
    }
}
