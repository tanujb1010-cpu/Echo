using System;
using System.Collections.Generic;

namespace Echo.Services
{
    /// <summary>Top-level persisted profile. Saved as versioned LZ4 binary (docs/05 §6).</summary>
    [Serializable]
    public sealed class SaveProfile
    {
        /// <summary>The layout <see cref="SaveProfileCodec"/> writes. Decode upgrades loaded profiles
        /// to this, because re-encoding always emits the newest layout.</summary>
        public const int CurrentSchema = 6;

        public int SchemaVersion = CurrentSchema;    // v2 BranchBlock; v3 MusicOn; v4 BestTimeTicks; v5 SeenPrompts; v6 LevelRecord.Revision
        public ulong SaveSeed;                       // seeds all deterministic RNG for this profile
        public string CurrentLevelId = "W1_L1";
        public long PlayTimeMonotonicMs;             // for cloud conflict resolution (docs/05 §7)
        public Dictionary<string, LevelRecord> Levels = new Dictionary<string, LevelRecord>();
        public SettingsBlock Settings = new SettingsBlock();
        public StatsBlock Stats = new StatsBlock();
        public BranchBlock Branch = new BranchBlock();
        public List<string> SeenPrompts = new List<string>(); // v5: onboarding shown once, ever
        public uint Crc;                             // integrity check
    }

    /// <summary>
    /// Persisted narrative drivers (docs/02 §8): mirrors <c>BranchState</c> across sessions so the
    /// stat-gated endings are earned over the whole campaign, not one sitting. v1 saves decode with
    /// the defaults below (a neutral run).
    /// </summary>
    [Serializable] public sealed class BranchBlock
    {
        public float Trust = 0.5f;
        public float Mercy = 0.5f;
        public List<int> SecretsFound = new List<int>();
    }

    [Serializable]
    public sealed class LevelRecord
    {
        public string LevelId;
        public bool Completed;
        public int BestRunCount = int.MaxValue;      // fewest Echoes used to solve
        public int BestTimeTicks;                    // v4: fastest full solve in sim ticks (0 = none); 60 = 1 s
        public int Revision = 1;                     // v6: geometry revision the records were earned on
        public List<byte[]> BankedTimelines = new List<byte[]>(); // compressed Timeline blobs (best solve's braid, final run last)
        public float HighestSalience;                // persisted "most evolved" Echo
    }

    [Serializable] public sealed class SettingsBlock
    {
        public bool AssistMode;
        public bool ReduceMotion;
        public bool PinEchoesObedient;
        public int ColorblindMode;
        public bool AnalyticsOptIn;                  // default false; explicit opt-in (docs/05 §12)
        public bool CloudSaveEnabled = true;
        public bool MusicOn = true;                  // v3
    }

    [Serializable] public sealed class StatsBlock
    {
        public int TotalRestarts;
        public int EchoesSacrificed;
        public int HintsUsed;
        public Dictionary<string, int> TraitCounts = new Dictionary<string, int>();
    }

    /// <summary>
    /// Local persistence. Implementations write atomically (temp → fsync → rename) so a crash never
    /// corrupts the live save, and fall back to the last good autosave on integrity failure.
    /// </summary>
    public interface ISaveService
    {
        SaveProfile Current { get; }
        void Load(int slot);
        void Save();                  // synchronous flush (e.g., on quit)
        void RequestAutosave();       // debounced; coalesces frequent calls (docs/05 §6)
        bool HasSave(int slot);
        void DeleteSlot(int slot);
        event Action<SaveProfile> OnLoaded;
    }
}
