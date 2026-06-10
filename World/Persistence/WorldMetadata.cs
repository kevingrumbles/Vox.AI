// WorldMetadata: world-level configuration and bookkeeping stored in world.meta.
// Serialized as indented JSON so it is human-readable and easy to inspect.
// Loaded first during world initialization; if absent a new file is created.
// File location: Saves/{WorldName}/world.meta

using System;

namespace Vox.AI.World.Persistence;

/// <summary>
/// Persistent world header. Describes the seed, generator version, and timing info.
/// Must be loaded before any chunk can be generated so the seed is available.
/// </summary>
public sealed class WorldMetadata
{
    /// <summary>Increment when the metadata schema changes to enable migration.</summary>
    public const int CurrentSaveVersion      = 1;

    /// <summary>Increment when TerrainGenerator logic changes, invalidating old saves.</summary>
    public const int CurrentGeneratorVersion = 1;

    // -----------------------------------------------------------------------
    // Persisted fields
    // -----------------------------------------------------------------------

    public int    SaveVersion      { get; set; } = CurrentSaveVersion;
    public long   Seed             { get; set; }
    public int    GeneratorVersion { get; set; } = CurrentGeneratorVersion;
    public string WorldName        { get; set; } = "World001";

    public DateTime CreatedUtc    { get; set; } = DateTime.UtcNow;
    public DateTime LastSavedUtc  { get; set; } = DateTime.UtcNow;
}
