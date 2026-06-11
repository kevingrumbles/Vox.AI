// Architecture: Generates the 64×64 procedural texture atlas at runtime.
// Tiles are painted in the same order as AtlasTile constants so the layout
// stays in sync without any extra coupling.
//
// Each 16×16 tile uses a base colour with one of several simple patterns:
//   Checker  — alternating 4×4 dark/light squares (stone, sand, wood)
//   Gradient — slight top-to-bottom darkening (grass side, wood side)
//   Ring     — concentric ring overlay (wood top cross-section)
//   Stipple  — fine noise-like variation (leaves)
//
// Tiles 9–15 are filled with a magenta "missing texture" colour so they
// are immediately visible if a block definition references an undefined tile.

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Vox.AI.Blocks;

namespace Vox.AI.Rendering;

public static class TextureGenerator
{
    private const int TileSize  = 16;
    private const int AtlasSize = TileSize * TextureAtlas.Columns;   // 64×64

    public static Texture2D CreateAtlas(GraphicsDevice device)
    {
        var data    = new Color[AtlasSize * AtlasSize];
        var texture = new Texture2D(device, AtlasSize, AtlasSize);

        // -----------------------------------------------------------------------
        // Row 0
        // -----------------------------------------------------------------------
        PaintChecker(data,   AtlasTile.GrassTop,  new Color( 88, 148,  50), 0.85f);  // bright green
        PaintGradient(data,  AtlasTile.GrassSide, new Color( 72, 110,  45), new Color(130, 90, 50));
        PaintChecker(data,   AtlasTile.Dirt,      new Color(130,  90,  50), 0.88f);  // earthy brown
        PaintChecker(data,   AtlasTile.Stone,     new Color(115, 115, 115), 0.88f);  // grey

        // -----------------------------------------------------------------------
        // Row 1
        // -----------------------------------------------------------------------
        PaintChecker(data,   AtlasTile.Sand,      new Color(240, 215, 130), 0.90f);  // sandy yellow
        PaintChecker(data,   AtlasTile.Water,     new Color( 40,  80, 200), 0.92f);  // blue
        PaintRings(data,     AtlasTile.WoodTop,   new Color(180, 130,  70), new Color(140, 100, 55));
        PaintGradient(data,  AtlasTile.WoodSide,  new Color(160, 115,  60), new Color(120,  85, 45));

        // -----------------------------------------------------------------------
        // Row 2
        // -----------------------------------------------------------------------
        PaintStipple(data,   AtlasTile.Leaves,    new Color( 50, 100,  30), new Color( 35,  75, 20));

        // -----------------------------------------------------------------------
        // Tiles 9–15: magenta "missing texture" fallback
        // -----------------------------------------------------------------------
        for (int tile = 9; tile < TextureAtlas.Columns * TextureAtlas.Rows; tile++)
            PaintSolid(data, tile, new Color(200, 0, 200));

        texture.SetData(data);
        return texture;
    }

    // -----------------------------------------------------------------------
    // Pattern painters
    // -----------------------------------------------------------------------

    // Solid fill — used for missing-texture fallback tiles.
    private static void PaintSolid(Color[] data, int tile, Color c) =>
        FillTile(data, tile, (_, _) => c);

    // Alternating 4×4 checker: dark/light variant of the base colour.
    private static void PaintChecker(Color[] data, int tile, Color base_, float darkFactor) =>
        FillTile(data, tile, (x, y) =>
        {
            bool dark = ((x / 4 + y / 4) & 1) == 0;
            return dark ? Darken(base_, darkFactor) : base_;
        });

    // Linear top-to-bottom gradient between two colours.
    private static void PaintGradient(Color[] data, int tile, Color top, Color bottom) =>
        FillTile(data, tile, (_, y) =>
        {
            float t = y / (float)(TileSize - 1);
            return new Color(
                (int)(top.R + t * (bottom.R - top.R)),
                (int)(top.G + t * (bottom.G - top.G)),
                (int)(top.B + t * (bottom.B - top.B)));
        });

    // Concentric ring pattern to suggest a wood cross-section.
    private static void PaintRings(Color[] data, int tile, Color light, Color dark) =>
        FillTile(data, tile, (x, y) =>
        {
            float cx = x - TileSize / 2.0f + 0.5f;
            float cy = y - TileSize / 2.0f + 0.5f;
            float r  = MathF.Sqrt(cx * cx + cy * cy);
            return ((int)r % 3 == 0) ? dark : light;
        });

    // Fine two-colour stipple to suggest foliage.
    private static void PaintStipple(Color[] data, int tile, Color a, Color b) =>
        FillTile(data, tile, (x, y) =>
        {
            // Simple hash to break up the pattern without external noise
            int h = (x * 7 + y * 13 + x * y) & 0xFF;
            return (h < 140) ? a : b;
        });

    // -----------------------------------------------------------------------
    // Core helper
    // -----------------------------------------------------------------------

    private static void FillTile(Color[] data, int tileIndex, Func<int, int, Color> colorAt)
    {
        int startX = (tileIndex % TextureAtlas.Columns) * TileSize;
        int startY = (tileIndex / TextureAtlas.Columns) * TileSize;

        for (int y = 0; y < TileSize; y++)
        for (int x = 0; x < TileSize; x++)
            data[(startY + y) * AtlasSize + (startX + x)] = colorAt(x, y);
    }

    private static Color Darken(Color c, float factor) =>
        new((int)(c.R * factor), (int)(c.G * factor), (int)(c.B * factor));
}

