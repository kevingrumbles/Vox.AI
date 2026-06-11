// Architecture: Stateless UV calculator for the shared voxel texture atlas.
// Callers identify tiles by flat index (AtlasTile constants), never by raw coordinates.
// A configurable inset is subtracted from each tile edge to prevent texture bleeding
// at tile borders when the GPU samples with bilinear filtering.
//
// UV generation happens only during mesh build — never inside the render loop.
// Index formula: tileIndex = col + row * Columns

using Microsoft.Xna.Framework;

namespace Vox.AI.Rendering;

public static class TextureAtlas
{
    public const int Columns = 4;
    public const int Rows    = 4;

    private const float TileW = 1.0f / Columns;   // 0.25
    private const float TileH = 1.0f / Rows;       // 0.25

    /// <summary>
    /// UV inset applied inward from each tile edge.
    /// Prevents texture bleeding when sampling near atlas tile boundaries.
    /// </summary>
    public const float AtlasInset = 0.001f;

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns the top-left UV of a tile (after inset).
    /// Useful for single-point UV queries.
    /// </summary>
    public static Vector2 GetUV(int tileIndex)
    {
        var (u0, v0, _, _) = GetUVRect(tileIndex);
        return new Vector2(u0, v0);
    }

    /// <summary>
    /// Returns the inset UV rectangle (U0, V0, U1, V1) for a tile.
    /// U0/V0 is the top-left corner; U1/V1 is the bottom-right corner.
    /// </summary>
    public static (float U0, float V0, float U1, float V1) GetUVRect(int tileIndex)
    {
        int col = tileIndex % Columns;
        int row = tileIndex / Columns;

        float u0 =  col      * TileW + AtlasInset;
        float v0 =  row      * TileH + AtlasInset;
        float u1 = (col + 1) * TileW - AtlasInset;
        float v1 = (row + 1) * TileH - AtlasInset;

        return (u0, v0, u1, v1);
    }

    /// <summary>
    /// Returns all four UV corners for a tile in ChunkMesh vertex order:
    /// TopLeft, TopRight, BottomRight, BottomLeft.
    /// </summary>
    public static (Vector2 TL, Vector2 TR, Vector2 BR, Vector2 BL) GetUVs(int tileIndex)
    {
        var (u0, v0, u1, v1) = GetUVRect(tileIndex);

        return (new Vector2(u0, v0),   // TL
                new Vector2(u1, v0),   // TR
                new Vector2(u1, v1),   // BR
                new Vector2(u0, v1));  // BL
    }
}

