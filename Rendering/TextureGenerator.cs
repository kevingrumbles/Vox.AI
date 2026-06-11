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
//
// Atlas loading priority:
//   1. Content/Textures/voxel_atlas.png  (artist-authored file next to the executable)
//   2. Procedurally generated fallback   (always available, no asset pipeline needed)

using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Vox.AI.Blocks;

namespace Vox.AI.Rendering;

public static class TextureGenerator
{
    private const int TileSize  = 16;
    private const int AtlasSize = TileSize * TextureAtlas.Columns;   // 64×64

    /// <summary>
    /// Relative path (from the executable directory) where an artist-authored
    /// atlas PNG is expected.  Drop a file there to override the procedural atlas.
    /// </summary>
    public const string AtlasFilePath = "Art/Vox.AI.Atlas.png";

    /// <summary>
    /// Returns a texture atlas, preferring an on-disk PNG over the procedural fallback.
    /// If the file exists it is loaded directly; otherwise <see cref="CreateAtlas"/> is used.
    /// </summary>
    public static Texture2D LoadAtlas(GraphicsDevice device)
    {
        string fullPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, AtlasFilePath);

        if (File.Exists(fullPath))
        {
            using var stream = File.OpenRead(fullPath);
            return Texture2D.FromStream(device, stream);
        }

        return CreateAtlas(device);
    }

    public static Texture2D CreateAtlas(GraphicsDevice device)
    {
        var data    = new Color[AtlasSize * AtlasSize];
        var texture = new Texture2D(device, AtlasSize, AtlasSize);

        // -----------------------------------------------------------------------
        // Row 0
        // -----------------------------------------------------------------------
        PaintGradient(data,  AtlasTile.GrassTop,  AtlasPalette.GrassTopLight,   AtlasPalette.GrassTopLight);
        PaintGradient(data,  AtlasTile.GrassSide, AtlasPalette.GrassSideTop,    AtlasPalette.GrassSideBottom);
        PaintChecker(data,   AtlasTile.Dirt,      AtlasPalette.DirtBase,        0.88f);
        PaintChecker(data,   AtlasTile.Stone,     AtlasPalette.StoneBase,       0.48f);

        // -----------------------------------------------------------------------
        // Row 1
        // -----------------------------------------------------------------------
        PaintChecker(data,   AtlasTile.Sand,      AtlasPalette.SandBase,        0.90f);
        PaintChecker(data,   AtlasTile.Water,     AtlasPalette.WaterBase,       0.92f);
        PaintRings(data,     AtlasTile.WoodTop,   AtlasPalette.WoodRingLight,   AtlasPalette.WoodRingDark);
        PaintGradient(data,  AtlasTile.WoodSide,  AtlasPalette.WoodSideTop,     AtlasPalette.WoodSideBottom);

        // -----------------------------------------------------------------------
        // Row 2
        // -----------------------------------------------------------------------
        PaintStipple(data,   AtlasTile.Leaves,    AtlasPalette.LeavesPrimary,   AtlasPalette.LeavesSecondary);

        // -----------------------------------------------------------------------
        // Tiles 9–15: magenta "missing texture" fallback
        // -----------------------------------------------------------------------
        for (int tile = 9; tile < TextureAtlas.Columns * TextureAtlas.Rows; tile++)
            PaintSolid(data, tile, AtlasPalette.MissingTexture);

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

