using DOTS.Aspect;
using DOTS.BufferElement;
using DOTS.Component.Anim;
using DOTS.Component.Common;
using DOTS.Component.FSM;
using DOTS.Component.Role;
using DOTS.SystemGroup;
using ProjectDawn.Navigation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace DOTS.System.FSM
{
    [UpdateInGroup(typeof(FsmSystemGroup))]
    public partial struct AbbyHFsmManagerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AnimationNameComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var animationNameComponent = SystemAPI.GetSingleton<AnimationNameComponent>();
            var entityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            new AbbyHFsmManagerSystemJob
            {
                AnimationNameComponent = animationNameComponent,
                Ecb = entityCommandBuffer.AsParallelWriter(),
            }.ScheduleParallel(state.Dependency).Complete();
            entityCommandBuffer.Playback(state.EntityManager);
            entityCommandBuffer.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        [BurstCompile]
        public partial struct AbbyHFsmManagerSystemJob : IJobEntity
        {
            public AnimationNameComponent AnimationNameComponent;
            public EntityCommandBuffer.ParallelWriter Ecb;
            private void Execute([EntityIndexInQuery]int index,ref AbbyHFlagComponent abbyHFlagComponent,FsmCommonAspect fsmCommonAspect
                ,in LocalTransform localTransform,ref AgentLocomotion agentLocomotion)
            {
                ref var fsmStateComponent = ref fsmCommonAspect.FsmStateComponent.ValueRW;
                if (fsmCommonAspect.CombatComponent.ValueRO.IsDead)
                {
                    if (fsmStateComponent.CurFsmStateName == EFsmStateName.Destroy)
                    {
                        return;
                    }

                    if (fsmStateComponent.CurFsmStateName == EFsmStateName.AfterDeath)
                    {
                        if (fsmStateComponent.WaitTime > 0)
                        {
                            return;
                        }
                    }
                    else
                    {
                        fsmStateComponent.CurFsmStateName = EFsmStateName.Dying;
                    }
                }
                else
                {
                    if (fsmStateComponent.WaitTime > 0)
                    {
                        return;
                    }

                    if (fsmStateComponent.CurFsmStateName == EFsmStateName.Search)
                    {
                        if (!abbyHFlagComponent.IsDisarmament)
                        {
                            if (fsmCommonAspect.HasFsmEventFlag(EFsmEventName.EnemyInFireRange))
                            {
                                fsmStateComponent.CurFsmStateName = EFsmStateName.Aim;
                            }
                        }else if (fsmCommonAspect.HasFsmEventFlag(EFsmEventName.EnemyInMeleeRange))
                        {
                            fsmStateComponent.CurFsmStateName = EFsmStateName.MeleeAim;
                        }
                    }
                }
                var animationNameComponent = AnimationNameComponent;
                if (abbyHFlagComponent.IsDisarmament)
                {
                    animationNameComponent.Move = AnimationNameComponent.Move2;
                    animationNameComponent.Idle = AnimationNameComponent.Idle2;
                    animationNameComponent.Death = AnimationNameComponent.Death2;
                    animationNameComponent.AfterDeath = AnimationNameComponent.AfterDeath2;
                }

                switch (fsmStateComponent.CurFsmStateName)
                {
                    case EFsmStateName.Create:
                        Ecb.SetComponentEnabled<KnnTriggerComponent>(index,abbyHFlagComponent.FireTriggerEntity,false);
                        break;
                    case EFsmStateName.Search:
                        fsmCommonAspect.NormalSearch(animationNameComponent, !abbyHFlagComponent.IsDisarmament);
                        break;
                    case EFsmStateName.Aim:
                        fsmCommonAspect.NormalAim(animationNameComponent,fsmCommonAspect.GetScaledAnimationTime(0.3f));
                        break;
                    case EFsmStateName.Reload:
                        fsmStateComponent.Combat.StopSkill1();
                        abbyHFlagComponent.CurReloadCount++;
                        if (abbyHFlagComponent.CurReloadCount >= abbyHFlagComponent.MaxReloadCount)
                        {
                            fsmCommonAspect.SwitchState(EFsmStateName.Disarmament,0);
                        }
                        else
                        {
                            fsmCommonAspect.NormalReload(animationNameComponent);
                            fsmCommonAspect.SwitchState(EFsmStateName.Search,fsmCommonAspect.GetScaledAnimationTime(1.8f));
                        }
                        break;
                    case EFsmStateName.FireAttack:
                        fsmStateComponent.Animation.CurAnim = animationNameComponent.Fire;
                        Ecb.SetComponentEnabled<KnnTriggerComponent>(index,abbyHFlagComponent.FireTriggerEntity,true);
                        fsmStateComponent.Combat.ReleaseSkill1(abbyHFlagComponent.FireTriggerEntity);
                        fsmCommonAspect.SwitchState(EFsmStateName.Reload,abbyHFlagComponent.FireDuration);
                        break;
                    case EFsmStateName.Disarmament:
                        abbyHFlagComponent.IsDisarmament = true;
                        fsmStateComponent.Animation.CurAnim = animationNameComponent.Disarmament;
                        fsmCommonAspect.SwitchState(EFsmStateName.DisarmamentOver,fsmCommonAspect.GetScaledAnimationTime(9f * 0.15f));
                        break;
                    case EFsmStateName.DisarmamentOver:
                        var gunInstanceEntity = Ecb.Instantiate(index,abbyHFlagComponent.GunPrefabEntity);
                        Ecb.SetComponent(index,gunInstanceEntity,localTransform);
                        fsmCommonAspect.SwitchState(EFsmStateName.Search,0);
                        Ecb.SetComponentEnabled<KnnTriggerComponent>(index,abbyHFlagComponent.FireTriggerEntity,false);
                        agentLocomotion.Speed *= 3;
                        break;
                    case EFsmStateName.MeleeAim:
                        fsmCommonAspect.NormalMeleeAim(animationNameComponent.Idle,animationNameComponent.Melee,fsmCommonAspect.GetScaledAnimationTime(0.6f));
                        break;
                    case EFsmStateName.MeleeAttack:
                        fsmCommonAspect.TryAttackAimedTarget(new FsmCombatDamage
                        {
                            DamageType = EDamageType.Melee,
                            Damage = fsmCommonAspect.CombatComponent.ValueRO.AttackDamage,
                            DamageCount = 1
                        });
                        fsmCommonAspect.SwitchState(EFsmStateName.Search,fsmCommonAspect.GetScaledAnimationTime(0.2f));
                        break;
                    case EFsmStateName.Dying:
                        fsmCommonAspect.NormalDying(in animationNameComponent);
                        break;
                    case EFsmStateName.AfterDeath:
                        fsmCommonAspect.NormalAfterDeath(in animationNameComponent);
                        break;
                }
            }
        }
    }
}