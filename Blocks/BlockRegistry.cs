// Architecture: Central registry that maps block IDs to BlockDefinition instances.
// All texture decisions flow through GetTextureForFace — mesh generation never
// picks tile indices on its own, and no raw atlas coordinates appear here.
//
// To add a new block type:
//   1. Add a byte constant to BlockId.
//   2. Add an AtlasTile constant if a new texture is needed.
//   3. Register a BlockDefinition in the static constructor below.
//   No changes are required in ChunkMesh or any renderer.

namespace Vox.AI.Blocks;

public static class BlockRegistry
{
    // Array indexed by block ID. Size must be at least (highest BlockId + 1).
    private static readonly BlockDefinition[] Definitions;

    static BlockRegistry()
    {
        Definitions = new BlockDefinition[7];   // IDs 0–6

        Definitions[BlockId.Air] = new BlockDefinition
        {
            Id            = BlockId.Air,
            Name          = "Air",
            IsSolid       = false,
            IsTransparent = true,
            TopTexture    = AtlasTile.Dirt,
            BottomTexture = AtlasTile.Dirt,
            SideTexture   = AtlasTile.Dirt,
        };

        Definitions[BlockId.Grass] = new BlockDefinition
        {
            Id            = BlockId.Grass,
            Name          = "Grass",
            IsSolid       = true,
            IsTransparent = false,
            TopTexture    = AtlasTile.GrassTop,
            BottomTexture = AtlasTile.Dirt,
            SideTexture   = AtlasTile.GrassSide,
        };

        Definitions[BlockId.Dirt] = new BlockDefinition
        {
            Id            = BlockId.Dirt,
            Name          = "Dirt",
            IsSolid       = true,
            IsTransparent = false,
            TopTexture    = AtlasTile.Dirt,
            BottomTexture = AtlasTile.Dirt,
            SideTexture   = AtlasTile.Dirt,
        };

        Definitions[BlockId.Stone] = new BlockDefinition
        {
            Id            = BlockId.Stone,
            Name          = "Stone",
            IsSolid       = true,
            IsTransparent = false,
            TopTexture    = AtlasTile.Stone,
            BottomTexture = AtlasTile.Stone,
            SideTexture   = AtlasTile.Stone,
        };

        Definitions[BlockId.Sand] = new BlockDefinition
        {
            Id            = BlockId.Sand,
            Name          = "Sand",
            IsSolid       = true,
            IsTransparent = false,
            TopTexture    = AtlasTile.Sand,
            BottomTexture = AtlasTile.Sand,
            SideTexture   = AtlasTile.Sand,
        };

        Definitions[BlockId.Wood] = new BlockDefinition
        {
            Id            = BlockId.Wood,
            Name          = "Wood",
            IsSolid       = true,
            IsTransparent = false,
            TopTexture    = AtlasTile.WoodTop,
            BottomTexture = AtlasTile.WoodTop,
            SideTexture   = AtlasTile.WoodSide,
        };

        Definitions[BlockId.Leaves] = new BlockDefinition
        {
            Id            = BlockId.Leaves,
            Name          = "Leaves",
            IsSolid       = true,
            IsTransparent = true,   // future: render leaves with alpha pass
            TopTexture    = AtlasTile.Leaves,
            BottomTexture = AtlasTile.Leaves,
            SideTexture   = AtlasTile.Leaves,
        };
    }

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    /// <summary>Returns the full definition for the given block ID.</summary>
    public static BlockDefinition Get(byte id) => Definitions[id];

    /// <summary>
    /// Returns the atlas tile index for a specific face of a block.
    /// This is the only place where BlockFace is mapped to a texture; mesh code
    /// calls this method and never accesses tile indices directly.
    /// </summary>
    public static int GetTextureForFace(byte id, BlockFace face) =>
        face switch
        {
            BlockFace.Top    => Definitions[id].TopTexture,
            BlockFace.Bottom => Definitions[id].BottomTexture,
            _                => Definitions[id].SideTexture,   // Front, Back, Right, Left
        };

    /// <summary>True if this block occludes neighbouring faces and is physically solid.</summary>
    public static bool IsSolid(byte id) => id < Definitions.Length && Definitions[id].IsSolid;
}

