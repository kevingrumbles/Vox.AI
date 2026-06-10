// WorldPersistenceManager: top-level persistence coordinator.
//
// Responsibilities:
//   LoadOrCreateMetadata  — read world.meta or create a fresh one
//   SaveMetadata          — write world.meta (JSON)
//   LoadChunk             — generate from seed, then overlay saved modifications
//   QueueChunkSave        — enqueue dirty chunks for background write (non-blocking)
//   SaveChunk             — synchronous write used only at shutdown
//   SaveWorld             — queue-save all dirty chunks, flush, then write metadata
//   FlushPendingSaves     — block until SaveQueue is empty
//
// The in-memory _chunkCache avoids re-reading .chunk files every time a chunk
// is loaded; it is populated on startup by PreloadChunkCache().

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Vox.AI.Generation;

namespace Vox.AI.World.Persistence;

public sealed class WorldPersistenceManager : IDisposable
{
    // -----------------------------------------------------------------------
    // Constants
    // -----------------------------------------------------------------------

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    // -----------------------------------------------------------------------
    // State
    // -----------------------------------------------------------------------

    private readonly WorldMetadata _metadata;
    private readonly string        _saveDir;     // Saves/{WorldName}/
    private readonly string        _chunksDir;   // Saves/{WorldName}/chunks/
    private readonly SaveQueue     _saveQueue;

    // In-memory mirror of every .chunk file loaded or saved this session
    private readonly Dictionary<ChunkPosition, ChunkSaveData> _chunkCache = new();

    public WorldMetadata Metadata => _metadata;

    // -----------------------------------------------------------------------
    // Construction
    // -----------------------------------------------------------------------

    public WorldPersistenceManager(WorldMetadata metadata, string savesRoot)
    {
        _metadata  = metadata;
        _saveDir   = Path.Combine(savesRoot, metadata.WorldName);
        _chunksDir = Path.Combine(_saveDir, "chunks");
        _saveQueue = new SaveQueue();

        Directory.CreateDirectory(_chunksDir);
        PreloadChunkCache();
    }

    // -----------------------------------------------------------------------
    // Metadata
    // -----------------------------------------------------------------------

    /// <summary>
    /// Loads world.meta from disk if it exists; otherwise creates a new metadata object
    /// using the supplied world name and seed.
    /// </summary>
    public static WorldMetadata LoadOrCreateMetadata(string worldName, long seed, string savesRoot)
    {
        string path = Path.Combine(savesRoot, worldName, "world.meta");

        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                var meta = JsonSerializer.Deserialize<WorldMetadata>(json);
                if (meta is not null) return meta;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Persistence] Failed to read world.meta: {ex.Message}. Creating new.");
            }
        }

        return new WorldMetadata
        {
            WorldName    = worldName,
            Seed         = seed,
            CreatedUtc   = DateTime.UtcNow,
            LastSavedUtc = DateTime.UtcNow,
        };
    }

    /// <summary>Writes world.meta to disk as indented JSON.</summary>
    public void SaveMetadata()
    {
        _metadata.LastSavedUtc = DateTime.UtcNow;
        Directory.CreateDirectory(_saveDir);
        string json = JsonSerializer.Serialize(_metadata, JsonOptions);
        File.WriteAllText(Path.Combine(_saveDir, "world.meta"), json);
    }

    // -----------------------------------------------------------------------
    // Chunk load — called from World during chunk initialization
    // -----------------------------------------------------------------------

    /// <summary>
    /// Loads a chunk by first generating it from the seed (via <paramref name="generator"/>),
    /// then applying any persisted player modifications on top.
    /// The chunk's HasUnsavedChanges flag is NOT set; only IsDirty is set for mesh rebuild.
    /// </summary>
    public void LoadChunk(Chunk chunk, TerrainGenerator generator)
    {
        // Step 1: Generate base terrain — does not mark HasUnsavedChanges
        generator.Generate(chunk);

        // Steps 2-3: Apply saved player modifications if any exist for this chunk
        var pos = ChunkPositionOf(chunk);
        if (_chunkCache.TryGetValue(pos, out var saveData))
            ApplyModifications(chunk, saveData);

        // Step 4: IsDirty is already true (set in Chunk constructor + by generation)
        // HasUnsavedChanges remains false — these are restored modifications, not new ones
    }

    // -----------------------------------------------------------------------
    // Chunk save — public API
    // -----------------------------------------------------------------------

    /// <summary>
    /// Builds a <see cref="ChunkSaveData"/> from the chunk's modifications and enqueues it
    /// for non-blocking background write. Clears HasUnsavedChanges immediately.
    /// </summary>
    public void QueueChunkSave(Chunk chunk)
    {
        if (!chunk.HasUnsavedChanges) return;

        ChunkSaveData saveData = BuildSaveData(chunk);
        _chunkCache[saveData.Position] = saveData;
        _saveQueue.Enqueue(saveData, GetChunkFilePath(saveData.Position));

        chunk.ClearUnsavedChanges();
    }

    /// <summary>
    /// Immediately serializes and writes the chunk file on the calling thread.
    /// Used only at application shutdown where a background write might be too late.
    /// </summary>
    public void SaveChunk(Chunk chunk)
    {
        if (!chunk.HasUnsavedChanges) return;

        ChunkSaveData saveData = BuildSaveData(chunk);
        _chunkCache[saveData.Position] = saveData;

        string  filePath = GetChunkFilePath(saveData.Position);
        byte[]  bytes    = ChunkSerializer.Serialize(saveData);
        string? dir      = Path.GetDirectoryName(filePath);
        if (dir is not null) Directory.CreateDirectory(dir);

        string tmp = filePath + ".tmp";
        File.WriteAllBytes(tmp, bytes);
        File.Move(tmp, filePath, overwrite: true);

        chunk.ClearUnsavedChanges();
    }

    /// <summary>
    /// Queues all dirty chunks for background save, waits for the queue to drain,
    /// then writes world.meta. Safe to call from the main thread (Update or shutdown).
    /// </summary>
    public void SaveWorld(IEnumerable<Chunk> chunks)
    {
        foreach (var chunk in chunks.Where(c => c.HasUnsavedChanges))
            QueueChunkSave(chunk);

        FlushPendingSaves();
        SaveMetadata();
    }

    /// <summary>Blocks until the background save queue is fully drained.</summary>
    public void FlushPendingSaves() => _saveQueue.Flush();

    // -----------------------------------------------------------------------
    // Disposal
    // -----------------------------------------------------------------------

    public void Dispose()
    {
        _saveQueue.Flush();   // drain any remaining queued saves before exit
        _saveQueue.Dispose();
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    // Reads all existing .chunk files into memory at startup so LoadChunk
    // doesn't need to touch the file system per-chunk during world generation.
    private void PreloadChunkCache()
    {
        if (!Directory.Exists(_chunksDir)) return;

        foreach (string filePath in Directory.GetFiles(_chunksDir, "*.chunk"))
        {
            try
            {
                byte[]          bytes    = File.ReadAllBytes(filePath);
                ChunkSaveData?  saveData = ChunkSerializer.Deserialize(bytes);
                if (saveData is not null)
                    _chunkCache[saveData.Position] = saveData;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Persistence] Could not preload {Path.GetFileName(filePath)}: {ex.Message}");
            }
        }
    }

    // Applies each stored modification to the chunk without touching HasUnsavedChanges.
    private static void ApplyModifications(Chunk chunk, ChunkSaveData saveData)
    {
        foreach (var mod in saveData.Modifications)
            chunk.ApplyModification(mod.BlockIndex, mod.BlockId);
    }

    // Builds a ChunkSaveData snapshot from the chunk's current modification set.
    private static ChunkSaveData BuildSaveData(Chunk chunk) => new()
    {
        Position      = ChunkPositionOf(chunk),
        Modifications = new List<BlockModification>(chunk.GetModifications()),
        ChunkVersion  = ChunkSaveData.CurrentVersion,
    };

    private static ChunkPosition ChunkPositionOf(Chunk chunk) =>
        new(chunk.ChunkCoord.X, chunk.ChunkCoord.Y, chunk.ChunkCoord.Z);

    private string GetChunkFilePath(ChunkPosition pos) =>
        Path.Combine(_chunksDir, $"{pos}.chunk");
}
