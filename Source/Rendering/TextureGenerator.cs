// Architecture: Generates a 64×64 procedural texture atlas at runtime.
// Eliminates the need for external texture assets in the prototype.
// Each 16×16 tile is a solid colour with a subtle checkerboard pattern to show texture seams.
// Tile layout (column, row): Grass-top(0,0) | Grass-side(1,0) | Dirt(2,0) | Stone(3,0)

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Vox.AI.Rendering;

public static class TextureGenerator
{
    private const int TileSize  = 16;
    private const int AtlasSize = TileSize * TextureAtlas.Columns;   // 64

    public static Texture2D CreateAtlas(GraphicsDevice device)
    {
        var data    = new Color[AtlasSize * AtlasSize];
        var texture = new Texture2D(device, AtlasSize, AtlasSize);

        // Row 0 — block types used by the game
        FillTile(data, 0, 0, new Color( 88, 148,  50));   // Grass top    (bright green)
        FillTile(data, 1, 0, new Color( 90, 108,  55));   // Grass side   (muted green-brown)
        FillTile(data, 2, 0, new Color(130,  90,  50));   // Dirt         (brown)
        FillTile(data, 3, 0, new Color(120, 120, 120));   // Stone        (grey)

        // Remaining rows — fill with a magenta "missing texture" colour for debugging
        for (int col = 0; col < TextureAtlas.Columns; col++)
        for (int row = 1; row < TextureAtlas.Rows;    row++)
            FillTile(data, col, row, new Color(200, 0, 200));

        texture.SetData(data);
        return texture;
    }

    private static void FillTile(Color[] data, int col, int row, Color baseColor)
    {
        int startX = col * TileSize;
        int startY = row * TileSize;

        for (int y = 0; y < TileSize; y++)
        for (int x = 0; x < TileSize; x++)
        {
            // Subtle 4×4 checker to give tiles visual texture
            bool darker = ((x / 4 + y / 4) & 1) == 0;
            var c = darker
                ? new Color((int)(baseColor.R * 0.88f), (int)(baseColor.G * 0.88f), (int)(baseColor.B * 0.88f))
                : baseColor;

            data[(startY + y) * AtlasSize + (startX + x)] = c;
        }
    }
}
