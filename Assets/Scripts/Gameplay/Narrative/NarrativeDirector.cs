using Echo.Infra;

namespace Echo.Gameplay.Narrative
{
    /// <summary>
    /// Wires live gameplay events to the invisible ending drivers in <see cref="BranchState"/> (docs/02 §8).
    /// Subscribes to the <see cref="EventBus"/> so gameplay code only raises semantic events
    /// (EchoSacrificed, EchoPruned, SecretFound, EchoReliedOn) and never reaches into narrative state —
    /// preserving decoupling. Pure C# → unit-testable by publishing events and inspecting the state.
    /// </summary>
    public sealed class NarrativeDirector
    {
        public BranchState State { get; }
        private readonly EventBus _bus;

        public NarrativeDirector(EventBus bus, BranchState state)
        {
            _bus = bus;
            State = state;
            _bus.Subscribe<EchoSacrificedEvent>(OnSacrificed);
            _bus.Subscribe<EchoPrunedEvent>(OnPruned);
            _bus.Subscribe<SecretFoundEvent>(OnSecret);
            _bus.Subscribe<EchoReliedOnEvent>(OnRelied);
        }

        public void Dispose()
        {
            _bus.Unsubscribe<EchoSacrificedEvent>(OnSacrificed);
            _bus.Unsubscribe<EchoPrunedEvent>(OnPruned);
            _bus.Unsubscribe<SecretFoundEvent>(OnSecret);
            _bus.Unsubscribe<EchoReliedOnEvent>(OnRelied);
        }

        private void OnSacrificed(EchoSacrificedEvent _) => State.OnEchoSacrificed();
        private void OnPruned(EchoPrunedEvent _) => State.OnEchoPruned();
        private void OnSecret(SecretFoundEvent _) => State.OnSecretFound();
        private void OnRelied(EchoReliedOnEvent _) => State.OnReliedOnEcho();
    }
}
