namespace Echo.Unity
{
    /// <summary>
    /// One plain-language line per level explaining the mechanic's RULES — what a "beacon", "crank",
    /// or "quorum" actually does and which key drives it. Shown on the HUD (under the goal) until the
    /// level is completed, then hidden. This is the glossary layer: LevelGoals says what to DO,
    /// HintContent nudges HOW to think, and this says what the words MEAN. Keys: F interact, E grab,
    /// Q lantern, S/Ctrl crouch (see LegacyInputProvider).
    /// </summary>
    public static class MechanicPrimers
    {
        public static string Primer(string levelId) => levelId switch
        {
            // World 1 — the loop and its first verbs
            "W1_L1" => "PLATE: the door stays open only WHILE a body stands in the glowing zone.",
            "W1_L3" => "HAZARD: red zones kill. A body that dies in one CLEARS it for everyone after — that run.",
            "W1_L4" => "QUORUM: this zone counts bodies — the door holds only while ENOUGH stand inside at once.",
            "W1_L5" => "TWIN SWITCHES: both held at the SAME instant — then they lock open for good.",
            "W1_L6" => "BOUNCE PAD: walking over it launches you. PHASE BRIDGE: solid only while its plate is held.",
            "W1_L8" => "CRATE: E grabs and carries; release mid-run to THROW. Crates press plates like bodies do.",

            // World 2 — space starts lying
            "W2_L1" => "PORTAL: step into one mouth, step out of the other. Obligations don't teleport.",
            "W2_L2" => "MARKS: touch them in their required order — touching one early does nothing at all.",
            "W2_L3" => "MAGNET: drags metal crates toward itself, always. Nothing can grab metal — bodies can block it.",
            "W2_L5" => "BEACON: stand near and press F to light it. Light them IN ORDER — one wrong press snuffs all.",

            // World 3 — new physics
            "W3_L1" => "SEE-SAW PADS: the gate opens only while BOTH pads hold the SAME number of bodies (1+ each).",
            "W3_L2" => "LANTERN: hold Q to shine. The light-platform is solid only while a shining body is near.",
            "W3_L3" => "FLIP ZONE: gravity reverses inside — you fall UP. Steer with A/D while floating.",
            "W3_L4" => "CHARGE: press F to arm it; it detonates after a delay, clearing what the blast touches.",
            "W3_L5" => "CRUMBLE TILES: each collapses moments after ANY body steps on it. One crossing per run.",
            "W3_L6" => "CRANK: the door lives only WHILE someone holds F in the crank zone. An Echo cranks exactly as long as its recording did.",
            "W3_L7" => "LADDER ZONE: an Echo inside is solid ONLY while its recording holds crouch (S/Ctrl). Stand on it.",
            "W3_L8" => "WINCH: the gate winds open while TWO bodies stand in the rope zone; fully wound, it stays open.",

            // World 4 — systems with clocks
            "W4_L1" => "SCALE: the door wants an EXACT body count. More is as wrong as fewer.",
            "W4_L2" => "STACKING: bodies are solid — you can stand on an Echo's head. Parked selves are height.",
            "W4_L3" => "ISOLATION FIELD: inside it, touching ANY Echo kills the live you. Keep your distance.",
            "W4_L4" => "DOMINOES: stand in the first zone to arm the chain; each fall arms the next. The door answers the LAST fall.",
            "W4_L5" => "PISTON: extends (lethal) and retracts (safe) on a fixed rhythm that never changes. Watch a cycle.",
            "W4_L6" => "DRAIN: water rises only while it's BLOCKED — a crate works. Bodies float; crates don't.",
            "W4_L7" => "TRUST PLATE: obeys only an Echo that trusts you. Trust grows each time you stand ON an Echo.",
            "W4_L8" => "WIND SWITCH: wind blows only WHILE the switch zone is held — it carries jumps further than legs do.",

            // World 5 — precision and identity
            "W5_L1" => "MEMORY LOCK: ONE body must touch every mark in order, alone, start to finish. Parked selves don't count as walkers.",
            "W5_L2" => "LEVER: charges while bodies sit in its zone — and the charge is KEPT across restarts.",
            "W5_L3" => "PASSWORD: visit the zones in the right sequence. Each run speaks; a recording repeats it forever.",
            "W5_L4" => "TWIN LOCKS: zones A and B pressed within a heartbeat of each other — then they latch open.",
            "W5_L5" => "COLOR GATES: each crate wears a color, each keyhole reads one. Carry (E) the match to its lock.",
            "W5_L6" => "ANTI-FIELD: any Echo that enters is erased on the spot. The live you passes untouched.",
            "W5_L7" => "COUNTERWEIGHT: the platform rises only WHILE the weight zone is held. Someone rides; someone holds.",
            "W5_L8" => "Everything at once: counted zones, freight, and selves that must STAY where they end.",

            // World 6 — synthesis
            "W6_L1" => "DRONE: hunts whichever Echo is most VIVID and deletes it on touch. Pistons keep their rhythm regardless.",
            "W6_L2" => "PENDULUM: swings on a fixed beat — a recorded ride replays perfectly. DECOY: chases whoever's nearest, slower than a sprint.",
            "W6_L3" => "TURRET: fires exactly when its gunner-Echo pressed F in the control zone. You author the pattern you'll dodge.",
            "W6_L4" => "MIRROR POINTS: every point must be occupied at the SAME moment to open the relay.",
            "W6_L5" => "MIRRORED PATH: you walk one route while an Echo walks its exact reflection — both must finish.",
            "W6_L6" => "SHIELD BEAM: kills exactly ONE body per shot, then recharges for 2 seconds. Someone goes first.",

            _ => "",
        };
    }
}
