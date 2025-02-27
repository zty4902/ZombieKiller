using DOTS.BufferElement;
using DOTS.Component.Combat.Skill;
using DOTS.Component.FSM;
using DOTS.SystemGroup;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace DOTS.System.Combat.Skill
{
    [UpdateInGroup(typeof(SkillHandleSystemGroup))]
    public partial struct SkillBurningBufferSystem : ISystem
    {
        private ComponentLookup<FsmStateComponent> _fsmStateLookup;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _fsmStateLookup = state.GetComponentLookup<FsmStateComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var entityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            _fsmStateLookup.Update(ref state);
            new SkillBurningBufferSystemJob
            {
                Ecb = entityCommandBuffer.AsParallelWriter(),
                FsmStateLookup = _fsmStateLookup,
            }.ScheduleParallel(state.Dependency).Complete();

            entityCommandBuffer.Playback(state.EntityManager);
            entityCommandBuffer.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        [BurstCompile]
        public partial struct SkillBurningBufferSystemJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<FsmStateComponent> FsmStateLookup;
            public EntityCommandBuffer.ParallelWriter Ecb;
            private void Execute([EntityIndexInQuery]int index,in SkillBurningBufferComponent skillBurningBufferComponent,in DynamicBuffer<KnnTriggerBufferElement> targetBuffer)
            {
                for (var i = 0; i < targetBuffer.Length; i++)
                {
                    var targetEntity = targetBuffer[i].Entity;
                    if (FsmStateLookup.TryGetComponent(targetEntity, out var fsmStateComponent))
                    {
                        var componentDataBuff = fsmStateComponent.Buff;
                        var duration = componentDataBuff.BurningTimer + skillBurningBufferComponent.BurningDuration;
                        var damage = skillBurningBufferComponent.BurningDamage;
                        componentDataBuff.BurningDuration = math.max(duration, componentDataBuff.BurningDuration);
                        componentDataBuff.BurningDamage = math.max(damage, componentDataBuff.BurningDamage);
                        fsmStateComponent.Buff = componentDataBuff;
                        fsmStateComponent.CurFsmBufferEvent |= EFsmBufferEventName.Burning;
                        Ecb.SetComponent(index,targetEntity,fsmStateComponent);
                    }
                }
            }
        }
    }
}