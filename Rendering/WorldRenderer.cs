// Architecture: Owns all ChunkMesh objects and drives the dirty-mesh rebuild loop.
// Keeps rendering concerns (GPU buffers) separated from world data (Chunk).
//
// Update() order per dirty chunk:
//   1. LightingManager.RecalculateSunlight  — must run before mesh build
//   2. ChunkMesh.Build                      — bakes lighting into vertex colours
//   3. chunk.IsDirty = false
//
// Debug mode (F4): switches BasicEffect to show only vertex colours (no texture),
// making sunlight and AO values directly visible on the mesh.

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Vox.AI.World;
using Vox.AI.World.Lighting;

namespace Vox.AI.Rendering;

public sealed class WorldRenderer : IDisposable
{
    private readonly Dictionary<(int, int, int), ChunkMesh> _meshes = new();
    private readonly Vox.AI.World.World _world;

    /// <summary>
    /// When true the renderer displays raw vertex colours (lighting values) without texture.
    /// Toggle with F4.
    /// </summary>
    public bool DebugLighting { get; set; }

    public WorldRenderer(Vox.AI.World.World world) => _world = world;

    /// <summary>
    /// Recalculates lighting and rebuilds meshes for any chunk marked dirty.
    /// Call once per frame before Draw.
    /// </summary>
    public void Update(GraphicsDevice device)
    {
        foreach (var chunk in _world.Chunks)
        {
            if (!chunk.IsDirty) continue;

            // Lighting must be current before mesh vertices are baked
            LightingManager.RecalculateSunlight(chunk);

            if (!_meshes.TryGetValue(chunk.ChunkCoord, out var mesh))
            {
                mesh = new ChunkMesh();
                _meshes[chunk.ChunkCoord] = mesh;
            }

            mesh.Build(chunk, _world, device);
            chunk.IsDirty = false;
        }
    }

    /// <summary>
    /// Draws all chunk meshes.
    /// The caller must configure and apply BasicEffect passes, then pass the same
    /// effect here so debug mode can toggle texture/vertex-colour settings.
    /// </summary>
    public void Draw(GraphicsDevice device, BasicEffect effect)
    {
        // In debug mode: show only vertex colours so lighting values are visible
        bool prevTexture     = effect.TextureEnabled;
        bool prevVertexColor = effect.VertexColorEnabled;

        if (DebugLighting)
        {
            effect.TextureEnabled     = false;
            effect.VertexColorEnabled = true;
        }

        foreach (var pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            foreach (var mesh in _meshes.Values)
                mesh.Draw(device);
        }

        // Restore original settings so the caller's state is unchanged
        if (DebugLighting)
        {
            effect.TextureEnabled     = prevTexture;
            effect.VertexColorEnabled = prevVertexColor;
        }
    }

    public void Dispose()
    {
        foreach (var mesh in _meshes.Values)
            mesh.Dispose();
        _meshes.Clear();
    }
}

