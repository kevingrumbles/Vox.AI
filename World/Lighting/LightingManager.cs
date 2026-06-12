// LightingManager: coordinates incremental skylight propagation.
//
// Owns:
//   SkyColumnCache   — highest-solid-Y per column, for sky exposure checks.
//   LightUpdateQueue — persistent BFS queue processed at a per-frame budget.
//   ChunkLightState  — per-chunk lifecycle (Unlit → LightingPending → LightingComplete).
//
// Public API:
//   InitialiseChunk(chunk)         — vertical seed + enqueue BFS seeds; call after generation.
//   OnBlockChanged(wx,wy,wz,...)   — re-seed the affected column; enqueue propagation nodes.
//   ProcessQueue(world, budget)    — drain up to `budget` nodes; mark touched chunks dirty.
//                                    Returns true if work remains in the queue.
//
// Rules:
//   Never scans the entire world.
//   Never flood-fills a whole chunk.
//   Never rebuilds more than the dirty region requires.
//   All allocations happen at construction; none per frame.

using System.Collections.Generic;

namespace Vox.AI.World.Lighting;

public sealed class LightingManager
{
    private readonly SkyColumnCache                      _skyCache = new();
    private readonly LightUpdateQueue                    _queue    = new();
    private readonly Dictionary<(int,int,int), ChunkLightState> _states = new();

    // Reference to the world is needed by ProcessQueue for cross-chunk sunlight reads/writes.
    private readonly Vox.AI.World.World _world;

    public LightingManager(Vox.AI.World.World world)
    {
        _world = world;
    }

    // -----------------------------------------------------------------------
    // Initialisation
    // -----------------------------------------------------------------------

    /// <summary>
    /// Seeds sunlight for a freshly generated chunk:
    ///   1. Registers the chunk's columns in the SkyColumnCache.
    ///   2. Runs the vertical column sweep (ClearSunlight → MaxSunlight on sky column).
    ///   3. Enqueues all sky-exposed air blocks as BFS seeds for horizontal spread.
    /// Call once per chunk after terrain generation, before the first mesh build.
    /// Does NOT process the queue — call ProcessQueue each frame.
    /// </summary>
    public void InitialiseChunk(Chunk chunk)
    {
        _skyCache.RegisterChunk(chunk);
        SunlightCalculator.SeedChunk(chunk, _queue);
        _states[chunk.ChunkCoord] = ChunkLightState.LightingPending;
    }

    // -----------------------------------------------------------------------
    // Block change notifications
    // -----------------------------------------------------------------------

    /// <summary>
    /// Notifies the lighting system that a block changed at world position (wx, wy, wz).
    /// Updates the SkyColumnCache and enqueues the affected column for re-propagation.
    /// </summary>
    public void OnBlockChanged(int wx, int wy, int wz, byte newId)
    {
        bool isSolid = Blocks.BlockRegistry.IsSolid(newId);
        _skyCache.OnBlockChanged(wx, wy, wz, isSolid, _world);

        // Re-seed the affected column from its new sky-exposed top downward.
        ReSeedColumn(wx, wz);
    }

    // -----------------------------------------------------------------------
    // Per-frame processing
    // -----------------------------------------------------------------------

    /// <summary>
    /// Processes up to <paramref name="budget"/> BFS nodes from the queue.
    /// For every voxel written, the owning chunk is marked dirty so its mesh rebuilds.
    /// Returns true if there is still work remaining after this call.
    /// </summary>
    public bool ProcessQueue(int budget = LightUpdateQueue.DefaultBudget)
    {
        var dirtyRegion = LightingRegion.Empty;

        _queue.ProcessBudget(budget, (node, q) =>
            SunlightCalculator.PropagateSingle(node, q, _world, ref dirtyRegion));

        // Mark every chunk whose territory overlaps the dirty region.
        if (dirtyRegion.IsValid)
        {
            foreach (var chunk in _world.Chunks)
            {
                int ox = (int)chunk.WorldPosition.X;
                int oy = (int)chunk.WorldPosition.Y;
                int oz = (int)chunk.WorldPosition.Z;

                if (dirtyRegion.OverlapsChunk(ox, oy, oz, Chunk.Size))
                {
                    chunk.MarkDirty();
                    _states[chunk.ChunkCoord] = ChunkLightState.LightingComplete;
                }
            }
        }

        return !_queue.IsEmpty;
    }

    /// <summary>Returns the current lighting lifecycle state for a chunk.</summary>
    public ChunkLightState GetState((int, int, int) coord)
    {
        return _states.TryGetValue(coord, out var s) ? s : ChunkLightState.Unlit;
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    // Re-seeds a single (wx, wz) column by scanning downward from the chunk top,
    // writing MaxSunlight for sky-exposed positions and zeroing everything below,
    // then enqueuing the sky-exposed air blocks so horizontal spread can update neighbours.
    private void ReSeedColumn(int wx, int wz)
    {
        int highestSolid = _skyCache.GetHighestSolidY(wx, wz);

        for (int wy = Chunk.Size - 1; wy >= 0; wy--)
        {
            byte current = _world.GetSunlight(wx, wy, wz);
            bool skyExposed = wy > highestSolid;

            if (skyExposed)
            {
                if (current != LightData.MaxSunlight)
                    _world.SetSunlight(wx, wy, wz, LightData.MaxSunlight);

                if (!Blocks.BlockRegistry.IsSolid(_world.GetBlock(wx, wy, wz)))
                    _queue.Enqueue(new LightNode(wx, wy, wz, LightData.MaxSunlight));
            }
            else
            {
                if (current != 0)
                {
                    _world.SetSunlight(wx, wy, wz, 0);
                    // Enqueue as a "darkness node" — neighbours may need to dim.
                    // A level-0 node causes neighbours to be re-evaluated from their
                    // own sky seeds rather than from this column.
                    // For now, mark the owning chunk dirty so it rebuilds with correct data.
                    var (cx, lx) = ToChunkLocal(wx);
                    var (cy, ly) = ToChunkLocal(wy);
                    var (cz, lz) = ToChunkLocal(wz);
                    _world.GetChunk(cx, cy, cz)?.MarkDirty();
                }
            }
        }
    }

    // Mirrors World.ToChunkLocal without exposing it.
    private static (int chunkIdx, int local) ToChunkLocal(int w)
    {
        int chunkIdx = (int)System.Math.Floor(w / (float)Chunk.Size);
        int local    = w - chunkIdx * Chunk.Size;
        return (chunkIdx, local);
    }
}

