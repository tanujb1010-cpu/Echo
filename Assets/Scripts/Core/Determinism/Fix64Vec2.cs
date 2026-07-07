using System.Runtime.CompilerServices;

namespace Echo.Core.Determinism
{
    /// <summary>Deterministic 2D vector built on <see cref="Fix64"/>. Used for all sim positions/velocities.</summary>
    public readonly struct Fix64Vec2
    {
        public readonly Fix64 X;
        public readonly Fix64 Y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Fix64Vec2(Fix64 x, Fix64 y) { X = x; Y = y; }

        public static readonly Fix64Vec2 Zero = new Fix64Vec2(Fix64.Zero, Fix64.Zero);
        public static readonly Fix64Vec2 Up = new Fix64Vec2(Fix64.Zero, Fix64.One);
        public static readonly Fix64Vec2 Right = new Fix64Vec2(Fix64.One, Fix64.Zero);

        public static Fix64Vec2 FromInt(int x, int y) => new Fix64Vec2(Fix64.FromInt(x), Fix64.FromInt(y));

        public static Fix64Vec2 operator +(Fix64Vec2 a, Fix64Vec2 b) => new Fix64Vec2(a.X + b.X, a.Y + b.Y);
        public static Fix64Vec2 operator -(Fix64Vec2 a, Fix64Vec2 b) => new Fix64Vec2(a.X - b.X, a.Y - b.Y);
        public static Fix64Vec2 operator -(Fix64Vec2 a) => new Fix64Vec2(-a.X, -a.Y);
        public static Fix64Vec2 operator *(Fix64Vec2 a, Fix64 s) => new Fix64Vec2(a.X * s, a.Y * s);

        public static bool operator ==(Fix64Vec2 a, Fix64Vec2 b) => a.X == b.X && a.Y == b.Y;
        public static bool operator !=(Fix64Vec2 a, Fix64Vec2 b) => !(a == b);

        public Fix64 SqrMagnitude => X * X + Y * Y;
        public Fix64 Magnitude => Fix64.Sqrt(SqrMagnitude);

        public Fix64Vec2 Normalized
        {
            get
            {
                Fix64 m = Magnitude;
                return m == Fix64.Zero ? Zero : new Fix64Vec2(X / m, Y / m);
            }
        }

        // Render-only convenience (UnityEngine.Vector2 conversion lives in a presentation adapter to
        // keep this assembly free of UnityEngine dependencies — see Architecture §3 asmdef boundaries).
        public override bool Equals(object obj) => obj is Fix64Vec2 v && v == this;
        public override int GetHashCode() => unchecked((X.GetHashCode() * 397) ^ Y.GetHashCode());
        public override string ToString() => $"({X}, {Y})";
    }
}
