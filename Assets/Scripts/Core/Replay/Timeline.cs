using System.Collections.Generic;

namespace Echo.Core.Replay
{
    /// <summary>A discrete interaction recorded alongside inputs (grab/throw/press), for fast scrub markers + audit.</summary>
    public readonly struct TimelineEvent
    {
        public readonly int Tick;
        public readonly int EntityId;
        public readonly EventType Type;
        public readonly int Payload;

        public TimelineEvent(int tick, int entityId, EventType type, int payload)
        {
            Tick = tick; EntityId = entityId; Type = type; Payload = payload;
        }
    }

    public enum EventType : byte { Grab, Release, Throw, Press, Land, Hazard, Spawn, Despawn }

    /// <summary>A keyframe: full deterministic state hash (+ optional packed snapshot) for resync/scrub.</summary>
    public readonly struct Keyframe
    {
        public readonly int Tick;
        public readonly ulong StateHash;
        public readonly byte[] PackedState; // null in lightweight keyframes; populated every K ticks

        public Keyframe(int tick, ulong stateHash, byte[] packedState)
        {
            Tick = tick; StateHash = stateHash; PackedState = packedState;
        }
    }

    /// <summary>
    /// One recorded run. Immutable once banked. This is the in-fiction "recorded intent" (Lore §4)
    /// and the on-disk replay format (docs/05 §2). Stores inputs (authoritative), an event log
    /// (markers), keyframes (resync/scrub) and evolution meta (salience/trait persisted per run).
    /// </summary>
    public sealed class Timeline
    {
        public const int SchemaVersion = 1;

        public string LevelId { get; }
        public int RunId { get; }
        public ulong SaveSeed { get; }
        public int TickRate { get; }

        private readonly List<InputCommand> _inputs = new List<InputCommand>(1024);
        private readonly List<TimelineEvent> _events = new List<TimelineEvent>(64);
        private readonly List<Keyframe> _keyframes = new List<Keyframe>(32);

        // Evolution metadata (see Clone AI doc). Persisted so an Echo's "self" carries across sessions.
        public Determinism.Fix64 Salience;
        public string DominantTrait = "Devoted";

        // Relationship metadata: how the player has treated THIS specific run's Echo. Persisted the same
        // way as Salience so it survives being banked/respawned across restarts (docs/04 §3 Trust/Grievance).
        public Determinism.Fix64 Trust;
        public Determinism.Fix64 Grievance;

        public Timeline(string levelId, int runId, ulong saveSeed, int tickRate)
        {
            LevelId = levelId; RunId = runId; SaveSeed = saveSeed; TickRate = tickRate;
        }

        public int TickCount => _inputs.Count;
        public IReadOnlyList<TimelineEvent> Events => _events;
        public IReadOnlyList<Keyframe> Keyframes => _keyframes;

        // --- recording API (used by Recorder) ---
        public void AppendInput(in InputCommand cmd) => _inputs.Add(cmd);
        public void AppendEvent(in TimelineEvent evt) => _events.Add(evt);
        public void AppendKeyframe(in Keyframe kf) => _keyframes.Add(kf);

        /// <summary>
        /// Deep-copy this run under a new runId — the basis for Resonance Plates (#44), where several
        /// *identical* Echoes (cloned from one run) must act together. A distinct runId keeps entity ids
        /// and gate-seed streams unique while the recorded intent is byte-for-byte the same.
        /// </summary>
        public Timeline CloneWithRunId(int newRunId)
        {
            var c = new Timeline(LevelId, newRunId, SaveSeed, TickRate)
            {
                Salience = Salience,
                DominantTrait = DominantTrait,
                Trust = Trust,
                Grievance = Grievance,
            };
            c._inputs.AddRange(_inputs);
            c._events.AddRange(_events);
            c._keyframes.AddRange(_keyframes);
            return c;
        }

        // --- playback API (used by ReplaySource) ---
        /// <summary>Input for a given tick. Past the end, the Echo holds Idle (its run is over).</summary>
        public InputCommand InputAt(int tick)
            => (tick >= 0 && tick < _inputs.Count) ? _inputs[tick] : InputCommand.Idle;

        /// <summary>Nearest keyframe at or before the tick (for scrubbing / partial load).</summary>
        public Keyframe? KeyframeAtOrBefore(int tick)
        {
            Keyframe? best = null;
            for (int i = 0; i < _keyframes.Count; i++)
            {
                if (_keyframes[i].Tick <= tick) best = _keyframes[i];
                else break;
            }
            return best;
        }
    }
}
