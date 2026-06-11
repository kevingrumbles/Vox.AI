// SunlightCalculator: propagates sunlight downward through a single chunk column by column.
//
// Algorithm (vertical sweep only — no flood fill):
//   For each (X, Z) column in the chunk:
//     Start from the top of the chunk (Y = Size-1) with light = MaxSunlight.
//     Scan downward:
//       Air block   → assign current light level (15)
//       Solid block → assign 0 and stop propagation for this column.
//
// This is the simplest correct sunlight model.  It does not propagate light
// sideways or through transparent blocks.  Flood-fill can be layered on later
// without changing the public API.
//
// Cross-chunk vertical propagation is intentionally omitted in this iteration.
// Chunks at Y=0 receive sky light from the top of their own column.

using Vox.AI.Blocks;

namespace Vox.AI.World.Lighting;

public static class SunlightCalculator
{
    /// <summary>
    /// Fills the sunlight values in <paramref name="chunk"/> using a top-down column sweep.
    /// Must be called after terrain generation and after any block modification
    /// that could change surface height.
    /// </summary>
    public static void Calculate(Chunk chunk)
    {
        for (int x = 0; x < Chunk.Size; x++)
        for (int z = 0; z < Chunk.Size; z++)
        {
            bool inSunlight = true;

            for (int y = Chunk.Size - 1; y >= 0; y--)
            {
                byte id = chunk.GetBlock(x, y, z);

                if (inSunlight)
                {
                    // Both air AND the first solid surface block receive full sunlight.
                    // The top face of the surface block is exposed to sky — it must be lit.
                    chunk.SetSunlight(x, y, z, LightData.MaxSunlight);

                    if (BlockRegistry.IsSolid(id))
                        inSunlight = false;   // blocks below this are underground
                }
                else
                {
                    chunk.SetSunlight(x, y, z, 0);
                }
            }
        }
    }
}
