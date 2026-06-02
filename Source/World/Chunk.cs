// Architecture: Stores voxel block data for a 16×16×16 region of the world.
// Block IDs are raw bytes; no objects are created per block.
// IsDirty signals the renderer to rebuild this chunk's mesh on the next frame.
// ChunkCoord is in chunk-space; multiply by Size to get world-space origin.

using Microsoft.Xna.Framework;
using Vox.AI.Blocks;

namespace Vox.AI.World;

public class Chunk
{
    public const int Size = 16;

    /// <summary>Chunk-space coordinate (not world-space).</summary>
    public readonly (int X, int Y, int Z) ChunkCoord;

    /// <summary>World-space position of this chunk's (0,0,0) corner.</summary>
    public readonly Vector3 WorldPosition;

    private readonly byte[,,] _blocks = new byte[Size, Size, Size];

    /// <summary>True when block data has changed and the mesh needs rebuilding.</summary>
    public bool IsDirty { get; set; } = true;

    public Chunk((int X, int Y, int Z) coord)
    {
        ChunkCoord    = coord;
        WorldPosition = new Vector3(coord.X * Size, coord.Y * Size, coord.Z * Size);
    }

    /// <summary>Returns the block at local coordinates, or Air if out of range.</summary>
    public byte GetBlock(int x, int y, int z)
    {
        if ((uint)x >= Size || (uint)y >= Size || (uint)z >= Size)
            return BlockId.Air;
        return _blocks[x, y, z];
    }

    /// <summary>Sets a block at local coordinates and marks the chunk dirty.</summary>
    public void SetBlock(int x, int y, int z, byte id)
    {
        if ((uint)x >= Size || (uint)y >= Size || (uint)z >= Size)
            return;
        _blocks[x, y, z] = id;
        IsDirty = true;
    }
}
