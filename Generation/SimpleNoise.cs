// Architecture: Self-contained, seedable 2D value noise with fBm (fractal Brownian motion).
// No external dependencies. Used exclusively by TerrainGenerator.
// Algorithm: integer hash → smoothstep interpolation → multiple octaves summed.

using System;

namespace Vox.AI.Generation;

public sealed class SimpleNoise
{
    private readonly int _seed;

    public SimpleNoise(int seed = 42) => _seed = seed;

    // Raw value noise in [-1, 1] at integer grid coordinates
    private float ValueNoise(int x, int z)
    {
        int n = x + z * 57 + _seed * 131;
        n = (n << 13) ^ n;
        return 1.0f - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824.0f;
    }

    // Bilinear interpolation with smoothstep easing over a grid cell
    private float SmoothNoise(float x, float z)
    {
        int   ix = (int)MathF.Floor(x),   iz = (int)MathF.Floor(z);
        float fx = x - ix,                fz = z - iz;

        // Smoothstep: 3t²-2t³
        float ux = fx * fx * (3 - 2 * fx);
        float uz = fz * fz * (3 - 2 * fz);

        float v00 = ValueNoise(ix,     iz);
        float v10 = ValueNoise(ix + 1, iz);
        float v01 = ValueNoise(ix,     iz + 1);
        float v11 = ValueNoise(ix + 1, iz + 1);

        return Lerp(Lerp(v00, v10, ux), Lerp(v01, v11, ux), uz);
    }

    private static float Lerp(float a, float b, float t) => a + t * (b - a);

    /// <summary>
    /// Fractal Brownian Motion — sums multiple noise octaves for natural-looking terrain.
    /// Returns a normalised value in approximately [-1, 1].
    /// </summary>
    public float Fbm(float x, float z, int octaves = 4, float frequency = 0.05f, float amplitude = 1.0f)
    {
        float value    = 0f;
        float maxValue = 0f;

        for (int i = 0; i < octaves; i++)
        {
            value    += SmoothNoise(x * frequency, z * frequency) * amplitude;
            maxValue += amplitude;
            frequency *= 2f;
            amplitude *= 0.5f;
        }

        return value / maxValue;
    }
}
