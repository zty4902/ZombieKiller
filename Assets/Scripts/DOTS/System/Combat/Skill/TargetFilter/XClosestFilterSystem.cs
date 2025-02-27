using DOTS.BufferElement;
using DOTS.Component.Combat.Skill.TargetFilter;
using DOTS.Component.Common;
using DOTS.SystemGroup;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DOTS.System.Combat.Skill.TargetFilter
{
    [UpdateInGroup(typeof(SkillTargetFilterHandleGroup))]
    public partial struct XClosestFilterSystem : ISystem
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
            _localTransformLookup.Update(ref state);
            new XClosestFilterSystemJob
            {
                LocalTransformLookup = _localTransformLookup
            }.ScheduleParallel(state.Dependency).Complete();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        [BurstCompile]
        public partial struct XClosestFilterSystemJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<LocalTransform> LocalTransformLookup;

            private void Execute(in XClosestFilterComponent xClosestFilterComponent,ref DynamicBuffer<KnnTriggerBufferElement> buffer,in KnnTriggerComponent knnTriggerComponent)
            {
                if (!LocalTransformLookup.TryGetComponent(knnTriggerComponent.Owner, out var localTransform))
                {
                    return;
                }
                var playerX = localTransform.Position.x;
                var closestDistance = float.MaxValue;
                var closestEntity = Entity.Null;
                foreach (var knnTriggerBufferElement in buffer)
                {
                    var entity = knnTriggerBufferElement.Entity;
                    if (!LocalTransformLookup.TryGetComponent(entity, out var targetLocalTransform))
                    {
                        continue;
                    }
                    var targetX = targetLocalTransform.Position.x;
                    var distance = math.abs(playerX - targetX);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestEntity = entity;
                    }
                }
                buffer.Clear();
                if (closestEntity == Entity.Null)
                {
                    return;
                }
                buffer.Add(new KnnTriggerBufferElement { Entity = closestEntity });
            }
        }
    }
}