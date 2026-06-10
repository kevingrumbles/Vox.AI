// SaveQueue: off-thread serialization and file writing so the render and input
// loops are never stalled by disk I/O.
//
// Design:
//   - Enqueue() is called from the main thread and returns immediately.
//   - A single background worker thread drains the queue and writes files.
//   - _pendingCount is kept via Interlocked so Flush() can spin-wait safely.
//   - Flush() blocks the caller until all queued items are written (used at shutdown).
//   - Dispose() signals the worker to finish any remaining work and joins it.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace Vox.AI.World.Persistence;

public sealed class SaveQueue : IDisposable
{
    private readonly ConcurrentQueue<(ChunkSaveData Data, string FilePath)> _queue = new();
    private readonly Thread  _worker;
    private volatile bool    _running = true;
    private volatile int     _pendingCount;

    public SaveQueue()
    {
        _worker = new Thread(WorkerLoop)
        {
            Name         = "ChunkSaveWorker",
            IsBackground = true,   // won't prevent process exit if Dispose is not called
        };
        _worker.Start();
    }

    // -----------------------------------------------------------------------
    // Public API — called from the main thread
    // -----------------------------------------------------------------------

    /// <summary>Queues a chunk for background serialization and disk write.</summary>
    public void Enqueue(ChunkSaveData data, string filePath)
    {
        Interlocked.Increment(ref _pendingCount);
        _queue.Enqueue((data, filePath));
    }

    /// <summary>
    /// Blocks the calling thread until every previously enqueued save has been written.
    /// Safe to call from the main thread at shutdown; expected to return in milliseconds.
    /// </summary>
    public void Flush()
    {
        while (_pendingCount > 0)
            Thread.Sleep(5);
    }

    public void Dispose()
    {
        _running = false;
        _worker.Join(TimeSpan.FromSeconds(15));
    }

    // -----------------------------------------------------------------------
    // Background worker
    // -----------------------------------------------------------------------

    private void WorkerLoop()
    {
        while (_running || _pendingCount > 0)
        {
            if (_queue.TryDequeue(out var item))
            {
                try
                {
                    WriteChunk(item.Data, item.FilePath);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[SaveQueue] Failed to write {item.FilePath}: {ex.Message}");
                }
                finally
                {
                    Interlocked.Decrement(ref _pendingCount);
                }
            }
            else
            {
                Thread.Sleep(5);   // avoid busy-spin when the queue is empty
            }
        }
    }

    private static void WriteChunk(ChunkSaveData data, string filePath)
    {
        byte[] bytes = ChunkSerializer.Serialize(data);

        // Create the chunks/ directory if it does not yet exist
        string? dir = Path.GetDirectoryName(filePath);
        if (dir is not null)
            Directory.CreateDirectory(dir);

        // Write to a temp file then rename — prevents a partial file on crash
        string tmp = filePath + ".tmp";
        File.WriteAllBytes(tmp, bytes);
        File.Move(tmp, filePath, overwrite: true);
    }
}
