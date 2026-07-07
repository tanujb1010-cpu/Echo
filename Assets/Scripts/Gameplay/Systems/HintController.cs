namespace Echo.Gameplay.Systems
{
    public enum HintTier { None, Nudge, Frame, Choreography }

    /// <summary>
    /// Opt-in, non-punishing hint escalation (docs/05 §10). The player can manually escalate, and the
    /// system *offers* (never forces) a hint after repeated failure. A cooldown prevents spoiler-spamming.
    /// Pure logic → unit-testable; the UI just renders <see cref="CurrentTier"/> and calls <see cref="RequestNext"/>.
    /// </summary>
    public sealed class HintController
    {
        public int AutoSurfaceAfterFailures = 4;
        public float CooldownSeconds = 30f;

        public HintTier CurrentTier { get; private set; } = HintTier.None;
        public bool OfferAvailable { get; private set; }

        private int _failedRuns;
        private float _cooldown;

        public void OnRunFailed()
        {
            _failedRuns++;
            if (_failedRuns >= AutoSurfaceAfterFailures && CurrentTier == HintTier.None)
                OfferAvailable = true; // surfaced, not forced
        }

        public void OnProgress()
        {
            _failedRuns = 0;
            OfferAvailable = false;
            CurrentTier = HintTier.None;
        }

        /// <summary>Player escalated a hint tier (respects cooldown). Returns the new tier.</summary>
        public HintTier RequestNext()
        {
            if (_cooldown > 0f) return CurrentTier;
            CurrentTier = CurrentTier switch
            {
                HintTier.None => HintTier.Nudge,
                HintTier.Nudge => HintTier.Frame,
                HintTier.Frame => HintTier.Choreography,
                _ => HintTier.Choreography,
            };
            OfferAvailable = false;
            _cooldown = CooldownSeconds;
            return CurrentTier;
        }

        public void Tick(float dt) { if (_cooldown > 0f) _cooldown = System.Math.Max(0f, _cooldown - dt); }
    }
}
