using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Echo Ladder (#2): inside a registered zone, an Echo is solid to other entities only during ticks
    /// where its own replayed input is holding Crouch — otherwise it's passable, letting the player walk
    /// through it. This only ever touches ECHOES, never the live player: an Echo Ladder step is something
    /// you climb ON, and the player must never become a step for its own future Echo. Distinguishes "Echo"
    /// from the player using the id-range convention already established by <see cref="LevelSimulation"/>:
    /// the live player's id always starts at 100,000 (SpawnPlayer), while every Echo's id is its (small)
    /// runId+1 — see <see cref="AntiEchoFieldModule"/> for the same trick. The override is also strictly
    /// zone-scoped: outside every registered zone an Echo's <see cref="SimEntity.SolidToEntities"/> is left
    /// exactly as the base simulation (or other modules) already set it, so this mechanic can't silently
    /// change Echo solidity — and thus break the "Echoes are solid platforms" assumption other puzzles rely
    /// on — anywhere outside its own zones.
    /// </summary>
    public sealed class EchoLadderModule : ILevelModule
    {
        private struct Zone { public Fix64Vec2 Min, Max; }
        private readonly List<Zone> _zones = new List<Zone>();
        private const int PlayerIdBase = 100000; // mirrors LevelSimulation.SpawnPlayer's id scheme

        public void AddZone(Fix64Vec2 min, Fix64Vec2 max) => _zones.Add(new Zone { Min = min, Max = max });

        public void OnCharacterStep(SimEntity character, in InputCommand cmd)
        {
            if (character.Id >= PlayerIdBase) return;

            for (int z = 0; z < _zones.Count; z++)
            {
                Zone zone = _zones[z];
                if (character.MaxX > zone.Min.X && character.MinX < zone.Max.X
                    && character.MaxY > zone.Min.Y && character.MinY < zone.Max.Y)
                {
                    character.SolidToEntities = cmd.Has(InputButtons.Crouch);
                    return;
                }
            }

            // Not in any zone: restore the sim's own default (Echoes are solid) rather than leaving
            // SolidToEntities stuck at whatever it was set to during the last tick inside a zone.
            character.SolidToEntities = true;
        }

        public void ResetModule() { /* zones are fixed at level-build time */ }
        public void CollectDynamicBodies(List<SimEntity> into) { }
        public void Tick(IReadOnlyList<SimEntity> allBodies) { }
        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids) { }
        public void CollectSolids(List<SimEntity> into) { }
        public void ContributeHash(ref StateHash h) { /* stateless: reacts to existing Echo id/position/input only */ }
    }
}
