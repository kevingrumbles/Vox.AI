// Architecture: Fills a Chunk with procedural terrain using a 2-D heightmap.
// Layers from bottom to top: Stone → Dirt (3 blocks deep) → Grass on surface → Air above.
// Height is kept below Chunk.Size (16) so the world fits in one chunk layer.
//
// Uses SetBlockFromGeneration() so terrain population never sets HasUnsavedChanges.
// The seed comes from WorldMetadata, making terrain fully deterministic and reproducible.

using System;
using Vox.AI.Blocks;
using Vox.AI.World;

namespace Vox.AI.Generation;

public sealed class TerrainGenerator
{
    private readonly SimpleNoise _noise;

    private const int BaseHeight  = 6;   // minimum surface Y
    private const int HeightRange = 8;   // extra blocks added by noise (0–8)

    /// <param name="seed">Must match the value stored in WorldMetadata.Seed for the save to be valid.</param>
    public TerrainGenerator(int seed = 12345)
    {
        _noise = new SimpleNoise(seed);
    }

    public void Generate(Chunk chunk)
    {
        for (int x = 0; x < Chunk.Size; x++)
        for (int z = 0; z < Chunk.Size; z++)
        {
            // Sample noise at world-space position for seamless cross-chunk terrain
            float wx = chunk.WorldPosition.X + x;
            float wz = chunk.WorldPosition.Z + z;

            float noise    = _noise.Fbm(wx, wz);             // [-1, 1]
            int   surfaceY = BaseHeight + (int)((noise * 0.5f + 0.5f) * HeightRange);
            surfaceY = Math.Clamp(surfaceY, 0, Chunk.Size - 1);

            for (int y = 0; y < Chunk.Size; y++)
            {
                byte id = y switch
                {
                    _ when y > surfaceY      => BlockId.Air,
                    _ when y == surfaceY     => BlockId.Grass,
                    _ when y >= surfaceY - 3 => BlockId.Dirt,
                    _                        => BlockId.Stone,
                };

                // Use the generation-specific setter so HasUnsavedChanges is never set.
                chunk.SetBlockFromGeneration(x, y, z, id);
            }
        }

        // IsDirty starts true from the Chunk constructor, but make it explicit here
        // so the renderer queues a mesh build after generation completes.
        chunk.IsDirty = true;
    }
}

