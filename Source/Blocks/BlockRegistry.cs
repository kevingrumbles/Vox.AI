// Architecture: Central registry mapping block IDs to their properties.
// Each block definition stores per-face texture atlas tile coordinates.
// Face index order: 0=Top, 1=Bottom, 2=Front(+Z), 3=Back(-Z), 4=Right(+X), 5=Left(-X)

namespace Vox.AI.Blocks;

/// <summary>Properties for a single block type.</summary>
public readonly struct BlockDefinition
{
    public readonly string Name;
    public readonly bool IsSolid;

    // Atlas tile (column, row) per face: [Top, Bottom, Front, Back, Right, Left]
    public readonly (int Col, int Row)[] FaceTiles;

    public BlockDefinition(string name, bool isSolid, (int, int)[] faceTiles)
    {
        Name      = name;
        IsSolid   = isSolid;
        FaceTiles = faceTiles;
    }
}

public static class BlockRegistry
{
    private static readonly BlockDefinition[] Definitions = new BlockDefinition[4];

    static BlockRegistry()
    {
        // Air — not solid, never rendered
        Definitions[BlockId.Air] = new BlockDefinition("Air", false,
            new[] { (0, 0), (0, 0), (0, 0), (0, 0), (0, 0), (0, 0) });

        // Grass — green top (0,0), dirt bottom (2,0), brown-green sides (1,0)
        Definitions[BlockId.Grass] = new BlockDefinition("Grass", true,
            new[] { (0, 0), (2, 0), (1, 0), (1, 0), (1, 0), (1, 0) });

        // Dirt — uniform brown tile (2,0)
        Definitions[BlockId.Dirt] = new BlockDefinition("Dirt", true,
            new[] { (2, 0), (2, 0), (2, 0), (2, 0), (2, 0), (2, 0) });

        // Stone — uniform grey tile (3,0)
        Definitions[BlockId.Stone] = new BlockDefinition("Stone", true,
            new[] { (3, 0), (3, 0), (3, 0), (3, 0), (3, 0), (3, 0) });
    }

    public static ref readonly BlockDefinition Get(byte id) => ref Definitions[id];
    public static bool IsSolid(byte id) => id != BlockId.Air && Definitions[id].IsSolid;
}
