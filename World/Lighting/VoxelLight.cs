// VoxelLight: lightweight value struct that carries both sunlight level and
// ambient-occlusion value for a single block face vertex.
//
// Kept as a struct to avoid heap allocations during mesh generation.
// Sunlight is block-level (same for all 4 vertices of a face).
// AO is vertex-level (each corner may differ).

using Vox.AI.Player;

namespace Vox.AI.World.Lighting;

/// <summary>
/// Lighting state for a single face vertex.
/// Combine with <see cref="LightData"/> to produce a final brightness float.
/// </summary>
public readonly struct VoxelLight
{
    /// <summary>Sunlight level, range 0–15.  0 = full dark, 15 = full sun.</summary>
    public readonly byte Sunlight;

    /// <summary>Ambient occlusion value, range 0–3.  0 = open air, 3 = deeply occluded corner.</summary>
    public readonly byte AO;

    public VoxelLight(byte sunlight, byte ao)
    {
        Sunlight = sunlight;
        AO       = ao;
    }

    /// <summary>
    /// Calculates the combined brightness float (0.0–1.0) for this vertex.
    /// Result = sunlightFraction × aoBrightness × faceBrightness.
    /// </summary>
    public float Brightness(int faceIndex)
    {
        if (PlayerSettings.DisableShadows) { return 1.0f; }

        float sun  = LightData.SunlightToFloat(Sunlight);
        float ao   = LightData.AoBrightness[AO];
        float face = LightData.ForFace(faceIndex);
        return sun * ao * face;
    }
}
