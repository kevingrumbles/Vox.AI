// LightingRegion: tracks the axis-aligned bounding box of voxels affected by a
// lighting update. Used to avoid marking an entire chunk dirty when only a small
// sub-region changed.
//
// Usage:
//   var region = LightingRegion.Empty;
//   region.Expand(wx, wy, wz);     // called for each voxel written
//   if (region.IsValid) { ... }    // check before using

using System;

namespace Vox.AI.World.Lighting;

public struct LightingRegion
{
    public int MinX, MinY, MinZ;
    public int MaxX, MaxY, MaxZ;

    /// <summary>Sentinel value for an uninitialised / empty region.</summary>
    public static LightingRegion Empty => new()
    {
        MinX = int.MaxValue, MinY = int.MaxValue, MinZ = int.MaxValue,
        MaxX = int.MinValue, MaxY = int.MinValue, MaxZ = int.MinValue,
    };

    /// <summary>True when at least one point has been added.</summary>
    public bool IsValid => MinX <= MaxX;

    /// <summary>Expands the region to include world position (wx, wy, wz).</summary>
    public void Expand(int wx, int wy, int wz)
    {
        MinX = Math.Min(MinX, wx); MaxX = Math.Max(MaxX, wx);
        MinY = Math.Min(MinY, wy); MaxY = Math.Max(MaxY, wy);
        MinZ = Math.Min(MinZ, wz); MaxZ = Math.Max(MaxZ, wz);
    }

    /// <summary>
    /// Returns true if this region overlaps the axis-aligned box defined by
    /// world-space chunk origin (ox, oy, oz) and chunk Size.
    /// </summary>
    public bool OverlapsChunk(int ox, int oy, int oz, int size)
    {
        return MaxX >= ox && MinX < ox + size &&
               MaxY >= oy && MinY < oy + size &&
               MaxZ >= oz && MinZ < oz + size;
    }
}
