// Architecture: Defines block type identifiers as byte constants.
// Stored directly in chunk byte arrays to keep memory overhead minimal.
// Add new block types here and register them in BlockRegistry.
// Register a corresponding AtlasTile entry if the block needs a new texture.

namespace Vox.AI.Blocks;

public static class BlockId
{
    public const byte Air    = 0;
    public const byte Grass  = 1;
    public const byte Dirt   = 2;
    public const byte Stone  = 3;
    public const byte Sand   = 4;
    public const byte Wood   = 5;
    public const byte Leaves = 6;
}
