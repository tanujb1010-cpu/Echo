using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;

namespace Echo.Core.Sim
{
    /// <summary>
    /// A pluggable, deterministic level mechanic (plates+doors, crates, hazards, time-fields, …). Keeps the
    /// Core simulation ignorant of specific gameplay: <see cref="LevelSimulation"/> ticks modules, lets them
    /// react to each character, simulate their own dynamic bodies, and contribute solids — but Gameplay
    /// supplies the concrete behavior. Preserves the downward-only dependency rule (docs/06 §2).
    ///
    /// Per-tick call order (all deterministic):
    ///   ResetModule (on restart) → [each tick] CollectDynamicBodies → Tick(sensors/hazards) →
    ///   (characters move; OnCharacterStep after each) → StepDynamics(own free bodies) → ContributeHash.
    /// </summary>
    public interface ILevelModule
    {
        /// <summary>Reset to initial state on level (re)start.</summary>
        void ResetModule();

        /// <summary>Append dynamic bodies this module owns (e.g., crates) so sensors see them and they're hashed.</summary>
        void CollectDynamicBodies(List<SimEntity> into);

        /// <summary>Sensors + hazards: read this tick's bodies; set actuators / mark lethal bodies inactive.</summary>
        void Tick(IReadOnlyList<SimEntity> allBodies);

        /// <summary>React to one character just after it moved (e.g., a carried crate follows its carrier).</summary>
        void OnCharacterStep(SimEntity character, in InputCommand cmd);

        /// <summary>Simulate this module's own free dynamic bodies (e.g., crate gravity/collision).</summary>
        void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids);

        /// <summary>Append solid bodies this module owns (closed doors, free crates) to the collision set.</summary>
        void CollectSolids(List<SimEntity> into);

        /// <summary>Feed deterministic state into the world hash (for desync/soak verification).</summary>
        void ContributeHash(ref StateHash h);
    }
}
