// BlockModification: compact record of a single changed block inside a chunk.
// BlockIndex is the flattened 1-D index of the block using the formula:
//   index = x + y * ChunkSize + z * ChunkSize * ChunkSize
// Valid range for ChunkSize=16: 0 – 4095, which fits in a ushort.
// ChunkSize is mirrored as a local constant to avoid a circular dependency on Chunk.

namespace Vox.AI.World.Persistence;

/// <summary>
/// Represents a single player-made block change within a chunk.
/// Only the final block ID at each position is stored — no history.
/// </summary>
public readonly record struct BlockModification(ushort BlockIndex, byte BlockId)
{
    // Mirrors Chunk.Size = 16 without creating a hard reference to the World assembly type.
    private const int ChunkSize = 16;

    /// <summary>Encodes local chunk coordinates (0–15 each) into a flat ushort index.</summary>
    public static ushort Encode(int x, int y, int z) =>
        (ushort)(x + y * ChunkSize + z * ChunkSize * ChunkSize);

    /// <summary>Decodes this modification's BlockIndex back to local (x, y, z) coordinates.</summary>
    public (int X, int Y, int Z) Decode() =>
    (
        X: BlockIndex % ChunkSize,
        Y: (BlockIndex / ChunkSize) % ChunkSize,
        Z: BlockIndex / (ChunkSize * ChunkSize)
    );
}
