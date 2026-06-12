// LightNode: a single element in the BFS sunlight propagation queue.
// Stores the world-space position and the light level that should be spread from there.

namespace Vox.AI.World.Lighting;

/// <summary>
/// Represents one voxel position in the sunlight BFS queue.
/// </summary>
/// <param name="X">World X coordinate.</param>
/// <param name="Y">World Y coordinate.</param>
/// <param name="Z">World Z coordinate.</param>
/// <param name="Level">Sunlight level (0–15) at this position.</param>
public readonly record struct LightNode(int X, int Y, int Z, byte Level);
