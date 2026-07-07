using UnityEngine;
using Echo.Gameplay.Player;

namespace Echo.Unity
{
    /// <summary>
    /// Minimal <see cref="IInputProvider"/> over Unity's built-in Input (zero package dependencies, so
    /// the starter project compiles out of the box). PRODUCTION: swap for a New Input System provider
    /// (and a touch provider on mobile) — the rest of the game is unaffected because everything downstream
    /// consumes the device-agnostic <see cref="IInputProvider"/>.
    /// </summary>
    public sealed class LegacyInputProvider : MonoBehaviour, IInputProvider
    {
        // Gamepad: "Horizontal" already reads the left stick / d-pad through the legacy axis config.
        // Buttons use the raw JoystickButton codes for the standard XInput layout:
        //   0 = A (jump)   1 = B (grab)   2 = X (interact)   3 = Y (lantern)
        //   4/5 = bumpers (crouch)        6/7 = select/start (restart hold)
        public float MoveAxis => Input.GetAxisRaw("Horizontal");
        public bool Jump => Input.GetButton("Jump") || Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W)
                          || Input.GetKey(KeyCode.JoystickButton0);
        public bool Grab => Input.GetKey(KeyCode.E) || Input.GetMouseButton(0)
                          || Input.GetKey(KeyCode.JoystickButton1);
        public bool Interact => Input.GetKey(KeyCode.F) || Input.GetMouseButton(1)
                          || Input.GetKey(KeyCode.JoystickButton2);
        public bool Crouch => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.S)
                          || Input.GetKey(KeyCode.JoystickButton4) || Input.GetKey(KeyCode.JoystickButton5);
        public bool Lantern => Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.JoystickButton3);
        public bool RestartHeld => Input.GetKey(KeyCode.R)
                          || Input.GetKey(KeyCode.JoystickButton6) || Input.GetKey(KeyCode.JoystickButton7);
    }
}
