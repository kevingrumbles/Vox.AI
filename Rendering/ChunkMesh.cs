// Architecture: Builds and stores the renderable GPU mesh for one chunk.
// Only visible faces are emitted — faces shared with a solid neighbour are skipped.
// Neighbour lookups cross chunk boundaries via the World reference.
// The mesh is rebuilt lazily: call Build() when Chunk.IsDirty is true.
//
// Texture pipeline per face (all UV work happens here, never in the render loop):
//   block ID + BlockFace  →  BlockRegistry.GetTextureForFace  →  tileIndex
//   tileIndex             →  TextureAtlas.GetUVs              →  4 UV corners
//
// CullMode.None is used in the prototype so winding order is not critical,
// but faces are defined consistently (top-left → top-right → bottom-right → bottom-left)
// to make future backface-culling easy to enable.

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Vox.AI.Blocks;
using Vox.AI.World;

namespace Vox.AI.Rendering;

public sealed class ChunkMesh : IDisposable
{
    private VertexBuffer? _vertexBuffer;
    private IndexBuffer?  _indexBuffer;
    private int           _indexCount;

    public bool IsEmpty => _indexCount == 0;

    // Face index → (dx, dy, dz) neighbour offsets.
    // Values must stay in sync with the BlockFace enum.
    private static readonly (int dx, int dy, int dz)[] FaceDir =
    {
        ( 0,  1,  0),  // 0 Top    (+Y)
        ( 0, -1,  0),  // 1 Bottom (-Y)
        ( 0,  0,  1),  // 2 Front  (+Z)
        ( 0,  0, -1),  // 3 Back   (-Z)
        ( 1,  0,  0),  // 4 Right  (+X)
        (-1,  0,  0),  // 5 Left   (-X)
    };

    /// <summary>
    /// Rebuilds the mesh from current block data.
    /// Uploads new buffers to the GPU and discards old ones.
    /// </summary>
    public void Build(Chunk chunk, Vox.AI.World.World world, GraphicsDevice device)
    {
        var vertices = new List<VertexPositionTexture>(2048);
        var indices  = new List<int>(4096);

        for (int lx = 0; lx < Chunk.Size; lx++)
        for (int ly = 0; ly < Chunk.Size; ly++)
        for (int lz = 0; lz < Chunk.Size; lz++)
        {
            byte id = chunk.GetBlock(lx, ly, lz);
            if (!BlockRegistry.IsSolid(id)) continue;

            // World-space block origin
            float bx = chunk.WorldPosition.X + lx;
            float by = chunk.WorldPosition.Y + ly;
            float bz = chunk.WorldPosition.Z + lz;

            for (int faceIdx = 0; faceIdx < 6; faceIdx++)
            {
                var (dx, dy, dz) = FaceDir[faceIdx];

                // Resolve neighbour — prefer local chunk, fall back to world query
                int nx = lx + dx, ny = ly + dy, nz = lz + dz;
                byte neighbour = (nx >= 0 && nx < Chunk.Size &&
                                  ny >= 0 && ny < Chunk.Size &&
                                  nz >= 0 && nz < Chunk.Size)
                    ? chunk.GetBlock(nx, ny, nz)
                    : world.GetBlock((int)(bx + dx), (int)(by + dy), (int)(bz + dz));

                if (BlockRegistry.IsSolid(neighbour)) continue;   // face is hidden

                // Resolve tile index via registry — no raw atlas coordinates here
                int tileIndex = BlockRegistry.GetTextureForFace(id, (BlockFace)faceIdx);
                var (tl, tr, br, bl) = TextureAtlas.GetUVs(tileIndex);

                int baseIdx = vertices.Count;
                AddFaceVertices(vertices, bx, by, bz, faceIdx, tl, tr, br, bl);

                // Two triangles per face (counter-clockwise — faces viewed from outside)
                indices.Add(baseIdx + 0); indices.Add(baseIdx + 1); indices.Add(baseIdx + 2);
                indices.Add(baseIdx + 0); indices.Add(baseIdx + 2); indices.Add(baseIdx + 3);
            }
        }

        // Dispose old GPU buffers before uploading new ones
        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();
        _vertexBuffer = null;
        _indexBuffer  = null;
        _indexCount   = indices.Count;

        if (_indexCount == 0) return;

        _vertexBuffer = new VertexBuffer(device, VertexPositionTexture.VertexDeclaration,
                                         vertices.Count, BufferUsage.WriteOnly);
        _vertexBuffer.SetData(vertices.ToArray());

        _indexBuffer = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits,
                                        indices.Count, BufferUsage.WriteOnly);
        _indexBuffer.SetData(indices.ToArray());
    }

    // Emits 4 vertices for a face in order: top-left, top-right, bottom-right, bottom-left.
    private static void AddFaceVertices(
        List<VertexPositionTexture> verts,
        float bx, float by, float bz, int face,
        Vector2 tl, Vector2 tr, Vector2 br, Vector2 bl)
    {
        switch (face)
        {
            case 0: // Top (+Y) — viewed from above
                verts.Add(new VertexPositionTexture(new Vector3(bx,   by + 1, bz),     tl));
                verts.Add(new VertexPositionTexture(new Vector3(bx+1, by + 1, bz),     tr));
                verts.Add(new VertexPositionTexture(new Vector3(bx+1, by + 1, bz + 1), br));
                verts.Add(new VertexPositionTexture(new Vector3(bx,   by + 1, bz + 1), bl));
                break;
            case 1: // Bottom (-Y) — viewed from below
                verts.Add(new VertexPositionTexture(new Vector3(bx,   by, bz + 1), tl));
                verts.Add(new VertexPositionTexture(new Vector3(bx+1, by, bz + 1), tr));
                verts.Add(new VertexPositionTexture(new Vector3(bx+1, by, bz),     br));
                verts.Add(new VertexPositionTexture(new Vector3(bx,   by, bz),     bl));
                break;
            case 2: // Front (+Z)
                verts.Add(new VertexPositionTexture(new Vector3(bx,   by + 1, bz + 1), tl));
                verts.Add(new VertexPositionTexture(new Vector3(bx+1, by + 1, bz + 1), tr));
                verts.Add(new VertexPositionTexture(new Vector3(bx+1, by,     bz + 1), br));
                verts.Add(new VertexPositionTexture(new Vector3(bx,   by,     bz + 1), bl));
                break;
            case 3: // Back (-Z)
                verts.Add(new VertexPositionTexture(new Vector3(bx+1, by + 1, bz), tl));
                verts.Add(new VertexPositionTexture(new Vector3(bx,   by + 1, bz), tr));
                verts.Add(new VertexPositionTexture(new Vector3(bx,   by,     bz), br));
                verts.Add(new VertexPositionTexture(new Vector3(bx+1, by,     bz), bl));
                break;
            case 4: // Right (+X)
                verts.Add(new VertexPositionTexture(new Vector3(bx+1, by + 1, bz + 1), tl));
                verts.Add(new VertexPositionTexture(new Vector3(bx+1, by + 1, bz),     tr));
                verts.Add(new VertexPositionTexture(new Vector3(bx+1, by,     bz),     br));
                verts.Add(new VertexPositionTexture(new Vector3(bx+1, by,     bz + 1), bl));
                break;
            case 5: // Left (-X)
                verts.Add(new VertexPositionTexture(new Vector3(bx, by + 1, bz),     tl));
                verts.Add(new VertexPositionTexture(new Vector3(bx, by + 1, bz + 1), tr));
                verts.Add(new VertexPositionTexture(new Vector3(bx, by,     bz + 1), br));
                verts.Add(new VertexPositionTexture(new Vector3(bx, by,     bz),     bl));
                break;
        }
    }

    /// <summary>Submits this mesh to the GPU. BasicEffect passes must already be applied by the caller.</summary>
    public void Draw(GraphicsDevice device)
    {
        if (IsEmpty || _vertexBuffer is null || _indexBuffer is null) return;

        device.SetVertexBuffer(_vertexBuffer);
        device.Indices = _indexBuffer;
        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _indexCount / 3);
    }

    public void Dispose()
    {
        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();
    }
}

