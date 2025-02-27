using Unity.Entities;
using Unity.Mathematics;

namespace Test
{
    public struct TestAgentComponentData : IComponentData
    {
        public float3 Destination;
    }
}