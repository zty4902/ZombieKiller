using DOTS.Component.Combat;
using DOTS.Component.FSM;
using NSprites;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace DOTS.System.Combat
{
    public partial struct DamageFlashSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var entityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            new DamageFlashSystemJob
            {
                Ecb = entityCommandBuffer.AsParallelWriter(),
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel(state.Dependency).Complete();
            entityCommandBuffer.Playback(state.EntityManager);
            entityCommandBuffer.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        [BurstCompile]
        public partial struct DamageFlashSystemJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter Ecb;

            private void Execute([EntityIndexInQuery]int index,Entity entity
                ,ref DamageFlashComponent damageFlashComponent,ref NsColor nsColor,in FsmStateRuntimeDataComponent runtimeDataComponent)
            {
                if (damageFlashComponent.Duration > 0)
                {
                    damageFlashComponent.Duration -= DeltaTime;
                    nsColor.value = new float4(1, 1, 1, 0.7f);
                }
                else
                {
                    nsColor.value = runtimeDataComponent.RenderColor;
                    Ecb.SetComponentEnabled<DamageFlashComponent>(index, entity, false);
                }
            }
        }
    }
}