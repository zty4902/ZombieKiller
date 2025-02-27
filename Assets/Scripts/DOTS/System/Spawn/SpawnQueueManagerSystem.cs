using DOTS.BufferElement;
using DOTS.Component.Spawn;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace DOTS.System.Spawn
{
    public partial struct SpawnQueueManagerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SpawnQueueManagerComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var spawnQueueManagerEntity = SystemAPI.GetSingletonEntity<SpawnQueueManagerComponent>();
            var spawnRequestBufferElements = SystemAPI.GetBuffer<SpawnRequestBufferElement>(spawnQueueManagerEntity).ToNativeArray(Allocator.Temp);
            var bufferElements = new NativeList<SpawnRequestBufferElement>(Allocator.Temp);
            foreach (var t in spawnRequestBufferElements)
            {
                bufferElements.Add(t);
            }

            var entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            for (var flag = 0; flag < 2; flag++)
            {
                foreach (var (spawnPointComponentRW, entity) in SystemAPI.Query<RefRW<SpawnPointComponent>>().WithEntityAccess())
                {
                    if (bufferElements.Length == 0)
                    {
                        break;
                    }
                    if (!spawnPointComponentRW.ValueRO.IsReady || spawnPointComponentRW.ValueRO.Flag != flag)
                    {
                        continue;
                    }

                    var maxCount = spawnPointComponentRW.ValueRO.SpawnCount.x * spawnPointComponentRW.ValueRO.SpawnCount.y;
                    var curCount = 0;
                    int index;
                    for (index = 0; index < bufferElements.Length; index++)
                    {
                        var spawnRequest = bufferElements[index];
                        if (spawnRequest.Flag!= flag)
                        {
                            continue;
                        }
                        curCount += spawnRequest.SpawnCount;
                        if (curCount >= maxCount)
                        {
                            index++;
                            break;
                        }
                    }
                    for (var i = 0; i < index; i++)
                    {
                        var spawnRequest = bufferElements[i];
                        if (spawnRequest.Flag != flag)
                        {
                            continue;
                        }
                        if (i == index-1)//最后一个
                        {
                            var lastUseCount = spawnRequest.SpawnCount;
                            if (curCount > maxCount)
                            {
                                lastUseCount = spawnRequest.SpawnCount - (curCount - maxCount);
                                spawnRequest.SpawnCount = curCount - maxCount;
                                bufferElements[index - 1] = spawnRequest;
                            }
                            else
                            {
                                bufferElements.RemoveAt(index - 1);
                            }
                            entityCommandBuffer.AppendToBuffer(entity,new SpawnRequestBufferElement
                            {
                                Flag = spawnRequest.Flag,
                                CharacterName = spawnRequest.CharacterName,
                                PlayerId = spawnRequest.PlayerId,
                                SpawnCount = lastUseCount
                            });
                        }
                        else
                        {
                            entityCommandBuffer.AppendToBuffer(entity,spawnRequest);
                            bufferElements.RemoveAt(i);
                            if (bufferElements.Length == 0)
                            {
                                break;
                            }
                            i--;
                            index--;
                        }
                    }
                }
            }

            var requestBufferElements = entityCommandBuffer.SetBuffer<SpawnRequestBufferElement>(spawnQueueManagerEntity);
            requestBufferElements.AddRange(bufferElements.AsArray());

            bufferElements.Dispose();
            spawnRequestBufferElements.Dispose();
            entityCommandBuffer.Playback(state.EntityManager);
            entityCommandBuffer.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}