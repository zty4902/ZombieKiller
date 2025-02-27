using System.Runtime.InteropServices;
using DOTS.Component.Common;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace DOTS.System.Common
{
    public partial struct AutoDestroySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var entityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            new AutoDestroySystemJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                Ecb = entityCommandBuffer.AsParallelWriter(),
                
            }.ScheduleParallel(state.Dependency).Complete();
            entityCommandBuffer.Playback(state.EntityManager);
            entityCommandBuffer.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile]
    [StructLayout(LayoutKind.Auto)]
    public partial struct AutoDestroySystemJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter Ecb;

        private void Execute([EntityIndexInQuery]int index,Entity entity,ref AutoDestroyComponent autoDestroyComponent)
        {
            if (autoDestroyComponent.DestroyTime > 0)
            {
                autoDestroyComponent.DestroyTime -= DeltaTime;
                return;
            }
            Ecb.DestroyEntity(index, entity);
        }
    }
}