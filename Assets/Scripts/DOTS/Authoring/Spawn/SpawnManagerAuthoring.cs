using DOTS.BufferElement;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring.Spawn
{
    public class SpawnManagerAuthoring : MonoBehaviour
    {
        private class SpawnManagerAuthoringBaker : Baker<SpawnManagerAuthoring>
        {
            public override void Bake(SpawnManagerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddBuffer<SpawnManagerBufferElement>(entity);
            }
        }
    }
}