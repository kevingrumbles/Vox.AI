// ChunkPosition: immutable value type that identifies a chunk in chunk-space.
// Used as a Dictionary key throughout the persistence layer and as the basis
// for chunk file names (e.g. "0_0_0.chunk", "-1_0_2.chunk").

namespace Vox.AI.World.Persistence;

/// <summary>Chunk-space coordinate triple used as a stable, hashable chunk identifier.</summary>
public readonly record struct ChunkPosition(int X, int Y, int Z)
{
    /// <summary>Returns the file-safe string used as the chunk filename stem (e.g. "-1_0_2").</summary>
    public override string ToString() => $"{X}_{Y}_{Z}";
}
