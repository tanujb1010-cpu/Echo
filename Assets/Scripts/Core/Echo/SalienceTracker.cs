using Echo.Core.Determinism;

namespace Echo.Core.Echo
{
    /// <summary>
    /// Tracks an Echo's Salience S ∈ [0,1] — "how strongly the foundry remembers it" — the single
    /// clock that drives evolution (docs/04 §2). Rises with age, co-presence (resonance) and reuse;
    /// decays with neglect. Persisted per banked Timeline so an Echo stays "evolved" across sessions.
    /// All increments are Fix64 → deterministic.
    /// </summary>
    public sealed class SalienceTracker
    {
        public Fix64 Salience;

        // Per-tick gains (tuned via ScriptableObject TuningProfile per world; defaults here).
        private static readonly Fix64 AgeGain = Fix64.FromRaw(8);        // ~0.00012 / tick
        private static readonly Fix64 ResonanceGain = Fix64.FromRaw(24); // co-present with a sibling Echo
        private static readonly Fix64 ReuseGain = Fix64.FromRaw(2000);   // ~0.03 per re-bank/reliance
        private static readonly Fix64 PivotalGain = Fix64.FromRaw(4000); // its action was load-bearing

        public void Init(Fix64 persisted) => Salience = DriveModel.Clamp01(persisted);

        /// <summary>Call each tick the Echo is active.</summary>
        public void Tick(bool nearSibling)
        {
            Salience += AgeGain;
            if (nearSibling) Salience += ResonanceGain;
            Salience = DriveModel.Clamp01(Salience);
        }

        public void AddReuse() => Salience = DriveModel.Clamp01(Salience + ReuseGain);
        public void AddPivotal() => Salience = DriveModel.Clamp01(Salience + PivotalGain);

        /// <summary>Decay when a run is left unused for a long stretch.</summary>
        public void Decay(Fix64 amount) => Salience = DriveModel.Clamp01(Salience - amount);
    }
}
