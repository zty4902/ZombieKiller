using DOTS.BufferElement;
using DOTS.Component.Combat.Skill;
using DOTS.SystemGroup;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace DOTS.System.Combat.Skill
{
    [UpdateBefore(typeof(SkillBaseSystem))][UpdateInGroup(typeof(SkillHandleSystemGroup),OrderLast = true)]
    public partial struct SkillDamageSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var entityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            new SkillDamageSystemJob
            {
                Ecb = entityCommandBuffer.AsParallelWriter()
            }.ScheduleParallel(state.Dependency).Complete();
            entityCommandBuffer.Playback(state.EntityManager);
            entityCommandBuffer.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        [BurstCompile]
        public partial struct SkillDamageSystemJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            private void Execute([EntityIndexInChunk]int index,in SkillDamageComponent skillDamageComponent,in DynamicBuffer<KnnTriggerBufferElement> knnTriggerBuffer)
            {
                if (knnTriggerBuffer.Length == 0)
                {
                    return;
                }
                foreach (var knnTriggerBufferElement in knnTriggerBuffer)
                {
                    Ecb.AppendToBuffer(index,knnTriggerBufferElement.Entity,new CombatDamageBufferElement
                    {
                        Damage = skillDamageComponent.Damage,
                        DamageType = skillDamageComponent.DamageType
                    });
                }
                //knnTriggerBuffer.Clear();
            }
        }
    }
}