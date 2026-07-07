using System;
using System.Threading.Tasks;

namespace Echo.Services
{
    /// <summary>
    /// Cloud save behind one interface; concrete impls wrap Steam Cloud / Google Play Saved Games /
    /// iCloud-GameKit (docs/05 §7). Gameplay never references a vendor SDK directly.
    /// </summary>
    public interface ICloudSaveProvider
    {
        bool IsAvailable { get; }
        Task<byte[]> DownloadAsync();
        Task UploadAsync(byte[] blob);
        /// <summary>Resolve a divergence by comparing progress; returns the blob to keep.</summary>
        Task<byte[]> ResolveConflictAsync(byte[] local, byte[] remote, Func<byte[], byte[], byte[]> picker);
    }

    /// <summary>Achievements behind one interface (Steam / Play Games / Game Center). Idempotent unlocks.</summary>
    public interface IAchievementProvider
    {
        void Unlock(string achievementId);
        void SetStat(string statId, int value);
        bool IsUnlocked(string achievementId);
        void Flush();
    }

    /// <summary>
    /// Privacy-first analytics sink. The DEFAULT binding is a no-op; real sinks are only attached when
    /// the player opts in (docs/05 §12). No PII, ever. Events are small + aggregated.
    /// </summary>
    public interface IAnalyticsSink
    {
        bool Enabled { get; }
        void Track(string eventName);
        void Track(string eventName, string key, double value);
        void TrackFunnel(string levelId, string step);
    }

    /// <summary>Null-object analytics: the safe default when the player has not opted in.</summary>
    public sealed class NoOpAnalytics : IAnalyticsSink
    {
        public bool Enabled => false;
        public void Track(string eventName) { }
        public void Track(string eventName, string key, double value) { }
        public void TrackFunnel(string levelId, string step) { }
    }

    /// <summary>Audio + haptics behind an interface so the sim/gameplay never touch FMOD/Unity audio directly.</summary>
    public interface IAudioService
    {
        void PlayOneShot(string cueId);
        void SetChoirLayer(int activeEchoes); // music stems grow with the braid (docs/05 §9)
        void Haptic(string pattern);
    }
}
