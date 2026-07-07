namespace Echo.Gameplay.Narrative
{
    /// <summary>
    /// Maps a <see cref="BranchState"/> to one of the 10 endings (docs/02 §8). The final-room choice is the
    /// dominant driver; some endings additionally gate on the invisible stats (Trust/Mercy/Truth) so they
    /// must be *earned*, not just picked. Pure, deterministic, total (always returns an ending) → every
    /// ending is provably reachable, and the mapping is unit-tested exhaustively.
    /// </summary>
    public static class EndingResolver
    {
        public static EndingId Resolve(BranchState s)
        {
            // Most specific narrative outcomes first.
            if (s.Choice == FinalChoice.Ritual && s.SecretsFound >= s.TotalSecrets) return EndingId.Quiet;
            if (s.Choice == FinalChoice.YieldToFirst && s.BefriendedFirst) return EndingId.FirstsEnding;
            if (s.Choice == FinalChoice.YieldToSaboteur) return EndingId.Saboteur;
            if (s.Choice == FinalChoice.SideCaretaker) return EndingId.CaretakersPeace;
            if (s.Choice == FinalChoice.WakeOrigin) return EndingId.OriginWakes;
            if (s.Choice == FinalChoice.Merge && s.Trust >= 0.6f && s.SecretsFound >= 5) return EndingId.Communion;
            if (s.Choice == FinalChoice.Refuse) return EndingId.Refusal;

            // Stat-shaded resolutions (also reachable from a plain Restart choice at the extremes).
            if (s.Choice == FinalChoice.Release || s.Mercy >= 0.70f) return EndingId.Release;
            if (s.Choice == FinalChoice.Erase || s.Mercy <= 0.20f) return EndingId.Erasure;

            return EndingId.Restart; // canonical-neutral default
        }

        /// <summary>Human-readable title for UI/credits.</summary>
        public static string Title(EndingId id) => id switch
        {
            EndingId.Restart => "Restart",
            EndingId.Release => "Release",
            EndingId.Erasure => "Erasure",
            EndingId.Communion => "Communion",
            EndingId.Refusal => "Refusal",
            EndingId.FirstsEnding => "First's Ending",
            EndingId.CaretakersPeace => "The Caretaker's Peace",
            EndingId.OriginWakes => "Origin Wakes",
            EndingId.Saboteur => "The Saboteur",
            EndingId.Quiet => "The Quiet Ending",
            _ => "Restart",
        };
    }
}
