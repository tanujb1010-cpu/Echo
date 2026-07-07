using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Echo;
using Echo.Core.Replay;
using Echo.Core.Sim;
using NUnit.Framework;

namespace Echo.Tests.EditMode
{
    /// <summary>
    /// The project's most important tests: if these fail, the game does not exist (docs/07 Phase 1).
    /// All run headless — LevelSimulation and the sim core have no UnityEngine dependency.
    /// </summary>
    public class ReplayDeterminismTests
    {
        private const ulong Seed = 0xEC0FEED5_1234_5678UL;

        private static TileCollisionWorld MakeWorld()
        {
            var w = new TileCollisionWorld(48, 24);
            w.FillFloor(0);
            // a small step the body can jump onto, to exercise vertical collision
            w.SetSolid(12, 1, true);
            w.SetSolid(12, 2, true);
            return w;
        }

        private static List<InputCommand> ScriptedInputs(int ticks)
        {
            var inputs = new List<InputCommand>(ticks);
            for (int t = 0; t < ticks; t++)
            {
                sbyte move = (sbyte)(t < ticks / 2 ? 8 : -4);     // right then drift left
                var buttons = InputButtons.None;
                if (t == 18 || t == 60 || t == 95) buttons |= InputButtons.Jump;
                if (t % 25 == 0) buttons |= InputButtons.Interact;
                inputs.Add(new InputCommand(move, buttons));
            }
            return inputs;
        }

        // ---- Record a single body's run into a Timeline (no gates: it's the "live human"). ----
        private static Timeline RecordRun(TileCollisionWorld world, IReadOnlyList<InputCommand> inputs)
        {
            var rec = new Recorder();
            rec.Begin("TEST", runId: 0, saveSeed: Seed, tickRate: 60);

            var body = new SimEntity { Id = 1, Position = Fix64Vec2.FromInt(4, 3), HalfExtents = new Fix64Vec2(Fix64.FromFloat(0.4f), Fix64.FromFloat(0.45f)) };
            var loco = new LocomotionState();
            body.Add(loco);
            var noSolids = new List<SimEntity>();
            var tuning = MotorTuning.Default;

            foreach (var cmd in inputs)
            {
                CharacterMotor.Step(body, loco, cmd, tuning, world, noSolids);
                var h = StateHash.New();
                body.ContributeHash(ref h);
                rec.RecordTick(cmd, h);
            }
            return rec.Bank();
        }

        [Test]
        public void Fix64_BasicMath_IsExact()
        {
            Assert.AreEqual(Fix64.FromInt(6), Fix64.FromInt(2) * Fix64.FromInt(3));
            Assert.AreEqual(Fix64.FromInt(2), Fix64.FromInt(6) / Fix64.FromInt(3));
            Assert.AreEqual(Fix64.FromInt(4), Fix64.Sqrt(Fix64.FromInt(16)));
            Assert.IsTrue(Fix64.FromFloat(0.5f) == Fix64.Half);
        }

        [Test]
        public void Replay_GatesOff_ReproducesRecordingBitIdentical()
        {
            var world = MakeWorld();
            var inputs = ScriptedInputs(120);
            Timeline timeline = RecordRun(world, inputs);

            // Replay the recorded inputs through the identical motor; assert per-tick hash matches.
            var guard = new DesyncGuard { ThrowOnDesync = true };
            guard.Arm(timeline);

            var src = new ReplaySource();
            src.Init(timeline);
            var body = new SimEntity { Id = 1, Position = Fix64Vec2.FromInt(4, 3), HalfExtents = new Fix64Vec2(Fix64.FromFloat(0.4f), Fix64.FromFloat(0.45f)) };
            var loco = new LocomotionState();
            body.Add(loco);
            var noSolids = new List<SimEntity>();
            var tuning = MotorTuning.Default;

            for (int t = 0; t < timeline.TickCount; t++)
            {
                CharacterMotor.Step(body, loco, src.CurrentInput(), tuning, world, noSolids);
                var h = StateHash.New();
                body.ContributeHash(ref h);
                Assert.IsTrue(guard.Verify(t, h.Value), $"Desync at tick {t}");
                src.Advance();
            }
            Assert.AreEqual(0, guard.Mismatches, "Replay diverged from recording.");
        }

        [Test]
        public void LevelSimulation_SoakReplay_IsReproducible()
        {
            // Run an identical scripted session twice; the per-tick world-hash streams must match.
            ulong[] RunSession()
            {
                var sim = new LevelSimulation(maxEchoes: 6);
                sim.Configure(MakeWorld(), MotorTuning.Default, Seed, "W1_L1",
                    Fix64Vec2.FromInt(4, 3), GateMask.All);
                sim.BeginLevel();

                var inputs = ScriptedInputs(90);
                var hashes = new List<ulong>();

                // run 1
                foreach (var cmd in inputs) hashes.Add(sim.Step(cmd));
                sim.Restart();                       // bank run 1 → becomes an Echo
                // run 2 (now with one Echo replaying alongside)
                foreach (var cmd in inputs) hashes.Add(sim.Step(cmd));
                sim.Restart();
                // run 3 (two Echoes)
                foreach (var cmd in inputs) hashes.Add(sim.Step(cmd));

                return hashes.ToArray();
            }

            ulong[] a = RunSession();
            ulong[] b = RunSession();
            CollectionAssert.AreEqual(a, b, "LevelSimulation is not reproducible across identical sessions.");
        }

        [Test]
        public void Gates_AreDeterministic_AndDormantBelowThreshold()
        {
            var drives = new DriveModel();
            drives.Set(Fix64.Zero, Fix64.One, Fix64.Zero, Fix64.Zero, Fix64.Zero); // max autonomy
            var action = new InputCommand(8, InputButtons.Interact);

            // Below awakening salience → never diverges, regardless of drives.
            var dormant = GateEvaluator.Evaluate(Trait.Devoted, drives, Fix64.FromFloat(0.1f),
                action, Seed, runId: 2, tick: 30, GateMask.All);
            Assert.IsFalse(dormant.Diverged);
            Assert.AreEqual(GateType.None, dormant.Type);

            // Awake + same inputs → identical decision twice (pure function).
            var d1 = GateEvaluator.Evaluate(Trait.Stubborn, drives, Fix64.One, action, Seed, 2, 30, GateMask.All);
            var d2 = GateEvaluator.Evaluate(Trait.Stubborn, drives, Fix64.One, action, Seed, 2, 30, GateMask.All);
            Assert.AreEqual(d1.Type, d2.Type);
            Assert.AreEqual(d1.Output, d2.Output);

            // Disabling the trait's gate mask suppresses divergence.
            var masked = GateEvaluator.Evaluate(Trait.Stubborn, drives, Fix64.One, action, Seed, 2, 30, GateMask.Improvise);
            Assert.IsFalse(masked.Diverged);
        }
    }
}
