using Unity.Entities;
using Unity.Mathematics;

namespace DOTS.Component.Spawn
{
    public struct SpawnPointComponent : IComponentData
    {
        public int Flag;
        public int2 SpawnCount;
        public float Space;
        public float SpawnInterval;
        
        public float SpawnTimer;
        public bool IsReady => SpawnTimer >= SpawnInterval;
    }
}