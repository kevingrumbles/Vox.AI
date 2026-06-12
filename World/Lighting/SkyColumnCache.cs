// SkyColumnCache: stores the Y of the highest solid block for every (worldX, worldZ)
// column that has been registered.
//
// Purpose:
//   Avoids repeated top-down scans when deciding whether a position is sky-exposed.
//   A block is sky-exposed if its Y > HighestSolidY(wx, wz).
//
// Lifecycle:
//   RegisterChunk  — called once per chunk after terrain generation.
//   OnBlockChanged — called by World.SetBlock to keep the cache current.
//   GetHighestSolidY — returns the cached value, or -1 if the column is not registered
//                      (treat unregistered columns as all-air / full sky exposure).

using System.Collections.Generic;

namespace Vox.AI.World.Lighting;

public sealed class SkyColumnCache
{
    // Key = (worldX, worldZ), value = highest solid block Y in that column (-1 = no solid).
    private readonly Dictionary<(int, int), int> _cache = new();

    /// <summary>
    /// Scans a chunk column-by-column and populates the cache for every (worldX, worldZ)
    /// pair owned by that chunk.
    /// </summary>
    public void RegisterChunk(Chunk chunk)
    {
        int ox = (int)chunk.WorldPosition.X;
        int oy = (int)chunk.WorldPosition.Y;
        int oz = (int)chunk.WorldPosition.Z;

        for (int lx = 0; lx < Chunk.Size; lx++)
        for (int lz = 0; lz < Chunk.Size; lz++)
        {
            int wx = ox + lx;
            int wz = oz + lz;
            int highest = -1;

            for (int ly = Chunk.Size - 1; ly >= 0; ly--)
            {
                if (Blocks.BlockRegistry.IsSolid(chunk.GetBlock(lx, ly, lz)))
                {
                    highest = oy + ly;
                    break;
                }
            }

            // Take the maximum across chunk layers if the column spans multiple chunks.
            if (_cache.TryGetValue((wx, wz), out int existing))
                highest = System.Math.Max(existing, highest);

            _cache[(wx, wz)] = highest;
        }
    }

    /// <summary>
    /// Updates the cache for a single column after a block change.
    /// </summary>
    /// <param name="wx">World X of the changed block.</param>
    /// <param name="wy">World Y of the changed block.</param>
    /// <param name="wz">World Z of the changed block.</param>
    /// <param name="isSolid">Whether the new block is solid.</param>
    /// <param name="world">World reference for re-scanning the column if needed.</param>
    public void OnBlockChanged(int wx, int wy, int wz, bool isSolid, Vox.AI.World.World world)
    {
        _cache.TryGetValue((wx, wz), out int current);

        if (isSolid)
        {
            // A new solid block: update only if it's higher than the current top.
            if (wy > current)
                _cache[(wx, wz)] = wy;
        }
        else
        {
            // A block was removed: if it was the highest solid, re-scan the column.
            if (wy == current)
                ReScanColumn(wx, wz, world);
        }
    }

    /// <summary>
    /// Returns the Y of the highest solid block in column (wx, wz),
    /// or -1 if the column is entirely air or not yet registered.
    /// </summary>
    public int GetHighestSolidY(int wx, int wz)
    {
        return _cache.TryGetValue((wx, wz), out int y) ? y : -1;
    }

    /// <summary>
    /// Returns true if the given world position is exposed to sky
    /// (i.e. no solid block above it in its column).
    /// </summary>
    public bool IsSkyExposed(int wx, int wy, int wz)
    {
        return wy > GetHighestSolidY(wx, wz);
    }

    // Re-scans the full column by querying the world, used when the top solid block
    // is removed and we need to find the new highest.
    private void ReScanColumn(int wx, int wz, Vox.AI.World.World world)
    {
        // Scan from the top of the world downward; stop at the first solid block.
        // MaxWorldY is the top of the single chunk layer.
        int highest = -1;
        for (int wy = Chunk.Size - 1; wy >= 0; wy--)
        {
            if (Blocks.BlockRegistry.IsSolid(world.GetBlock(wx, wy, wz)))
            {
                highest = wy;
                break;
            }
        }
        _cache[(wx, wz)] = highest;
    }
}
