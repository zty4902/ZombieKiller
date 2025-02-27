using Unity.Entities;

namespace DOTS.BufferElement
{
    public struct PrefabEntityBufferElement : IBufferElementData
    {
        public int Flag;
        public EPrefabName PrefabName;
        public Entity Prefab;
    }
}