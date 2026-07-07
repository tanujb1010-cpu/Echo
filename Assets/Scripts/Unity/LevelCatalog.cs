using System;
using System.Collections.Generic;

namespace Echo.Unity
{
    /// <summary>
    /// The ordered spine of the campaign: every level in play order, with its display title and world
    /// grouping. GameFlow walks this list for level select, unlock progression ("beat N to open N+1"),
    /// and next-level advancement. Factories (not instances) so a fresh LevelDefinition is built per load.
    /// </summary>
    public static class LevelCatalog
    {
        public readonly struct Entry
        {
            public readonly string Id;
            public readonly string Title;
            public readonly int World;
            public readonly Func<LevelDefinition> Create;
            public Entry(string id, string title, int world, Func<LevelDefinition> create)
            { Id = id; Title = title; World = world; Create = create; }
        }

        public static readonly IReadOnlyList<Entry> Levels = new List<Entry>
        {
            // World 1 — the verbs, one at a time
            new Entry("W1_L1", "First Light",   1, SampleLevels.World1Level1),
            new Entry("W1_L2", "Carry",         1, SampleLevels.World1Level2),
            new Entry("W1_L3", "Sacrifice",     1, SampleLevels.World1Level3),
            new Entry("W1_L4", "Quorum",        1, SampleLevels.World1Level4),
            new Entry("W1_L5", "Same Tick",     1, SampleLevels.World1Level5),
            new Entry("W1_L6", "Bounce",        1, SampleLevels.World1Level6),
            new Entry("W1_L7", "Phase",         1, SampleLevels.World1Level7),
            new Entry("W1_L8", "Momentum",      1, SampleLevels.World1Level8),
            // World 2 — space starts lying to you
            new Entry("W2_L1", "Doorways",      2, SampleLevels.World2Level1),
            new Entry("W2_L2", "Resonance",     2, SampleLevels.World2Level2),
            new Entry("W2_L3", "Polarity",      2, SampleLevels.World2Level3),
            new Entry("W2_L4", "Backwards",     2, SampleLevels.World2Level4),
            new Entry("W2_L5", "Beacons",       2, SampleLevels.World2Level5),
            new Entry("W2_L6", "Freight",       2, SampleLevels.World2Level6),
            // World 3 — new physics
            new Entry("W3_L1", "Balance",       3, SampleLevels.World3Level1),
            new Entry("W3_L2", "Radiant",       3, SampleLevels.World3Level2),
            new Entry("W3_L3", "Inversion",     3, SampleLevels.World3Level3),
            new Entry("W3_L4", "Trap",          3, SampleLevels.World3Level4),
            new Entry("W3_L5", "Crumble",       3, SampleLevels.World3Level5),
            new Entry("W3_L6", "Crank",         3, SampleLevels.World3Level6),
            new Entry("W3_L7", "Climb",         3, SampleLevels.World3Level7),
            new Entry("W3_L8", "Pulley",        3, SampleLevels.World3Level8),
            // World 4 — the facility pushes back
            new Entry("W4_L1", "Weight",        4, SampleLevels.World4Level1),
            new Entry("W4_L2", "Stack",         4, SampleLevels.World4Level2),
            new Entry("W4_L3", "Isolation",     4, SampleLevels.World4Level3),
            new Entry("W4_L4", "Cascade",       4, SampleLevels.World4Level4),
            new Entry("W4_L5", "Rhythm",        4, SampleLevels.World4Level5),
            new Entry("W4_L6", "Flood",         4, SampleLevels.World4Level6),
            new Entry("W4_L7", "Negotiation",   4, SampleLevels.World4Level7),
            new Entry("W4_L8", "Winds",         4, SampleLevels.World4Level8),
            // World 5 — precision and identity
            new Entry("W5_L1", "Memory",        5, SampleLevels.World5Level1),
            new Entry("W5_L2", "Cumulative",    5, SampleLevels.World5Level2),
            new Entry("W5_L3", "Password",      5, SampleLevels.World5Level3),
            new Entry("W5_L4", "Lock",          5, SampleLevels.World5Level4),
            new Entry("W5_L5", "Color",         5, SampleLevels.World5Level5),
            new Entry("W5_L6", "Anti",          5, SampleLevels.World5Level6),
            new Entry("W5_L7", "Elevator",      5, SampleLevels.World5Level7),
            new Entry("W5_L8", "Resonance II",  5, SampleLevels.World5Level8),
            // World 6 — everything at once
            new Entry("W6_L1", "Hazards",       6, SampleLevels.World6Level1),
            new Entry("W6_L2", "Gauntlet",      6, SampleLevels.World6Level2),
            new Entry("W6_L3", "Crossfire",     6, SampleLevels.World6Level3),
            new Entry("W6_L4", "Mirror",        6, SampleLevels.World6Level4),
            new Entry("W6_L5", "Path",          6, SampleLevels.World6Level5),
            new Entry("W6_L6", "Shield",        6, SampleLevels.World6Level6),
        };

        public static int Count => Levels.Count;

        public static int IndexOf(string levelId)
        {
            for (int i = 0; i < Levels.Count; i++) if (Levels[i].Id == levelId) return i;
            return -1;
        }

        public static bool IsLast(int index) => index == Levels.Count - 1;
    }
}
