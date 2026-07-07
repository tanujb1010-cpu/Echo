namespace Echo.Unity
{
    /// <summary>
    /// What the player is trying to accomplish in each level — shown on the HUD so it's immediately
    /// clear. One goal per level; simple, direct imperatives (e.g. "Reach the door," "Stand on the plate").
    /// </summary>
    public static class LevelGoals
    {
        public static string Goal(string levelId) => levelId switch
        {
            // World 1 — the verbs, one at a time (then together)
            "W1_L1" => "Reach the door",
            "W1_L2" => "Keep the plate held (something can stand in for you)",
            "W1_L3" => "Cross the spikes (they only kill once)",
            "W1_L4" => "Three bodies must HOLD the zone — while you walk free",
            "W1_L5" => "Both switches on the same tick — but the road to one is lethal",
            "W1_L6" => "Cross the high ledge — someone below holds the way open",
            "W1_L7" => "Two pits, two plates — the order you record them matters",
            "W1_L8" => "Throw the crate onto the plate; the last stretch bites",

            // World 2 — space starts lying to you
            "W2_L1" => "Portal past the wall — the door beyond answers to the plate behind you",
            "W2_L2" => "Three hold the zone; tag the FAR mark first, then the near one",
            "W2_L3" => "The magnet steals the crate — block its slide with a body",
            "W2_L4" => "Clear the road, hold the bridge, tag far, tag near, leave",
            "W2_L5" => "Light the beacons in order (F) — the third is walled off; a plate behind spawn holds the way",
            "W2_L6" => "One recording does every job — then walk through your own clockwork",

            // World 3 — new physics
            "W3_L1" => "Equal bodies on BOTH pads — and the road to the far pad is lethal",
            "W3_L2" => "Stand where the light lands",
            "W3_L3" => "Navigate the flip zone (prepare for the ceiling)",
            "W3_L4" => "Trigger the trap only once",
            "W3_L5" => "Cross the crumbling tiles (one path per run)",
            "W3_L6" => "A LONG crank buys a short window — spend it on the plate beyond the door",
            "W3_L7" => "Build a staircase of yourself",
            "W3_L8" => "Two wind the winch, one dies on the road — you just walk",

            // World 4 — the facility pushes back
            "W4_L1" => "EXACTLY three on the scale, a fourth on the far plate — JUMP the pile",
            "W4_L2" => "Build a tower of parked selves — the plate floats at tower height",
            "W4_L3" => "Reach the far side without the isolation field",
            "W4_L4" => "Arm the dominoes behind you, clear the deadly road, ride the cascade home",
            "W4_L5" => "Learn the piston rhythm and slip through",
            "W4_L6" => "Wait for the flood and swim across",
            "W4_L7" => "One holds the door, one passes through",
            "W4_L8" => "Hold the switch and ride the wind",

            // World 5 — precision and identity
            "W5_L1" => "ONE walker must tag all three marks — the middle one is up high",
            "W5_L2" => "Stand on each plate (more bodies each time)",
            "W5_L3" => "Speak the password sequence",
            "W5_L4" => "Press both locks within a heartbeat",
            "W5_L5" => "Carry each colored crate to its matching keyhole",
            "W5_L6" => "Avoid the erasure field",
            "W5_L7" => "Call and catch the elevator",
            "W5_L8" => "Resonate across the distance",

            // World 6 — everything at once
            "W6_L1" => "Survive every hazard type at once",
            "W6_L2" => "Spend early runs on early hazards",
            "W6_L3" => "Use one blast shadow to cross another",
            "W6_L4" => "Mirror the other side perfectly",
            "W6_L5" => "Read the safe path, then walk it",
            "W6_L6" => "One shield charge can cross everything",

            _ => "Solve the puzzle and reach the door",
        };
    }
}
