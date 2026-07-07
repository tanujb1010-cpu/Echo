using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;

namespace Echo.Core.Sim
{
    /// <summary>Deterministic platformer tuning, all in tile-units / second. Authored as Fix64 (no runtime floats).</summary>
    public struct MotorTuning
    {
        public Fix64 MoveSpeed;
        public Fix64 JumpSpeed;
        public Fix64 Gravity;        // negative (pulls -Y)
        public Fix64 TerminalFall;   // negative clamp
        public int CoyoteTicks;      // grace ticks to still jump after leaving ground
        public int JumpBufferTicks;  // grace ticks: a jump pressed just before landing still fires
        public Fix64 JumpCut;        // upward-velocity multiplier when jump released early (variable height)

        public static MotorTuning Default => new MotorTuning
        {
            MoveSpeed = Fix64.FromInt(8),
            JumpSpeed = Fix64.FromInt(15),
            Gravity = Fix64.FromInt(-55),
            TerminalFall = Fix64.FromInt(-24),
            CoyoteTicks = 6,
            JumpBufferTicks = 6,
            JumpCut = Fix64.FromFloat(0.45f),
        };
    }

    /// <summary>Per-entity locomotion memory needed for deterministic edge/coyote/buffer logic. Hashed → desync-safe.</summary>
    public sealed class LocomotionState : ISimComponent
    {
        public SimEntity Owner { get; set; }
        public bool JumpHeldPrev;
        public int CoyoteCounter;
        public int JumpBufferCounter;
        public bool IsJumping;       // true while rising from a jump (gates the variable-height cut)

        public void Reset()
        {
            JumpHeldPrev = false; CoyoteCounter = 0; JumpBufferCounter = 0; IsJumping = false;
        }

        public void ContributeHash(ref StateHash h)
        {
            h.Add(JumpHeldPrev);
            h.Add(CoyoteCounter);
            h.Add(JumpBufferCounter);
            h.Add(IsJumping);
        }
    }

    /// <summary>
    /// Turns one <see cref="InputCommand"/> into deterministic motion: gravity, snappy horizontal control,
    /// coyote-time + jump-buffering, variable jump height, and collision via <see cref="KinematicSolver"/>.
    /// Both the live player AND every Echo run through this identical function — which is why a recorded
    /// run reproduces exactly on replay (verified bit-identical in TEST_RESULTS_PHASE_1.md).
    ///
    /// Game-feel note: jump-buffer + coyote + variable height are standard "forgiving platformer" feel
    /// AND double as accessibility (looser input timing). All are deterministic and input-driven, so they
    /// never threaten replay fidelity.
    /// </summary>
    public static class CharacterMotor
    {
        public static readonly Fix64 Dt = Fix64.One / Fix64.FromInt(60); // fixed 60 Hz step

        public static void Step(SimEntity e, LocomotionState loco, in InputCommand cmd,
            in MotorTuning t, ICollisionWorld world, IReadOnlyList<SimEntity> solids)
        {
            // Horizontal: snappy, velocity = intent * speed (deterministic, no float accumulation).
            Fix64 vx = cmd.MoveXNormalized() * t.MoveSpeed;
            if (cmd.MoveX != 0) e.FacingSign = cmd.MoveX > 0 ? 1 : -1;

            // Vertical: integrate gravity, clamp terminal.
            Fix64 vy = e.Velocity.Y + t.Gravity * Dt;
            if (vy < t.TerminalFall) vy = t.TerminalFall;

            bool jumpHeld = cmd.Has(InputButtons.Jump);
            bool jumpPressed = jumpHeld && !loco.JumpHeldPrev;
            bool jumpReleased = !jumpHeld && loco.JumpHeldPrev;

            // Jump buffer: remember a press for a few ticks so it can fire the instant we land.
            if (jumpPressed) loco.JumpBufferCounter = t.JumpBufferTicks;
            else if (loco.JumpBufferCounter > 0) loco.JumpBufferCounter--;

            // Coyote: keep "can jump" alive briefly after walking off a ledge.
            loco.CoyoteCounter = e.Grounded ? t.CoyoteTicks : (loco.CoyoteCounter > 0 ? loco.CoyoteCounter - 1 : 0);

            // Fire a jump if one is buffered and we're (still) allowed to jump.
            if (loco.JumpBufferCounter > 0 && loco.CoyoteCounter > 0)
            {
                vy = t.JumpSpeed;
                loco.CoyoteCounter = 0;
                loco.JumpBufferCounter = 0;
                loco.IsJumping = true;
            }

            // Variable height: releasing jump while still rising cuts the ascent (short tap = short hop).
            if (jumpReleased && loco.IsJumping && vy > Fix64.Zero)
            {
                vy = vy * t.JumpCut;
                loco.IsJumping = false;
            }
            if (vy <= Fix64.Zero) loco.IsJumping = false;

            loco.JumpHeldPrev = jumpHeld;
            e.Velocity = new Fix64Vec2(vx, vy);

            // Integrate + resolve collisions against tiles and solid entities (Echoes/crates/doors).
            CollisionFlags flags = KinematicSolver.MoveAndCollide(e, e.Velocity * Dt, world, solids);

            if (flags.Down || flags.Up)
            {
                e.Velocity = new Fix64Vec2(e.Velocity.X, Fix64.Zero);
                if (flags.Up) loco.IsJumping = false; // bonked head → stop treating as rising
            }
            e.Grounded = flags.Grounded;
        }
    }
}
