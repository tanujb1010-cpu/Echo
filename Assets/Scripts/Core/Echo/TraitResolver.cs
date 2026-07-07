using Echo.Core.Determinism;

namespace Echo.Core.Echo
{
    public enum Trait { Devoted, Curious, Stubborn, Skittish, Trickster, Mournful, Brave }

    /// <summary>
    /// Maps (drives, salience) → a dominant Trait (docs/04 §3.2). Below the awakening threshold an
    /// Echo is always Devoted (a perfect, obedient replay) — guaranteeing early-game Echoes behave
    /// predictably while the player learns. Pure function → testable, reproducible.
    /// </summary>
    public static class TraitResolver
    {
        /// <summary>Salience below this → the Echo cannot diverge yet (World 1–2 stay obedient).</summary>
        public static readonly Fix64 AwakeningThreshold = Fix64.FromFloat(0.35f);

        public static Trait Resolve(DriveModel d, Fix64 salience)
        {
            if (salience < AwakeningThreshold) return Trait.Devoted;

            return d.Dominant() switch
            {
                DriveModel.Drive.Curiosity        => Trait.Curious,
                DriveModel.Drive.Autonomy         => Trait.Stubborn,
                DriveModel.Drive.Attachment       => Trait.Devoted,
                DriveModel.Drive.SelfPreservation => Trait.Skittish,
                DriveModel.Drive.Spite            => Trait.Trickster,
                _                                 => Trait.Devoted,
            };
        }

        public static string Name(Trait t) => t.ToString();
    }
}
