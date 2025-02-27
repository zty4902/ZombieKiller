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
using Unity.Mathematics;

namespace DOTS.System.FSM
{
    [UpdateInGroup(typeof(FsmSystemGroup))]
    public partial struct MechanicHFsmManagerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AnimationNameComponent>();
            state.RequireForUpdate<RandomComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var animationNameComponent = SystemAPI.GetSingleton<AnimationNameComponent>();
            var randomComponent = SystemAPI.GetSingletonRW<RandomComponent>();
            var entityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            new MechanicHFsmManagerSystemJob
            {
                AnimationNameComponent = animationNameComponent,
                Random = randomComponent.ValueRW.GetRandom(),
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
        public partial struct MechanicHFsmManagerSystemJob : IJobEntity
        {
            public AnimationNameComponent AnimationNameComponent;
            public Random Random;
            public EntityCommandBuffer.ParallelWriter Ecb;

            private void Execute([EntityIndexInQuery]int index,ref MechanicHFlagComponent mechanicHFlagComponent,FsmCommonAspect fsmCommonAspect,ref AgentLocomotion agentLocomotion)
            {
                ref var fsmStateComponent = ref fsmCommonAspect.FsmStateComponent.ValueRW;
                if (fsmStateComponent.WaitTime > 0 || fsmCommonAspect.CombatComponent.ValueRO.IsDead)
                {
                    return;
                }

                if (fsmCommonAspect.HasFsmEventFlag(EFsmEventName.EnemyInMeleeRange) && fsmStateComponent.CurFsmStateName == EFsmStateName.Search)
                {
                    fsmStateComponent.CurFsmStateName = EFsmStateName.MeleeAim;
                }
                switch (fsmStateComponent.CurFsmStateName)
                {
                    case EFsmStateName.Create:
                        agentLocomotion.Speed *= 3f;
                        break;
                    case EFsmStateName.Search:
                        var normalSearch = fsmCommonAspect.NormalSearch(in AnimationNameComponent,false);
                        if (normalSearch && mechanicHFlagComponent.FirstCharge)
                        {
                            fsmStateComponent.Animation.CurAnim = AnimationNameComponent.MoveS;
                        }
                        break;
                    case EFsmStateName.MeleeAim:
                        if (mechanicHFlagComponent.FirstCharge)
                        {
                            fsmCommonAspect.NormalMeleeAim(AnimationNameComponent.Idle,AnimationNameComponent.MeleeS,0.2f);
                        }
                        else
                        {
                            var nextFloat = Random.NextFloat();
                            mechanicHFlagComponent.CurrentCritical = nextFloat <= mechanicHFlagComponent.CriticalRate;
                            fsmCommonAspect.NormalMeleeAim(AnimationNameComponent.Idle,
                                mechanicHFlagComponent.CurrentCritical ? AnimationNameComponent.Melee2 : AnimationNameComponent.Melee,0.4f);
                        }
                        break;
                    case EFsmStateName.MeleeAttack:
                        var currentCritical = mechanicHFlagComponent.FirstCharge || mechanicHFlagComponent.CurrentCritical;
                        var baseDamage = fsmCommonAspect.CombatComponent.ValueRO.AttackDamage;
                        if (currentCritical)
                        {
                            baseDamage *= 2;
                        }
                        if (mechanicHFlagComponent.FirstCharge)//首次3倍伤害加暴击
                        {
                            baseDamage *= 3;
                        }
                        fsmCommonAspect.TryAttackAimedTarget(new FsmCombatDamage
                        {
                            DamageType = mechanicHFlagComponent.FirstCharge ? EDamageType.Smash : EDamageType.Melee,
                            Damage = baseDamage,
                            DamageCount = 1
                        });
                        if (currentCritical)
                        {
                            fsmStateComponent.Combat.ReleaseSkill1(fsmCommonAspect.CombatComponent.ValueRO.MeleeTrigger);
                        }
                        if (mechanicHFlagComponent.FirstCharge)
                        {
                            mechanicHFlagComponent.FirstCharge = false;
                            agentLocomotion.Speed /= 3f;
                            var instantiate = Ecb.Instantiate(index,mechanicHFlagComponent.MeleeSWeaponEntity);
                            Ecb.SetComponent(index,instantiate,fsmCommonAspect.LocalTransform.ValueRO);
                        }
                        fsmCommonAspect.SwitchState(EFsmStateName.Search,fsmCommonAspect.GetScaledAnimationTime(0.2f));
                        break;
                }
            }
        }
    }
}