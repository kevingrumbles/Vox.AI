// ChunkSerializer: writes and reads the binary .chunk file format.
// No JSON, XML, or reflection-based serialization is used.
//
// Binary layout (all values little-endian):
//   uint   Magic            = 0x564F5843  ("VOXC")
//   ushort FormatVersion    = 1
//   int    ChunkX
//   int    ChunkY
//   int    ChunkZ
//   int    ModificationCount
//   [ModificationCount × (ushort BlockIndex, byte BlockId)]
//   uint   CRC32            (covers all bytes before the checksum)
//
// CRC32 is computed over the entire payload preceding the checksum field.
// A mismatch on load causes Deserialize to return null (data is discarded safely).

using System;
using System.IO;
using Vox.AI.World;

namespace Vox.AI.World.Persistence;

public static class ChunkSerializer
{
    private const uint   Magic         = 0x564F5843;  // "VOXC" as uint
    private const ushort FormatVersion = 1;

    // -----------------------------------------------------------------------
    // Serialize
    // -----------------------------------------------------------------------

    /// <summary>Encodes a <see cref="ChunkSaveData"/> to a byte array ready for disk write.</summary>
    public static byte[] Serialize(ChunkSaveData data)
    {
        using var ms     = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(Magic);
        writer.Write(FormatVersion);
        writer.Write(data.Position.X);
        writer.Write(data.Position.Y);
        writer.Write(data.Position.Z);
        writer.Write(data.Modifications.Count);

        foreach (var mod in data.Modifications)
        {
            writer.Write(mod.BlockIndex);
            writer.Write(mod.BlockId);
        }

        writer.Flush();

        // Compute CRC32 over everything written so far, then append it
        byte[] payload = ms.ToArray();
        uint   crc     = Crc32.Compute(payload);
        writer.Write(crc);
        writer.Flush();

        return ms.ToArray();
    }

    // -----------------------------------------------------------------------
    // Deserialize
    // -----------------------------------------------------------------------

    /// <summary>
    /// Decodes a byte array back into <see cref="ChunkSaveData"/>.
    /// Returns <c>null</c> if the data is corrupt (bad magic, version mismatch, or CRC failure).
    /// </summary>
    public static ChunkSaveData? Deserialize(byte[] bytes)
    {
        if (bytes.Length < 4 + 2 + 4 + 4 + 4 + 4 + 4)  // minimum header + CRC
            return null;

        try
        {
            using var ms     = new MemoryStream(bytes);
            using var reader = new BinaryReader(ms);

            uint   magic   = reader.ReadUInt32();
            ushort version = reader.ReadUInt16();

            if (magic != Magic)
            {
                Console.Error.WriteLine($"[ChunkSerializer] Bad magic 0x{magic:X8}; expected 0x{Magic:X8}");
                return null;
            }

            if (version != FormatVersion)
            {
                Console.Error.WriteLine($"[ChunkSerializer] Unsupported format version {version}");
                return null;
            }

            int cx    = reader.ReadInt32();
            int cy    = reader.ReadInt32();
            int cz    = reader.ReadInt32();
            int count = reader.ReadInt32();

            if (count < 0 || count > Chunk.Size * Chunk.Size * Chunk.Size)
            {
                Console.Error.WriteLine($"[ChunkSerializer] Implausible modification count {count}");
                return null;
            }

            var modifications = new System.Collections.Generic.List<BlockModification>(count);
            for (int i = 0; i < count; i++)
            {
                ushort index = reader.ReadUInt16();
                byte   id    = reader.ReadByte();
                modifications.Add(new BlockModification(index, id));
            }

            // CRC covers all bytes before the checksum itself
            int    payloadLen  = (int)ms.Position;
            uint   storedCrc   = reader.ReadUInt32();
            uint   actualCrc   = Crc32.Compute(bytes, 0, payloadLen);

            if (storedCrc != actualCrc)
            {
                Console.Error.WriteLine($"[ChunkSerializer] CRC mismatch for chunk ({cx},{cy},{cz}): " +
                                        $"stored 0x{storedCrc:X8}, actual 0x{actualCrc:X8}");
                return null;
            }

            return new ChunkSaveData
            {
                Position      = new ChunkPosition(cx, cy, cz),
                Modifications = modifications,
                ChunkVersion  = version,
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ChunkSerializer] Deserialize failed: {ex.Message}");
            return null;
        }
    }

    // -----------------------------------------------------------------------
    // CRC32 — standard polynomial 0xEDB88320 (reversed IEEE 802.3)
    // -----------------------------------------------------------------------

    private static class Crc32
    {
        private static readonly uint[] Table = BuildTable();

        private static uint[] BuildTable()
        {
            var table = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint c = i;
                for (int j = 0; j < 8; j++)
                    c = (c & 1) != 0 ? (c >> 1) ^ 0xEDB88320u : c >> 1;
                table[i] = c;
            }
            return table;
        }

        public static uint Compute(byte[] data) => Compute(data, 0, data.Length);

        public static uint Compute(byte[] data, int offset, int length)
        {
            uint crc = 0xFFFFFFFF;
            int  end = offset + length;
            for (int i = offset; i < end; i++)
                crc = (crc >> 8) ^ Table[(crc ^ data[i]) & 0xFF];
            return ~crc;
        }
    }
}
