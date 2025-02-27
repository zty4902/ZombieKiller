using Unity.Entities;

namespace DOTS.BufferElement
{
    public struct SpawnRequestBufferElement : IBufferElementData
    {
        public int PlayerId;
        public int Flag;
        public EPrefabName CharacterName;
        public int SpawnCount;
    }
}