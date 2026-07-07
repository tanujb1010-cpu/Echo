using System;
using System.Collections.Generic;

namespace Echo.Core.Replay
{
    /// <summary>
    /// Verifies that a replay reproduces the world-state hash recorded at each keyframe
    /// (docs/05 §1.3). This is the safety net that turns "determinism drift" — the project's
    /// fatal risk — into a loud, testable signal instead of a silent gameplay glitch.
    ///
    /// In tests: a mismatch throws (the crown-jewel assertion).
    /// In release: a mismatch raises <see cref="OnDesync"/> so the caller can snap to the stored
    /// keyframe and continue gracefully; the player never sees a hard failure.
    /// </summary>
    public sealed class DesyncGuard
    {
        public bool ThrowOnDesync { get; set; } // true in tests/dev, false in shipping builds

        /// <summary>Raised on the first divergence: (tick, expectedHash, actualHash).</summary>
        public event Action<int, ulong, ulong> OnDesync;

        public int Mismatches { get; private set; }

        private readonly Dictionary<int, ulong> _expected = new Dictionary<int, ulong>(64);

        /// <summary>Load the authoritative keyframe hashes from the timeline being replayed.</summary>
        public void Arm(Timeline timeline)
        {
            _expected.Clear();
            Mismatches = 0;
            foreach (Keyframe kf in timeline.Keyframes)
                _expected[kf.Tick] = kf.StateHash;
        }

        /// <summary>Call each tick during replay with the freshly computed hash.</summary>
        public bool Verify(int tick, ulong actualHash)
        {
            if (!_expected.TryGetValue(tick, out ulong expected)) return true; // not a keyframe tick
            if (expected == actualHash) return true;

            Mismatches++;
            OnDesync?.Invoke(tick, expected, actualHash);
            if (ThrowOnDesync)
                throw new InvalidOperationException(
                    $"Determinism desync at tick {tick}: expected {expected:X16}, got {actualHash:X16}");
            return false;
        }
    }
}
