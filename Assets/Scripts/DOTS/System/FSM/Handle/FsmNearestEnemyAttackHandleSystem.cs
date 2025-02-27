using System.Runtime.InteropServices;
using DOTS.BufferElement;
using DOTS.Component.Combat;
using DOTS.Component.FSM;
using DOTS.SystemGroup;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace DOTS.System.FSM.Handle
{
    [UpdateInGroup(typeof(FsmHandlerSystemGroup))]
    public partial struct FsmNearestEnemyAttackHandleSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var entityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            new FsmNearestEnemyAttackHandleSystemJob()
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
        [StructLayout(LayoutKind.Auto)]
        public partial struct FsmNearestEnemyAttackHandleSystemJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;

            private void Execute([EntityIndexInChunk]int index,ref FsmStateComponent fsmStateComponent,in CombatComponent combatComponent)
            {
                if (fsmStateComponent.Combat.AimedEnemyAttack)
                {
                    fsmStateComponent.Combat.AimedEnemyAttack = false;
                    if (!fsmStateComponent.Combat.NearestEnemyCombat.IsDead && fsmStateComponent.Combat.NearestEnemy == fsmStateComponent.Combat.CurAimedTarget)
                    {
                        var combatCurDamage = fsmStateComponent.Combat.CurDamage;
                        for (var i = 0; i < combatCurDamage.DamageCount; i++)
                        {
                            Ecb.AppendToBuffer(index,fsmStateComponent.Combat.CurAimedTarget,new CombatDamageBufferElement
                            {
                                DamageType = combatCurDamage.DamageType,
                                Damage = combatCurDamage.Damage
                            });
                        }
                    }
                }
            }
        }
    }
}