// LightingManager: public API entry point for all lighting operations.
//
// Called by WorldRenderer before rebuilding a dirty chunk mesh.
// Orchestrates sunlight calculation for individual chunks.
//
// AO is not managed here — it is computed on-the-fly in ChunkMesh.Build
// because it requires cross-chunk world queries that are already available
// in the mesh builder and do not need pre-storage.
//
// Future expansion points (do not change public API):
//   - Flood-fill horizontal sunlight propagation
//   - Torch / point light sources
//   - Coloured light channels
//   - Night/day cycle (scale MaxSunlight by time-of-day factor)

using System.Collections.Generic;

namespace Vox.AI.World.Lighting;

public static class LightingManager
{
    /// <summary>
    /// Recalculates sunlight for <paramref name="chunk"/> using a vertical column sweep.
    /// Must be called after terrain generation and after any block modification
    /// that could alter surface height.
    /// </summary>
    public static void RecalculateSunlight(Chunk chunk)
    {
        SunlightCalculator.Calculate(chunk);
    }

    /// <summary>
    /// Recalculates sunlight for every chunk in the collection.
    /// Use during world initialisation before the first mesh build.
    /// </summary>
    public static void RecalculateAll(IEnumerable<Chunk> chunks)
    {
        foreach (var chunk in chunks)
            SunlightCalculator.Calculate(chunk);
    }
}
