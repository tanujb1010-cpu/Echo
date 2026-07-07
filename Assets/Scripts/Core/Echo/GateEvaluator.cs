using System;
using Echo.Core.Determinism;
using Echo.Core.Replay;

namespace Echo.Core.Echo
{
    public enum GateType { None, Improvise, Hesitate, Refuse, Sabotage }

    [Flags]
    public enum GateMask
    {
        None      = 0,
        Improvise = 1 << 0,
        Hesitate  = 1 << 1,
        Refuse    = 1 << 2,
        Sabotage  = 1 << 3,
        All       = Improvise | Hesitate | Refuse | Sabotage,
    }

    public readonly struct GateDecision
    {
        public readonly GateType Type;
        public readonly InputCommand Output;   // the (possibly modified) input fed to the motor
        public readonly bool Diverged;         // true if Output differs from the recording

        public GateDecision(GateType type, InputCommand output, bool diverged)
        {
            Type = type; Output = output; Diverged = diverged;
        }

        public static GateDecision Pass(InputCommand baseInput) => new GateDecision(GateType.None, baseInput, false);
    }

    /// <summary>
    /// The ONLY place an Echo's authoritative input is allowed to diverge from its recording
    /// (docs/04 §4). Divergence is a PURE FUNCTION of (trait, drives, salience, saveSeed, runId, tick)
    /// — reproducible, unit-testable, and previewable by the Assist intent overlay. This is what makes
    /// an evolving clone *believable* (consistent + motivated) rather than *unfair* (random).
    ///
    /// Hard guarantees enforced here:
    ///  • Below the awakening threshold → never diverges (early Echoes are perfect).
    ///  • Only fires at sparse candidate ticks, and only when the recording carries a real action.
    ///  • Each gate change is bounded (one action), and the chosen type must be enabled for the level.
    /// </summary>
    public static class GateEvaluator
    {
        public const int CandidateInterval = 30; // evaluate at most ~twice/second

        // Per-drive scaling of fire probability; tuned per world via TuningProfile in production.
        private static readonly Fix64 ProbScale = Fix64.FromFloat(0.6f);

        public static GateDecision Evaluate(
            Trait trait, DriveModel drives, Fix64 salience, in InputCommand baseInput,
            ulong saveSeed, int runId, int tick, GateMask enabled)
        {
            // Gate 0: not awake yet → perfect replay.
            if (salience < TraitResolver.AwakeningThreshold) return GateDecision.Pass(baseInput);

            // Gate 1: only consider sparse candidate ticks where a real action is happening.
            if (tick % CandidateInterval != 0) return GateDecision.Pass(baseInput);
            bool actionThisTick = baseInput.MoveX != 0 || baseInput.Buttons != InputButtons.None;
            if (!actionThisTick) return GateDecision.Pass(baseInput);

            // Pick the candidate gate the trait is inclined toward, and its driving need.
            (GateType type, GateMask flag, DriveModel.Drive drive) = trait switch
            {
                Trait.Curious   => (GateType.Improvise, GateMask.Improvise, DriveModel.Drive.Curiosity),
                Trait.Skittish  => (GateType.Hesitate,  GateMask.Hesitate,  DriveModel.Drive.SelfPreservation),
                Trait.Stubborn  => (GateType.Refuse,    GateMask.Refuse,    DriveModel.Drive.Autonomy),
                Trait.Trickster => (GateType.Sabotage,  GateMask.Sabotage,  DriveModel.Drive.Spite),
                _               => (GateType.None,      GateMask.None,      DriveModel.Drive.Attachment),
            };
            if (type == GateType.None || (enabled & flag) == 0) return GateDecision.Pass(baseInput);

            // Deterministic fire test: probability driven by the relevant need, scaled by salience.
            Fix64 prob = drives.ValueOf(drive) * salience * ProbScale;
            var rng = DeterministicRng.Seeded(saveSeed, runId, tick, (int)type);
            if (!rng.Chance(prob)) return GateDecision.Pass(baseInput);

            // Fired → produce a bounded, telegraphable modification of this single action.
            InputCommand output = ApplyGate(type, baseInput);
            return new GateDecision(type, output, !output.Equals(baseInput));
        }

        private static InputCommand ApplyGate(GateType type, in InputCommand baseInput)
        {
            switch (type)
            {
                case GateType.Refuse:
                case GateType.Hesitate:
                    // Skip / delay the recorded action this tick (telegraphed; repairable by the player).
                    return InputCommand.Idle;

                case GateType.Improvise:
                    // Helpful extra: also press Interact (e.g., re-press a plate it sees released).
                    return new InputCommand(baseInput.MoveX, baseInput.Buttons | InputButtons.Interact);

                case GateType.Sabotage:
                    // Spiteful near-miss: walk the opposite way and drop the interaction (fair, telegraphed).
                    return new InputCommand((sbyte)(-baseInput.MoveX), baseInput.Buttons & ~InputButtons.Interact);

                default:
                    return baseInput;
            }
        }
    }
}
