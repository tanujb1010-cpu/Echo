using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// "Delayed Charge" (#11): arming (entering the arm zone) and detonation (killing bodies in the blast
    /// zone) are decoupled in time by a fixed fuse — so an Echo can arm the charge on an earlier run and it
    /// detonates a fuse-length later, by which point the live player has moved elsewhere.
    /// </summary>
    public sealed class DelayedChargeModule : ILevelModule
    {
        private struct Charge
        {
            public Fix64Vec2 ArmMin, ArmMax;
            public Fix64Vec2 BlastMin, BlastMax;
            public int FuseTicks;
            public bool Armed;
            public int Countdown;
            public bool Spent;
        }

        private readonly List<Charge> _charges = new List<Charge>();

        /// <summary>Bodies killed this level by any charge. Cleared on reset.</summary>
        public int KillsThisRun { get; private set; }

        public void AddCharge(Fix64Vec2 armMin, Fix64Vec2 armMax, Fix64Vec2 blastMin, Fix64Vec2 blastMax, int fuseTicks)
            => _charges.Add(new Charge
            {
                ArmMin = armMin,
                ArmMax = armMax,
                BlastMin = blastMin,
                BlastMax = blastMax,
                FuseTicks = fuseTicks,
                Armed = false,
                Countdown = 0,
                Spent = false
            });

        public bool IsArmed(int index) => _charges[index].Armed;
        public bool HasFired(int index) => _charges[index].Spent;

        public void Tick(IReadOnlyList<SimEntity> allBodies)
        {
            for (int c = 0; c < _charges.Count; c++)
            {
                Charge ch = _charges[c];
                if (ch.Spent) continue;

                if (!ch.Armed)
                {
                    for (int b = 0; b < allBodies.Count; b++)
                    {
                        SimEntity body = allBodies[b];
                        if (!body.Active) continue;
                        if (body.MaxX > ch.ArmMin.X && body.MinX < ch.ArmMax.X && body.MaxY > ch.ArmMin.Y && body.MinY < ch.ArmMax.Y)
                        {
                            ch.Armed = true;
                            ch.Countdown = ch.FuseTicks;
                            break;
                        }
                    }
                }
                else
                {
                    ch.Countdown--;
                    if (ch.Countdown <= 0)
                    {
                        for (int b = 0; b < allBodies.Count; b++)
                        {
                            SimEntity body = allBodies[b];
                            if (!body.Active) continue;
                            if (body.MaxX > ch.BlastMin.X && body.MinX < ch.BlastMax.X && body.MaxY > ch.BlastMin.Y && body.MinY < ch.BlastMax.Y)
                            {
                                body.Active = false;      // killed
                                KillsThisRun++;
                            }
                        }
                        ch.Spent = true;
                    }
                }

                _charges[c] = ch;
            }
        }

        public void CollectDynamicBodies(List<SimEntity> into) { }
        public void OnCharacterStep(SimEntity character, in InputCommand cmd) { }
        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids) { }
        public void CollectSolids(List<SimEntity> into) { }

        public void ContributeHash(ref StateHash h)
        {
            for (int i = 0; i < _charges.Count; i++)
            {
                Charge ch = _charges[i];
                h.Add(ch.Armed);
                h.Add(ch.Countdown);
                h.Add(ch.Spent);
            }
            h.Add(KillsThisRun);
        }

        public void ResetModule()
        {
            KillsThisRun = 0;
            for (int i = 0; i < _charges.Count; i++)
            {
                Charge ch = _charges[i];
                ch.Armed = false;
                ch.Countdown = 0;
                ch.Spent = false;
                _charges[i] = ch;
            }
        }
    }
}
