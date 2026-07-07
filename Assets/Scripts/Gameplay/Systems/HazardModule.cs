using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Lethal regions (Content Bible hazards + "Echo Sacrifice" #42, "Body Shield" #12). A body entering a
    /// hazard is killed (deactivated). A *consumable* hazard is spent on its first kill — so an Echo can
    /// throw itself onto it to clear the way for the live self. Deterministic: kills are a pure function of
    /// body AABBs, so the sacrifice reproduces identically every run.
    /// </summary>
    public sealed class HazardModule : ILevelModule
    {
        private struct Hazard { public Fix64Vec2 Min, Max; public bool Consumable; public bool Spent; }
        private readonly List<Hazard> _hazards = new List<Hazard>();

        /// <summary>Bodies killed this level (for analytics / sacrifice counting). Cleared on reset.</summary>
        public int KillsThisRun { get; private set; }

        public void AddHazard(Fix64Vec2 min, Fix64Vec2 max, bool consumable)
            => _hazards.Add(new Hazard { Min = min, Max = max, Consumable = consumable, Spent = false });

        public void Tick(IReadOnlyList<SimEntity> allBodies)
        {
            for (int h = 0; h < _hazards.Count; h++)
            {
                Hazard hz = _hazards[h];
                if (hz.Spent) continue;
                for (int b = 0; b < allBodies.Count; b++)
                {
                    SimEntity body = allBodies[b];
                    if (!body.Active) continue;
                    if (body.MaxX > hz.Min.X && body.MinX < hz.Max.X && body.MaxY > hz.Min.Y && body.MinY < hz.Max.Y)
                    {
                        body.Active = false;          // killed
                        KillsThisRun++;
                        if (hz.Consumable) { hz.Spent = true; break; } // sacrifice clears the hazard
                    }
                }
                _hazards[h] = hz;
            }
        }

        public void CollectDynamicBodies(List<SimEntity> into) { }
        public void OnCharacterStep(SimEntity character, in InputCommand cmd) { }
        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids) { }
        public void CollectSolids(List<SimEntity> into) { }

        public void ContributeHash(ref StateHash h)
        {
            for (int i = 0; i < _hazards.Count; i++) h.Add(_hazards[i].Spent);
            h.Add(KillsThisRun);
        }

        public void ResetModule()
        {
            KillsThisRun = 0;
            for (int i = 0; i < _hazards.Count; i++)
            {
                Hazard hz = _hazards[i]; hz.Spent = false; _hazards[i] = hz;
            }
        }
    }
}
