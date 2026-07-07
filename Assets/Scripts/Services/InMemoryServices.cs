using System.Collections.Generic;

namespace Echo.Services
{
    /// <summary>Idempotent in-memory achievements (mirrors Steam/Play/GameCenter behavior for tests + offline).</summary>
    public sealed class InMemoryAchievements : IAchievementProvider
    {
        private readonly HashSet<string> _unlocked = new HashSet<string>();
        private readonly Dictionary<string, int> _stats = new Dictionary<string, int>();
        public int FlushCount { get; private set; }

        public void Unlock(string achievementId) => _unlocked.Add(achievementId); // idempotent
        public void SetStat(string statId, int value) => _stats[statId] = value;
        public bool IsUnlocked(string achievementId) => _unlocked.Contains(achievementId);
        public int StatOf(string statId) => _stats.TryGetValue(statId, out var v) ? v : 0;
        public void Flush() => FlushCount++;
    }

    /// <summary>Opt-in analytics that records events in memory (for tests + a local debug overlay). No PII.</summary>
    public sealed class RecordingAnalytics : IAnalyticsSink
    {
        public readonly List<string> Events = new List<string>();
        public bool Enabled { get; }
        public RecordingAnalytics(bool enabled = true) { Enabled = enabled; }

        public void Track(string eventName) { if (Enabled) Events.Add(eventName); }
        public void Track(string eventName, string key, double value) { if (Enabled) Events.Add($"{eventName}:{key}={value}"); }
        public void TrackFunnel(string levelId, string step) { if (Enabled) Events.Add($"funnel:{levelId}:{step}"); }
    }
}
