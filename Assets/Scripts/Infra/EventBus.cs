using System;
using System.Collections.Generic;

namespace Echo.Infra
{
    /// <summary>
    /// Typed, decoupled publish/subscribe (docs/06 §5). Gameplay raises semantic events
    /// (e.g. EchoSacrificed, PuzzleSolved) that UI, achievements and analytics observe without the
    /// sim ever referencing those subsystems — preserving the downward-only dependency rule.
    ///
    /// Event payloads should be small structs to stay allocation-friendly. Handlers run synchronously
    /// in subscription order; keep them light (no long work on the sim thread).
    /// </summary>
    public sealed class EventBus
    {
        // One delegate list per event type, keyed by Type for O(1) dispatch.
        private readonly Dictionary<Type, Delegate> _handlers = new Dictionary<Type, Delegate>();

        public void Subscribe<T>(Action<T> handler) where T : struct
        {
            Type t = typeof(T);
            _handlers[t] = _handlers.TryGetValue(t, out Delegate existing)
                ? Delegate.Combine(existing, handler)
                : handler;
        }

        public void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            Type t = typeof(T);
            if (_handlers.TryGetValue(t, out Delegate existing))
            {
                Delegate remaining = Delegate.Remove(existing, handler);
                if (remaining == null) _handlers.Remove(t);
                else _handlers[t] = remaining;
            }
        }

        public void Publish<T>(in T evt) where T : struct
        {
            if (_handlers.TryGetValue(typeof(T), out Delegate d) && d is Action<T> typed)
                typed(evt);
        }

        public void Clear() => _handlers.Clear();
    }

    // --- Example semantic events (extended per system; kept here as a shared contract) ---
    public readonly struct PuzzleSolvedEvent { public readonly string LevelId; public readonly int RunsUsed; public PuzzleSolvedEvent(string id, int runs) { LevelId = id; RunsUsed = runs; } }
    public readonly struct EchoSpawnedEvent { public readonly int RunId; public EchoSpawnedEvent(int runId) { RunId = runId; } }
    public readonly struct EchoSacrificedEvent { public readonly int RunId; public EchoSacrificedEvent(int runId) { RunId = runId; } }
    public readonly struct EchoDefiedEvent { public readonly int RunId; public readonly string Trait; public EchoDefiedEvent(int runId, string trait) { RunId = runId; Trait = trait; } }
    public readonly struct EchoPrunedEvent { public readonly int RunId; public EchoPrunedEvent(int runId) { RunId = runId; } }
    public readonly struct SecretFoundEvent { public readonly int SecretId; public SecretFoundEvent(int secretId) { SecretId = secretId; } }
    public readonly struct EchoReliedOnEvent { public readonly int RunId; public EchoReliedOnEvent(int runId) { RunId = runId; } }
}
