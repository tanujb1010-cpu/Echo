using System.Collections.Generic;
using UnityEngine;

namespace Echo.Unity
{
    /// <summary>
    /// Code-built sample levels so the project is playable the moment you press Play (no asset authoring
    /// required). Mirrors the headless-tested "Held Plate" puzzle. Designers later replace these with
    /// authored <see cref="LevelDefinition"/> assets.
    /// </summary>
    public static class SampleLevels
    {
        /// <summary>W1-L1 "First Light": a past self holds a plate (left) to open a door (right).</summary>
        public static LevelDefinition World1Level1()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W1_L1";
            def.SaveSeed = 0x5EED;
            def.Width = 48; def.Height = 24; def.FloorRow = 0;
            def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 3;
            def.EnabledGates = Echo.Core.Echo.GateMask.None; // tutorial: Echoes stay perfectly obedient

            def.Plates = new List<LevelDefinition.PlateDef>
            {
                new LevelDefinition.PlateDef { LinkId = 1, Min = new Vector2(3f, 0.9f), Max = new Vector2(6f, 2f) },
            };
            def.Doors = new List<LevelDefinition.DoorDef>
            {
                new LevelDefinition.DoorDef { LinkId = 1, Center = new Vector2(20.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W1-L2 "Crate Expectations": carry a crate onto a plate (mechanic #3). Headless-tested.</summary>
        public static LevelDefinition World1Level2()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W1_L2"; def.SaveSeed = 0x5EED2;
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 3; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            def.Crates = new List<LevelDefinition.CrateDef>
            {
                new LevelDefinition.CrateDef { Position = new Vector2(7f, 1.4f), HalfExtents = new Vector2(0.4f, 0.4f) },
            };
            def.Plates = new List<LevelDefinition.PlateDef>
            {
                new LevelDefinition.PlateDef { LinkId = 1, Min = new Vector2(2.5f, 0.9f), Max = new Vector2(5.5f, 2.2f) },
            };
            def.Doors = new List<LevelDefinition.DoorDef>
            {
                new LevelDefinition.DoorDef { LinkId = 1, Center = new Vector2(20.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W1-L3 "Martyr": sacrifice a past self to a one-shot hazard (mechanic #42). Headless-tested.</summary>
        public static LevelDefinition World1Level3()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W1_L3"; def.SaveSeed = 0x5EED3;
            def.Secrets = new List<LevelDefinition.SecretDef> { new LevelDefinition.SecretDef { Id = 1, Min = new Vector2(1f, 0.9f), Max = new Vector2(2.5f, 2.2f) } }; // walk LEFT from spawn — away from everything
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 3; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            def.Hazards = new List<LevelDefinition.HazardDef>
            {
                new LevelDefinition.HazardDef { Min = new Vector2(15f, 0.9f), Max = new Vector2(16.5f, 3.5f), Consumable = true },
            };
            return def;
        }

        /// <summary>W1-L4 "All Hands": the door needs THREE bodies in the zone at once (mechanic #8 Quorum)
        /// and the budget is exactly three. FALSE-OBVIOUS: you + two Echoes = three, the door opens — but
        /// the moment you leave to cross, the count drops and it slams. All three Echoes must END their
        /// runs standing in the zone, leaving you free. This is the level that teaches "end your run where
        /// you want your ghost stationed" — and, if you wasted a run exploring, pruning. Quorum semantics
        /// proven in EchoHarness TestQuorumDoor/TestResonance (threshold counts any bodies, live included).</summary>
        public static LevelDefinition World1Level4()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W1_L4"; def.SaveSeed = 0x5EED4;
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 3; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            def.Quorums = new List<LevelDefinition.QuorumDef>
            {
                new LevelDefinition.QuorumDef { LinkId = 1, Min = new Vector2(8f, 0.9f), Max = new Vector2(12f, 2.5f), Threshold = 3 },
            };
            def.Doors = new List<LevelDefinition.DoorDef>
            {
                new LevelDefinition.DoorDef { LinkId = 1, Center = new Vector2(20.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W1-L5 "In Step": two switches, far apart, must be held on the SAME tick (mechanic #5) —
        /// and the road to the far switch is lethal (consumable hazard, mechanic #42). FALSE-OBVIOUS: park
        /// an Echo on switch A and sprint to B — you die on the way. A martyr run must clear the hazard
        /// FIRST, and it re-dies there every replay, so the live self must let its ghost go ahead — the
        /// first real echo-TIMING read. Same-tick latch proven in EchoHarness TestSameTickSwitch; the
        /// echo-dies-first shared-tick rule proven in TestConsumableSharedTick.</summary>
        public static LevelDefinition World1Level5()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W1_L5"; def.SaveSeed = 0x5EED5;
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 2; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            def.Hazards = new List<LevelDefinition.HazardDef>
            {
                new LevelDefinition.HazardDef { Min = new Vector2(17f, 0.9f), Max = new Vector2(18.5f, 3.5f), Consumable = true },
            };
            def.Switches = new List<LevelDefinition.SwitchDef>
            {
                new LevelDefinition.SwitchDef { LinkId = 1, Min = new Vector2(4f, 0.9f), Max = new Vector2(6f, 2.2f) },   // switch A (safe side)
                new LevelDefinition.SwitchDef { LinkId = 1, Min = new Vector2(24f, 0.9f), Max = new Vector2(26f, 2.2f) }, // switch B (past the hazard)
            };
            def.SwitchDoors = new List<LevelDefinition.DoorDef>
            {
                new LevelDefinition.DoorDef { LinkId = 1, Center = new Vector2(35f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W1-L6 "Springboard": the bounce pad (mechanic #15, apex ~6.9 vs jump ~3.6) is the only
        /// way up, the ledge is the only way across — and a door ON the ledge answers to a plate on the
        /// floor BELOW it (mechanic #1). Two layers, two selves: one stays downstairs, one crosses upstairs.</summary>
        public static LevelDefinition World1Level6()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W1_L6"; def.SaveSeed = 0x5EED6;
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 2; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            // The high road is now the ONLY road: a wall under the ledge's far end seals the floor route,
            // and a door ON the ledge (y6..10) is held open by a plate on the floor — someone stays
            // downstairs while you cross upstairs. The plate sits BEFORE the pad so the plate-runner never
            // gets launched by accident. FALSE-OBVIOUS: stand on the plate yourself, bounce up — the door
            // re-closed the moment you left. Full traversal proven in EchoHarness TestLedgeDoorHold.
            def.BouncePads = new List<LevelDefinition.BouncePadDef>
            {
                // Flush with the floor (top y=1.0): pads only fire on a body RESTING on top, and the
                // motor can't step up a raised lip — flush means simply walking over it launches you.
                new LevelDefinition.BouncePadDef { Center = new Vector2(25f, 0.8f), HalfExtents = new Vector2(0.6f, 0.2f) },
            };
            def.Solids = new List<LevelDefinition.SolidRect>
            {
                new LevelDefinition.SolidRect { X = 27, Y = 5, W = 13, H = 1 }, // the ledge (top y=6), 1.4u past the pad
                new LevelDefinition.SolidRect { X = 40, Y = 1, W = 2, H = 4 },  // seals the floor route under the ledge's end
            };
            def.Plates = new List<LevelDefinition.PlateDef>
            {
                new LevelDefinition.PlateDef { LinkId = 1, Min = new Vector2(18f, 0.9f), Max = new Vector2(21f, 2.2f) }, // floor, short of the pad
            };
            def.Doors = new List<LevelDefinition.DoorDef>
            {
                new LevelDefinition.DoorDef { LinkId = 1, Center = new Vector2(34.5f, 8f), HalfExtents = new Vector2(0.5f, 2f) }, // blocks the ledge walk
            };
            return def;
        }

        /// <summary>W1-L7 "Bridge of Self": two pits, two phase bridges (mechanic #13) — and the second
        /// bridge's plate is on the island between them, so recording ORDER is the puzzle: bridge 1's
        /// holder must exist before bridge 2's holder can even reach its post.</summary>
        public static LevelDefinition World1Level7()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W1_L7"; def.SaveSeed = 0x5EED7;
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 2; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            // TWO pits, TWO plates — and plate 2 is on the island BETWEEN them. Bridge 1's holder can be
            // recorded any time, but bridge 2's holder must CROSS bridge 1 during its own recording, which
            // only works if the first Echo already exists. FALSE-OBVIOUS: record the island run first — that
            // recording walks into an unbridged pit forever. Teaches the braid's core invariant: Echoes only
            // see the Echoes recorded BEFORE them. Traversal proven in EchoHarness TestTwoGapBridges.
            // 8-wide gaps: max jump reach incl. coyote time is ~6.8u (the cheese audit measured hop
            // bots crossing 6-wide pits), so 8 is unhoppable, period. The phase bridges are the road.
            def.FloorGaps = new List<LevelDefinition.FloorGapDef>
            {
                new LevelDefinition.FloorGapDef { X = 12, W = 8 },
                new LevelDefinition.FloorGapDef { X = 24, W = 8 },
            };
            def.Plates = new List<LevelDefinition.PlateDef>
            {
                new LevelDefinition.PlateDef { LinkId = 1, Min = new Vector2(3f, 0.9f), Max = new Vector2(5f, 2.2f) },      // holds bridge 1 (safe side)
                new LevelDefinition.PlateDef { LinkId = 2, Min = new Vector2(20.5f, 0.9f), Max = new Vector2(22.5f, 2.2f) }, // holds bridge 2 (on the island)
            };
            def.Doors = new List<LevelDefinition.DoorDef>
            {
                // Invert = Phase Platform: solid ONLY while its plate is held.
                new LevelDefinition.DoorDef { LinkId = 1, Center = new Vector2(15.5f, 0.6f), HalfExtents = new Vector2(4.5f, 0.4f), Invert = true },
                new LevelDefinition.DoorDef { LinkId = 2, Center = new Vector2(27.5f, 0.6f), HalfExtents = new Vector2(4.5f, 0.4f), Invert = true },
            };
            return def;
        }

        /// <summary>W1-L8 "Momentum Bank": throw a crate — only a THROWN crate (not a gentle drop) lands in
        /// the target zone (mechanic #10). Crate geometry and target zone mirror EchoHarness TestMomentumThrow
        /// exactly (proven: thrown lands ~X23.1 inside the zone; dropped lands ~X18.7, outside it).</summary>
        public static LevelDefinition World1Level8()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W1_L8"; def.SaveSeed = 0x5EED8;
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 2; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            def.Crates = new List<LevelDefinition.CrateDef>
            {
                new LevelDefinition.CrateDef { Position = new Vector2(12f, 1.4f), HalfExtents = new Vector2(0.4f, 0.4f) },
            };
            def.Plates = new List<LevelDefinition.PlateDef>
            {
                // Catches the thrown landing spot (~23.1); a gentle drop (~18.7) falls short of it.
                new LevelDefinition.PlateDef { LinkId = 1, Min = new Vector2(22f, 0.9f), Max = new Vector2(25f, 2.2f) },
            };
            // W1 finale: the throw opens the door (thrown-vs-dropped proven in TestMomentumThrow), and a
            // consumable hazard guards the stretch between plate and door — the whole world's toolkit in
            // one room: throw + sacrifice + timing. FALSE-OBVIOUS: a clean throw, a proud walk, a red flash.
            def.Hazards = new List<LevelDefinition.HazardDef>
            {
                new LevelDefinition.HazardDef { Min = new Vector2(28f, 0.9f), Max = new Vector2(29.5f, 3.5f), Consumable = true },
            };
            def.Doors = new List<LevelDefinition.DoorDef>
            {
                new LevelDefinition.DoorDef { LinkId = 1, Center = new Vector2(35f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W2-L1 "Threshold": the portal (mechanic #34, proven in TestPortals) bypasses the wall,
        /// but the door beyond it answers to a plate back on the spawn side (mechanic #1) — space lies,
        /// the braid doesn't.</summary>
        public static LevelDefinition World2Level1()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W2_L1"; def.SaveSeed = 0x5EED9;
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 2; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            def.Solids = new List<LevelDefinition.SolidRect> { new LevelDefinition.SolidRect { X = 15, Y = 1, W = 2, H = 12 } };
            def.Portals = new List<LevelDefinition.PortalPairDef>
            {
                new LevelDefinition.PortalPairDef
                {
                    AMin = new Vector2(12f, 0.9f), AMax = new Vector2(13f, 2.5f), APoint = new Vector2(12f, 2f),
                    BMin = new Vector2(40f, 0f), BMax = new Vector2(41f, 1f), BPoint = new Vector2(20f, 2f),
                },
            };
            // W2 opener: the portal beats the wall, but the door BEYOND it answers to a plate back on the
            // spawn side. FALSE-OBVIOUS: stand on the plate, watch the far door open, portal through — and
            // arrive at a door that closed the moment you stepped off. W1_L1's lesson, at teleport range.
            def.Plates = new List<LevelDefinition.PlateDef>
            {
                new LevelDefinition.PlateDef { LinkId = 1, Min = new Vector2(5f, 0.9f), Max = new Vector2(7f, 2.2f) },
            };
            def.Doors = new List<LevelDefinition.DoorDef>
            {
                new LevelDefinition.DoorDef { LinkId = 1, Center = new Vector2(30.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W2-L2 "Roll Call": three Echoes hold a quorum zone (mechanic #8, threshold proven in
        /// TestResonance) while the live self threads arrival checkpoints in far-then-near order
        /// (mechanic #24) — station-keeping behind you, backtracking ahead of you.</summary>
        public static LevelDefinition World2Level2()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W2_L2"; def.SaveSeed = 0x5EEDA;
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 3; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            // Three Echoes hold the quorum zone behind you while you thread the checkpoints in an order
            // that contradicts the geography: FAR mark first, NEAR mark second. FALSE-OBVIOUS: assuming
            // the near mark counted when you brushed past it on the way out (early touches are noops —
            // proven forgiving in EchoHarness TestArrivalOrder). W1_L4's station-keeping plus backtracking.
            // Zone width matters: three 0.8-wide bodies entering from the spawn side PILE UP, spanning
            // ~2.4u rightward from wherever the first one parks. The original 3-wide zone (6..9) let the
            // third body straddle the lip and miss the strict overlap — door shut forever (playtest bug).
            def.Quorums = new List<LevelDefinition.QuorumDef>
            {
                new LevelDefinition.QuorumDef { LinkId = 1, Min = new Vector2(4f, 0.9f), Max = new Vector2(9.5f, 2.5f), Threshold = 3 },
            };
            def.Doors = new List<LevelDefinition.DoorDef>
            {
                new LevelDefinition.DoorDef { LinkId = 1, Center = new Vector2(13.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            def.ArrivalCheckpoints = new List<LevelDefinition.CheckpointDef>
            {
                new LevelDefinition.CheckpointDef { Min = new Vector2(22f, 0.9f), Max = new Vector2(24f, 2.5f) }, // required 1st — the far one
                new LevelDefinition.CheckpointDef { Min = new Vector2(16f, 0.9f), Max = new Vector2(18f, 2.5f) }, // required 2nd — the near one
            };
            def.ArrivalDoors = new List<LevelDefinition.ArrivalDoorDef>
            {
                new LevelDefinition.ArrivalDoorDef { LinkId = 1, Center = new Vector2(30.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W2-L3 "Lodestone": the magnet (mechanic #20) drags the metal crate OFF the plate — an
        /// Echo must stand as a pin so the crate wedges against it, on the plate (proven in
        /// TestMagnetEchoPin). Bodies as architecture.</summary>
        public static LevelDefinition World2Level3()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W2_L3"; def.SaveSeed = 0x5EEDB;
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 2; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            // The magnet is the THIEF, not the solution: the crate spawns BEHIND you and the field drags
            // it rightward through the plate zone and away, forever. Metal can't be grabbed — the only
            // wall that can stop the slide is a body. An Echo stands just past the plate as a pin; the
            // crate wedges against it, ON the plate; you hop over both. FALSE-OBVIOUS: chase the crate.
            // The crate starts behind spawn so the pin-runner's short walk never crosses its path, and
            // the transit's brief door-open window closes ~0.8 s before a spawn-sprinter could reach the
            // door. Pin position, door state, and the hop proven in EchoHarness TestMagnetEchoPin.
            def.Metals = new List<LevelDefinition.MetalDef>
            {
                new LevelDefinition.MetalDef { Position = new Vector2(4f, 1.4f), HalfExtents = new Vector2(0.4f, 0.4f) },
            };
            def.Magnets = new List<LevelDefinition.MagnetDef>
            {
                new LevelDefinition.MagnetDef { Position = new Vector2(20f, 1.4f), Radius = 18f, Strength = 80f },
            };
            // A QUORUM (threshold 2), not a plate: the crate transiting alone is one body and never
            // opens the door — cheese-audited; a waiting player can't just catch the transit window.
            // Crate + pin-Echo together = two, held forever.
            def.Quorums = new List<LevelDefinition.QuorumDef>
            {
                new LevelDefinition.QuorumDef { LinkId = 1, Min = new Vector2(11f, 0.9f), Max = new Vector2(13.5f, 2.2f), Threshold = 2 },
            };
            def.Doors = new List<LevelDefinition.DoorDef>
            {
                new LevelDefinition.DoorDef { LinkId = 1, Center = new Vector2(30.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W2-L4 "Waypoints": reversed arrival order (mechanic #24) ACROSS a phase bridge (#13)
        /// AND past a consumable hazard (#42) — martyr, bridge-holder, and a there-and-back-and-out-again
        /// crossing. The W2 midterm.</summary>
        public static LevelDefinition World2Level4()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W2_L4"; def.SaveSeed = 0x5EEDC;
            def.Secrets = new List<LevelDefinition.SecretDef> { new LevelDefinition.SecretDef { Id = 2, Min = new Vector2(1f, 0.9f), Max = new Vector2(2.5f, 2.2f) } }; // in the room about arriving backwards, the secret is behind you
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 2; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            // The far checkpoint lives across a pit whose bridge only exists while a plate is held, and a
            // consumable hazard guards the road — so the braid needs a martyr AND a bridge-holder, and the
            // player crosses that bridge THREE times (out, back for the near mark, out again). Every W2
            // mechanic so far, one room. FALSE-OBVIOUS: sprint for the far mark; die at x19.
            def.Plates = new List<LevelDefinition.PlateDef>
            {
                new LevelDefinition.PlateDef { LinkId = 1, Min = new Vector2(3f, 0.9f), Max = new Vector2(5f, 2.2f) },
            };
            // 8-wide gap: beyond any jump's reach (see W1_L7's note) — the phase bridge is the road.
            def.FloorGaps = new List<LevelDefinition.FloorGapDef> { new LevelDefinition.FloorGapDef { X = 24, W = 8 } };
            def.Doors = new List<LevelDefinition.DoorDef>
            {
                new LevelDefinition.DoorDef { LinkId = 1, Center = new Vector2(27.5f, 0.6f), HalfExtents = new Vector2(4.5f, 0.4f), Invert = true },
            };
            def.Hazards = new List<LevelDefinition.HazardDef>
            {
                new LevelDefinition.HazardDef { Min = new Vector2(19f, 0.9f), Max = new Vector2(20.5f, 3.5f), Consumable = true },
            };
            def.ArrivalCheckpoints = new List<LevelDefinition.CheckpointDef>
            {
                new LevelDefinition.CheckpointDef { Min = new Vector2(33f, 0.9f), Max = new Vector2(35f, 2.5f) }, // required 1st — beyond the pit
                new LevelDefinition.CheckpointDef { Min = new Vector2(14f, 0.9f), Max = new Vector2(16f, 2.5f) }, // required 2nd — near, before the hazard
            };
            def.ArrivalDoors = new List<LevelDefinition.ArrivalDoorDef>
            {
                new LevelDefinition.ArrivalDoorDef { LinkId = 1, Center = new Vector2(38.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W2-L5 "Beacons": three torches lit in order by Interact (mechanic #28, wrong press
        /// snuffs all — proven in TestTorchSequence), with beacon 2 up a bounce-pad ledge (#15) and beacon 3
        /// behind a plate-door (#1). The lighting run is a choreography, not a stroll.</summary>
        public static LevelDefinition World2Level5()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W2_L5"; def.SaveSeed = 0x5EEDD;
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 2; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            // Beacon 2 is up on a ledge (bounce pad to reach it) and beacon 3 is behind a plate-door —
            // so the lighting run needs a plate-holder recorded first, and the ORDER (floor, ledge, floor)
            // forces a bounce mid-sequence. Wrong-order presses snuff everything (TorchSequenceModule),
            // so the run has to be REASONED, not sprinted. FALSE-OBVIOUS: bounce to the shiny high one first.
            def.Solids = new List<LevelDefinition.SolidRect>
            {
                new LevelDefinition.SolidRect { X = 22, Y = 5, W = 4, H = 1 }, // beacon 2's ledge (top y=6), 1.4u past the pad
            };
            def.BouncePads = new List<LevelDefinition.BouncePadDef>
            {
                // Flush with the floor: walking over it launches (see W1_L6 note).
                new LevelDefinition.BouncePadDef { Center = new Vector2(20f, 0.8f), HalfExtents = new Vector2(0.6f, 0.2f) },
            };
            def.Torches = new List<LevelDefinition.TorchDef>
            {
                new LevelDefinition.TorchDef { Position = new Vector2(14f, 1.45f), Radius = 1.5f }, // 1st — floor
                new LevelDefinition.TorchDef { Position = new Vector2(24f, 6.7f), Radius = 1.5f },  // 2nd — on the ledge
                new LevelDefinition.TorchDef { Position = new Vector2(34f, 1.45f), Radius = 1.5f }, // 3rd — behind the plate-door
            };
            def.Plates = new List<LevelDefinition.PlateDef>
            {
                new LevelDefinition.PlateDef { LinkId = 1, Min = new Vector2(2f, 0.9f), Max = new Vector2(4f, 2.2f) },
            };
            def.Doors = new List<LevelDefinition.DoorDef>
            {
                new LevelDefinition.DoorDef { LinkId = 1, Center = new Vector2(30.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            def.TorchDoors = new List<LevelDefinition.TorchDoorDef>
            {
                new LevelDefinition.TorchDoorDef { LinkId = 1, Center = new Vector2(38.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W2-L6 "Freight": the W2 finale — one recording throws the crate onto the plate (#10,
        /// proven in TestMomentumThrow), walks through the door it opened, and ends standing on the far
        /// switch; the live self takes the near switch (same-tick latch, #5) and walks through its own
        /// clockwork. One ghost, many jobs.</summary>
        public static LevelDefinition World2Level6()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W2_L6"; def.SaveSeed = 0x5EEDE;
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 2; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            // W2 finale — ONE recording does every job: throw the crate onto the plate (opens the mid
            // door), walk through, and END standing on the far switch. The live run then stands on the
            // near switch (same-tick latch — impossible solo, the switches are 30 units apart) and strolls
            // through its own clockwork. FALSE-OBVIOUS: doing it all live and finding the far switch dead.
            def.Crates = new List<LevelDefinition.CrateDef>
            {
                new LevelDefinition.CrateDef { Position = new Vector2(12f, 1.4f), HalfExtents = new Vector2(0.4f, 0.4f) },
            };
            def.Plates = new List<LevelDefinition.PlateDef>
            {
                // Catches a running throw (~23.1, proven in TestMomentumThrow); a drop falls short.
                new LevelDefinition.PlateDef { LinkId = 1, Min = new Vector2(22f, 0.9f), Max = new Vector2(25f, 2.2f) },
            };
            def.Doors = new List<LevelDefinition.DoorDef>
            {
                new LevelDefinition.DoorDef { LinkId = 1, Center = new Vector2(30.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            def.Switches = new List<LevelDefinition.SwitchDef>
            {
                new LevelDefinition.SwitchDef { LinkId = 1, Min = new Vector2(2f, 0.9f), Max = new Vector2(4f, 2.2f) },   // near — the live self
                new LevelDefinition.SwitchDef { LinkId = 1, Min = new Vector2(34f, 0.9f), Max = new Vector2(36f, 2.2f) }, // far — the recording ends here
            };
            def.SwitchDoors = new List<LevelDefinition.DoorDef>
            {
                new LevelDefinition.DoorDef { LinkId = 1, Center = new Vector2(40.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W3-L1 "Balance": two pressure pads must be held simultaneously (mechanic #29). The pads
        /// are far apart, and the exit door is beyond both — so two Echoes hold the pair while the player
        /// walks. Module behavior is proven in EchoHarness TestPressureBalance.</summary>
        public static LevelDefinition World3Level1()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W3_L1"; def.SaveSeed = 0x5EEDF;
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 3; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            // Difficulty pass (2026-07-07): the road to pad B is now lethal — a martyr run has to clear
            // the hazard BEFORE the pad-B holder's recording walks it (braid order, per W1_L5/W1_L7).
            // Jobs: martyr + pad A + pad B = exactly the budget. FALSE-OBVIOUS: sprint for pad B → die.
            def.PressurePairs = new List<LevelDefinition.PressurePairDef>
            {
                new LevelDefinition.PressurePairDef
                {
                    LinkId = 1,
                    AMin = new Vector2(4f, 0.9f), AMax = new Vector2(6f, 2.2f),
                    BMin = new Vector2(30f, 0.9f), BMax = new Vector2(32f, 2.2f),
                },
            };
            def.Hazards = new List<LevelDefinition.HazardDef>
            {
                new LevelDefinition.HazardDef { Min = new Vector2(22f, 0.9f), Max = new Vector2(23.5f, 3.5f), Consumable = true },
            };
            def.PressureDoors = new List<LevelDefinition.PressureDoorDef>
            {
                new LevelDefinition.PressureDoorDef { LinkId = 1, Center = new Vector2(40.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W3-L2 "Radiant": a Light-Solid platform bridges a pit but is only solid while a held
        /// lantern is near (mechanic #40). Hold Lantern as you cross — and note your Echo's recording
        /// carries its own lantern light, so it can cross for you.</summary>
        public static LevelDefinition World3Level2()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W3_L2"; def.SaveSeed = 0x5EEE0;
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 2; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            // Pit under x=18..22; the light platform sits flush with the floor line (top at y=1) so a lit
            // crossing feels like ordinary ground and an unlit one drops you.
            def.FloorGaps = new List<LevelDefinition.FloorGapDef> { new LevelDefinition.FloorGapDef { X = 18, W = 4 } };
            def.LightPlatforms = new List<LevelDefinition.LightPlatformDef>
            {
                new LevelDefinition.LightPlatformDef { Center = new Vector2(20f, 0.6f), HalfExtents = new Vector2(2f, 0.4f) },
            };
            def.Plates = new List<LevelDefinition.PlateDef>
            {
                new LevelDefinition.PlateDef { LinkId = 1, Min = new Vector2(30f, 0.9f), Max = new Vector2(32f, 2.5f) },
            };
            def.Doors = new List<LevelDefinition.DoorDef>
            {
                new LevelDefinition.DoorDef { LinkId = 1, Center = new Vector2(40.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W3-L3 "Inversion": a gravity-flip zone (mechanic #37) is the only way over a wall too
        /// tall to jump (top y=7 vs. jump reach ~3.2). Step into the zone, float up, drift right with air
        /// control, and drop past the wall onto the plate.</summary>
        public static LevelDefinition World3Level3()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W3_L3"; def.SaveSeed = 0x5EEE1;
            def.Secrets = new List<LevelDefinition.SecretDef> { new LevelDefinition.SecretDef { Id = 3, Min = new Vector2(14.5f, 8f), Max = new Vector2(16.5f, 10f) } }; // hang at the flip zone's left edge instead of drifting right
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 2; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            def.Solids = new List<LevelDefinition.SolidRect>
            {
                new LevelDefinition.SolidRect { X = 22, Y = 1, W = 1, H = 6 }, // wall y=1..7: unjumpable
            };
            def.GravityZones = new List<LevelDefinition.GravityZoneDef>
            {
                // Reaches the ground so walking in flips you; capped at y=10 so you pop out above the wall.
                // Max.x abuts the wall face (x=22) so there is no dead slot to get stuck in at ground level.
                new LevelDefinition.GravityZoneDef { Min = new Vector2(14f, 0f), Max = new Vector2(22f, 10f) },
            };
            def.Plates = new List<LevelDefinition.PlateDef>
            {
                new LevelDefinition.PlateDef { LinkId = 1, Min = new Vector2(26f, 0.9f), Max = new Vector2(28f, 2.2f) },
            };
            def.Doors = new List<LevelDefinition.DoorDef>
            {
                new LevelDefinition.DoorDef { LinkId = 1, Center = new Vector2(40.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W3-L4 "Trap": a delayed-charge blast waits for your input, then fires when you're in position
        /// (mechanic #11). Arm it early, approach the plate from safety, then trigger it once you're ready.</summary>
        public static LevelDefinition World3Level4()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W3_L4"; def.SaveSeed = 0x5EEE2;
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 2; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            def.DelayedCharges = new List<LevelDefinition.DelayedChargeDef>
            {
                new LevelDefinition.DelayedChargeDef
                {
                    ArmMin = new Vector2(4f, 0.9f), ArmMax = new Vector2(6f, 2.2f),
                    BlastMin = new Vector2(28f, 0.9f), BlastMax = new Vector2(32f, 3.5f),
                    FuseTicks = 60
                },
            };
            def.Plates = new List<LevelDefinition.PlateDef>
            {
                new LevelDefinition.PlateDef { LinkId = 1, Min = new Vector2(30f, 0.9f), Max = new Vector2(32f, 2.2f) },
            };
            def.Doors = new List<LevelDefinition.DoorDef>
            {
                new LevelDefinition.DoorDef { LinkId = 1, Center = new Vector2(40.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W3-L5 "Crumble": floating tiles bridge a wide pit and collapse shortly after being
        /// stepped on (mechanic #30). The plate sits BEFORE the pit — the Echo holds it from safety while
        /// the player hops the crumbling bridge alone (tiles crumble per run, so an Echo crossing first
        /// would leave the player nothing to stand on).</summary>
        public static LevelDefinition World3Level5()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W3_L5"; def.SaveSeed = 0x5EEE3;
            def.Secrets = new List<LevelDefinition.SecretDef> { new LevelDefinition.SecretDef { Id = 4, Min = new Vector2(1f, 0.9f), Max = new Vector2(2.5f, 2.2f) } };
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 2; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            def.FloorGaps = new List<LevelDefinition.FloorGapDef> { new LevelDefinition.FloorGapDef { X = 14, W = 14 } };
            def.CrumbleTiles = new List<LevelDefinition.CrumbleTileDef>
            {
                // Tops flush with the floor line (y=1); ~3.5-unit hops between tiles, well inside jump range.
                new LevelDefinition.CrumbleTileDef { Center = new Vector2(16f, 0.6f), HalfExtents = new Vector2(0.5f, 0.4f), FuseTicks = 30 },
                new LevelDefinition.CrumbleTileDef { Center = new Vector2(20f, 0.6f), HalfExtents = new Vector2(0.5f, 0.4f), FuseTicks = 30 },
                new LevelDefinition.CrumbleTileDef { Center = new Vector2(24f, 0.6f), HalfExtents = new Vector2(0.5f, 0.4f), FuseTicks = 30 },
            };
            def.Plates = new List<LevelDefinition.PlateDef>
            {
                new LevelDefinition.PlateDef { LinkId = 1, Min = new Vector2(6f, 0.9f), Max = new Vector2(8f, 2.2f) },
            };
            def.Doors = new List<LevelDefinition.DoorDef>
            {
                new LevelDefinition.DoorDef { LinkId = 1, Center = new Vector2(40.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W3-L6 "Crank": a door opens ONLY while you hold interact in a crank zone (mechanic #31).
        /// Release and it closes. Coordinate with an Echo to hold the crank while you cross.</summary>
        public static LevelDefinition World3Level6()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W3_L6"; def.SaveSeed = 0x5EEE4;
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 2; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            // Difficulty pass (2026-07-07): the crank is a TIMED window — an Echo only holds F for as
            // long as its recording lasted (past the end it idles), so the crank door is open exactly
            // that long. Behind it: a plate + second door. Braid: run 1 cranks LONG; run 2 walks the
            // open door and station-keeps the plate; live crosses both inside run 1's window.
            // FALSE-OBVIOUS: tap-crank (short recording) → the door shuts before anyone's through.
            def.CrankZones = new List<LevelDefinition.CrankZoneDef>
            {
                new LevelDefinition.CrankZoneDef { LinkId = 1, Min = new Vector2(3f, 0.9f), Max = new Vector2(5f, 2.2f) },
            };
            def.CrankDoors = new List<LevelDefinition.CrankDoorDef>
            {
                new LevelDefinition.CrankDoorDef { LinkId = 1, Center = new Vector2(22.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            def.Plates = new List<LevelDefinition.PlateDef>
            {
                new LevelDefinition.PlateDef { LinkId = 1, Min = new Vector2(26f, 0.9f), Max = new Vector2(28f, 2.2f) }, // past the crank door
            };
            def.Doors = new List<LevelDefinition.DoorDef>
            {
                new LevelDefinition.DoorDef { LinkId = 1, Center = new Vector2(34.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W3-L7 "Climb": inside the ladder zone an Echo is solid only while its recording holds
        /// Crouch (mechanic #2). The plate hangs just above head height: one Echo crouches as a step, a
        /// second stands on it to hold the plate, and the player walks the open door.</summary>
        public static LevelDefinition World3Level7()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W3_L7"; def.SaveSeed = 0x5EEE5;
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 3; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            def.EchoLadderZones = new List<LevelDefinition.EchoLadderZoneDef>
            {
                new LevelDefinition.EchoLadderZoneDef { Min = new Vector2(18f, 0f), Max = new Vector2(26f, 6f) },
            };
            def.Plates = new List<LevelDefinition.PlateDef>
            {
                // Bottom edge y=2.0: above a grounded body's head (top 1.9), below the head of a body
                // standing on a crouched Echo (body spans 1.9..2.8) — only a boosted body can HOLD it.
                new LevelDefinition.PlateDef { LinkId = 1, Min = new Vector2(19.5f, 2.0f), Max = new Vector2(22.5f, 3.2f) },
            };
            def.Doors = new List<LevelDefinition.DoorDef>
            {
                new LevelDefinition.DoorDef { LinkId = 1, Center = new Vector2(40.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W3-L8 "Pulley": a platform rises as long as occupants are in its zone (mechanic #21).
        /// Time your movements and use an Echo to keep the platform raised while you cross.</summary>
        public static LevelDefinition World3Level8()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W3_L8"; def.SaveSeed = 0x5EEE6;
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 3; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            def.Pulleys = new List<LevelDefinition.PulleyDef>
            {
                new LevelDefinition.PulleyDef
                {
                    LinkId = 1,
                    ZoneMin = new Vector2(8f, 0.9f), ZoneMax = new Vector2(12f, 2.2f),
                    Threshold = 2, // one body walking through must never raise it (cheese-audited)
                    RiseRate = 0.1f, SlipRate = 0.05f
                },
            };
            // Difficulty pass (2026-07-07): the walk from the winch to the gate is lethal — the third
            // Echo is a martyr who clears it. Winders 2 + martyr 1 = the whole budget; the live self
            // owns nothing but the timing. FALSE-OBVIOUS: gate opens → sprint → die at the road you
            // never watched your martyr clear.
            def.Hazards = new List<LevelDefinition.HazardDef>
            {
                new LevelDefinition.HazardDef { Min = new Vector2(20f, 0.9f), Max = new Vector2(21.5f, 3.5f), Consumable = true },
            };
            def.PulleyDoors = new List<LevelDefinition.PulleyDoorDef>
            {
                new LevelDefinition.PulleyDoorDef { LinkId = 1, Center = new Vector2(30.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W4-L1 "Weight": a scale must hold exactly a target weight to open its door (mechanic #18).
        /// Manage Echo weight carefully, adding or removing Echoes until the scale reads right.</summary>
        public static LevelDefinition World4Level1()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W4_L1"; def.SaveSeed = 0x5EEE7;
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 4; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            // Difficulty pass (2026-07-07): the scale wants EXACTLY three, and a fourth job (plate past
            // the scale door) eats the whole budget. The three sitters form a solid wall on the scale —
            // everyone after them JUMPS the pile, which also keeps the scale reading 3 mid-crossing
            // (walking through the zone would make it 4 and slam the door). FALSE-OBVIOUS: pile all four
            // on the scale ("more weight = better") — EXACT means overshoot fails; or sit on it yourself
            // as the third and discover you can't leave (station-keeping, W1_L4's lesson at arity 3).
            def.MassScales = new List<LevelDefinition.MassScaleDef>
            {
                new LevelDefinition.MassScaleDef { LinkId = 1, Min = new Vector2(20f, 0.9f), Max = new Vector2(24f, 2.2f), Target = 3 },
            };
            def.MassScaleDoors = new List<LevelDefinition.MassScaleDoorDef>
            {
                new LevelDefinition.MassScaleDoorDef { LinkId = 1, Center = new Vector2(33.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            def.Plates = new List<LevelDefinition.PlateDef>
            {
                new LevelDefinition.PlateDef { LinkId = 1, Min = new Vector2(36f, 0.9f), Max = new Vector2(38f, 2.2f) }, // past the scale door
            };
            def.Doors = new List<LevelDefinition.DoorDef>
            {
                new LevelDefinition.DoorDef { LinkId = 1, Center = new Vector2(40.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W4-L2 "Stack": Echoes are solid platforms (mechanic #23) — build a living staircase.
        /// The plate zone floats at y=3.8..5.0: a body needs three Echoes under it to hold that height
        /// (each body is 0.9 tall; jump reach from ground tops out ~4.1, enough to touch but not to HOLD).</summary>
        public static LevelDefinition World4Level2()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W4_L2"; def.SaveSeed = 0x5EEE8;
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 4; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            def.Plates = new List<LevelDefinition.PlateDef>
            {
                new LevelDefinition.PlateDef { LinkId = 1, Min = new Vector2(26f, 3.8f), Max = new Vector2(30f, 5.0f) },
            };
            def.Doors = new List<LevelDefinition.DoorDef>
            {
                new LevelDefinition.DoorDef { LinkId = 1, Center = new Vector2(40.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W4-L3 "Isolation": the live player cannot touch any active Echo (mechanic #25).
        /// Use spatial separation or careful timing to keep them apart while solving the puzzle.</summary>
        public static LevelDefinition World4Level3()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W4_L3"; def.SaveSeed = 0x5EEE9;
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 2; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            def.NoTouchZones = new List<LevelDefinition.NoTouchZoneDef>
            {
                new LevelDefinition.NoTouchZoneDef { Min = new Vector2(0f, 0f), Max = new Vector2(48f, 24f) },
            };
            def.Plates = new List<LevelDefinition.PlateDef>
            {
                new LevelDefinition.PlateDef { LinkId = 1, Min = new Vector2(30f, 0.9f), Max = new Vector2(32f, 2.2f) },
            };
            def.Doors = new List<LevelDefinition.DoorDef>
            {
                new LevelDefinition.DoorDef { LinkId = 1, Center = new Vector2(40.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W4-L4 "Cascade": a Domino Crew chain (mechanic #39) — occupy the trigger zone to arm
        /// domino 0; each fall auto-arms the next. The door is a domino door: read the cascade's timing
        /// and be in position when the chain completes.</summary>
        public static LevelDefinition World4Level4()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W4_L4"; def.SaveSeed = 0x5EEEA;
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            // Difficulty pass (2026-07-07): budget cut 3 → 2, and the road past the dominoes is lethal.
            // One Echo starts the cascade (the trigger is BEHIND spawn), one martyr clears the hazard,
            // and the live self reads the cascade's timing with no spare bodies left.
            def.MaxEchoes = 2; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            def.Dominoes = new List<LevelDefinition.DominoDef>
            {
                new LevelDefinition.DominoDef { ZoneMin = new Vector2(6f, 0.9f), ZoneMax = new Vector2(8f, 2.2f), FallTicks = 25 },
                new LevelDefinition.DominoDef { ZoneMin = new Vector2(12f, 0.9f), ZoneMax = new Vector2(14f, 2.2f), FallTicks = 25 },
                new LevelDefinition.DominoDef { ZoneMin = new Vector2(18f, 0.9f), ZoneMax = new Vector2(20f, 2.2f), FallTicks = 25 },
            };
            def.Hazards = new List<LevelDefinition.HazardDef>
            {
                new LevelDefinition.HazardDef { Min = new Vector2(30f, 0.9f), Max = new Vector2(31.5f, 3.5f), Consumable = true },
            };
            def.DominoDoors = new List<LevelDefinition.DominoDoorDef>
            {
                new LevelDefinition.DominoDoorDef { LinkId = 1, Center = new Vector2(40.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W4-L5 "Rhythm": two Crusher Pistons (Content Bible D2) with different cycle lengths
        /// gate the corridor — the intro room for reading autonomous hazard rhythms before W6 combines
        /// them with drones. An Echo holds the far plate; the player times both crossings.</summary>
        public static LevelDefinition World4Level5()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W4_L5"; def.SaveSeed = 0x5EEEB;
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 2; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            def.CrusherPistons = new List<LevelDefinition.CrusherPistonDef>
            {
                new LevelDefinition.CrusherPistonDef { Min = new Vector2(18f, 0f), Max = new Vector2(21f, 4f), ExtendedTicks = 45, RetractedTicks = 45 },
                new LevelDefinition.CrusherPistonDef { Min = new Vector2(25f, 0f), Max = new Vector2(28f, 4f), ExtendedTicks = 30, RetractedTicks = 60 },
            };
            def.Plates = new List<LevelDefinition.PlateDef>
            {
                new LevelDefinition.PlateDef { LinkId = 1, Min = new Vector2(31f, 0.9f), Max = new Vector2(33f, 2.2f) },
            };
            def.Doors = new List<LevelDefinition.DoorDef>
            {
                new LevelDefinition.DoorDef { LinkId = 1, Center = new Vector2(40.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W4-L6 "Flood": water rises only while the drain is blocked (mechanic #32). Carry the
        /// crate onto the drain (crates don't float; bodies do), let the room fill, then swim through the
        /// high gap in the exit wall (y=8..11) that walking could never reach.</summary>
        public static LevelDefinition World4Level6()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W4_L6"; def.SaveSeed = 0x5EEEC;
            def.Secrets = new List<LevelDefinition.SecretDef> { new LevelDefinition.SecretDef { Id = 5, Min = new Vector2(1f, 7.5f), Max = new Vector2(3f, 9.5f) } }; // only reachable by swimming LEFT once the flood is up
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 2; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            def.Solids = new List<LevelDefinition.SolidRect>
            {
                new LevelDefinition.SolidRect { X = 40, Y = 1, W = 1, H = 7 },   // exit wall below the gap (y=1..8)
                new LevelDefinition.SolidRect { X = 40, Y = 11, W = 1, H = 13 }, // exit wall above the gap (y=11..24)
            };
            def.Flood = new LevelDefinition.FloodConfigDef { StartY = 0f, MaxY = 9f }; // float height ~9 lines up with the gap
            def.Drains = new List<LevelDefinition.DrainDef>
            {
                // Standard standing-zone y-range: a resting crate (y=1.0..1.8) or a standing body blocks it;
                // a body floating overhead once the water is up does NOT (its MinY is far above 2.2).
                new LevelDefinition.DrainDef { Min = new Vector2(3f, 0.9f), Max = new Vector2(5f, 2.2f) },
            };
            def.Crates = new List<LevelDefinition.CrateDef>
            {
                new LevelDefinition.CrateDef { Position = new Vector2(8f, 1.4f), HalfExtents = new Vector2(0.6f, 0.4f) },
            };
            return def;
        }

        /// <summary>W4-L7 "Negotiation": a platform opens its door based on an Echo's trust level (mechanic #50).
        /// Invest in trust with your Echo before the platform grants passage.</summary>
        public static LevelDefinition World4Level7()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W4_L7"; def.SaveSeed = 0x5EEED;
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 2; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            def.NegotiationPlates = new List<LevelDefinition.NegotiationPlateDef>
            {
                new LevelDefinition.NegotiationPlateDef { LinkId = 1, Min = new Vector2(20f, 0.9f), Max = new Vector2(24f, 2.2f), TrustThreshold = 0.5f },
            };
            def.NegotiationDoors = new List<LevelDefinition.NegotiationDoorDef>
            {
                new LevelDefinition.NegotiationDoorDef { LinkId = 1, Center = new Vector2(40.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W4-L8 "Winds": wind blows only while the switch zone is held (mechanic #33). The pit is
        /// wider than a bare jump — an Echo holds the switch so the tailwind carries your jump across; then
        /// a second Echo (recorded riding the wind) holds the far plate while you cross the door.</summary>
        public static LevelDefinition World4Level8()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W4_L8"; def.SaveSeed = 0x5EEEE;
            def.Secrets = new List<LevelDefinition.SecretDef> { new LevelDefinition.SecretDef { Id = 6, Min = new Vector2(1f, 0.9f), Max = new Vector2(2.5f, 2.2f) } };
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 2; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            def.FloorGaps = new List<LevelDefinition.FloorGapDef> { new LevelDefinition.FloorGapDef { X = 18, W = 6 } }; // ~6 units: past bare jump range (~4.4)
            def.WindZones = new List<LevelDefinition.WindZoneDef>
            {
                new LevelDefinition.WindZoneDef { LinkId = 1, Min = new Vector2(16f, 0f), Max = new Vector2(26f, 8f), Force = new Vector2(6f, 0f) },
            };
            def.WindSwitches = new List<LevelDefinition.WindSwitchDef>
            {
                new LevelDefinition.WindSwitchDef { LinkId = 1, Min = new Vector2(5f, 0.9f), Max = new Vector2(7f, 2.2f) },
            };
            def.Plates = new List<LevelDefinition.PlateDef>
            {
                new LevelDefinition.PlateDef { LinkId = 2, Min = new Vector2(30f, 0.9f), Max = new Vector2(32f, 2.2f) },
            };
            def.Doors = new List<LevelDefinition.DoorDef>
            {
                new LevelDefinition.DoorDef { LinkId = 2, Center = new Vector2(40.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W5-L1 "Memory": checkpoints must be visited in order, separately per body (mechanic #43).
        /// Send an Echo through one checkpoint sequence while you take another.</summary>
        public static LevelDefinition World5Level1()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W5_L1"; def.SaveSeed = 0x5EEEF;
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 2; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            // Memory-Lock counts ONE walker soloing the whole sequence — so the sequence itself must be
            // unwalkable alone: the middle mark sits on a ledge (top y=4) above bare-jump reach (~3.15),
            // reachable only by jumping OFF a parked Echo's head (proven in TestStackToReach). The lock
            // is solo; the route isn't. Cheese-audited.
            def.Solids = new List<LevelDefinition.SolidRect>
            {
                new LevelDefinition.SolidRect { X = 18, Y = 3, W = 4, H = 1 }, // the mark's perch (top y=4)
            };
            def.MemoryCheckpoints = new List<LevelDefinition.MemoryCheckpointDef>
            {
                new LevelDefinition.MemoryCheckpointDef { Min = new Vector2(12f, 0.9f), Max = new Vector2(14f, 2.2f) },
                new LevelDefinition.MemoryCheckpointDef { Min = new Vector2(18.5f, 3.9f), Max = new Vector2(21.5f, 5.2f) }, // up on the perch
                new LevelDefinition.MemoryCheckpointDef { Min = new Vector2(28f, 0.9f), Max = new Vector2(30f, 2.2f) },
            };
            def.MemoryDoors = new List<LevelDefinition.MemoryDoorDef>
            {
                new LevelDefinition.MemoryDoorDef { LinkId = 1, Center = new Vector2(40.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W5-L2 "Cumulative": a lever charges when bodies sit in its zone; when fully charged,
        /// it opens its door (mechanic #49). Manage multiple bodies to reach threshold faster.</summary>
        public static LevelDefinition World5Level2()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W5_L2"; def.SaveSeed = 0x5EEF0;
            def.Secrets = new List<LevelDefinition.SecretDef> { new LevelDefinition.SecretDef { Id = 7, Min = new Vector2(1f, 0.9f), Max = new Vector2(2.5f, 2.2f) } };
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 3; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            def.CumulativeLevers = new List<LevelDefinition.CumulativeLeverDef>
            {
                new LevelDefinition.CumulativeLeverDef
                {
                    LinkId = 1,
                    ZoneMin = new Vector2(4f, 0.9f), ZoneMax = new Vector2(8f, 2.2f),
                    ChargePerActivation = 0.25f,
                    Threshold = 1f
                },
            };
            def.CumulativeLeverDoors = new List<LevelDefinition.CumulativeLeverDoorDef>
            {
                new LevelDefinition.CumulativeLeverDoorDef { LinkId = 1, Center = new Vector2(40.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W5-L3 "Password": enter a specific button sequence in a designated zone to open a door
        /// (mechanic #36). The sequence is short and learnable, testing Echo command precision.</summary>
        public static LevelDefinition World5Level3()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W5_L3"; def.SaveSeed = 0x5EEF1;
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 2; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            def.EchoPassword = new LevelDefinition.EchoPasswordDef
            {
                EntryMin = new Vector2(8f, 0.9f),
                EntryMax = new Vector2(12f, 2.2f),
                TargetSequence = new[]
                {
                    Echo.Core.Replay.InputButtons.Jump,
                    Echo.Core.Replay.InputButtons.Crouch,
                    Echo.Core.Replay.InputButtons.Jump,
                }
            };
            def.EchoPasswordDoors = new List<LevelDefinition.EchoPasswordDoorDef>
            {
                new LevelDefinition.EchoPasswordDoorDef { LinkId = 1, Center = new Vector2(40.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W5-L4 "Lock": two bodies must occupy specific zones simultaneously and remain there
        /// within a tolerance window (mechanic #41). Coordinate precisely to trigger the door.</summary>
        public static LevelDefinition World5Level4()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W5_L4"; def.SaveSeed = 0x5EEF2;
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 2; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            def.TwoBodyLocks = new List<LevelDefinition.TwoBodyLockDef>
            {
                new LevelDefinition.TwoBodyLockDef
                {
                    LinkId = 1,
                    AMin = new Vector2(8f, 0.9f), AMax = new Vector2(10f, 2.2f),
                    BMin = new Vector2(20f, 0.9f), BMax = new Vector2(22f, 2.2f),
                    ToleranceTicks = 120
                },
            };
            def.TwoBodyLockDoors = new List<LevelDefinition.TwoBodyLockDoorDef>
            {
                new LevelDefinition.TwoBodyLockDoorDef { LinkId = 1, Center = new Vector2(40.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W5-L5 "Color": carry colored crates to matching keyholes (mechanic #35). Each crate
        /// is a specific color; find the matching keyhole and unlock the corresponding door.</summary>
        public static LevelDefinition World5Level5()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W5_L5"; def.SaveSeed = 0x5EEF3;
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 2; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            def.Crates = new List<LevelDefinition.CrateDef>
            {
                new LevelDefinition.CrateDef { Position = new Vector2(12f, 1.4f), HalfExtents = new Vector2(0.6f, 0.4f) },
            };
            def.ColoredItems = new List<LevelDefinition.ColoredItemDef>
            {
                new LevelDefinition.ColoredItemDef { CrateIndex = 0, Color = 1 },
            };
            def.Keyholes = new List<LevelDefinition.KeyholeDef>
            {
                new LevelDefinition.KeyholeDef { LinkId = 1, RequiredColor = 1, Min = new Vector2(20f, 0.9f), Max = new Vector2(22f, 2.2f) },
            };
            def.ColorDoors = new List<LevelDefinition.ColorDoorDef>
            {
                new LevelDefinition.ColorDoorDef { LinkId = 1, Center = new Vector2(40.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W5-L6 "Anti": an anti-echo field spans the middle of the room, deleting any Echo that
        /// enters while sparing the player (mechanic #45). Your helper holds the plate from the safe side —
        /// the crossing itself you make alone, and nothing recorded in there can ever follow you.</summary>
        public static LevelDefinition World5Level6()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W5_L6"; def.SaveSeed = 0x5EEF4;
            def.Secrets = new List<LevelDefinition.SecretDef> { new LevelDefinition.SecretDef { Id = 8, Min = new Vector2(1f, 0.9f), Max = new Vector2(2.5f, 2.2f) } };
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 3; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            def.AntiEchoFields = new List<LevelDefinition.AntiEchoFieldDef>
            {
                new LevelDefinition.AntiEchoFieldDef { Min = new Vector2(16f, 0f), Max = new Vector2(32f, 24f) },
            };
            def.Plates = new List<LevelDefinition.PlateDef>
            {
                new LevelDefinition.PlateDef { LinkId = 1, Min = new Vector2(4f, 0.9f), Max = new Vector2(6f, 2.2f) },
            };
            def.Doors = new List<LevelDefinition.DoorDef>
            {
                new LevelDefinition.DoorDef { LinkId = 1, Center = new Vector2(40.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W5-L7 "Elevator": a counterweight rises when you hold it (mechanic #4). Position the
        /// counterweight crate and ride the platform up to the goal.</summary>
        public static LevelDefinition World5Level7()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W5_L7"; def.SaveSeed = 0x5EEF5;
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 2; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            def.Elevators = new List<LevelDefinition.ElevatorDef>
            {
                new LevelDefinition.ElevatorDef
                {
                    StartCenter = new Vector2(24f, 2f),
                    HalfExtents = new Vector2(2f, 0.4f),
                    ZoneMin = new Vector2(10f, 0.9f), ZoneMax = new Vector2(12f, 2.2f),
                    MaxWeightForFullRise = 1,
                    MaxRise = 6f
                },
            };
            def.Plates = new List<LevelDefinition.PlateDef>
            {
                // Rider body at full rise spans y=8.4..9.3 — zone top 9.0 gives comfortable overlap.
                new LevelDefinition.PlateDef { LinkId = 1, Min = new Vector2(22f, 7.5f), Max = new Vector2(26f, 9.0f) },
            };
            def.Doors = new List<LevelDefinition.DoorDef>
            {
                new LevelDefinition.DoorDef { LinkId = 1, Center = new Vector2(40.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W5-L8 "Resonance": combine Quorum Door + Carry + Echoes to meet multiple simultaneous
        /// constraints — a capstone showing all core mechanics working in concert.</summary>
        public static LevelDefinition World5Level8()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W5_L8"; def.SaveSeed = 0x5EEF6;
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 3; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            def.Crates = new List<LevelDefinition.CrateDef>
            {
                new LevelDefinition.CrateDef { Position = new Vector2(8f, 1.4f), HalfExtents = new Vector2(0.6f, 0.4f) },
            };
            def.Quorums = new List<LevelDefinition.QuorumDef>
            {
                new LevelDefinition.QuorumDef { LinkId = 1, Min = new Vector2(20f, 0.9f), Max = new Vector2(24f, 2.2f), Threshold = 2 },
            };
            def.Doors = new List<LevelDefinition.DoorDef>
            {
                new LevelDefinition.DoorDef { LinkId = 1, Center = new Vector2(40.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W6-L1 "Hazards": navigate autonomous crusher pistons and caretaker drones that hunt by
        /// salience (Content Bible D2 and D19). Time your movements carefully.</summary>
        public static LevelDefinition World6Level1()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W6_L1"; def.SaveSeed = 0x5EEF7;
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 3; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            def.CrusherPistons = new List<LevelDefinition.CrusherPistonDef>
            {
                new LevelDefinition.CrusherPistonDef { Min = new Vector2(18f, 0f), Max = new Vector2(22f, 4f), ExtendedTicks = 30, RetractedTicks = 30 },
                new LevelDefinition.CrusherPistonDef { Min = new Vector2(26f, 0f), Max = new Vector2(30f, 4f), ExtendedTicks = 40, RetractedTicks = 20 },
            };
            def.CaretakerDrones = new List<LevelDefinition.CaretakerDroneDef>
            {
                new LevelDefinition.CaretakerDroneDef { Start = new Vector2(24f, 10f), HalfExtents = new Vector2(0.6f, 0.6f), ChaseSpeed = 0.05f },
            };
            def.Plates = new List<LevelDefinition.PlateDef>
            {
                new LevelDefinition.PlateDef { LinkId = 1, Min = new Vector2(30f, 0.9f), Max = new Vector2(32f, 2.2f) },
            };
            def.Doors = new List<LevelDefinition.DoorDef>
            {
                new LevelDefinition.DoorDef { LinkId = 1, Center = new Vector2(40.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W6-L2 "Gauntlet": a swinging pendulum platform (mechanic #47) is the only way across a
        /// pit, while a Decoy Self (mechanic #9) hunts whichever body is nearest — slower than a running
        /// player, so someone must always be playing bait. The pendulum's phase is identical every run, so
        /// an Echo's recorded ride replays perfectly.</summary>
        public static LevelDefinition World6Level2()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W6_L2"; def.SaveSeed = 0x5EEF8;
            def.Secrets = new List<LevelDefinition.SecretDef> { new LevelDefinition.SecretDef { Id = 9, Min = new Vector2(1f, 0.9f), Max = new Vector2(2.5f, 2.2f) } };
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 4; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            def.FloorGaps = new List<LevelDefinition.FloorGapDef> { new LevelDefinition.FloorGapDef { X = 17, W = 6 } };
            def.Pendulums = new List<LevelDefinition.PendulumDef>
            {
                // Boardable (top y=2.8 < jump reach) and clears a walking body's head at its low point;
                // swing spans x=16..24, kissing both pit edges once per 4-second period.
                new LevelDefinition.PendulumDef { Center = new Vector2(20f, 2.4f), HalfExtents = new Vector2(1f, 0.4f), SwingHalfWidth = 4f, PeriodTicks = 240 },
            };
            def.DecoyHazards = new List<LevelDefinition.DecoyHazardDef>
            {
                // 0.08/tick = 4.8 u/s: slower than a full run (8 u/s) — outrunnable, never ignorable.
                new LevelDefinition.DecoyHazardDef { Start = new Vector2(30f, 8f), HalfExtents = new Vector2(0.6f, 0.6f), ChaseSpeed = 0.08f },
            };
            def.Plates = new List<LevelDefinition.PlateDef>
            {
                new LevelDefinition.PlateDef { LinkId = 1, Min = new Vector2(30f, 0.9f), Max = new Vector2(32f, 2.2f) },
            };
            def.Doors = new List<LevelDefinition.DoorDef>
            {
                new LevelDefinition.DoorDef { LinkId = 1, Center = new Vector2(40.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W6-L3 "Crossfire": two Self-Turrets (mechanic #48/#22) whose firing rhythm IS an Echo's
        /// recorded Interact rhythm in each control zone. Record your two gunner-selves' rhythms, then a
        /// plate-holder self must cross both staggered blast corridors — and finally so must you, dodging
        /// the exact pattern you authored.</summary>
        public static LevelDefinition World6Level3()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W6_L3"; def.SaveSeed = 0x5EEF9;
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 3; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            def.TurretEmitters = new List<LevelDefinition.TurretEmitterDef>
            {
                new LevelDefinition.TurretEmitterDef
                {
                    ControlMin = new Vector2(4f, 0.9f), ControlMax = new Vector2(6f, 2.2f),
                    BlastMin = new Vector2(20f, 0f), BlastMax = new Vector2(23f, 6f)
                },
                new LevelDefinition.TurretEmitterDef
                {
                    ControlMin = new Vector2(7f, 0.9f), ControlMax = new Vector2(9f, 2.2f),
                    BlastMin = new Vector2(25f, 0f), BlastMax = new Vector2(28f, 6f)
                },
            };
            def.Plates = new List<LevelDefinition.PlateDef>
            {
                new LevelDefinition.PlateDef { LinkId = 1, Min = new Vector2(31f, 0.9f), Max = new Vector2(33f, 2.2f) },
            };
            def.Doors = new List<LevelDefinition.DoorDef>
            {
                new LevelDefinition.DoorDef { LinkId = 1, Center = new Vector2(40.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W6-L4 "Mirror": occupy all mirror points simultaneously to open a relay door
        /// (mechanic #6). Spread your Echoes to cover the zones.</summary>
        public static LevelDefinition World6Level4()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W6_L4"; def.SaveSeed = 0x5EEFA;
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 4; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            def.MirrorPoints = new List<LevelDefinition.MirrorPointDef>
            {
                new LevelDefinition.MirrorPointDef { Min = new Vector2(8f, 0.9f), Max = new Vector2(10f, 2.2f) },
                new LevelDefinition.MirrorPointDef { Min = new Vector2(16f, 0.9f), Max = new Vector2(18f, 2.2f) },
                new LevelDefinition.MirrorPointDef { Min = new Vector2(24f, 0.9f), Max = new Vector2(26f, 2.2f) },
            };
            def.MirrorRelayDoors = new List<LevelDefinition.MirrorRelayDoorDef>
            {
                new LevelDefinition.MirrorRelayDoorDef { LinkId = 1, Center = new Vector2(40.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W6-L5 "Path": complete a mirrored path challenge — player follows one route while any
        /// Echo follows the inverse route simultaneously (mechanic #17).</summary>
        public static LevelDefinition World6Level5()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W6_L5"; def.SaveSeed = 0x5EEFB;
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 2; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            def.MirroredPlayerCheckpoints = new List<LevelDefinition.MirroredCheckpointDef>
            {
                new LevelDefinition.MirroredCheckpointDef { Min = new Vector2(12f, 0.9f), Max = new Vector2(14f, 2.2f) },
                new LevelDefinition.MirroredCheckpointDef { Min = new Vector2(18f, 0.9f), Max = new Vector2(20f, 2.2f) },
                new LevelDefinition.MirroredCheckpointDef { Min = new Vector2(24f, 0.9f), Max = new Vector2(26f, 2.2f) },
            };
            def.MirrorPathCheckpoints = new List<LevelDefinition.MirrorCheckpointDef>
            {
                new LevelDefinition.MirrorCheckpointDef { Min = new Vector2(34f, 0.9f), Max = new Vector2(36f, 2.2f) },
                new LevelDefinition.MirrorCheckpointDef { Min = new Vector2(28f, 0.9f), Max = new Vector2(30f, 2.2f) },
                new LevelDefinition.MirrorCheckpointDef { Min = new Vector2(22f, 0.9f), Max = new Vector2(24f, 2.2f) },
            };
            def.MirroredPathDoors = new List<LevelDefinition.MirroredPathDoorDef>
            {
                new LevelDefinition.MirroredPathDoorDef { LinkId = 1, Center = new Vector2(40.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }

        /// <summary>W6-L6 "Shield": a Body Shield beam (mechanic #12) guards the corridor. It kills one
        /// body per shot and needs 2 seconds to recharge — an Echo walks in first and takes the hit, and
        /// everyone behind it crosses inside the recharge window. The purest sacrifice in the game: your
        /// past self absorbs the shot meant for you.</summary>
        public static LevelDefinition World6Level6()
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.LevelId = "W6_L6"; def.SaveSeed = 0x5EEFC;
            def.Secrets = new List<LevelDefinition.SecretDef> { new LevelDefinition.SecretDef { Id = 10, Min = new Vector2(1f, 0.9f), Max = new Vector2(2.5f, 2.2f) } }; // the last secret is where you started
            def.Width = 48; def.Height = 24; def.Spawn = new Vector2(10, 3);
            def.MaxEchoes = 3; def.EnabledGates = Echo.Core.Echo.GateMask.None;
            def.BodyShieldBeams = new List<LevelDefinition.BodyShieldBeamDef>
            {
                // Corridor takes ~36 ticks to walk at full speed; 120-tick recharge is a generous window.
                new LevelDefinition.BodyShieldBeamDef { Min = new Vector2(20f, 0f), Max = new Vector2(24f, 6f), RechargeTicks = 120 },
            };
            def.Plates = new List<LevelDefinition.PlateDef>
            {
                new LevelDefinition.PlateDef { LinkId = 1, Min = new Vector2(31f, 0.9f), Max = new Vector2(33f, 2.2f) },
            };
            def.Doors = new List<LevelDefinition.DoorDef>
            {
                new LevelDefinition.DoorDef { LinkId = 1, Center = new Vector2(40.5f, 3f), HalfExtents = new Vector2(0.5f, 3f) },
            };
            return def;
        }
    }
}
