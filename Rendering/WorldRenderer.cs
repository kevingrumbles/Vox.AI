// Architecture: Owns all ChunkMesh objects and drives the dirty-mesh rebuild loop.
// Keeps rendering concerns (GPU buffers) separated from world data (Chunk).
// Update() rebuilds meshes for dirty chunks each frame before Draw() is called.

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Vox.AI.World;

namespace Vox.AI.Rendering;

public sealed class WorldRenderer : IDisposable
{
    private readonly Dictionary<(int, int, int), ChunkMesh> _meshes = new();
    private readonly Vox.AI.World.World _world;

    public WorldRenderer(Vox.AI.World.World world) => _world = world;

    /// <summary>Rebuilds meshes for any chunk marked dirty. Call once per frame before Draw.</summary>
    public void Update(GraphicsDevice device)
    {
        foreach (var chunk in _world.Chunks)
        {
            if (!chunk.IsDirty) continue;

            if (!_meshes.TryGetValue(chunk.ChunkCoord, out var mesh))
            {
                mesh = new ChunkMesh();
                _meshes[chunk.ChunkCoord] = mesh;
            }

            mesh.Build(chunk, _world, device);
            chunk.IsDirty = false;
        }
    }

    /// <summary>Draws all chunk meshes. The caller must apply BasicEffect passes first.</summary>
    public void Draw(GraphicsDevice device)
    {
        foreach (var mesh in _meshes.Values)
            mesh.Draw(device);
    }

    public void Dispose()
    {
        foreach (var mesh in _meshes.Values)
            mesh.Dispose();
        _meshes.Clear();
    }
}
