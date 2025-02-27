using System.Runtime.InteropServices;
using DOTS.BufferElement;
using DOTS.Component.Combat.Skill;
using DOTS.Component.FSM;
using DOTS.SystemGroup;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DOTS.System.Combat.Skill
{
    [StructLayout(LayoutKind.Auto)][UpdateInGroup(typeof(SkillHandleSystemGroup))]
    public partial struct SkillBeatOffSystem : ISystem
    {
        private ComponentLookup<FsmStateComponent> _fsmStateLookup;
        private ComponentLookup<LocalTransform> _localTransformLookup;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _fsmStateLookup = state.GetComponentLookup<FsmStateComponent>();
            _localTransformLookup = state.GetComponentLookup<LocalTransform>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _fsmStateLookup.Update(ref state);
            _localTransformLookup.Update(ref state);
            //ecb
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            new BeatOffSystemJob
            {
                FsmStateLookup = _fsmStateLookup,
                LocalTransformLookup = _localTransformLookup,
                Ecb = ecb.AsParallelWriter(),
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel(state.Dependency).Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        [BurstCompile]
        [StructLayout(LayoutKind.Auto)]
        public partial struct BeatOffSystemJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<FsmStateComponent> FsmStateLookup;
            [ReadOnly]
            public ComponentLookup<LocalTransform> LocalTransformLookup;
            public EntityCommandBuffer.ParallelWriter Ecb;
            public float DeltaTime;

            private void Execute([EntityIndexInQuery]int index,ref SkillBeatOffComponent skillBeatOffComponent,in SkillBaseComponent skillBaseComponent,
                in DynamicBuffer<KnnTriggerBufferElement> buffer)
            {
                if (buffer.Length == 0 && skillBeatOffComponent.Target == Entity.Null)
                {
                    return;
                }

                if (skillBeatOffComponent.Target == Entity.Null)
                {
                    skillBeatOffComponent.Target = buffer[0].Entity;
                }
                if (skillBeatOffComponent.AttackTimer < skillBeatOffComponent.Time)
                {
                    skillBeatOffComponent.AttackTimer += DeltaTime;
                    return;
                }

                if (LocalTransformLookup.TryGetComponent(skillBaseComponent.Owner,out var ownerTransform))
                {
                    /*foreach (var knnTriggerBufferElement in buffer)
                    {*/
                        if (FsmStateLookup.TryGetComponent(skillBeatOffComponent.Target, out var fsmStateComponent)
                            && LocalTransformLookup.TryGetComponent(skillBeatOffComponent.Target, out var enemyTransform))
                        {
                            var dir = enemyTransform.Position.x - ownerTransform.Position.x;
                            fsmStateComponent.FlashMove.MoveOffset = new float3(skillBeatOffComponent.Force * math.sign(dir), 0, 0);
                            fsmStateComponent.CurFsmBufferEvent |= EFsmBufferEventName.Pause;
                            Ecb.SetComponent(index,skillBeatOffComponent.Target, fsmStateComponent);
                        }
                    //}
                }
                
                //buffer.Clear();
                skillBeatOffComponent.AttackTimer = 0;
                skillBeatOffComponent.Target = Entity.Null;
            }
        }
    }
}