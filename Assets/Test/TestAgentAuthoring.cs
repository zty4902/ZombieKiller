using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Test
{
    public class TestAgentAuthoring : MonoBehaviour
    {
        public float3 destination;
        private class TestAgentAuthoringBaker : Baker<TestAgentAuthoring>
        {
            public override void Bake(TestAgentAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity,new TestAgentComponentData()
                {
                    Destination = authoring.destination
                });
            }
        }
    }
}