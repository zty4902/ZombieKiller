using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Mathematics;

namespace DOTS.BufferElement
{
    public struct SpawnManagerBufferElement : IBufferElementData
    {
        public int PlayerId;
        public Entity Prefab;
        public float3 Position;
    }
}