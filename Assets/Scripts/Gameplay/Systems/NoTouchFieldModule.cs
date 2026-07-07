using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// No-Touch Paradox (#25): a zone that inverts the normal "Echoes are safe solid platforms" rule —
    /// inside a flagged zone, if the live player's AABB overlaps ANY Echo's AABB, the player is killed
    /// (deactivated), since the instability of contacting your own past self can't be tolerated here. This
    /// is the mirror image of <see cref="AntiEchoFieldModule"/>: same id-range trick, opposite victim. Anti-
    /// Echo Field deletes Echoes that enter a field and spares the player; this module spares Echoes and
    /// instead kills the live player when it touches one while inside the zone. Distinguishes "Echo" from
    /// the player using the id-range convention already established by <see cref="LevelSimulation"/>: the
    /// live player's id always starts at 100,000 (SpawnPlayer), while every Echo's id is its (small)
    /// runId+1 — so an entity below that boundary is unambiguously an Echo, never the player.
    /// </summary>
    public sealed class NoTouchFieldModule : ILevelModule
    {
        private struct Zone { public Fix64Vec2 Min, Max; }
        private readonly List<Zone> _zones = new List<Zone>();
        private const int PlayerIdBase = 100000; // mirrors LevelSimulation.SpawnPlayer's id scheme

        public int KillsThisRun { get; private set; }

        public void AddZone(Fix64Vec2 min, Fix64Vec2 max) => _zones.Add(new Zone { Min = min, Max = max });

        public void Tick(IReadOnlyList<SimEntity> allBodies)
        {
            SimEntity player = null;
            for (int b = 0; b < allBodies.Count; b++)
            {
                SimEntity body = allBodies[b];
                if (body.Active && body.Id >= PlayerIdBase) { player = body; break; }
            }
            if (player == null) return;

            bool playerInZone = false;
            for (int z = 0; z < _zones.Count; z++)
            {
                Zone zone = _zones[z];
                if (player.MaxX > zone.Min.X && player.MinX < zone.Max.X && player.MaxY > zone.Min.Y && player.MinY < zone.Max.Y)
                {
                    playerInZone = true;
                    break;
                }
            }
            if (!playerInZone) return;

            for (int b = 0; b < allBodies.Count; b++)
            {
                SimEntity body = allBodies[b];
                if (!body.Active || body.Id >= PlayerIdBase) continue; // only Echoes count as contact
                if (player.MaxX > body.MinX && player.MinX < body.MaxX && player.MaxY > body.MinY && player.MinY < body.MaxY)
                {
                    player.Active = false;
                    KillsThisRun++;
                    break;
                }
            }
        }

        public void CollectDynamicBodies(List<SimEntity> into) { }
        public void OnCharacterStep(SimEntity character, in InputCommand cmd) { }
        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids) { }
        public void CollectSolids(List<SimEntity> into) { }
        public void ContributeHash(ref StateHash h) => h.Add(KillsThisRun);
        public void ResetModule() => KillsThisRun = 0;
    }
}
