using UnityEngine;
using Echo.Gameplay.Player;

namespace Echo.Unity
{
    /// <summary>
    /// Touch <see cref="IInputProvider"/> for mobile (docs/01 §7.2). Left side = a floating virtual stick
    /// (horizontal only — this is a side-scroller); right side = action buttons. Because everything funnels
    /// through <see cref="InputRouter"/> into the same quantized <c>InputCommand</c>, a touch-recorded run
    /// replays bit-identically to one recorded on PC.
    ///
    /// Minimal but functional; the shipping version uses a UI-Toolkit layout editor with safe-area,
    /// resize/opacity and a one-handed mode (docs/01 §7.2). Buttons are drawn here via IMGUI for zero setup.
    /// </summary>
    public sealed class TouchInputProvider : MonoBehaviour, IInputProvider
    {
        [SerializeField] private float _stickDeadzonePixels = 18f;
        [SerializeField] private float _stickRangePixels = 90f;

        private int _stickFinger = -1;
        private Vector2 _stickOrigin;
        private float _move;
        private bool _jump, _grab, _interact, _restart;

        public float MoveAxis => _move;
        public bool Jump => _jump;
        public bool Grab => _grab;
        public bool Interact => _interact;
        public bool Crouch => false;
        public bool Lantern => false;
        public bool RestartHeld => _restart;

        private Rect JumpBtn => new Rect(Screen.width - 130, Screen.height - 130, 110, 110);
        private Rect GrabBtn => new Rect(Screen.width - 250, Screen.height - 110, 100, 100);
        private Rect InteractBtn => new Rect(Screen.width - 250, Screen.height - 220, 100, 100);
        private Rect RestartBtn => new Rect(Screen.width - 120, 20, 100, 60);

        private void Update()
        {
            _move = 0f; _jump = _grab = _interact = _restart = false;
            float half = Screen.width * 0.5f;

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                Vector2 p = t.position;

                if (p.x < half) // left half → virtual stick
                {
                    if (_stickFinger == -1 && t.phase == TouchPhase.Began) { _stickFinger = t.fingerId; _stickOrigin = p; }
                    if (t.fingerId == _stickFinger)
                    {
                        if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) _stickFinger = -1;
                        else
                        {
                            float dx = p.x - _stickOrigin.x;
                            if (Mathf.Abs(dx) > _stickDeadzonePixels)
                                _move = Mathf.Clamp(dx / _stickRangePixels, -1f, 1f);
                        }
                    }
                }
                else // right half → action buttons
                {
                    if (JumpBtn.Contains(Flip(p))) _jump = true;
                    if (GrabBtn.Contains(Flip(p))) _grab = true;
                    if (InteractBtn.Contains(Flip(p))) _interact = true;
                    if (RestartBtn.Contains(Flip(p))) _restart = true;
                }
            }
        }

        // GUI rects are top-left origin; touch positions are bottom-left origin.
        private static Vector2 Flip(Vector2 p) => new Vector2(p.x, Screen.height - p.y);

        private void OnGUI()
        {
            GUI.Box(JumpBtn, "JUMP");
            GUI.Box(GrabBtn, "GRAB");
            GUI.Box(InteractBtn, "USE");
            GUI.Box(RestartBtn, "RESTART");
        }
    }
}
