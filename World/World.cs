// Architecture: Manages the collection of all loaded chunks.
// Translates world-space block coordinates to chunk + local coordinates.
//
// Construction now requires a WorldPersistenceManager so that:
//   1. TerrainGenerator is seeded from WorldMetadata.Seed (deterministic).
//   2. Each chunk is generated then overlaid with any persisted player modifications.
//
// SetBlock marks only the owning chunk dirty; cross-chunk neighbour invalidation
// is a known limitation acceptable for this prototype.

using System;
using System.Collections.Generic;
using Vox.AI.Blocks;
using Vox.AI.Generation;
using Vox.AI.World.Persistence;

namespace Vox.AI.World;

public class World
{
    private readonly Dictionary<(int, int, int), Chunk> _chunks = new();
    private readonly TerrainGenerator                   _generator;
    private readonly WorldPersistenceManager            _persistence;

    /// <summary>Half-width in chunks. 3 → 7×7 grid = 49 chunks.</summary>
    public const int ChunkRadius = 3;

    public IEnumerable<Chunk> Chunks => _chunks.Values;

    public World(WorldPersistenceManager persistence)
    {
        _persistence = persistence;

        // Seed the generator from the stored metadata so terrain is always reproducible.
        _generator = new TerrainGenerator((int)persistence.Metadata.Seed);

        GenerateInitialWorld();
    }

    private void GenerateInitialWorld()
    {
        // One-layer-tall world: a flat grid of chunks at Y = 0.
        // LoadChunk handles: generate → apply saved modifications → mark IsDirty.
        for (int cx = -ChunkRadius; cx <= ChunkRadius; cx++)
        for (int cz = -ChunkRadius; cz <= ChunkRadius; cz++)
        {
            var coord = (cx, 0, cz);
            var chunk = new Chunk(coord);
            _persistence.LoadChunk(chunk, _generator);
            _chunks[coord] = chunk;
        }
    }

    public Chunk? GetChunk(int cx, int cy, int cz)
    {
        _chunks.TryGetValue((cx, cy, cz), out var chunk);
        return chunk;
    }

    /// <summary>Returns the block at world-space integer coordinates.</summary>
    public byte GetBlock(int wx, int wy, int wz)
    {
        var (cx, lx) = ToChunkLocal(wx);
        var (cy, ly) = ToChunkLocal(wy);
        var (cz, lz) = ToChunkLocal(wz);
        return GetChunk(cx, cy, cz)?.GetBlock(lx, ly, lz) ?? BlockId.Air;
    }

    /// <summary>Sets a block at world-space coordinates and marks its chunk dirty.</summary>
    public void SetBlock(int wx, int wy, int wz, byte id)
    {
        var (cx, lx) = ToChunkLocal(wx);
        var (cy, ly) = ToChunkLocal(wy);
        var (cz, lz) = ToChunkLocal(wz);
        GetChunk(cx, cy, cz)?.SetBlock(lx, ly, lz, id);
    }

    /// <summary>Returns the sunlight level (0–15) at world-space coordinates, or 0 if unloaded.</summary>
    public byte GetSunlight(int wx, int wy, int wz)
    {
        var (cx, lx) = ToChunkLocal(wx);
        var (cy, ly) = ToChunkLocal(wy);
        var (cz, lz) = ToChunkLocal(wz);
        return GetChunk(cx, cy, cz)?.GetSunlight(lx, ly, lz) ?? 0;
    }

    /// <summary>Sets the sunlight level at world-space coordinates. No-op if the chunk is not loaded.</summary>
    public void SetSunlight(int wx, int wy, int wz, byte level)
    {
        var (cx, lx) = ToChunkLocal(wx);
        var (cy, ly) = ToChunkLocal(wy);
        var (cz, lz) = ToChunkLocal(wz);
        GetChunk(cx, cy, cz)?.SetSunlight(lx, ly, lz, level);
    }

    /// <summary>
    /// Queues all dirty chunks for background save and writes world.meta.
    /// Non-blocking — returns after enqueuing; use FlushAndSave for a guaranteed write.
    /// </summary>
    public void Save() => _persistence.SaveWorld(_chunks.Values);

    /// <summary>Synchronously saves all dirty chunks and metadata. Use at shutdown.</summary>
    public void FlushAndSave()
    {
        foreach (var chunk in _chunks.Values)
            if (chunk.HasUnsavedChanges)
                _persistence.SaveChunk(chunk);
        _persistence.SaveMetadata();
    }

    // Converts a world-axis value to (chunkIndex, localIndex).
    // Uses floor division so negative coordinates are handled correctly.
    private static (int chunkIdx, int local) ToChunkLocal(int w)
    {
        int chunkIdx = (int)Math.Floor(w / (float)Chunk.Size);
        int local    = w - chunkIdx * Chunk.Size;
        return (chunkIdx, local);
    }
}

