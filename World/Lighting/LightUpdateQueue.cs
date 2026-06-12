// LightUpdateQueue: persistent, pooled queue for incremental sunlight propagation.
//
// Design:
//   A single Queue<LightNode> allocated once at startup.
//   ProcessBudget() dequeues up to `budget` nodes per call to bound per-frame work.
//   The caller (LightingManager) owns the queue and passes it to SunlightCalculator.
//
// Why persistent:
//   Allocating a new Queue per block edit would cause GC pressure and frame spikes.
//   A shared queue amortises allocation cost across many frames.
//
// Thread safety: NOT thread-safe. Must only be called from the main game thread.
//
// Extension points (reserved):
//   - Add a priority field to LightNode for coloured-light or torch channels.
//   - Add a second queue for torch light without changing this class's API.

using System.Collections.Generic;

namespace Vox.AI.World.Lighting;

public sealed class LightUpdateQueue
{
    private readonly Queue<LightNode> _pending;

    /// <summary>Default per-frame processing budget (voxels).</summary>
    public const int DefaultBudget = 512;

    /// <summary>Number of nodes currently waiting to be processed.</summary>
    public int Count => _pending.Count;

    /// <summary>True when there is no pending work.</summary>
    public bool IsEmpty => _pending.Count == 0;

    public LightUpdateQueue(int initialCapacity = 2048)
    {
        _pending = new Queue<LightNode>(initialCapacity);
    }

    /// <summary>Adds a voxel position to the propagation queue.</summary>
    public void Enqueue(LightNode node) => _pending.Enqueue(node);

    /// <summary>
    /// Processes up to <paramref name="budget"/> nodes by invoking
    /// <paramref name="processNode"/> for each one.
    /// Returns the number of nodes actually processed.
    /// </summary>
    public int ProcessBudget(int budget, System.Action<LightNode, LightUpdateQueue> processNode)
    {
        int processed = 0;
        while (processed < budget && _pending.Count > 0)
        {
            var node = _pending.Dequeue();
            processNode(node, this);
            processed++;
        }
        return processed;
    }

    /// <summary>Removes all pending nodes without processing them.</summary>
    public void Clear() => _pending.Clear();
}
