using System.IO;
using Echo.Core.Determinism;

namespace Echo.Core.Replay
{
    /// <summary>
    /// Serializes a <see cref="Timeline"/> to/from bytes (docs/05 §2). Inputs are RLE-encoded over idle
    /// and held ticks (a 90-second run compresses to a few KB before the outer Deflate). The crucial
    /// guarantee — verified by tests — is that a decoded timeline replays BIT-IDENTICALLY to the original.
    /// </summary>
    public static class TimelineCodec
    {
        public static byte[] Encode(Timeline t)
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);

            w.Write(Timeline.SchemaVersion);
            w.Write(t.LevelId);
            w.Write(t.RunId);
            w.Write(t.SaveSeed);
            w.Write(t.TickRate);
            w.Write(t.Salience.Raw);
            w.Write(t.DominantTrait ?? "");

            // Inputs as RLE: [runs u32] then per run [packed u16][length u32].
            int n = t.TickCount;
            w.Write(n);
            int i = 0;
            using (var runMs = new MemoryStream())
            using (var runW = new BinaryWriter(runMs))
            {
                uint runCount = 0;
                while (i < n)
                {
                    ushort packed = t.InputAt(i).Pack();
                    uint len = 1;
                    while (i + (int)len < n && t.InputAt(i + (int)len).Pack() == packed) len++;
                    runW.Write(packed); runW.Write(len);
                    i += (int)len; runCount++;
                }
                w.Write(runCount);
                w.Write(runMs.ToArray());
            }

            // Events.
            w.Write(t.Events.Count);
            foreach (var e in t.Events) { w.Write(e.Tick); w.Write(e.EntityId); w.Write((byte)e.Type); w.Write(e.Payload); }

            // Keyframes.
            w.Write(t.Keyframes.Count);
            foreach (var k in t.Keyframes)
            {
                w.Write(k.Tick); w.Write(k.StateHash);
                if (k.PackedState == null) { w.Write(-1); }
                else { w.Write(k.PackedState.Length); w.Write(k.PackedState); }
            }
            return ms.ToArray();
        }

        public static Timeline Decode(byte[] bytes)
        {
            using var ms = new MemoryStream(bytes);
            using var r = new BinaryReader(ms);

            r.ReadInt32(); // schema version (migrate here when it changes)
            string levelId = r.ReadString();
            int runId = r.ReadInt32();
            ulong seed = r.ReadUInt64();
            int tickRate = r.ReadInt32();
            long salienceRaw = r.ReadInt64();
            string trait = r.ReadString();

            var t = new Timeline(levelId, runId, seed, tickRate)
            {
                Salience = Fix64.FromRaw(salienceRaw),
                DominantTrait = trait,
            };

            int n = r.ReadInt32();
            uint runCount = r.ReadUInt32();
            int produced = 0;
            for (uint run = 0; run < runCount; run++)
            {
                ushort packed = r.ReadUInt16();
                uint len = r.ReadUInt32();
                InputCommand cmd = InputCommand.Unpack(packed);
                for (uint c = 0; c < len && produced < n; c++) { t.AppendInput(cmd); produced++; }
            }

            int events = r.ReadInt32();
            for (int e = 0; e < events; e++)
                t.AppendEvent(new TimelineEvent(r.ReadInt32(), r.ReadInt32(), (EventType)r.ReadByte(), r.ReadInt32()));

            int kf = r.ReadInt32();
            for (int k = 0; k < kf; k++)
            {
                int tick = r.ReadInt32();
                ulong hash = r.ReadUInt64();
                int len = r.ReadInt32();
                byte[] packedState = len < 0 ? null : r.ReadBytes(len);
                t.AppendKeyframe(new Keyframe(tick, hash, packedState));
            }
            return t;
        }
    }
}
