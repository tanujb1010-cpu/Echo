namespace Echo.Core.Replay
{
    /// <summary>
    /// Feeds a banked <see cref="Timeline"/> back into an Echo, one tick at a time. This is the
    /// authoritative Layer A of an Echo (docs/04 §1). Supports the time mechanics that operate by
    /// offsetting playback (Echo-Delay / Echo-Advance / Reverse-Replay, Content Bible §E).
    ///
    /// IMPORTANT: this is pooled and reset, never re-allocated per restart (docs/05 §5).
    /// </summary>
    public sealed class ReplaySource
    {
        public Timeline Timeline { get; private set; }

        private int _playhead;
        private int _startOffset;   // Echo-Delay: positive delays the start
        private bool _reversed;     // Reverse-Replay mechanic
        private bool _paused;       // Pause-Self mechanic

        public int Playhead => _playhead;
        public bool Finished => !_reversed
            ? _playhead >= Timeline.TickCount
            : _playhead < 0;

        public void Init(Timeline timeline, int startOffsetTicks = 0, bool reversed = false)
        {
            Timeline = timeline;
            _startOffset = startOffsetTicks;
            _reversed = reversed;
            _paused = false;
            _playhead = reversed ? timeline.TickCount - 1 : -startOffsetTicks;
        }

        public void Reset() { Timeline = null; _playhead = 0; _startOffset = 0; _reversed = false; _paused = false; }

        public void SetPaused(bool paused) => _paused = paused; // Pause-Self (mechanic in §E)

        /// <summary>Input for the current playhead. Before the offset start or after the end → Idle.</summary>
        public InputCommand CurrentInput()
        {
            if (_playhead < 0) return InputCommand.Idle;       // still inside Echo-Delay window
            return Timeline.InputAt(_playhead);
        }

        /// <summary>Advance one tick (called by the deterministic sim loop).</summary>
        public void Advance()
        {
            if (_paused) return;
            _playhead += _reversed ? -1 : 1;
        }

        /// <summary>Jump the playhead (timeline scrubbing / Causal Marker mechanic). Snaps to keyframe state externally.</summary>
        public void Seek(int tick) => _playhead = tick;
    }
}
