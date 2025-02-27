using DOTS.BufferElement;
using DOTS.Component.Common;
using DOTS.Component.Spawn;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DOTS.System.Spawn
{
    public partial struct SpawnPointSystem : ISystem
    {
        private NativeHashMap<int, Entity> _spawnedPrefabs;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PrefabManagerComponent>();
            _spawnedPrefabs = new NativeHashMap<int, Entity>(23, Allocator.Persistent);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (_spawnedPrefabs.Count == 0)
            {
                var prefabManagerComponent = SystemAPI.GetSingleton<PrefabManagerComponent>();
                foreach (var prefabItem in prefabManagerComponent.Zombies.Value)
                {
                    var key = (int)prefabItem.Name;
                    _spawnedPrefabs[key] = prefabItem.Prefab;
                }
                foreach (var prefabItem in prefabManagerComponent.Heroes.Value)
                {
                    var key = (int)prefabItem.Name;
                    _spawnedPrefabs[key] = prefabItem.Prefab;
                }
            }

            var spawnManagerBufferElements = SystemAPI.GetSingletonBuffer<SpawnManagerBufferElement>();
            var delTime = SystemAPI.Time.DeltaTime;
            foreach (var (spawnPointComponentRW, spawnRequestBufferElements, localTransform) in SystemAPI.Query<RefRW<SpawnPointComponent>,DynamicBuffer<SpawnRequestBufferElement>,RefRO<LocalTransform>>())
            {
                ref var spawnPointComponent = ref spawnPointComponentRW.ValueRW;
                spawnPointComponent.SpawnTimer += delTime;
                if (spawnPointComponent.SpawnTimer < spawnPointComponent.SpawnInterval)
                {
                    continue;
                }

                if (spawnRequestBufferElements.Length == 0)
                {
                    continue;
                }

                var fac = spawnPointComponent.Flag == 0 ? 1 : -1;
                var xIndex = 0;
                var yIndex = 0;
                var sizeX = spawnPointComponent.SpawnCount.x * spawnPointComponent.Space;
                var sizeY = spawnPointComponent.SpawnCount.y * spawnPointComponent.Space;
                var pos = localTransform.ValueRO.Position + new float3(sizeX/2*fac, sizeY/2, 0) - new float3(0.1f*fac,0.1f,0);
                foreach (var spawnRequestBufferElement in spawnRequestBufferElements)
                {
                    for (var i = 0; i < spawnRequestBufferElement.SpawnCount; i++)
                    {
                        if (!_spawnedPrefabs.TryGetValue((int)spawnRequestBufferElement.CharacterName, out var prefab))
                        {
                            continue;
                        }
                        var spawnPos = pos + new float3(xIndex*fac,yIndex,0) * -spawnPointComponent.Space;
                        spawnManagerBufferElements.Add(new SpawnManagerBufferElement
                        {
                            PlayerId = spawnRequestBufferElement.PlayerId,
                            Position = spawnPos,
                            Prefab = prefab
                        });
                        if (yIndex < spawnPointComponent.SpawnCount.y - 1)
                        {
                            yIndex++;
                        }
                        else
                        {
                            yIndex = 0;
                            xIndex++;
                        }
                    }
                }
                spawnRequestBufferElements.Clear();
                spawnPointComponent.SpawnTimer = (1 - (xIndex+1) * 1.0f / spawnPointComponent.SpawnCount.x) * spawnPointComponent.SpawnInterval;
                //spawnPointComponent.SpawnTimer = 0;
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            _spawnedPrefabs.Dispose();
        }
    }
}