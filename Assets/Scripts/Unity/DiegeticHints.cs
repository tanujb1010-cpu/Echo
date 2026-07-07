using System.Collections.Generic;
using UnityEngine;

namespace Echo.Unity
{
    /// <summary>One in-world teaching label: a short line floating above the object it explains.</summary>
    public readonly struct WorldHint
    {
        public readonly Vector2 Pos;
        public readonly string Text;
        public WorldHint(Vector2 pos, string text) { Pos = pos; Text = text; }
    }

    /// <summary>
    /// Diegetic teaching: the first level where a mechanic appears gets one or two minimal labels
    /// anchored to the mechanic itself — never a wall of text. Positions are derived from the live
    /// LevelDefinition (not hardcoded), so redesigned layouts keep their hints anchored correctly.
    /// The flow layer only shows these until the level is first completed.
    /// </summary>
    public static class DiegeticHints
    {
        public static void Collect(LevelDefinition d, List<WorldHint> into)
        {
            into.Clear();
            if (d == null) return;
            switch (d.LevelId)
            {
                case "W1_L1":
                    if (d.Plates.Count > 0)
                        into.Add(new WorldHint(Above(d.Plates[0].Min, d.Plates[0].Max),
                            "STAND HERE — the door holds while this is pressed"));
                    if (d.Doors.Count > 0)
                        into.Add(new WorldHint(d.Doors[0].Center + new Vector2(0f, d.Doors[0].HalfExtents.y + 1.2f),
                            "someone must stay on the plate\nHOLD R — your recorded self becomes that someone"));
                    break;

                case "W1_L2":
                    if (d.Plates.Count > 0)
                        into.Add(new WorldHint(Above(d.Plates[0].Min, d.Plates[0].Max),
                            "an Echo can hold this for you"));
                    break;

                case "W1_L3":
                    if (d.Hazards.Count > 0)
                        into.Add(new WorldHint(Above(d.Hazards[0].Min, d.Hazards[0].Max),
                            "LETHAL — but a body that falls here is not wasted"));
                    break;

                case "W1_L4":
                    if (d.Quorums.Count > 0)
                        into.Add(new WorldHint(Above(d.Quorums[0].Min, d.Quorums[0].Max),
                            "THREE bodies at once — you count as one"));
                    else if (d.Plates.Count > 0)
                        into.Add(new WorldHint(Above(d.Plates[0].Min, d.Plates[0].Max),
                            "THREE bodies at once — you count as one"));
                    break;

                case "W1_L5":
                    if (d.Switches.Count > 0)
                        into.Add(new WorldHint(Above(d.Switches[0].Min, d.Switches[0].Max),
                            "both switches — at the same moment"));
                    if (d.Hazards.Count > 0)
                        into.Add(new WorldHint(Above(d.Hazards[0].Min, d.Hazards[0].Max),
                            "someone must go first"));
                    break;

                case "W1_L6":
                    if (d.Plates.Count > 0)
                        into.Add(new WorldHint(Above(d.Plates[0].Min, d.Plates[0].Max),
                            "holds the door UPSTAIRS"));
                    break;

                case "W1_L7":
                    if (d.Plates.Count > 1)
                        into.Add(new WorldHint(Above(d.Plates[1].Min, d.Plates[1].Max),
                            "whoever stands here must cross the first bridge to arrive"));
                    break;

                case "W2_L1":
                    if (d.Portals.Count > 0)
                        into.Add(new WorldHint(Above(d.Portals[0].AMin, d.Portals[0].AMax),
                            "steps through to the far side"));
                    if (d.Plates.Count > 0)
                        into.Add(new WorldHint(Above(d.Plates[0].Min, d.Plates[0].Max),
                            "holds the door on the OTHER side of the wall"));
                    break;

                case "W2_L2":
                    if (d.Quorums.Count > 0)
                        into.Add(new WorldHint(Above(d.Quorums[0].Min, d.Quorums[0].Max),
                            "THREE must stand fully inside — and stay"));
                    if (d.ArrivalCheckpoints.Count > 1)
                        into.Add(new WorldHint(Above(d.ArrivalCheckpoints[1].Min, d.ArrivalCheckpoints[1].Max),
                            "this mark counts SECOND — passing it early does nothing"));
                    break;

                case "W2_L3":
                    if (d.Quorums.Count > 0)
                        into.Add(new WorldHint(Above(d.Quorums[0].Min, d.Quorums[0].Max),
                            "wants TWO — the crate won't stay, but a body can block its slide"));
                    break;

                case "W2_L5":
                    if (d.Torches.Count > 0)
                        into.Add(new WorldHint(d.Torches[0].Position + new Vector2(0f, 1.4f),
                            "press F here FIRST — wrong order snuffs them all"));
                    if (d.Plates.Count > 0)
                        into.Add(new WorldHint(Above(d.Plates[0].Min, d.Plates[0].Max),
                            "holds the door before the third beacon — an Echo stays here"));
                    break;

                case "W2_L6":
                    if (d.Switches.Count > 1)
                        into.Add(new WorldHint(Above(d.Switches[1].Min, d.Switches[1].Max),
                            "must be held the same moment as its twin — 30 units away"));
                    break;

                case "W2_L4":
                    if (d.ArrivalCheckpoints.Count > 1)
                        into.Add(new WorldHint(Above(d.ArrivalCheckpoints[1].Min, d.ArrivalCheckpoints[1].Max),
                            "counts SECOND — the far mark comes first"));
                    break;

                case "W3_L1":
                    if (d.PressurePairs.Count > 0)
                        into.Add(new WorldHint(Above(d.PressurePairs[0].AMin, d.PressurePairs[0].AMax),
                            "the see-saw's NEAR end — its twin is far right, past the red"));
                    if (d.Hazards.Count > 0)
                        into.Add(new WorldHint(Above(d.Hazards[0].Min, d.Hazards[0].Max),
                            "eats the first body through — send one to die"));
                    break;

                case "W3_L6":
                    if (d.CrankZones.Count > 0)
                        into.Add(new WorldHint(Above(d.CrankZones[0].Min, d.CrankZones[0].Max),
                            "hold F HERE — the door lives only while the recording cranks"));
                    if (d.Plates.Count > 0)
                        into.Add(new WorldHint(Above(d.Plates[0].Min, d.Plates[0].Max),
                            "someone must reach this WHILE the crank window is open"));
                    break;

                case "W3_L8":
                    if (d.Hazards.Count > 0)
                        into.Add(new WorldHint(Above(d.Hazards[0].Min, d.Hazards[0].Max),
                            "the road eats one — spend a body before you spend yourself"));
                    break;

                case "W4_L1":
                    if (d.MassScales.Count > 0)
                        into.Add(new WorldHint(Above(d.MassScales[0].Min, d.MassScales[0].Max),
                            "EXACTLY three — jump the pile, don't join it"));
                    break;

                case "W4_L4":
                    if (d.Dominoes.Count > 0)
                        into.Add(new WorldHint(Above(d.Dominoes[0].ZoneMin, d.Dominoes[0].ZoneMax),
                            "the chain starts HERE — behind you"));
                    if (d.Hazards.Count > 0)
                        into.Add(new WorldHint(Above(d.Hazards[0].Min, d.Hazards[0].Max),
                            "one body clears this — you have exactly enough"));
                    break;

                case "W3_L3":
                    if (d.GravityZones.Count > 0)
                        into.Add(new WorldHint(Above(d.GravityZones[0].Min, d.GravityZones[0].Max, lift: 0.6f),
                            "gravity flips inside"));
                    break;
            }
        }

        private static Vector2 Above(Vector2 min, Vector2 max, float lift = 1.2f)
            => new Vector2((min.x + max.x) * 0.5f, max.y + lift);
    }
}
