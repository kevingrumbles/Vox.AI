// BlockFace: identifies which face of a block cube is being processed.
// Enum values are intentionally identical to the FaceDir array indices in ChunkMesh
// so that a plain cast — (BlockFace)faceIndex — is always valid.

namespace Vox.AI.Blocks;

/// <summary>
/// The six faces of a unit cube, ordered to match ChunkMesh.FaceDir.
/// </summary>
public enum BlockFace
{
    Top    = 0,   // +Y
    Bottom = 1,   // -Y
    Front  = 2,   // +Z
    Back   = 3,   // -Z
    Right  = 4,   // +X
    Left   = 5,   // -X
}
