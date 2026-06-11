// AmbientOcclusionCalculator: computes per-vertex AO for a block face.
//
// Standard voxel AO algorithm (Mikola Lysenko / Minecraft-style):
//   For each of the 4 corners of a visible face:
//     Sample 3 neighbours in the plane perpendicular to the face normal:
//       side1  — block along one tangent axis
//       side2  — block along the other tangent axis
//       corner — block at the diagonal
//     If both side1 AND side2 are solid:
//       AO = 3   (fully occluded corner)
//     Else:
//       AO = (side1 ? 1 : 0) + (side2 ? 1 : 0) + (corner ? 1 : 0)
//     Range: 0 (open air) – 3 (deeply occluded)
//
// Neighbour coordinates are expressed as world-space integer offsets and resolved
// via World.GetBlock so cross-chunk corners are handled automatically.

using Vox.AI.Blocks;

namespace Vox.AI.World.Lighting;

public static class AmbientOcclusionCalculator
{
    // Each face has 4 vertices.  For each vertex we need 3 neighbour offsets:
    //   (side1, side2, corner) — all expressed relative to the block origin.
    //
    // Layout: [faceIndex][vertexIndex][neighbourIndex (0=side1, 1=side2, 2=corner)]
    //                                                                x   y   z
    private static readonly (int, int, int)[,,] NeighbourOffsets =
    {
        // Face 0 — Top (+Y)  — normal (0,+1,0)
        // vertices: TL=(bx,   by+1, bz),   TR=(bx+1, by+1, bz),
        //           BR=(bx+1, by+1, bz+1), BL=(bx,   by+1, bz+1)
        {
            { (-1, 1, 0),  (0, 1, -1), (-1, 1, -1) },   // TL
            { ( 1, 1, 0),  (0, 1, -1), ( 1, 1, -1) },   // TR
            { ( 1, 1, 0),  (0, 1,  1), ( 1, 1,  1) },   // BR
            { (-1, 1, 0),  (0, 1,  1), (-1, 1,  1) },   // BL
        },
        // Face 1 — Bottom (-Y)  — normal (0,-1,0)
        // vertices: TL=(bx, by, bz+1), TR=(bx+1, by, bz+1), BR=(bx+1, by, bz), BL=(bx, by, bz)
        {
            { (-1, -1,  0), (0, -1,  1), (-1, -1,  1) },  // TL
            { ( 1, -1,  0), (0, -1,  1), ( 1, -1,  1) },  // TR
            { ( 1, -1,  0), (0, -1, -1), ( 1, -1, -1) },  // BR
            { (-1, -1,  0), (0, -1, -1), (-1, -1, -1) },  // BL
        },
        // Face 2 — Front (+Z)  — normal (0,0,+1)
        // vertices: TL=(bx, by+1, bz+1), TR=(bx+1, by+1, bz+1), BR=(bx+1, by, bz+1), BL=(bx, by, bz+1)
        {
            { (-1, 0, 1), (0,  1, 1), (-1,  1, 1) },   // TL
            { ( 1, 0, 1), (0,  1, 1), ( 1,  1, 1) },   // TR
            { ( 1, 0, 1), (0, -1, 1), ( 1, -1, 1) },   // BR
            { (-1, 0, 1), (0, -1, 1), (-1, -1, 1) },   // BL
        },
        // Face 3 — Back (-Z)  — normal (0,0,-1)
        // vertices: TL=(bx+1, by+1, bz), TR=(bx, by+1, bz), BR=(bx, by, bz), BL=(bx+1, by, bz)
        {
            { ( 1, 0, -1), (0,  1, -1), ( 1,  1, -1) },  // TL
            { (-1, 0, -1), (0,  1, -1), (-1,  1, -1) },  // TR
            { (-1, 0, -1), (0, -1, -1), (-1, -1, -1) },  // BR
            { ( 1, 0, -1), (0, -1, -1), ( 1, -1, -1) },  // BL
        },
        // Face 4 — Right (+X)  — normal (+1,0,0)
        // vertices: TL=(bx+1, by+1, bz+1), TR=(bx+1, by+1, bz), BR=(bx+1, by, bz), BL=(bx+1, by, bz+1)
        {
            { (1, 0,  1), (1,  1, 0), (1,  1,  1) },   // TL
            { (1, 0, -1), (1,  1, 0), (1,  1, -1) },   // TR
            { (1, 0, -1), (1, -1, 0), (1, -1, -1) },   // BR
            { (1, 0,  1), (1, -1, 0), (1, -1,  1) },   // BL
        },
        // Face 5 — Left (-X)  — normal (-1,0,0)
        // vertices: TL=(bx, by+1, bz), TR=(bx, by+1, bz+1), BR=(bx, by, bz+1), BL=(bx, by, bz)
        {
            { (-1, 0, -1), (-1,  1, 0), (-1,  1, -1) },  // TL
            { (-1, 0,  1), (-1,  1, 0), (-1,  1,  1) },  // TR
            { (-1, 0,  1), (-1, -1, 0), (-1, -1,  1) },  // BR
            { (-1, 0, -1), (-1, -1, 0), (-1, -1, -1) },  // BL
        },
    };

    /// <summary>
    /// Calculates AO values for the 4 vertices of a visible face.
    /// Returns (TL, TR, BR, BL) each in range 0–3.
    /// </summary>
    /// <param name="world">Used to resolve neighbours that may cross chunk boundaries.</param>
    /// <param name="wx">World-space X of the block whose face we are evaluating.</param>
    /// <param name="wy">World-space Y of the block.</param>
    /// <param name="wz">World-space Z of the block.</param>
    /// <param name="faceIndex">Face index matching the BlockFace enum (0–5).</param>
    public static (byte TL, byte TR, byte BR, byte BL) Calculate(
        Vox.AI.World.World world, int wx, int wy, int wz, int faceIndex)
    {
        var offsets = NeighbourOffsets;

        byte tl = VertexAO(world, wx, wy, wz, offsets[faceIndex, 0, 0], offsets[faceIndex, 0, 1], offsets[faceIndex, 0, 2]);
        byte tr = VertexAO(world, wx, wy, wz, offsets[faceIndex, 1, 0], offsets[faceIndex, 1, 1], offsets[faceIndex, 1, 2]);
        byte br = VertexAO(world, wx, wy, wz, offsets[faceIndex, 2, 0], offsets[faceIndex, 2, 1], offsets[faceIndex, 2, 2]);
        byte bl = VertexAO(world, wx, wy, wz, offsets[faceIndex, 3, 0], offsets[faceIndex, 3, 1], offsets[faceIndex, 3, 2]);

        return (tl, tr, br, bl);
    }

    private static byte VertexAO(
        Vox.AI.World.World world, int wx, int wy, int wz,
        (int dx, int dy, int dz) side1,
        (int dx, int dy, int dz) side2,
        (int dx, int dy, int dz) corner)
    {
        bool s1 = BlockRegistry.IsSolid(world.GetBlock(wx + side1.dx, wy + side1.dy, wz + side1.dz));
        bool s2 = BlockRegistry.IsSolid(world.GetBlock(wx + side2.dx, wy + side2.dy, wz + side2.dz));
        bool c  = BlockRegistry.IsSolid(world.GetBlock(wx + corner.dx, wy + corner.dy, wz + corner.dz));

        if (s1 && s2) return 3;
        return (byte)((s1 ? 1 : 0) + (s2 ? 1 : 0) + (c ? 1 : 0));
    }
}
