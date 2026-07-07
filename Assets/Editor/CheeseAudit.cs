using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Echo.Core.Echo;
using Echo.Core.Replay;
using Echo.Core.Sim;
using Echo.Unity;

namespace Echo.EditorTools
{
    /// <summary>
    /// Phase-4 audit: runs every catalog level headless (real sim, real LevelBuilder) against a squad
    /// of "cheese bots" — input policies with zero Echo usage. Any level a bot completes is solvable
    /// without the core mechanic and gets flagged. Also runs static authoring sanity checks (zones at
    /// the strict-overlap y=1.0 trap, spawn inside solids, unreachable plates).
    ///
    /// CI usage:
    ///   Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod Echo.EditorTools.CheeseAudit.Run -quit
    /// Grep the log for "[CheeseAudit]".
    /// </summary>
    public static class CheeseAudit
    {
        private delegate InputCommand Bot(int tick);

        private static readonly (string Name, Bot Policy)[] Bots =
        {
            ("walk",         t => new InputCommand(8, InputButtons.None)),
            ("hop",          t => new InputCommand(8, t % 30 < 10 ? InputButtons.Jump : InputButtons.None)),
            ("jump-held",    t => new InputCommand(8, InputButtons.Jump)),
            ("grab-walk",    t => new InputCommand(8, InputButtons.Grab)),
            ("wait-walk",    t => t < 600 ? InputCommand.Idle : new InputCommand(8, InputButtons.None)),
            ("interact-hop", t => new InputCommand(8, (t % 30 < 10 ? InputButtons.Jump : InputButtons.None) | InputButtons.Interact)),
        };

        private const int MaxTicks = 3600; // 60 simulated seconds per bot

        [MenuItem("Echo/Audit/Run Cheese Audit")]
        public static void Run()
        {
            int issues = 0;
            for (int li = 0; li < LevelCatalog.Count; li++)
            {
                var entry = LevelCatalog.Levels[li];
                issues += AuditStatic(entry);
                issues += AuditCheese(entry);
            }
            Debug.Log($"[CheeseAudit] RESULT: {issues} issue(s) across {LevelCatalog.Count} levels");
        }

        // ------------------------------------------------------------- static authoring sanity
        private static int AuditStatic(LevelCatalog.Entry entry)
        {
            int issues = 0;
            var def = entry.Create();
            var (world, spawn, _) = LevelBuilder.Build(def);

            int sx = Mathf.FloorToInt(spawn.X.ToFloat()), sy = Mathf.FloorToInt(spawn.Y.ToFloat());
            if (world.IsSolid(sx, sy))
            { Debug.Log($"[CheeseAudit] {def.LevelId}: STATIC spawn tile ({sx},{sy}) is solid"); issues++; }

            // The strict-overlap trap: a ground-level zone starting exactly at y=1.0 never triggers.
            foreach (var (min, what) in GroundZones(def))
                if (Mathf.Approximately(min.y, 1.0f))
                { Debug.Log($"[CheeseAudit] {def.LevelId}: STATIC {what} zone starts at y=1.0 (strict overlap — use 0.9)"); issues++; }

            // The pile-up trap: N bodies entering a quorum zone from one side stack ~0.8u apart, so the
            // pile spans threshold*0.8 from wherever the first parks. A zone without at least one extra
            // body-width of slack leaves the last body straddling the lip (W2_L2 shipped this way once).
            foreach (var q in def.Quorums)
                if (q.Max.x - q.Min.x < q.Threshold * 0.8f + 0.8f)
                { Debug.Log($"[CheeseAudit] {def.LevelId}: STATIC quorum zone {q.Max.x - q.Min.x:0.0}u wide can't fit a {q.Threshold}-body pile (needs ≥{q.Threshold * 0.8f + 0.8f:0.0})"); issues++; }
            foreach (var pl in def.Pulleys)
                if (pl.ZoneMax.x - pl.ZoneMin.x < pl.Threshold * 0.8f + 0.8f)
                { Debug.Log($"[CheeseAudit] {def.LevelId}: STATIC pulley zone {pl.ZoneMax.x - pl.ZoneMin.x:0.0}u wide can't fit a {pl.Threshold}-body pile (needs ≥{pl.Threshold * 0.8f + 0.8f:0.0})"); issues++; }

            return issues;
        }

        private static IEnumerable<(Vector2 min, string what)> GroundZones(LevelDefinition d)
        {
            foreach (var p in d.Plates) yield return (p.Min, "plate");
            foreach (var q in d.Quorums) yield return (q.Min, "quorum");
            foreach (var s in d.Switches) yield return (s.Min, "switch");
            foreach (var h in d.Hazards) yield return (h.Min, "hazard");
            foreach (var c in d.ArrivalCheckpoints) yield return (c.Min, "checkpoint");
            foreach (var s in d.Secrets) yield return (s.Min, "secret");
        }

        // ------------------------------------------------------------- echo-free cheese bots
        private static int AuditCheese(LevelCatalog.Entry entry)
        {
            int issues = 0;
            foreach (var (name, policy) in Bots)
            {
                var def = entry.Create(); // fresh def + sim per bot: modules hold state
                var (world, spawn, modules) = LevelBuilder.Build(def);
                var sim = new LevelSimulation(def.MaxEchoes);
                sim.Configure(world, MotorTuning.Default, (ulong)def.SaveSeed, def.LevelId, spawn, def.EnabledGates);
                foreach (var m in modules) sim.AddModule(m);
                sim.BeginLevel();

                for (int t = 0; t < MaxTicks; t++)
                {
                    sim.Step(policy(t));
                    if (sim.PlayerDead) break;
                    float px = sim.PlayerPosition.X.ToFloat(), py = sim.PlayerPosition.Y.ToFloat();
                    if (py < -5f) break; // fell out of the world
                    if (px > def.ExitMin.x && px < def.ExitMax.x && py > def.ExitMin.y && py < def.ExitMax.y)
                    {
                        Debug.Log($"[CheeseAudit] {def.LevelId}: CHEESE '{name}' bot finished with zero Echoes at tick {t}");
                        issues++;
                        break;
                    }
                }
            }
            return issues;
        }
    }
}
