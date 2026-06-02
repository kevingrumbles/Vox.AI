// Architecture: Manages the collection of all loaded chunks.
// Translates world-space block coordinates to chunk + local coordinates.
// SetBlock marks only the owning chunk dirty; cross-chunk neighbour invalidation
// is a known limitation acceptable for this prototype.

using System;
using System.Collections.Generic;
using Vox.AI.Blocks;
using Vox.AI.Generation;

namespace Vox.AI.World;

public class World
{
    private readonly Dictionary<(int, int, int), Chunk> _chunks = new();
    private readonly TerrainGenerator _generator = new();

    /// <summary>Half-width in chunks. 3 → 7×7 grid = 49 chunks.</summary>
    public const int ChunkRadius = 3;

    public IEnumerable<Chunk> Chunks => _chunks.Values;

    public World()
    {
        GenerateInitialWorld();
    }

    private void GenerateInitialWorld()
    {
        // One-layer-tall world: a flat grid of chunks at Y = 0
        for (int cx = -ChunkRadius; cx <= ChunkRadius; cx++)
        for (int cz = -ChunkRadius; cz <= ChunkRadius; cz++)
        {
            var coord = (cx, 0, cz);
            var chunk = new Chunk(coord);
            _generator.Generate(chunk);
            _chunks[coord] = chunk;
        }
    }

    public Chunk GetChunk(int cx, int cy, int cz)
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

    // Converts a world-axis value to (chunkIndex, localIndex).
    // Uses floor division so negative coordinates are handled correctly.
    private static (int chunkIdx, int local) ToChunkLocal(int w)
    {
        int chunkIdx = (int)Math.Floor(w / (float)Chunk.Size);
        int local    = w - chunkIdx * Chunk.Size;
        return (chunkIdx, local);
    }
}
