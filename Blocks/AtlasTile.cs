// AtlasTile: named constants for every tile in the 4×4 texture atlas.
// Each constant is a flat tile index: index = col + row * 4.
// BlockRegistry uses these constants exclusively — no magic numbers appear
// in block definitions or mesh generation code.
//
// Atlas layout (4 columns × 4 rows = 16 tiles):
//
//   Col:    0           1           2           3
//   Row 0:  GrassTop    GrassSide   Dirt        Stone
//   Row 1:  Sand        Water       WoodTop     WoodSide
//   Row 2:  Leaves      (unused)    (unused)    (unused)
//   Row 3:  (unused)    (unused)    (unused)    (unused)

namespace Vox.AI.Blocks;

/// <summary>Named atlas tile indices. Add a new constant here when adding a new texture.</summary>
public static class AtlasTile
{
    // -----------------------------------------------------------------------
    // Row 0
    // -----------------------------------------------------------------------
    public const int GrassTop  = 0;
    public const int GrassSide = 1;
    public const int Dirt      = 2;
    public const int Stone     = 3;

    // -----------------------------------------------------------------------
    // Row 1
    // -----------------------------------------------------------------------
    public const int Sand      = 4;
    public const int Water     = 5;
    public const int WoodTop   = 6;
    public const int WoodSide  = 7;

    // -----------------------------------------------------------------------
    // Row 2
    // -----------------------------------------------------------------------
    public const int Leaves    = 8;

    // Tiles 9–15 are reserved for future textures.
}
