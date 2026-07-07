using System;
using System.IO;
using System.IO.Compression;

namespace Echo.Infra
{
    /// <summary>
    /// Wraps a payload in a versioned, compressed, integrity-checked container (docs/05 §6).
    /// Layout: [magic 'E','K'][version u8][crc32 of compressed u32][rawLen u32][Deflate(payload)].
    /// Deflate (System.IO.Compression) is used instead of LZ4 to avoid a third-party dependency; it is
    /// available in both .NET and Unity (IL2CPP). Corruption is detected via CRC before decompression.
    /// </summary>
    public static class SaveCodec
    {
        private const byte Magic0 = (byte)'E';
        private const byte Magic1 = (byte)'K';
        private const byte Version = 1;

        public static byte[] Encode(byte[] payload)
        {
            byte[] compressed;
            using (var ms = new MemoryStream())
            {
                using (var df = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: true))
                    df.Write(payload, 0, payload.Length);
                compressed = ms.ToArray();
            }

            uint crc = Crc32.Compute(compressed);
            using (var outMs = new MemoryStream())
            using (var w = new BinaryWriter(outMs))
            {
                w.Write(Magic0); w.Write(Magic1); w.Write(Version);
                w.Write(crc);
                w.Write((uint)payload.Length);
                w.Write(compressed);
                return outMs.ToArray();
            }
        }

        /// <summary>Returns false (and null) on bad magic / version / CRC mismatch — caller falls back to last good save.</summary>
        public static bool TryDecode(byte[] blob, out byte[] payload)
        {
            payload = null;
            if (blob == null || blob.Length < 11) return false;
            using var ms = new MemoryStream(blob);
            using var r = new BinaryReader(ms);
            if (r.ReadByte() != Magic0 || r.ReadByte() != Magic1) return false;
            if (r.ReadByte() != Version) return false;
            uint crc = r.ReadUInt32();
            uint rawLen = r.ReadUInt32();
            byte[] compressed = r.ReadBytes(blob.Length - 11);
            if (Crc32.Compute(compressed) != crc) return false; // corruption detected

            using var inMs = new MemoryStream(compressed);
            using var df = new DeflateStream(inMs, CompressionMode.Decompress);
            var buf = new byte[rawLen];
            int read = 0;
            while (read < buf.Length)
            {
                int n = df.Read(buf, read, buf.Length - read);
                if (n <= 0) break;
                read += n;
            }
            if (read != rawLen) return false;
            payload = buf;
            return true;
        }
    }
}
