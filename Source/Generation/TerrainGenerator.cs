// Architecture: Fills a Chunk with procedural terrain using a 2-D heightmap.
// Layers from bottom to top: Stone → Dirt (3 blocks deep) → Grass on surface → Air above.
// Height is kept below Chunk.Size (16) so the world fits in one chunk layer.

using Vox.AI.Blocks;
using Vox.AI.World;

namespace Vox.AI.Generation;

public sealed class TerrainGenerator
{
    private readonly SimpleNoise _noise = new(seed: 12345);

    private const int BaseHeight  = 6;   // minimum surface Y
    private const int HeightRange = 8;   // extra blocks added by noise (0–8)

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
            surfaceY = System.Math.Clamp(surfaceY, 0, Chunk.Size - 1);

            for (int y = 0; y < Chunk.Size; y++)
            {
                byte id = y switch
                {
                    _ when y > surfaceY          => BlockId.Air,
                    _ when y == surfaceY         => BlockId.Grass,
                    _ when y >= surfaceY - 3     => BlockId.Dirt,
                    _                            => BlockId.Stone,
                };
                chunk.SetBlock(x, y, z, id);
            }
        }

        // Ensure the chunk is flagged for mesh build after generation
        chunk.IsDirty = true;
    }
}
