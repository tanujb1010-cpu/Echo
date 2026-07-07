using System.Collections.Generic;
using UnityEngine;
using Echo.Core.Determinism;
using Echo.Core.Sim;
using Echo.Gameplay.Systems;

namespace Echo.Unity
{
    /// <summary>
    /// Translates a <see cref="LevelDefinition"/> asset into the deterministic runtime objects the
    /// simulation needs (collision world + level modules). The only place Unity data crosses into the
    /// pure-C# core — and it does so by value (ints/floats → Fix64), never by reference.
    /// </summary>
    public static class LevelBuilder
    {
        public static Fix64Vec2 ToFix(Vector2 v) => new Fix64Vec2(Fix64.FromFloat(v.x), Fix64.FromFloat(v.y));

        public static (TileCollisionWorld world, Fix64Vec2 spawn, List<ILevelModule> modules) Build(LevelDefinition def)
        {
            var world = new TileCollisionWorld(def.Width, def.Height);
            world.FillFloor(def.FloorRow);
            foreach (var g in def.FloorGaps)
            {
                for (int x = g.X; x < g.X + g.W; x++)
                    world.SetSolid(x, def.FloorRow, false);

                // Pits KILL. The collision world's bottom boundary is solid, so a carved gap is
                // otherwise a 1-tile trench a body can stand in — and jump back out of (the cheese
                // audit's hop bot crossed every pit that way). Injecting the kill zone into the def's
                // hazard list also gets it rendered red, so the pit telegraphs itself. Below-floor
                // only (zone top 0.9 < a bridge-walker's feet at 1.0), so bridge crossings never touch it.
                def.Hazards.Add(new LevelDefinition.HazardDef
                {
                    Min = new Vector2(g.X, def.FloorRow - 0.5f),
                    Max = new Vector2(g.X + g.W, def.FloorRow + 0.9f),
                    Consumable = false,
                });
            }
            foreach (var r in def.Solids)
                for (int x = r.X; x < r.X + r.W; x++)
                    for (int y = r.Y; y < r.Y + r.H; y++)
                        world.SetSolid(x, y, true);

            var modules = new List<ILevelModule>();

            // Crates first so they exist as bodies before plates evaluate (a crate can press a plate).
            // Kept in scope (not just added to `modules`) since Color Carry (#35) needs to reference
            // specific crate bodies by index.
            CrateModule crates = null;
            if (def.Crates.Count > 0)
            {
                crates = new CrateModule();
                foreach (var c in def.Crates) crates.AddCrate(ToFix(c.Position), ToFix(c.HalfExtents));
                modules.Add(crates);
            }

            if (def.Plates.Count > 0 || def.Doors.Count > 0 || def.Quorums.Count > 0)
            {
                var pd = new PlateDoorModule();
                foreach (var p in def.Plates) pd.AddPlate(p.LinkId, ToFix(p.Min), ToFix(p.Max));
                foreach (var q in def.Quorums) pd.AddQuorum(q.LinkId, ToFix(q.Min), ToFix(q.Max), q.Threshold);
                foreach (var d in def.Doors) pd.AddDoor(d.LinkId, ToFix(d.Center), ToFix(d.HalfExtents), d.Invert);
                modules.Add(pd);
            }

            if (def.Switches.Count > 0 || def.SwitchDoors.Count > 0)
            {
                var sw = new SameTickSwitchModule();
                foreach (var s in def.Switches) sw.AddSwitch(s.LinkId, ToFix(s.Min), ToFix(s.Max));
                foreach (var d in def.SwitchDoors) sw.AddDoor(d.LinkId, ToFix(d.Center), ToFix(d.HalfExtents));
                modules.Add(sw);
            }

            if (def.BouncePads.Count > 0)
            {
                var bp = new BouncePadModule();
                foreach (var p in def.BouncePads) bp.AddPad(ToFix(p.Center), ToFix(p.HalfExtents));
                modules.Add(bp);
            }

            if (def.Hazards.Count > 0)
            {
                var hz = new HazardModule();
                foreach (var h in def.Hazards) hz.AddHazard(ToFix(h.Min), ToFix(h.Max), h.Consumable);
                modules.Add(hz);
            }

            if (def.Portals.Count > 0)
            {
                var pm = new PortalModule();
                foreach (var p in def.Portals)
                    pm.AddPair(ToFix(p.AMin), ToFix(p.AMax), ToFix(p.APoint), ToFix(p.BMin), ToFix(p.BMax), ToFix(p.BPoint));
                modules.Add(pm);
            }

            if (def.TimeFields.Count > 0)
            {
                var tf = new TimeFieldModule();
                foreach (var t in def.TimeFields) tf.AddField(ToFix(t.Min), ToFix(t.Max), Fix64.FromFloat(t.Scale));
                modules.Add(tf);
            }

            if (def.Magnets.Count > 0 || def.Metals.Count > 0)
            {
                var mm = new MagnetModule();
                foreach (var m in def.Metals) mm.AddMetal(ToFix(m.Position), ToFix(m.HalfExtents));
                foreach (var g in def.Magnets) mm.AddMagnet(ToFix(g.Position), Fix64.FromFloat(g.Radius), Fix64.FromFloat(g.Strength));
                modules.Add(mm);
            }

            if (def.ArrivalCheckpoints.Count > 0 || def.ArrivalDoors.Count > 0)
            {
                var ao = new ArrivalOrderModule();
                foreach (var c in def.ArrivalCheckpoints) ao.AddCheckpoint(ToFix(c.Min), ToFix(c.Max));
                foreach (var d in def.ArrivalDoors) ao.AddDoor(d.LinkId, ToFix(d.Center), ToFix(d.HalfExtents));
                modules.Add(ao);
            }

            if (def.Torches.Count > 0 || def.TorchDoors.Count > 0)
            {
                var ts = new TorchSequenceModule();
                foreach (var t in def.Torches) ts.AddTorch(ToFix(t.Position), Fix64.FromFloat(t.Radius));
                foreach (var d in def.TorchDoors) ts.AddDoor(d.LinkId, ToFix(d.Center), ToFix(d.HalfExtents));
                modules.Add(ts);
            }

            if (def.AntiEchoFields.Count > 0)
            {
                var aef = new AntiEchoFieldModule();
                foreach (var f in def.AntiEchoFields) aef.AddField(ToFix(f.Min), ToFix(f.Max));
                modules.Add(aef);
            }

            if (def.WindZones.Count > 0 || def.WindSwitches.Count > 0)
            {
                var wm = new WindModule();
                foreach (var s in def.WindSwitches) wm.AddSwitch(s.LinkId, ToFix(s.Min), ToFix(s.Max));
                foreach (var z in def.WindZones) wm.AddZone(z.LinkId, ToFix(z.Min), ToFix(z.Max), ToFix(z.Force));
                modules.Add(wm);
            }

            if (def.LightPlatforms.Count > 0)
            {
                var lm = new LightSolidModule();
                foreach (var p in def.LightPlatforms) lm.AddPlatform(ToFix(p.Center), ToFix(p.HalfExtents));
                modules.Add(lm);
            }

            if (def.PressurePairs.Count > 0 || def.PressureDoors.Count > 0)
            {
                var pb = new PressureBalanceModule();
                foreach (var p in def.PressurePairs) pb.AddPair(p.LinkId, ToFix(p.AMin), ToFix(p.AMax), ToFix(p.BMin), ToFix(p.BMax));
                foreach (var d in def.PressureDoors) pb.AddDoor(d.LinkId, ToFix(d.Center), ToFix(d.HalfExtents));
                modules.Add(pb);
            }

            if (def.GravityZones.Count > 0)
            {
                var gm = new GravityFieldModule();
                foreach (var z in def.GravityZones) gm.AddZone(ToFix(z.Min), ToFix(z.Max));
                modules.Add(gm);
            }

            if (def.DelayedCharges.Count > 0)
            {
                var dc = new DelayedChargeModule();
                foreach (var c in def.DelayedCharges) dc.AddCharge(ToFix(c.ArmMin), ToFix(c.ArmMax), ToFix(c.BlastMin), ToFix(c.BlastMax), c.FuseTicks);
                modules.Add(dc);
            }

            if (def.Elevators.Count > 0)
            {
                var em = new ElevatorModule();
                foreach (var e in def.Elevators)
                    em.AddPlatform(ToFix(e.StartCenter), ToFix(e.HalfExtents), ToFix(e.ZoneMin), ToFix(e.ZoneMax), e.MaxWeightForFullRise, Fix64.FromFloat(e.MaxRise));
                modules.Add(em);
            }

            if (def.MassScales.Count > 0 || def.MassScaleDoors.Count > 0)
            {
                var ms = new MassScaleModule();
                foreach (var s in def.MassScales) ms.AddScale(s.LinkId, ToFix(s.Min), ToFix(s.Max), s.Target);
                foreach (var d in def.MassScaleDoors) ms.AddDoor(d.LinkId, ToFix(d.Center), ToFix(d.HalfExtents));
                modules.Add(ms);
            }

            if (def.Pulleys.Count > 0 || def.PulleyDoors.Count > 0)
            {
                var pm = new PulleyModule();
                foreach (var p in def.Pulleys) pm.AddPulley(p.LinkId, ToFix(p.ZoneMin), ToFix(p.ZoneMax), p.Threshold, Fix64.FromFloat(p.RiseRate), Fix64.FromFloat(p.SlipRate));
                foreach (var d in def.PulleyDoors) pm.AddDoor(d.LinkId, ToFix(d.Center), ToFix(d.HalfExtents));
                modules.Add(pm);
            }

            if (def.NoTouchZones.Count > 0)
            {
                var nt = new NoTouchFieldModule();
                foreach (var z in def.NoTouchZones) nt.AddZone(ToFix(z.Min), ToFix(z.Max));
                modules.Add(nt);
            }

            if (def.CrankZones.Count > 0 || def.CrankDoors.Count > 0)
            {
                var gc = new GeneratorCrankModule();
                foreach (var z in def.CrankZones) gc.AddZone(z.LinkId, ToFix(z.Min), ToFix(z.Max));
                foreach (var d in def.CrankDoors) gc.AddDoor(d.LinkId, ToFix(d.Center), ToFix(d.HalfExtents));
                modules.Add(gc);
            }

            if (def.EchoLadderZones.Count > 0)
            {
                var el = new EchoLadderModule();
                foreach (var z in def.EchoLadderZones) el.AddZone(ToFix(z.Min), ToFix(z.Max));
                modules.Add(el);
            }

            if (def.ColoredItems.Count > 0 || def.Keyholes.Count > 0 || def.ColorDoors.Count > 0)
            {
                var cc = new ColorCarryModule();
                foreach (var ci in def.ColoredItems)
                    if (crates != null && ci.CrateIndex >= 0 && ci.CrateIndex < crates.Crates.Count)
                        cc.RegisterColoredItem(crates.Crates[ci.CrateIndex], ci.Color);
                foreach (var k in def.Keyholes) cc.AddKeyhole(k.LinkId, k.RequiredColor, ToFix(k.Min), ToFix(k.Max));
                foreach (var d in def.ColorDoors) cc.AddDoor(d.LinkId, ToFix(d.Center), ToFix(d.HalfExtents));
                modules.Add(cc);
            }

            if (def.MemoryCheckpoints.Count > 0 || def.MemoryDoors.Count > 0)
            {
                var ml = new MemoryLockModule();
                foreach (var c in def.MemoryCheckpoints) ml.AddCheckpoint(ToFix(c.Min), ToFix(c.Max));
                foreach (var d in def.MemoryDoors) ml.AddDoor(d.LinkId, ToFix(d.Center), ToFix(d.HalfExtents));
                modules.Add(ml);
            }

            if (def.CumulativeLevers.Count > 0 || def.CumulativeLeverDoors.Count > 0)
            {
                var cl = new CumulativeLeverModule();
                foreach (var l in def.CumulativeLevers) cl.AddLever(l.LinkId, ToFix(l.ZoneMin), ToFix(l.ZoneMax), Fix64.FromFloat(l.ChargePerActivation), Fix64.FromFloat(l.Threshold));
                foreach (var d in def.CumulativeLeverDoors) cl.AddDoor(d.LinkId, ToFix(d.Center), ToFix(d.HalfExtents));
                modules.Add(cl);
            }

            if (def.EchoPassword.TargetSequence != null && def.EchoPassword.TargetSequence.Length > 0)
            {
                var ep = new EchoPasswordModule();
                ep.Configure(ToFix(def.EchoPassword.EntryMin), ToFix(def.EchoPassword.EntryMax), def.EchoPassword.TargetSequence);
                foreach (var d in def.EchoPasswordDoors) ep.AddDoor(d.LinkId, ToFix(d.Center), ToFix(d.HalfExtents));
                modules.Add(ep);
            }

            if (def.TwoBodyLocks.Count > 0 || def.TwoBodyLockDoors.Count > 0)
            {
                var tbl = new TwoBodyLockModule();
                foreach (var l in def.TwoBodyLocks) tbl.Configure(l.LinkId, ToFix(l.AMin), ToFix(l.AMax), ToFix(l.BMin), ToFix(l.BMax), l.ToleranceTicks);
                foreach (var d in def.TwoBodyLockDoors) tbl.AddDoor(d.LinkId, ToFix(d.Center), ToFix(d.HalfExtents));
                modules.Add(tbl);
            }

            if (def.CrumbleTiles.Count > 0)
            {
                var cf = new CrumbleFloorModule();
                foreach (var t in def.CrumbleTiles) cf.AddTile(ToFix(t.Center), ToFix(t.HalfExtents), t.FuseTicks);
                modules.Add(cf);
            }

            if (def.Flood.MaxY > 0)
            {
                var fl = new FloodModule();
                fl.Configure(Fix64.FromFloat(def.Flood.StartY), Fix64.FromFloat(def.Flood.MaxY));
                foreach (var d in def.Drains) fl.AddDrain(ToFix(d.Min), ToFix(d.Max));
                modules.Add(fl);
            }

            if (def.EnableEchoConveyor)
                modules.Add(new EchoConveyorModule());

            if (def.NegotiationPlates.Count > 0 || def.NegotiationDoors.Count > 0)
            {
                var sn = new SelfNegotiationModule();
                foreach (var p in def.NegotiationPlates) sn.AddPlate(p.LinkId, ToFix(p.Min), ToFix(p.Max), Fix64.FromFloat(p.TrustThreshold));
                foreach (var d in def.NegotiationDoors) sn.AddDoor(d.LinkId, ToFix(d.Center), ToFix(d.HalfExtents));
                modules.Add(sn); // AddModule (called by the caller after Configure) wires ISimAware automatically
            }

            if (def.TurretEmitters.Count > 0)
            {
                var st = new SelfTurretModule();
                foreach (var t in def.TurretEmitters) st.AddEmitter(ToFix(t.ControlMin), ToFix(t.ControlMax), ToFix(t.BlastMin), ToFix(t.BlastMax));
                modules.Add(st);
            }

            if (def.Dominoes.Count > 0 || def.DominoDoors.Count > 0)
            {
                var dc = new DominoChainModule();
                foreach (var d in def.Dominoes) dc.AddDomino(ToFix(d.ZoneMin), ToFix(d.ZoneMax), d.FallTicks);
                foreach (var d in def.DominoDoors) dc.AddDoor(d.LinkId, ToFix(d.Center), ToFix(d.HalfExtents));
                modules.Add(dc);
            }

            if (def.Pendulums.Count > 0)
            {
                var pm = new PendulumModule();
                foreach (var p in def.Pendulums) pm.AddPendulum(ToFix(p.Center), ToFix(p.HalfExtents), Fix64.FromFloat(p.SwingHalfWidth), p.PeriodTicks);
                modules.Add(pm);
            }

            if (def.DecoyHazards.Count > 0)
            {
                var dh = new DecoyHazardModule();
                foreach (var d in def.DecoyHazards) dh.AddDecoyHazard(ToFix(d.Start), ToFix(d.HalfExtents), Fix64.FromFloat(d.ChaseSpeed));
                modules.Add(dh);
            }

            if (def.BodyShieldBeams.Count > 0)
            {
                var bs = new BodyShieldModule();
                foreach (var b in def.BodyShieldBeams) bs.AddBeam(ToFix(b.Min), ToFix(b.Max), b.RechargeTicks);
                modules.Add(bs);
            }

            if (def.MirrorPoints.Count > 0 || def.MirrorRelayDoors.Count > 0)
            {
                var mr = new MirrorRelayModule();
                foreach (var m in def.MirrorPoints) mr.AddMirrorPoint(ToFix(m.Min), ToFix(m.Max));
                foreach (var d in def.MirrorRelayDoors) mr.AddDoor(d.LinkId, ToFix(d.Center), ToFix(d.HalfExtents));
                modules.Add(mr);
            }

            if (def.MirroredPlayerCheckpoints.Count > 0 || def.MirrorPathCheckpoints.Count > 0 || def.MirroredPathDoors.Count > 0)
            {
                var mp = new MirroredPathModule();
                foreach (var c in def.MirroredPlayerCheckpoints) mp.AddPlayerCheckpoint(ToFix(c.Min), ToFix(c.Max));
                foreach (var c in def.MirrorPathCheckpoints) mp.AddMirrorCheckpoint(ToFix(c.Min), ToFix(c.Max));
                foreach (var d in def.MirroredPathDoors) mp.AddDoor(d.LinkId, ToFix(d.Center), ToFix(d.HalfExtents));
                modules.Add(mp);
            }

            if (def.CrusherPistons.Count > 0)
            {
                var cp = new CrusherPistonModule();
                foreach (var p in def.CrusherPistons) cp.AddPiston(ToFix(p.Min), ToFix(p.Max), p.ExtendedTicks, p.RetractedTicks);
                modules.Add(cp);
            }

            if (def.CaretakerDrones.Count > 0)
            {
                var cd = new CaretakerDroneModule();
                foreach (var d in def.CaretakerDrones) cd.AddDrone(ToFix(d.Start), ToFix(d.HalfExtents), Fix64.FromFloat(d.ChaseSpeed));
                modules.Add(cd); // AddModule wires ISimAware automatically
            }

            return (world, ToFix(def.Spawn), modules);
        }
    }
}
