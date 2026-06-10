// ChunkSaveData: all persisted information for one chunk.
// Contains ONLY player modifications — never generated terrain, mesh data, or render state.
// Serialized to disk by ChunkSerializer using a compact binary format.

using System.Collections.Generic;

namespace Vox.AI.World.Persistence;

/// <summary>
/// Persisted state for a single chunk: its position and the list of
/// player-made block changes that override procedurally generated terrain.
/// </summary>
public sealed class ChunkSaveData
{
    public const int CurrentVersion = 1;

    /// <summary>Chunk-space position that uniquely identifies this chunk on disk.</summary>
    public ChunkPosition Position { get; set; }

    /// <summary>
    /// All player block modifications for this chunk.
    /// Only the latest value per block index is kept — duplicates are merged before saving.
    /// </summary>
    public List<BlockModification> Modifications { get; set; } = new();

    /// <summary>Incremented when the save format for this chunk changes.</summary>
    public int ChunkVersion { get; set; } = 1;
}
