using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Anti-Echo Field (#45): a zone that instantly deletes any ECHO that enters it — the live player
    /// passes through unharmed. Forces the player to route the braid's approach paths AROUND the field,
    /// since an Echo caught inside is gone for the rest of the run (Active=false, the same despawn
    /// semantics as a hazard kill — including triggering the same EchoSacrificed/Grievance wiring in
    /// LevelSimulation, since that's a generic PrevActive→inactive transition, not something specific to
    /// HazardModule). Distinguishes "Echo" from the player using the id-range convention already
    /// established by <see cref="LevelSimulation"/>: the live player's id always starts at 100,000
    /// (SpawnPlayer), while every Echo's id is its (small) runId+1 — so an entity below that boundary is
    /// unambiguously an Echo, never the player.
    /// </summary>
    public sealed class AntiEchoFieldModule : ILevelModule
    {
        private struct Field { public Fix64Vec2 Min, Max; }
        private readonly List<Field> _fields = new List<Field>();
        private const int PlayerIdBase = 100000; // mirrors LevelSimulation.SpawnPlayer's id scheme

        public int DeletionsThisRun { get; private set; }

        public void AddField(Fix64Vec2 min, Fix64Vec2 max) => _fields.Add(new Field { Min = min, Max = max });

        public void Tick(IReadOnlyList<SimEntity> allBodies)
        {
            for (int b = 0; b < allBodies.Count; b++)
            {
                SimEntity body = allBodies[b];
                if (!body.Active || body.Id >= PlayerIdBase) continue; // only Echoes are vulnerable
                for (int f = 0; f < _fields.Count; f++)
                {
                    Field field = _fields[f];
                    if (body.MaxX > field.Min.X && body.MinX < field.Max.X && body.MaxY > field.Min.Y && body.MinY < field.Max.Y)
                    {
                        body.Active = false;
                        DeletionsThisRun++;
                        break;
                    }
                }
            }
        }

        public void CollectDynamicBodies(List<SimEntity> into) { }
        public void OnCharacterStep(SimEntity character, in InputCommand cmd) { }
        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids) { }
        public void CollectSolids(List<SimEntity> into) { }
        public void ContributeHash(ref StateHash h) => h.Add(DeletionsThisRun);
        public void ResetModule() => DeletionsThisRun = 0;
    }
}
