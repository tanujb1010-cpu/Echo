using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Crusher Pistons (Content Bible D2): a rhythmic hazard that alternates between an extended (lethal)
    /// phase and a retracted (safe) phase on a fixed, autonomous cycle — no Echo interaction required to
    /// arm or disarm it, unlike Self-Turret (#22). The player must read and time the piston's own rhythm,
    /// exactly as a real Echo's recorded crossing would have to. Deterministic: a pure function of tick
    /// count modulo the cycle length, so every run sees the identical extend/retract schedule.
    /// </summary>
    public sealed class CrusherPistonModule : ILevelModule
    {
        private struct Piston { public Fix64Vec2 Min, Max; public int ExtendedTicks; public int RetractedTicks; public int Counter; }

        private readonly List<Piston> _pistons = new List<Piston>();

        public int KillsThisRun { get; private set; }
        public int PistonCount => _pistons.Count;

        public void AddPiston(Fix64Vec2 min, Fix64Vec2 max, int extendedTicks, int retractedTicks)
            => _pistons.Add(new Piston { Min = min, Max = max, ExtendedTicks = extendedTicks, RetractedTicks = retractedTicks, Counter = 0 });

        public bool IsExtended(int index)
        {
            Piston p = _pistons[index];
            int cycle = p.ExtendedTicks + p.RetractedTicks;
            if (cycle <= 0) return false;
            return (p.Counter % cycle) < p.ExtendedTicks;
        }

        /// <summary>Read-only telegraph data for the presentation layer (never mutates sim state).</summary>
        public void GetPistonBounds(int index, out Fix64Vec2 min, out Fix64Vec2 max)
        {
            min = _pistons[index].Min;
            max = _pistons[index].Max;
        }

        /// <summary>Ticks until this piston next flips phase (extends or retracts) — drives rhythm hints.</summary>
        public int TicksUntilPhaseFlip(int index)
        {
            Piston p = _pistons[index];
            int cycle = p.ExtendedTicks + p.RetractedTicks;
            if (cycle <= 0) return int.MaxValue;
            int pos = p.Counter % cycle;
            return pos < p.ExtendedTicks ? p.ExtendedTicks - pos : cycle - pos;
        }

        public void Tick(IReadOnlyList<SimEntity> allBodies)
        {
            for (int i = 0; i < _pistons.Count; i++)
            {
                Piston p = _pistons[i];
                if (IsExtended(i))
                {
                    for (int b = 0; b < allBodies.Count; b++)
                    {
                        SimEntity body = allBodies[b];
                        if (!body.Active) continue;
                        if (body.MaxX > p.Min.X && body.MinX < p.Max.X && body.MaxY > p.Min.Y && body.MinY < p.Max.Y)
                        {
                            body.Active = false;
                            KillsThisRun++;
                        }
                    }
                }
                p.Counter++;
                _pistons[i] = p;
            }
        }

        public void CollectDynamicBodies(List<SimEntity> into) { }
        public void OnCharacterStep(SimEntity character, in InputCommand cmd) { }
        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids) { }
        public void CollectSolids(List<SimEntity> into) { }

        public void ContributeHash(ref StateHash h)
        {
            for (int i = 0; i < _pistons.Count; i++) h.Add(_pistons[i].Counter);
            h.Add(KillsThisRun);
        }

        public void ResetModule()
        {
            KillsThisRun = 0;
            for (int i = 0; i < _pistons.Count; i++)
            {
                Piston p = _pistons[i];
                p.Counter = 0;
                _pistons[i] = p;
            }
        }
    }
}
