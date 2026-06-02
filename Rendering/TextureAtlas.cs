// Architecture: Stateless helper that converts atlas tile coordinates to UV rectangles.
// The atlas is a uniform NxN grid where each tile occupies (1/N) of the texture in each axis.
// All UV values are in [0, 1] normalised texture space.

using Microsoft.Xna.Framework;

namespace Vox.AI.Rendering;

public static class TextureAtlas
{
    public const int Columns = 4;
    public const int Rows    = 4;

    private const float TileW = 1.0f / Columns;   // 0.25
    private const float TileH = 1.0f / Rows;       // 0.25

    /// <summary>
    /// Returns UV corners for the tile at (col, row).
    /// Order: TopLeft, TopRight, BottomRight, BottomLeft — matching the face vertex order used in ChunkMesh.
    /// </summary>
    public static (Vector2 TL, Vector2 TR, Vector2 BR, Vector2 BL) GetUVs(int col, int row)
    {
        float u0 = col * TileW;
        float v0 = row * TileH;
        float u1 = u0 + TileW;
        float v1 = v0 + TileH;

        return (new Vector2(u0, v0),   // TL
                new Vector2(u1, v0),   // TR
                new Vector2(u1, v1),   // BR
                new Vector2(u0, v1));  // BL
    }
}
