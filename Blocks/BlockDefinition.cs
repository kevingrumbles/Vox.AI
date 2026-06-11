// BlockDefinition: describes every property of a single block type.
// Texture fields hold AtlasTile index values — never raw atlas coordinates.
// Adding a new block only requires a new BlockDefinition registered in BlockRegistry;
// no changes are needed in ChunkMesh or any renderer.

namespace Vox.AI.Blocks;

/// <summary>
/// Complete description of one block type: identity, physics behaviour,
/// and per-face-group texture tile indices into the shared atlas.
/// </summary>
public sealed class BlockDefinition
{
    /// <summary>Byte ID stored in chunk block arrays. Matches a BlockId constant.</summary>
    public byte Id { get; init; }

    /// <summary>Human-readable name used for debugging and future UI.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// True if this block occludes neighbouring faces and prevents the player from passing through.
    /// Air and non-collidable blocks should be false.
    /// </summary>
    public bool IsSolid { get; init; }

    /// <summary>
    /// True if this block can be seen through (water, leaves, glass).
    /// Transparent blocks are registered here for future rendering support;
    /// the current renderer does not yet separate opaque and transparent passes.
    /// </summary>
    public bool IsTransparent { get; init; }

    // -----------------------------------------------------------------------
    // Texture tile indices — values from AtlasTile constants
    // -----------------------------------------------------------------------

    /// <summary>Atlas tile index for the top face (BlockFace.Top).</summary>
    public int TopTexture { get; init; }

    /// <summary>Atlas tile index for the bottom face (BlockFace.Bottom).</summary>
    public int BottomTexture { get; init; }

    /// <summary>Atlas tile index for all four side faces (Front, Back, Left, Right).</summary>
    public int SideTexture { get; init; }
}
