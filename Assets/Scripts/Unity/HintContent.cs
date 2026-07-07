namespace Echo.Unity
{
    /// <summary>
    /// One hint per level, available from the pause menu (docs/03 "stuck is a state, not a wall").
    /// Hints name the *principle* the room is teaching, never the input sequence — the aha stays
    /// the player's. Pure data, same single-file-for-writers policy as <see cref="NarrativeContent"/>.
    /// </summary>
    public static class HintContent
    {
        public static string Hint(string levelId) => levelId switch
        {
            // World 1 — the verbs, one at a time
            "W1_L1" => "There is nothing here but walking and one door. Restart isn't failure — try it once, just to meet yourself.",
            "W1_L2" => "An Echo repeats exactly what you did — including standing somewhere useful while it waits for a door.",
            "W1_L3" => "Some hazards spend a body. A run whose only job is to be spent is still a good run.",
            "W1_L4" => "The zone counts bodies, and you are one of them — until you leave. A run that ENDS in the zone stands there forever.",
            "W1_L5" => "Two things must be true at once, and the road to one of them kills. A recording can afford to die — let it go first.",
            "W1_L6" => "The door is upstairs; its plate is downstairs. Two floors is just two places — and you come in pairs.",
            "W1_L7" => "An Echo only sees the Echoes recorded before it. If a run needs a bridge, the bridge-holder must already exist.",
            "W1_L8" => "A thrown crate flies; a dropped one doesn't. And the walk to the door isn't free — someone pays it first.",

            // World 2 — space starts lying to you
            "W2_L1" => "The portal moves you; it doesn't move your obligations. The plate still wants a body — one that stays.",
            "W2_L2" => "Passing a mark isn't tagging it. The sequence has its own geography: far first, then back.",
            "W2_L3" => "Nothing grabs metal, and the field never sleeps. But a slide can be BLOCKED — a body is a wall that shows up on schedule.",
            "W2_L4" => "Count the jobs: one clears the road, one holds the bridge, and you cross it three times. That's everyone.",
            "W2_L5" => "The beacons want an order, and the order climbs: floor, then the ledge (bounce up), then the far one — behind a door with its plate BEHIND your spawn point. Send an Echo left to stand on it before you go right.",
            "W2_L6" => "One recording can throw, walk, and stand guard — in that order. Write the whole shift, then clock in beside it.",

            // World 3 — new physics
            "W3_L1" => "The see-saw wants the SAME weight on both ends, held. And the road to the far end eats the first body that walks it — someone must die there first, on purpose, so the real holder can follow.",
            "W3_L2" => "Light has weight here. Stand where the beam should land, and be the thing it lands on.",
            "W3_L3" => "In the flip zone, down changes its mind. Enter with the speed you'll want on the ceiling, not the floor.",
            "W3_L4" => "The trap only springs once. Feed it something it can't hurt twice.",
            "W3_L5" => "Crumbling tiles remember one visit. The first crossing is a gift to no one — unless someone follows a different path.",
            "W3_L6" => "An Echo cranks only as long as its recording held F — the recording's LENGTH is the door's open window. Someone still has to reach the plate beyond it, and you still have to walk both doors before the window shuts.",
            "W3_L7" => "The wall is climbable in pieces. Each run can leave a body parked at a higher ledge — build a staircase of selves.",
            "W3_L8" => "Two on the rope, and they stay. The road past the gate kills the first walker — send a martyr before you stroll, and wait for their death before you follow.",

            // World 4 — the facility pushes back
            "W4_L1" => "The scale wants an EXACT count — three, no more. Your pile of sitters is also a wall: JUMP it, because walking through the zone makes you the fourth and slams the door. The last door still wants its own body.",
            "W4_L2" => "Bodies stack — a parked self is a stair. The plate floats at three-selves height, and holding it means someone stands on the tower's top and STAYS.",
            "W4_L3" => "The isolation field splits the room. Whatever must happen on the far side must be recorded before you commit.",
            "W4_L4" => "The chain's first zone is BEHIND you, and the cascade runs on its own clock once armed. The road ahead eats one body. Two Echoes, two jobs — every recording spends itself on exactly one.",
            "W4_L5" => "The pistons don't care where you are. Watch a full cycle doing nothing. The rhythm is the level.",
            "W4_L6" => "The water rises whether you're ready or not. What's unreachable now is a swim later — patience is a platform.",
            "W4_L7" => "The plate obeys only an Echo that TRUSTS you — and trust grows each time you stand on one. Ride your Echo like a platform before you ask it for a favor.",
            "W4_L8" => "The wind only blows while the switch is held. Your Echo is the weather. Time your flight to your own forecast.",

            // World 5 — precision and identity
            "W5_L1" => "The lock counts a single walker, start to finish — no relay teams. But a parked self is a step, and steps don't count as walkers.",
            "W5_L2" => "Each plate needs one more body than the last. This room is arithmetic — budget every run before you spend the first.",
            "W5_L3" => "The password is a sequence of stands. One run IS the password; speak it slowly enough to be quoted.",
            "W5_L4" => "The twin locks want two presses within a heartbeat. Record a press, then meet your recording at its moment.",
            "W5_L5" => "Each crate wears a color; each keyhole reads one. Carry (E) the match to its lock — the routing is the puzzle, and a wrong delivery wastes the walk.",
            "W5_L6" => "The anti-field erases Echoes that enter. Some ground only the original can walk. Plan what must be done in person.",
            "W5_L7" => "The elevator leaves on schedule, with or without you. Someone has to call it; someone has to catch it.",
            "W5_L8" => "Resonance again — but now the plates are far apart and the window is thin. Same tick, different rooms. Rehearse.",

            // World 6 — everything at once
            "W6_L1" => "Every hazard you've met, sharing one room. Nothing here is new. That's the mercy of it.",
            "W6_L2" => "The gauntlet is long and your Echoes die early in it. Spend the early runs on the early hazards — on purpose.",
            "W6_L3" => "Two turret corridors cross. A body standing in one blast shadow makes a door of it for the other.",
            "W6_L4" => "The mirrored halves demand symmetry. Your Echo already knows one half perfectly. Walk its reflection.",
            "W6_L5" => "The safe path is written in the floor — briefly. One run to read, one run to walk. Reading is a job.",
            "W6_L6" => "The beam takes exactly one body per shot, then breathes. Someone must be taken. Choose them, thank them, cross.",

            _ => "Whatever this room is asking, one of your past selves can hold half of it. Split the problem, then be both halves.",
        };
    }
}
