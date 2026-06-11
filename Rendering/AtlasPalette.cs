// AtlasPalette: named colour constants used by TextureGenerator when painting
// the procedural atlas tiles.  Every magic colour value lives here so
// TextureGenerator reads as a description of what each tile looks like rather
// than a list of RGB numbers.
//
// Colours are grouped by the tile they belong to.  Where two tiles share a
// colour (e.g. GrassSide bottom blends into Dirt) the shared name is used in
// both places to make the relationship explicit.

using Microsoft.Xna.Framework;

namespace Vox.AI.Rendering;

internal static class AtlasPalette
{
    // -----------------------------------------------------------------------
    // Grass
    // -----------------------------------------------------------------------
    public static readonly Color GrassTopLight   = new( 72, 110,  45);   // solid green (gradient start = end)

    public static readonly Color GrassSideTop    = GrassTopLight;   // same green as grass top
    public static readonly Color GrassSideBottom = new(100, 60, 20);   // blends into dirt

    // -----------------------------------------------------------------------
    // Dirt
    // -----------------------------------------------------------------------
    public static readonly Color DirtBase        = GrassSideBottom;   // earthy brown

    // -----------------------------------------------------------------------
    // Stone
    // -----------------------------------------------------------------------
    public static readonly Color StoneBase       = new(115, 115, 115);   // mid grey

    // -----------------------------------------------------------------------
    // Sand
    // -----------------------------------------------------------------------
    public static readonly Color SandBase        = new(240, 215, 130);   // sandy yellow

    // -----------------------------------------------------------------------
    // Water
    // -----------------------------------------------------------------------
    public static readonly Color WaterBase       = new( 40,  80, 200);   // deep blue

    // -----------------------------------------------------------------------
    // Wood
    // -----------------------------------------------------------------------
    public static readonly Color WoodRingLight   = new(180, 130,  70);   // pale ring band
    public static readonly Color WoodRingDark    = new(140, 100,  55);   // dark ring band

    public static readonly Color WoodSideTop     = new(160, 115,  60);   // lighter bark
    public static readonly Color WoodSideBottom  = new(120,  85,  45);   // darker bark

    // -----------------------------------------------------------------------
    // Leaves
    // -----------------------------------------------------------------------
    public static readonly Color LeavesPrimary   = new( 50, 100,  30);   // bright foliage
    public static readonly Color LeavesSecondary = new( 35,  75,  20);   // dark foliage

    // -----------------------------------------------------------------------
    // Fallback
    // -----------------------------------------------------------------------
    public static readonly Color MissingTexture  = new(200,   0, 200);   // magenta — immediately obvious
}
