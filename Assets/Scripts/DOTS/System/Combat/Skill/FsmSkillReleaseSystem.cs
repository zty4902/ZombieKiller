using System.Runtime.InteropServices;
using DOTS.BufferElement;
using DOTS.Component.Combat.Skill;
using DOTS.Component.FSM;
using DOTS.SystemGroup;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace DOTS.System.Combat.Skill
{
    [StructLayout(LayoutKind.Auto)][UpdateInGroup(typeof(SkillHandleSystemGroup), OrderFirst = true)]
    public partial struct FsmSkillReleaseSystem : ISystem
    {
        private BufferLookup<KnnTriggerBufferElement> _knnTriggerBufferLookup;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _knnTriggerBufferLookup = state.GetBufferLookup<KnnTriggerBufferElement>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _knnTriggerBufferLookup.Update(ref state);
            //Ecb
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            new FsmSkillReleaseSystemJob
            {
                KnnTriggerBufferLookup = _knnTriggerBufferLookup,
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
        public partial struct FsmSkillReleaseSystemJob : IJobEntity
        {
            [ReadOnly] public BufferLookup<KnnTriggerBufferElement> KnnTriggerBufferLookup;
            public EntityCommandBuffer.ParallelWriter Ecb;
            public float DeltaTime;

            private void Execute([EntityIndexInQuery]int index,ref FsmStateComponent fsmStateComponent,ref SkillManagerComponent skillManagerComponent)
            {
                if (fsmStateComponent.Combat.ClearSkillTargetFlag)
                {
                    Ecb.SetBuffer<KnnTriggerBufferElement>(index,skillManagerComponent.Skill1.SkillEntity);
                }
                Entity skillEntity;
                if (fsmStateComponent.Combat.Skill1Attack)
                {
                    skillEntity = skillManagerComponent.Skill1.SkillEntity;
                    skillManagerComponent.Skill1.CurrentSkillCd = 0;
                    if (skillManagerComponent.Skill1.HolderSkill)
                    {
                        skillManagerComponent.Skill1.HolderSkillTimer += DeltaTime;
                        if (skillManagerComponent.Skill1.HolderSkillTimer >= skillManagerComponent.Skill1.HolderSkillInterval)
                        {
                            skillManagerComponent.Skill1.HolderSkillTimer = 0;
                        }
                        else
                        {
                            return;
                        }
                    }
                    else
                    {
                        fsmStateComponent.Combat.Skill1Attack = false;
                    }
                }
                else
                {
                    return;
                }
                if (fsmStateComponent.Combat.SkillAttackTarget != Entity.Null)
                {
                    var knnTriggerBufferElements = Ecb.SetBuffer<KnnTriggerBufferElement>(index,skillEntity);
                    if (KnnTriggerBufferLookup.TryGetBuffer(fsmStateComponent.Combat.SkillAttackTarget,out var buffer))
                    {
                        knnTriggerBufferElements.AddRange(buffer.AsNativeArray());
                    }
                }
            }
        }
    }
}