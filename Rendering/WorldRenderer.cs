// Architecture: Owns all ChunkMesh objects and drives the dirty-mesh rebuild loop.
// Keeps rendering concerns (GPU buffers) separated from world data (Chunk).
//
// Update() order each frame:
//   1. LightingManager.ProcessQueue — drains BFS budget; marks touched chunks dirty.
//   2. For each dirty chunk: ChunkMesh.Build → chunk.IsDirty = false.
//
// Lighting is processed once per frame before any mesh rebuild.
// No per-chunk lighting recalculation; no world-wide flood fill.
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
    private readonly LightingManager    _lighting;

    /// <summary>
    /// When true the renderer displays raw vertex colours (lighting values) without texture.
    /// Toggle with F4.
    /// </summary>
    public bool DebugLighting { get; set; }

    public WorldRenderer(Vox.AI.World.World world, LightingManager lighting)
    {
        _world    = world;
        _lighting = lighting;
    }

    /// <summary>
    /// Drains the lighting queue (up to budget), then rebuilds meshes for dirty chunks.
    /// Call once per frame before Draw.
    /// </summary>
    public void Update(GraphicsDevice device)
    {
        // Process incremental lighting work first so any newly dirtied chunks
        // are caught in the mesh-rebuild pass below.
        _lighting.ProcessQueue();

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

