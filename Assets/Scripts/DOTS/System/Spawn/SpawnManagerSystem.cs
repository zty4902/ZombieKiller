using DOTS.BufferElement;
using DOTS.Component.Common;
using DOTS.Component.Player;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace DOTS.System.Spawn
{
    public partial struct SpawnManagerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<SpawnManagerBufferElement>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var spawnManagerBufferElements = SystemAPI.GetSingletonBuffer<SpawnManagerBufferElement>();
            var entityCommandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var spawnManagerBufferElement in spawnManagerBufferElements)
            {
                var instantiate = entityCommandBuffer.Instantiate(spawnManagerBufferElement.Prefab);
                entityCommandBuffer.SetComponent(instantiate,LocalTransform.FromPosition(spawnManagerBufferElement.Position));
                entityCommandBuffer.AddComponent(instantiate,new PlayerComponent
                {
                    PlayerId = spawnManagerBufferElement.PlayerId
                });
            }
            spawnManagerBufferElements.Clear();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}