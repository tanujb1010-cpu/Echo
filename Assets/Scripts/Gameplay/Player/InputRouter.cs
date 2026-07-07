using Echo.Core.Replay;

namespace Echo.Gameplay.Player
{
    /// <summary>
    /// Device-agnostic input source. KBM, gamepad and touch each implement this; the router collapses
    /// them all into one <see cref="InputCommand"/> (docs/05 §13), so the recorder is identical on every
    /// platform and a PC-recorded run replays bit-identically on mobile.
    /// </summary>
    public interface IInputProvider
    {
        float MoveAxis { get; }   // -1..1
        bool Jump { get; }
        bool Grab { get; }
        bool Interact { get; }
        bool Crouch { get; }
        bool Lantern { get; }
        bool RestartHeld { get; } // not recorded; drives the restart flow
    }

    /// <summary>
    /// Pure converter from a raw provider to a deterministic, quantized <see cref="InputCommand"/>.
    /// Pure C# (no UnityEngine) → unit-testable and reusable across all platforms.
    /// </summary>
    public static class InputRouter
    {
        public static InputCommand ToCommand(IInputProvider p)
        {
            sbyte move = InputCommand.QuantizeAxis(p.MoveAxis);
            InputButtons b = InputButtons.None;
            if (p.Jump) b |= InputButtons.Jump;
            if (p.Grab) b |= InputButtons.Grab;
            if (p.Interact) b |= InputButtons.Interact;
            if (p.Crouch) b |= InputButtons.Crouch;
            if (p.Lantern) b |= InputButtons.Lantern;
            return new InputCommand(move, b);
        }
    }
}
