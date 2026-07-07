namespace Echo.Unity
{
    /// <summary>
    /// Geometry revision per level — the single source of truth for "has this layout changed since a
    /// record was earned?" Banked replay braids are recorded INPUTS: replayed against different
    /// geometry they desync into nonsense, and old best times are meaningless. The flow layer retires
    /// records whose revision doesn't match (completion is kept — only replays/times are cleared).
    ///
    /// Bump a level's number here whenever its LevelDefinition geometry or module wiring changes.
    /// </summary>
    public static class LevelRevisions
    {
        public static int Of(string levelId) => levelId switch
        {
            // Playtest fix (2026-07-06): quorum zone widened 3u → 5.5u (echo pile-up missed the lip)
            "W2_L2" => 3,
            // W3-W6 difficulty pass (2026-07-07): second jobs + tighter budgets
            "W3_L8" => 3,
            "W3_L1" or "W3_L6" or "W4_L1" or "W4_L4" => 2,
            // Phase 3 puzzle-depth redesign (2026-07-04)
            "W1_L4" or "W1_L5" or "W1_L6" or "W1_L7" or "W1_L8" => 2,
            "W2_L1" or "W2_L3" or "W2_L4" or "W2_L5" or "W2_L6" => 2,
            // Phase 4 cheese-audit fixes (2026-07-04)
            "W5_L1" => 2,
            _ => 1,
        };
    }
}
