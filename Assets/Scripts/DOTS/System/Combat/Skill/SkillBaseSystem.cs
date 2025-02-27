using DOTS.BufferElement;
using DOTS.Component.Combat.Skill;
using DOTS.SystemGroup;
using Unity.Burst;
using Unity.Entities;

namespace DOTS.System.Combat.Skill
{
    [UpdateInGroup(typeof(SkillHandleSystemGroup),OrderLast = true)]
    public partial struct SkillBaseSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new SkillBaseSystemJob().ScheduleParallel(state.Dependency).Complete();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        [BurstCompile]
        public partial struct SkillBaseSystemJob : IJobEntity
        {
            private void Execute(in SkillBaseComponent skillBaseComponent,DynamicBuffer<KnnTriggerBufferElement> knnTriggerBufferElements)
            {
                knnTriggerBufferElements.Clear();
            }
        }
    }
}