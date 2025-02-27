using DOTS.BufferElement;
using DOTS.Component.Combat.Skill;
using DOTS.SystemGroup;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace DOTS.System.Combat.Skill
{
    [UpdateInGroup(typeof(SkillHandleSystemGroup))]
    public partial struct SkillAnimationSystem : ISystem
    {
        private ComponentLookup<LocalTransform> _localTransformLookup;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _localTransformLookup = state.GetComponentLookup<LocalTransform>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var entityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            _localTransformLookup.Update(ref state);
            new SkillAnimationSystemJob
            {
                Ecb = entityCommandBuffer.AsParallelWriter(),
                LocalTransformLookup = _localTransformLookup
            }.ScheduleParallel(state.Dependency).Complete();
            entityCommandBuffer.Playback(state.EntityManager);
            entityCommandBuffer.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        [BurstCompile]
        public partial struct SkillAnimationSystemJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            [ReadOnly]
            public ComponentLookup<LocalTransform> LocalTransformLookup;

            private void Execute([EntityIndexInQuery]int index,in SkillAnimationComponent skillAnimationComponent,in DynamicBuffer<KnnTriggerBufferElement> knnTriggerBuffer)
            {
                if (knnTriggerBuffer.Length == 0)
                {
                    return;
                }
                foreach (var knnTriggerBufferElement in knnTriggerBuffer)
                {
                    if (LocalTransformLookup.TryGetComponent(knnTriggerBufferElement.Entity, out var localTransform))
                    {
                        var instantiate = Ecb.Instantiate(index,skillAnimationComponent.AnimationEntity);
                        Ecb.SetComponent(index,instantiate,LocalTransform.FromPosition(localTransform.Position));
                    }
                }

                //knnTriggerBuffer.Clear();
            }
        }
    }
}