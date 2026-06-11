// LightData: compile-time constants for directional face brightness.
// These values create visual depth by making each face of a block slightly
// different in brightness, simulating an overhead light source.
//
// Values are normalised floats (0.0–1.0).
// Top faces are brightest; bottom faces are darkest.
// Applied during mesh generation — never recalculated at runtime.

namespace Vox.AI.World.Lighting;

public static class LightData
{
    // -----------------------------------------------------------------------
    // Face brightness multipliers (index matches BlockFace enum order)
    // -----------------------------------------------------------------------

    /// <summary>Top face (+Y) — receives full light from above.</summary>
    public const float FaceTop    = 1.00f;

    /// <summary>Bottom face (-Y) — barely lit; rarely visible.</summary>
    public const float FaceBottom = 0.60f;

    /// <summary>Front face (+Z).</summary>
    public const float FaceFront  = 0.75f;

    /// <summary>Back face (-Z).</summary>
    public const float FaceBack   = 0.75f;

    /// <summary>Right face (+X).</summary>
    public const float FaceRight  = 0.80f;

    /// <summary>Left face (-X).</summary>
    public const float FaceLeft   = 0.80f;

    /// <summary>
    /// Returns the directional brightness for the given face index.
    /// Index must match the BlockFace enum: Top=0, Bottom=1, Front=2, Back=3, Right=4, Left=5.
    /// </summary>
    public static float ForFace(int faceIndex) => faceIndex switch
    {
        0 => FaceTop,
        1 => FaceBottom,
        2 => FaceFront,
        3 => FaceBack,
        4 => FaceRight,
        5 => FaceLeft,
        _ => 1.0f,
    };

    // -----------------------------------------------------------------------
    // Sunlight constants
    // -----------------------------------------------------------------------

    /// <summary>Maximum sunlight level (full sky exposure).</summary>
    public const byte MaxSunlight = 15;

    /// <summary>Converts a sunlight byte (0–15) to a normalised float (0.0–1.0).</summary>
    public static float SunlightToFloat(byte level) => level / (float)MaxSunlight;

    // -----------------------------------------------------------------------
    // Ambient Occlusion constants
    // -----------------------------------------------------------------------

    /// <summary>
    /// Per-AO-level brightness factors.  Index = AO value (0 = open, 3 = deeply occluded).
    /// </summary>
    public static readonly float[] AoBrightness = { 1.00f, 0.80f, 0.60f, 0.40f };
}
