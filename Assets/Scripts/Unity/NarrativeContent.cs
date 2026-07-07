using Echo.Gameplay.Narrative;

namespace Echo.Unity
{
    /// <summary>
    /// The written story (docs/02): world intro cards shown on entering each world, the final-room choice
    /// copy, and the prose for all 10 endings resolved by <see cref="EndingResolver"/>. Pure data — the
    /// flow layer decides when to show it. Kept in one file so a writer can iterate without touching code.
    /// </summary>
    public static class NarrativeContent
    {
        public const string GameTitle = "ECHO";
        public const string GameSubtitle = "every run you finish walks beside you";

        /// <summary>Shown the first time the player enters each world: exactly one sentence of story,
        /// then one sentence of "what's new here" — read in five seconds, gone.</summary>
        public static string WorldIntro(int world) => world switch
        {
            1 => "The facility wakes when you do.\n\n" +
                 "NEW HERE — restarting doesn't erase your run: it replays beside you, and it can hold " +
                 "plates, block spikes, and be stood on.",

            2 => "Space starts lying to you.\n\n" +
                 "NEW HERE — portals, torch sequences, and cargo: your Echoes repeat what you DID, " +
                 "not what you meant.",

            3 => "The deeper floors run on stranger physics.\n\n" +
                 "NEW HERE — see-saws, flipped gravity, and floors that crumble behind you: almost " +
                 "nothing down here can be done alone.",

            4 => "The facility stops cooperating.\n\n" +
                 "NEW HERE — pistons, floods, and wind on rhythms that don't care where you are: " +
                 "spend early runs learning the timing.",

            5 => "Now it tests who you are, not what you can do.\n\n" +
                 "NEW HERE — split-second double-locks, erasure fields, and doors only a faithful " +
                 "recording can open.",

            6 => "Everything it knows, all at once.\n\n" +
                 "NEW HERE — nothing new: this floor combines all of it, and asks what you're " +
                 "willing to spend.",

            _ => "",
        };

        /// <summary>The final room: the choice the whole game has been building toward.</summary>
        public const string FinalRoomPrompt =
            "The last door is open. Behind you, every self you ever were stands in a quiet line.\n\n" +
            "The facility offers you the console. One input remains.";

        /// <summary>Player-facing labels. The last five only appear when the campaign earned them.</summary>
        public static string ChoiceLabel(FinalChoice c) => c switch
        {
            FinalChoice.Restart => "RESTART — begin again, keep everything as it was",
            FinalChoice.Release => "RELEASE — let every Echo walk out of the recording",
            FinalChoice.Erase => "ERASE — delete the braid, every self, including theirs",
            FinalChoice.Merge => "MERGE — stop being the exception; become one of them",
            FinalChoice.Refuse => "REFUSE — put the console down and simply leave",
            FinalChoice.YieldToFirst => "✦ YIELD TO FIRST — your oldest self is waiting; let it choose",
            FinalChoice.SideCaretaker => "✦ SIDE WITH THE CARETAKERS — hand your selves to gentler hands",
            FinalChoice.YieldToSaboteur => "✦ YIELD TO THE SABOTEUR — the one that always turned left",
            FinalChoice.WakeOrigin => "✦ WAKE THE ORIGIN — knock on the thing beneath the console",
            FinalChoice.Ritual => "✦ THE RITUAL — the input the console doesn't list",
            _ => c.ToString(),
        };

        /// <summary>Ending prose, keyed by the resolver's verdict.</summary>
        public static string EndingText(EndingId id) => id switch
        {
            EndingId.Restart =>
                "You press the key you've pressed ten thousand times.\n\n" +
                "The facility exhales. The line of selves shivers, and is gone, and is you, standing at " +
                "the first door with a familiar itch in your hands.\n\nSomewhere, faintly, someone starts walking.",

            EndingId.Release =>
                "One by one the recordings end — not deleted: finished. Each self flickers as its loop " +
                "closes on its own terms, a run allowed, finally, to be over.\n\n" +
                "The last one waves. You don't know which run it was. You wave back anyway.\n\n" +
                "The facility is very quiet, and for the first time, so are you.",

            EndingId.Erasure =>
                "You choose the clean floor.\n\n" +
                "The braid unwinds fast — days of your timing, your patience, your small heroisms, " +
                "gone in the order you spent them. When it finishes there is exactly one of you, " +
                "which is what you asked for.\n\nIt is quieter than you hoped. It is not peace.",

            EndingId.Communion =>
                "You step into the line.\n\n" +
                "It doesn't feel like ending; it feels like arriving late to a conversation that was " +
                "always about you. Ten thousand runs look back with your eyes and, at last, none of " +
                "them are waiting for orders.\n\nThe facility files you, gently, under 'complete.'",

            EndingId.Refusal =>
                "You put the console down.\n\n" +
                "It was a trick, of course — not the choice, the *having* to choose. The door behind " +
                "the last door was never locked. You walk out with every self still walking somewhere " +
                "behind the walls, unfinished, unresolved, alive.\n\nThe facility does not stop you. It takes notes.",

            EndingId.FirstsEnding =>
                "First — the self from your very first run, the one that walked before it knew it was " +
                "being kept — meets you at the console.\n\n" +
                "You yield it your place. It has been repeating your oldest mistake for so long that " +
                "watching it choose something new feels like being forgiven.\n\nIt presses nothing. It just stops walking.",

            EndingId.CaretakersPeace =>
                "You side with the drones.\n\n" +
                "They were never hunting your Echoes — they were tending them, culling the loops that " +
                "hurt to run. You hand over the braid and the caretakers carry your selves away like " +
                "sleeping children.\n\nThe facility dims to a hum. Someone else's problem now. Someone kinder.",

            EndingId.OriginWakes =>
                "The console was a lid.\n\n" +
                "Beneath it, the Origin — the first recorder, the machine every Echo is a copy of a " +
                "copy of — opens one enormous eye. It has been dreaming you, all of you, the whole time.\n\n" +
                "It is very glad you finally knocked.",

            EndingId.Saboteur =>
                "You yield to the one that always turned left when you said right.\n\n" +
                "The Saboteur takes the console grinning with your mouth. Every lock in the facility " +
                "opens at once; every recording starts improvising.\n\n" +
                "It was never trying to ruin your runs. It was trying to show you they were yours to ruin.",

            EndingId.Quiet =>
                "You found all of it — every secret, every room the facility hoped you'd skip — and so " +
                "you know the ritual, the one input the console doesn't list.\n\n" +
                "You enter it. Nothing dramatic happens. The lights stay on. The selves keep walking.\n\n" +
                "But nothing is recording anymore. Whatever happens next is the only time it will ever happen.",

            _ => "",
        };
    }
}
