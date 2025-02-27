using DOTS.Aspect;
using DOTS.BufferElement;
using DOTS.Component.Anim;
using DOTS.Component.FSM;
using DOTS.Component.Role;
using DOTS.SystemGroup;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace DOTS.System.FSM
{
    [UpdateInGroup(typeof(FsmSystemGroup))]
    public partial struct ForestFireFighterZFsmManagerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AnimationNameComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var entityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            var animationNameComponent = SystemAPI.GetSingleton<AnimationNameComponent>();
            new ForestFireFighterZFsmManagerSystemJob
            {
                Ecb = entityCommandBuffer.AsParallelWriter(),
                AnimationNameComponent = animationNameComponent,
            }.ScheduleParallel(state.Dependency).Complete();
            entityCommandBuffer.Playback(state.EntityManager);
            entityCommandBuffer.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        [BurstCompile]
        public partial struct ForestFireFighterZFsmManagerSystemJob : IJobEntity
        {
            public AnimationNameComponent AnimationNameComponent;
            public EntityCommandBuffer.ParallelWriter Ecb;

            private void Execute([EntityIndexInQuery]int index,in ForestFireFighterZFlagComponent forestFireFighterZFlagComponent,FsmCommonAspect fsmCommonAspect)
            {
                ref var fsmStateComponent = ref fsmCommonAspect.FsmStateComponent.ValueRW;
                if (!fsmCommonAspect.CombatComponent.ValueRO.IsDead)
                {
                    if (fsmStateComponent.WaitTime > 0)
                    {
                        return;
                    }
                    if (fsmCommonAspect.HasFsmEventFlag(EFsmEventName.EnemyInMeleeRange) && fsmStateComponent.CurFsmStateName == EFsmStateName.Search)
                    {
                        fsmStateComponent.CurFsmStateName = EFsmStateName.MeleeAim;
                    }
                }

                switch (fsmStateComponent.CurFsmStateName)
                {
                    case EFsmStateName.Search:
                        fsmCommonAspect.NormalSearch(AnimationNameComponent, false);
                        break;
                    case EFsmStateName.MeleeAim:
                        fsmCommonAspect.NormalMeleeAim(AnimationNameComponent.Idle, AnimationNameComponent.Melee,
                            fsmCommonAspect.GetScaledAnimationTime(0.3f));
                        break;
                    
                    case EFsmStateName.MeleeAttack:
                        var attackDamage = fsmCommonAspect.CombatComponent.ValueRO.AttackDamage;
                        var tryAttackAimedTarget = fsmCommonAspect.TryAttackAimedTarget(new FsmCombatDamage
                        {
                            Damage = attackDamage,
                            DamageType = EDamageType.None,
                            DamageCount = 1
                        });
                        if (!tryAttackAimedTarget)
                        {
                            fsmCommonAspect.SwitchState(EFsmStateName.Search, fsmCommonAspect.GetScaledAnimationTime(0.4f));
                            return;
                        }
                        var nearestAttackTarget = fsmCommonAspect.GetNearestAttackTarget();
                        var knnTriggerBufferElements = Ecb.SetBuffer<KnnTriggerBufferElement>(index,forestFireFighterZFlagComponent.Skill1Target);
                        knnTriggerBufferElements.Add(new KnnTriggerBufferElement
                        {
                            Entity = nearestAttackTarget
                        });
                        fsmStateComponent.Combat.ReleaseSkill1(forestFireFighterZFlagComponent.Skill1Target);
                        fsmCommonAspect.SwitchState(EFsmStateName.Search,fsmCommonAspect.GetScaledAnimationTime(0.4f));
                        break;
                }
            }
        }
    }
}