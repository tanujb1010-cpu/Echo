namespace Echo.Core.Sim
{
    /// <summary>
    /// Opt-in for a module that needs a live reference back to its owning <see cref="LevelSimulation"/> —
    /// e.g. to query per-Echo relationship state (Self-Negotiation #50) that isn't otherwise reachable from
    /// the plain <see cref="SimEntity"/> bodies a module normally sees. Modules are built by
    /// <c>LevelBuilder.Build()</c> BEFORE the simulation itself is constructed, so this can't be a
    /// constructor dependency; the caller sets it once, right after both exist and before the first tick.
    /// </summary>
    public interface ISimAware
    {
        void SetSimulation(LevelSimulation sim);
    }
}
