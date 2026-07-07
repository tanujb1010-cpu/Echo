using Echo.Core.Determinism;

namespace Echo.Core.Replay
{
    /// <summary>
    /// Records the live player's run, one InputCommand per fixed tick, plus events and periodic
    /// keyframes. On bank, hands back an immutable <see cref="Timeline"/> that will replay as an Echo.
    ///
    /// Allocation note: the backing lists are owned by the Timeline and reused; the per-tick path
    /// performs no allocation (docs/08 §3 zero-GC hot loop).
    /// </summary>
    public sealed class Recorder
    {
        public const int KeyframeInterval = 30; // every 0.5s at 60Hz (docs/05 §1.2)

        private Timeline _current;
        private int _tick;

        public bool IsRecording => _current != null;
        public int CurrentTick => _tick;
        public Timeline Current => _current;

        public void Begin(string levelId, int runId, ulong saveSeed, int tickRate)
        {
            _current = new Timeline(levelId, runId, saveSeed, tickRate);
            _tick = 0;
        }

        /// <summary>Call once per fixed tick with the resolved live input and current world hash.</summary>
        public void RecordTick(in InputCommand cmd, in StateHash worldHash, byte[] packedStateOrNull = null)
        {
            _current.AppendInput(cmd);

            if (_tick % KeyframeInterval == 0)
            {
                // Heavy packed snapshots only occasionally; lightweight hash keyframes are cheap.
                _current.AppendKeyframe(new Keyframe(_tick, worldHash.Value, packedStateOrNull));
            }
            _tick++;
        }

        public void RecordEvent(int entityId, EventType type, int payload = 0)
            => _current?.AppendEvent(new TimelineEvent(_tick, entityId, type, payload));

        /// <summary>Finalize the run into a banked Timeline (immutable from the sim's perspective).</summary>
        public Timeline Bank()
        {
            Timeline banked = _current;
            _current = null;
            _tick = 0;
            return banked;
        }

        /// <summary>Discard the in-progress recording (e.g., Echo-Prime died → restart this run only).</summary>
        public void Discard()
        {
            _current = null;
            _tick = 0;
        }
    }
}
