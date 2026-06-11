// Architecture: Stores voxel block data for a 16×16×16 region of the world.
// Block IDs are raw bytes; no objects are created per block.
//
// Dirty-state split:
//   IsDirty          — the renderer uses this to know when to rebuild the mesh.
//   HasUnsavedChanges — the persistence layer uses this to know when to save the chunk.
//
// SetBlock()              — player action; marks BOTH flags.
// SetBlockFromGeneration()— internal; marks IsDirty only (generation never creates save data).
// ApplyModification()     — internal; marks IsDirty only (loading restores, not creates changes).
//
// ChunkCoord is in chunk-space; multiply by Size to get world-space origin.

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Vox.AI.Blocks;
using Vox.AI.World.Lighting;
using Vox.AI.World.Persistence;

namespace Vox.AI.World;

public class Chunk
{
    public const int Size = 16;

    /// <summary>Chunk-space coordinate (not world-space).</summary>
    public readonly (int X, int Y, int Z) ChunkCoord;

    /// <summary>World-space position of this chunk's (0,0,0) corner.</summary>
    public readonly Vector3 WorldPosition;

    private readonly byte[,,] _blocks   = new byte[Size, Size, Size];
    private readonly byte[,,] _sunlight = new byte[Size, Size, Size];

    // Per-block modification tracking: key = flat BlockIndex, value = current BlockId.
    // Only populated by player SetBlock calls and by ApplyModification during load.
    private readonly Dictionary<ushort, byte> _modifications = new();

    // -----------------------------------------------------------------------
    // Dirty flags
    // -----------------------------------------------------------------------

    /// <summary>True when block data has changed and the mesh needs rebuilding.</summary>
    public bool IsDirty { get; set; } = true;

    /// <summary>True when the chunk contains player changes that have not yet been saved.</summary>
    public bool HasUnsavedChanges { get; private set; } = false;

    // -----------------------------------------------------------------------
    // Construction
    // -----------------------------------------------------------------------

    public Chunk((int X, int Y, int Z) coord)
    {
        ChunkCoord    = coord;
        WorldPosition = new Vector3(coord.X * Size, coord.Y * Size, coord.Z * Size);
    }

    // -----------------------------------------------------------------------
    // Block access
    // -----------------------------------------------------------------------

    /// <summary>Returns the block at local coordinates, or Air if out of range.</summary>
    public byte GetBlock(int x, int y, int z)
    {
        if ((uint)x >= Size || (uint)y >= Size || (uint)z >= Size)
            return BlockId.Air;
        return _blocks[x, y, z];
    }

    // -----------------------------------------------------------------------
    // Sunlight access
    // -----------------------------------------------------------------------

    /// <summary>Returns the sunlight level (0–15) at local coordinates, or 0 if out of range.</summary>
    public byte GetSunlight(int x, int y, int z)
    {
        if ((uint)x >= Size || (uint)y >= Size || (uint)z >= Size)
            return 0;
        return _sunlight[x, y, z];
    }

    /// <summary>Sets the sunlight level at local coordinates. Called only by SunlightCalculator.</summary>
    internal void SetSunlight(int x, int y, int z, byte level)
    {
        if ((uint)x >= Size || (uint)y >= Size || (uint)z >= Size)
            return;
        _sunlight[x, y, z] = level;
    }

    /// <summary>Resets all sunlight values to zero. Call before recalculating lighting.</summary>
    internal void ClearSunlight()
    {
        Array.Clear(_sunlight, 0, _sunlight.Length);
    }

    /// <summary>
    /// Sets a block at local coordinates as a player action.
    /// Marks IsDirty (mesh rebuild) and HasUnsavedChanges (persistence).
    /// </summary>
    public void SetBlock(int x, int y, int z, byte id)
    {
        if ((uint)x >= Size || (uint)y >= Size || (uint)z >= Size)
            return;

        _blocks[x, y, z]  = id;
        IsDirty            = true;
        HasUnsavedChanges  = true;
        _modifications[BlockModification.Encode(x, y, z)] = id;
    }

    // -----------------------------------------------------------------------
    // Internal helpers — not part of the player-facing API
    // -----------------------------------------------------------------------

    /// <summary>
    /// Sets a block during terrain generation.
    /// Marks IsDirty for initial mesh build but does NOT set HasUnsavedChanges.
    /// </summary>
    internal void SetBlockFromGeneration(int x, int y, int z, byte id)
    {
        if ((uint)x >= Size || (uint)y >= Size || (uint)z >= Size)
            return;
        _blocks[x, y, z] = id;
        // IsDirty starts true in the constructor; no need to set it again here.
        // HasUnsavedChanges must NOT be set — generation is not a player modification.
    }

    /// <summary>
    /// Applies a persisted modification when loading a chunk from disk.
    /// Marks IsDirty for mesh rebuild but does NOT set HasUnsavedChanges.
    /// </summary>
    internal void ApplyModification(ushort blockIndex, byte id)
    {
        var (x, y, z) = new BlockModification(blockIndex, id).Decode();
        if ((uint)x >= Size || (uint)y >= Size || (uint)z >= Size)
            return;

        _blocks[x, y, z] = id;
        _modifications[blockIndex] = id;   // keep cache consistent for future saves
        IsDirty = true;
        // HasUnsavedChanges stays false — this data came from disk, not from the player.
    }

    // -----------------------------------------------------------------------
    // Persistence helpers
    // -----------------------------------------------------------------------

    /// <summary>Returns a snapshot of all player modifications for serialization.</summary>
    public BlockModification[] GetModifications()
    {
        var result = new BlockModification[_modifications.Count];
        int i      = 0;
        foreach (var (index, id) in _modifications)
            result[i++] = new BlockModification(index, id);
        return result;
    }

    /// <summary>Clears HasUnsavedChanges after the persistence layer has saved this chunk.</summary>
    public void ClearUnsavedChanges() => HasUnsavedChanges = false;

    // -----------------------------------------------------------------------
    // Explicit dirty-flag helpers (convenience, used by persistence + renderer)
    // -----------------------------------------------------------------------

    public void MarkDirty()  => IsDirty = true;
    public void ClearDirty() => IsDirty = false;
}

