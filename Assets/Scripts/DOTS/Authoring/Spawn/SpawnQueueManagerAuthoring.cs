using DOTS.BufferElement;
using DOTS.Component.Spawn;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring.Spawn
{
    public class SpawnQueueManagerAuthoring : MonoBehaviour
    {
        private class SpawnQueueManagerAuthoringBaker : Baker<SpawnQueueManagerAuthoring>
        {
            public override void Bake(SpawnQueueManagerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddBuffer<SpawnRequestBufferElement>(entity);
                AddComponent(entity, new SpawnQueueManagerComponent());
            }
        }
    }
}