// SunlightCalculator: local, chunk-scoped sunlight operations.
//
// Responsibilities:
//   SeedChunk      — vertical top-down column sweep for one chunk.
//                    Writes MaxSunlight into sky-exposed air and the top solid block.
//                    Enqueues sky-exposed air into the LightUpdateQueue as BFS seeds.
//                    Never touches neighbouring chunks.
//
//   PropagateSingle — processes ONE LightNode from the queue.
//                    Spreads light to the 6 face-adjacent neighbours in world space.
//                    Skips solid blocks, blocks already at >= new level.
//                    Writes through World.SetSunlight (cross-chunk safe).
//                    Re-enqueues neighbours that need further spreading.
//                    Tracks the dirty region so callers know which chunks to rebuild.
//
// The LightingManager owns the queue and the region, and calls these methods.
// Budget enforcement (max N nodes per frame) is done in LightUpdateQueue, not here.

using Vox.AI.Blocks;

namespace Vox.AI.World.Lighting;

public static class SunlightCalculator
{
    // The 6 face-adjacent neighbour directions (no diagonals).
    private static readonly (int dx, int dy, int dz)[] Neighbours =
    {
        ( 0,  1,  0),  // Up
        ( 0, -1,  0),  // Down
        ( 1,  0,  0),  // East
        (-1,  0,  0),  // West
        ( 0,  0,  1),  // South
        ( 0,  0, -1),  // North
    };

    // Y upper bound: the world is a single chunk layer; propagation must not escape above it.
    private const int MaxWorldY = Chunk.Size;

    /// <summary>
    /// Performs the vertical top-down column sweep for a single chunk.
    /// Clears the chunk's sunlight, assigns MaxSunlight to sky-exposed positions,
    /// and enqueues sky-exposed AIR positions as BFS seeds for horizontal spread.
    /// The top solid surface block receives full light (its top face is sky-facing)
    /// but is NOT enqueued — light does not propagate through solid blocks.
    /// </summary>
    public static void SeedChunk(Chunk chunk, LightUpdateQueue queue)
    {
        chunk.ClearSunlight();

        int ox = (int)chunk.WorldPosition.X;
        int oy = (int)chunk.WorldPosition.Y;
        int oz = (int)chunk.WorldPosition.Z;

        for (int lx = 0; lx < Chunk.Size; lx++)
        for (int lz = 0; lz < Chunk.Size; lz++)
        {
            bool inSky = true;

            for (int ly = Chunk.Size - 1; ly >= 0; ly--)
            {
                byte id = chunk.GetBlock(lx, ly, lz);

                if (inSky)
                {
                    chunk.SetSunlight(lx, ly, lz, LightData.MaxSunlight);

                    if (BlockRegistry.IsSolid(id))
                    {
                        // Surface block: fully lit top face but propagation stops.
                        inSky = false;
                    }
                    else
                    {
                        // Sky-exposed air: seed the BFS so light can spread sideways.
                        queue.Enqueue(new LightNode(ox + lx, oy + ly, oz + lz, LightData.MaxSunlight));
                    }
                }
                else
                {
                    chunk.SetSunlight(lx, ly, lz, 0);
                }
            }
        }
    }

    /// <summary>
    /// Processes a single <see cref="LightNode"/>: spreads its light level to all
    /// 6 neighbours that are air and have a lower current value.
    /// Writes sunlight through <paramref name="world"/> (cross-chunk safe).
    /// Expands <paramref name="dirtyRegion"/> for every voxel written.
    /// Re-enqueues any neighbour that received a higher light level.
    /// </summary>
    public static void PropagateSingle(
        LightNode node,
        LightUpdateQueue queue,
        Vox.AI.World.World world,
        ref LightingRegion dirtyRegion)
    {
        if (node.Level == 0) return;

        byte nextLevel = (byte)(node.Level - 1);

        foreach (var (dx, dy, dz) in Neighbours)
        {
            int nx = node.X + dx;
            int ny = node.Y + dy;
            int nz = node.Z + dz;

            // Never propagate above the sky ceiling or below the world floor.
            if (ny < 0 || ny >= MaxWorldY) continue;

            // Solid blocks absorb light — do not propagate through them.
            if (BlockRegistry.IsSolid(world.GetBlock(nx, ny, nz))) continue;

            // Skip if the neighbour is already at or above the new level.
            if (world.GetSunlight(nx, ny, nz) >= nextLevel) continue;

            // Write the new light value and track the affected region.
            world.SetSunlight(nx, ny, nz, nextLevel);
            dirtyRegion.Expand(nx, ny, nz);

            // Enqueue only if there is remaining light to spread.
            if (nextLevel > 0)
                queue.Enqueue(new LightNode(nx, ny, nz, nextLevel));
        }
    }
}
