using System.IO;
using Echo.Infra;

namespace Echo.Services
{
    /// <summary>
    /// (De)serializes a <see cref="SaveProfile"/> to a compressed, CRC-checked blob via <see cref="SaveCodec"/>.
    /// Manual binary (not Unity JsonUtility) so Dictionaries and byte[] timelines round-trip exactly and
    /// the format is stable across platforms. Banked timelines are stored as opaque blobs (already encoded
    /// by <see cref="Echo.Core.Replay.TimelineCodec"/>).
    /// </summary>
    public static class SaveProfileCodec
    {
        public static byte[] Encode(SaveProfile p)
        {
            byte[] payload;
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                w.Write(p.SchemaVersion);
                w.Write(p.SaveSeed);
                w.Write(p.CurrentLevelId ?? "");
                w.Write(p.PlayTimeMonotonicMs);

                w.Write(p.Levels.Count);
                foreach (var kv in p.Levels)
                {
                    var lr = kv.Value;
                    w.Write(kv.Key);
                    w.Write(lr.LevelId ?? kv.Key);
                    w.Write(lr.Completed);
                    w.Write(lr.BestRunCount);
                    w.Write(lr.HighestSalience);
                    w.Write(lr.BestTimeTicks); // v4 (readers gate on SchemaVersion)
                    w.Write(lr.Revision);      // v6
                    w.Write(lr.BankedTimelines.Count);
                    foreach (var blob in lr.BankedTimelines) { w.Write(blob.Length); w.Write(blob); }
                }

                var s = p.Settings;
                w.Write(s.AssistMode); w.Write(s.ReduceMotion); w.Write(s.PinEchoesObedient);
                w.Write(s.ColorblindMode); w.Write(s.AnalyticsOptIn); w.Write(s.CloudSaveEnabled);

                var st = p.Stats;
                w.Write(st.TotalRestarts); w.Write(st.EchoesSacrificed); w.Write(st.HintsUsed);
                w.Write(st.TraitCounts.Count);
                foreach (var kv in st.TraitCounts) { w.Write(kv.Key); w.Write(kv.Value); }

                // v2: narrative branch drivers. Written last so v1 readers (none shipped) simply stop early.
                var br = p.Branch;
                w.Write(br.Trust); w.Write(br.Mercy);
                w.Write(br.SecretsFound.Count);
                foreach (int id in br.SecretsFound) w.Write(id);

                // v3: settings added after the v2 block shipped.
                w.Write(s.MusicOn);

                // v5: onboarding prompts already shown (v4 added BestTimeTicks inside LevelRecord).
                w.Write(p.SeenPrompts.Count);
                foreach (string id in p.SeenPrompts) w.Write(id);

                payload = ms.ToArray();
            }
            return SaveCodec.Encode(payload);
        }

        public static bool TryDecode(byte[] blob, out SaveProfile profile)
        {
            profile = null;
            if (!SaveCodec.TryDecode(blob, out byte[] payload)) return false;

            using var ms = new MemoryStream(payload);
            using var r = new BinaryReader(ms);
            var p = new SaveProfile
            {
                SchemaVersion = r.ReadInt32(),
                SaveSeed = r.ReadUInt64(),
                CurrentLevelId = r.ReadString(),
                PlayTimeMonotonicMs = r.ReadInt64(),
            };

            int levels = r.ReadInt32();
            for (int i = 0; i < levels; i++)
            {
                string key = r.ReadString();
                var lr = new LevelRecord
                {
                    LevelId = r.ReadString(),
                    Completed = r.ReadBoolean(),
                    BestRunCount = r.ReadInt32(),
                    HighestSalience = r.ReadSingle(),
                    BestTimeTicks = p.SchemaVersion >= 4 ? r.ReadInt32() : 0,
                    Revision = p.SchemaVersion >= 6 ? r.ReadInt32() : 1, // pre-v6 records were earned on rev-1 layouts
                };
                int blobs = r.ReadInt32();
                for (int b = 0; b < blobs; b++) lr.BankedTimelines.Add(r.ReadBytes(r.ReadInt32()));
                p.Levels[key] = lr;
            }

            var s = p.Settings;
            s.AssistMode = r.ReadBoolean(); s.ReduceMotion = r.ReadBoolean(); s.PinEchoesObedient = r.ReadBoolean();
            s.ColorblindMode = r.ReadInt32(); s.AnalyticsOptIn = r.ReadBoolean(); s.CloudSaveEnabled = r.ReadBoolean();

            var st = p.Stats;
            st.TotalRestarts = r.ReadInt32(); st.EchoesSacrificed = r.ReadInt32(); st.HintsUsed = r.ReadInt32();
            int traits = r.ReadInt32();
            for (int i = 0; i < traits; i++) st.TraitCounts[r.ReadString()] = r.ReadInt32();

            // v2 branch block: absent from v1 saves, which decode to the neutral defaults.
            if (p.SchemaVersion >= 2)
            {
                var br = p.Branch;
                br.Trust = r.ReadSingle(); br.Mercy = r.ReadSingle();
                int secrets = r.ReadInt32();
                for (int i = 0; i < secrets; i++) br.SecretsFound.Add(r.ReadInt32());
            }

            // v3 settings tail: older saves keep the class default (music on).
            if (p.SchemaVersion >= 3) s.MusicOn = r.ReadBoolean();

            // v5: onboarding prompts (older saves: empty → prompts show once more, which is harmless).
            if (p.SchemaVersion >= 5)
            {
                int prompts = r.ReadInt32();
                for (int i = 0; i < prompts; i++) p.SeenPrompts.Add(r.ReadString());
            }

            // Upgrade: Encode always writes the newest layout, so a re-saved profile must claim it —
            // otherwise the next load would gate off fields that ARE in the stream and desync.
            p.SchemaVersion = SaveProfile.CurrentSchema;

            profile = p;
            return true;
        }
    }
}
