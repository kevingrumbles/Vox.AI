// ChunkLightState: lifecycle state for a chunk's sunlight data.
//
// States:
//   Unlit             — chunk has just been generated; no sunlight has been assigned.
//   LightingPending   — initial column sweep is done; border exchange with neighbours
//                       has been requested but not yet completed.
//   LightingComplete  — sunlight data is fully propagated and up to date.
//                       The chunk mesh may be built from this data.
//
// Transitions:
//   Unlit → LightingPending   : LightingManager.InitialiseChunk()
//   LightingPending → Complete : LightingManager.ProcessQueue() drains the chunk's seeds
//   Complete → LightingPending : OnBlockChanged() enqueues new nodes

namespace Vox.AI.World.Lighting;

public enum ChunkLightState
{
    Unlit,
    LightingPending,
    LightingComplete,
}
