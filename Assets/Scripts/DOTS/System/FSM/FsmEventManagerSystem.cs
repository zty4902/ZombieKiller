using System.Runtime.InteropServices;
using DOTS.BufferElement;
using DOTS.Component.Combat;
using DOTS.Component.Combat.Skill;
using DOTS.Component.Common;
using DOTS.Component.FSM;
using DOTS.SystemGroup;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DOTS.System.FSM
{
    [UpdateInGroup(typeof(FsmSystemGroup),OrderFirst = true)]
    [StructLayout(LayoutKind.Auto)]
    public partial struct FsmEventManagerSystem : ISystem
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
            new FsmDyingEventHandleJob().ScheduleParallel(state.Dependency).Complete();
            _fsmStateLookup.Update(ref state);
            //Ecb
            var entityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            new FsmMeleeEnemyEventHandleJob
            {
                FsmStateLookup = _fsmStateLookup,
                Ecb = entityCommandBuffer.AsParallelWriter()
            }
           .ScheduleParallel(state.Dependency).Complete();
            entityCommandBuffer.Playback(state.EntityManager);
            entityCommandBuffer.Dispose();
            
            new FsmEnemyInFireRangeEventHandleJob().ScheduleParallel(state.Dependency).Complete();
            new FsmSkillReadyEventHandleJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel(state.Dependency).Complete();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        [BurstCompile]
        public partial struct FsmDyingEventHandleJob : IJobEntity
        {
            private void Execute(ref FsmStateComponent fsmStateComponent,in CombatComponent combatComponent)
            {
                if (combatComponent.IsDead)
                {
                    fsmStateComponent.CurFsmEvent |= EFsmEventName.Dying;
                }
            }
        }

        [BurstCompile]
        [StructLayout(LayoutKind.Auto)]
        public partial struct FsmMeleeEnemyEventHandleJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<FsmStateComponent> FsmStateLookup;
            public EntityCommandBuffer.ParallelWriter Ecb;

            private void Execute([EntityIndexInQuery]int index,in KnnTriggerComponent knnTriggerComponent,ref DynamicBuffer<KnnTriggerBufferElement> knnTriggerBuffer)
            {
                if (knnTriggerBuffer.Length == 0)
                {
                    return;
                }
                if (FsmStateLookup.TryGetComponent(knnTriggerComponent.Owner, out var fsmStateComponent))
                {
                    fsmStateComponent.CurFsmEvent |= knnTriggerComponent.TriggerUpdateEventName;
                    Ecb.SetComponent(index,knnTriggerComponent.Owner,fsmStateComponent);
                }
            }
        }

        [BurstCompile]
        public partial struct FsmEnemyInFireRangeEventHandleJob : IJobEntity
        {
            private void Execute(ref FsmStateComponent fsmStateComponent,in LocalTransform localTransform,in CombatComponent combatComponent)
            {
                if (fsmStateComponent.Combat.NearestEnemyCombat.IsDead)
                {
                    return;
                }
                var nearestEnemyPos = fsmStateComponent.Combat.NearestEnemyTransform.Position;
                var distance = math.sqrt(math.pow(localTransform.Position.x - nearestEnemyPos.x, 2) +
                                         math.pow((localTransform.Position.y - nearestEnemyPos.y) * 2, 2));
                if (math.distance(localTransform.Position,fsmStateComponent.Navigation.Destination) < 0.2f
                    && distance <= combatComponent.FireRange)//到达攻击点
                {
                    fsmStateComponent.CurFsmEvent |= EFsmEventName.EnemyInFireRange;
                }
            }
        }

        [BurstCompile]
        public partial struct FsmSkillReadyEventHandleJob : IJobEntity
        {
            public float DeltaTime;
            private void Execute(ref SkillManagerComponent skillManagerComponent,ref FsmStateComponent fsmStateComponent)
            {
                skillManagerComponent.Skill1.CurrentSkillCd += DeltaTime;

                if (skillManagerComponent.Skill1.CurrentSkillCd >= skillManagerComponent.Skill1.SkillCd)
                {
                    fsmStateComponent.CurFsmEvent |= EFsmEventName.Skill1Ready;
                }
            }
        }
    }
}