using System;

namespace Echo.Core.Replay
{
    [Flags]
    public enum InputButtons : byte
    {
        None     = 0,
        Jump     = 1 << 0,
        Grab     = 1 << 1,   // grab / release toggle edge handled by systems
        Interact = 1 << 2,   // press / throw
        Crouch   = 1 << 3,   // used by "Echo Ladder" (mechanic #2)
        Lantern  = 1 << 4,   // light-solid mechanics
    }

    /// <summary>
    /// The atomic unit of recording: one tick of player intent, device-agnostic.
    /// KBM, gamepad and touch all collapse into this struct (docs/05 §13), so a run recorded on any
    /// platform replays bit-identically on any other. ~3 bytes on the wire before compression.
    ///
    /// MoveX is quantized to whole steps [-8..8] (8 = full tilt) so analog sticks are deterministic.
    /// </summary>
    public readonly struct InputCommand : IEquatable<InputCommand>
    {
        public readonly sbyte MoveX;        // quantized horizontal intent, -8..8
        public readonly InputButtons Buttons;

        public InputCommand(sbyte moveX, InputButtons buttons)
        {
            MoveX = moveX < -8 ? (sbyte)-8 : (moveX > 8 ? (sbyte)8 : moveX);
            Buttons = buttons;
        }

        public static readonly InputCommand Idle = new InputCommand(0, InputButtons.None);

        public bool Has(InputButtons b) => (Buttons & b) != 0;

        /// <summary>Quantize a raw analog axis [-1,1] to the deterministic step domain.</summary>
        public static sbyte QuantizeAxis(float rawAxis)
        {
            int q = (int)Math.Round(Math.Clamp(rawAxis, -1f, 1f) * 8f);
            return (sbyte)q;
        }

        /// <summary>Normalized move as Fix64 in [-1,1] for the kinematic solver.</summary>
        public Determinism.Fix64 MoveXNormalized()
            => Determinism.Fix64.FromRaw((long)MoveX << 16) / Determinism.Fix64.FromInt(8);

        public bool Equals(InputCommand other) => MoveX == other.MoveX && Buttons == other.Buttons;
        public override bool Equals(object obj) => obj is InputCommand c && Equals(c);
        public override int GetHashCode() => (MoveX << 8) | (byte)Buttons;

        public ushort Pack() => (ushort)(((byte)Buttons << 8) | (byte)(MoveX + 8));
        public static InputCommand Unpack(ushort packed)
            => new InputCommand((sbyte)((packed & 0xFF) - 8), (InputButtons)((packed >> 8) & 0xFF));
    }
}
