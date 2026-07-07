namespace Echo.Gameplay.Narrative
{
    /// <summary>The 10 alternate endings (docs/02 §8). Stable ids → safe to persist in saves.</summary>
    public enum EndingId
    {
        Restart = 1, Release, Erasure, Communion, Refusal,
        FirstsEnding, CaretakersPeace, OriginWakes, Saboteur, Quiet
    }

    /// <summary>The final-room decision the player makes; the dominant ending driver (docs/02 §8).</summary>
    public enum FinalChoice
    {
        Restart, Release, Erase, Merge, Refuse,
        YieldToFirst, SideCaretaker, WakeOrigin, YieldToSaboteur, Ritual
    }

    /// <summary>
    /// Accumulates the invisible branch drivers (docs/02 §8): Trust (reliance on high-salience Echoes),
    /// Mercy (avoiding needless pruning/sacrifice), Truth (secrets), Selfhood, and the relationship with
    /// First. Fed by gameplay events; consumed by <see cref="EndingResolver"/>. Pure data → testable.
    /// </summary>
    public sealed class BranchState
    {
        public float Trust = 0.5f;   // 0..1
        public float Mercy = 0.5f;   // 0..1
        public int SecretsFound;
        public int TotalSecrets = 10;
        public bool AcceptedSelfhood;
        public bool BefriendedFirst;
        public FinalChoice Choice = FinalChoice.Restart;

        public void OnReliedOnEcho() => Trust = Clamp01(Trust + 0.05f);
        public void OnEchoSacrificed() => Mercy = Clamp01(Mercy - 0.10f);
        public void OnEchoPruned() => Mercy = Clamp01(Mercy - 0.05f);
        public void OnMercifulRestart() => Mercy = Clamp01(Mercy + 0.03f);
        public void OnSecretFound() { if (SecretsFound < TotalSecrets) SecretsFound++; }
        public void AcceptSelfhood() => AcceptedSelfhood = true;
        public void BefriendFirst() => BefriendedFirst = true;

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
