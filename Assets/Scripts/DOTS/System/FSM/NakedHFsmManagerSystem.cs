using System.Runtime.InteropServices;
using DOTS.Aspect;
using DOTS.BufferElement;
using DOTS.Component.Anim;
using DOTS.Component.Common;
using DOTS.Component.FSM;
using DOTS.Component.Role;
using DOTS.SystemGroup;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace DOTS.System.FSM
{
    [UpdateInGroup(typeof(FsmSystemGroup))]
    public partial struct NakedHFsmManagerSystem : ISystem
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
            var randomComponent = SystemAPI.GetSingletonRW<RandomComponent>();
            new NakedHFsmManagerSystemJob
            {
                AnimationNameComponent = SystemAPI.GetSingleton<AnimationNameComponent>(),
                Random = randomComponent.ValueRW.GetRandom()
            }.ScheduleParallel(state.Dependency).Complete();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        [BurstCompile]
        [StructLayout(LayoutKind.Auto)]
        public partial struct NakedHFsmManagerSystemJob : IJobEntity
        {
            public AnimationNameComponent AnimationNameComponent;
            public Random Random;
            private void Execute(ref NakedHFlagComponent nakedHFlagComponent,FsmCommonAspect fsmCommonAspect)
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
                    case EFsmStateName.Search:
                        //fsmStateComponent.Navigation.Layers = NavigationLayers.Layer1;
                        fsmCommonAspect.NormalSearch(in AnimationNameComponent,false);
                        break;
                    case EFsmStateName.MeleeAim:
                        var nextInt = Random.NextInt(0,7);
                        nakedHFlagComponent.IsMeleeCrit = false;
                        switch (nextInt)
                        {
                            case 0:
                            case 1:
                                fsmCommonAspect.NormalMeleeAim(AnimationNameComponent.Idle,AnimationNameComponent.Melee,fsmCommonAspect.GetScaledAnimationTime(0.4f));
                                break;
                            case 2:
                            case 3:
                                fsmCommonAspect.NormalMeleeAim(AnimationNameComponent.Idle,AnimationNameComponent.Melee2,fsmCommonAspect.GetScaledAnimationTime(0.5f));
                                break;
                            case 4:
                            case 5:
                                fsmCommonAspect.NormalMeleeAim(AnimationNameComponent.Idle,AnimationNameComponent.Melee3,fsmCommonAspect.GetScaledAnimationTime(0.4f));
                                break;
                            case 6:
                                fsmCommonAspect.NormalMeleeAim(AnimationNameComponent.Idle,AnimationNameComponent.MeleeCrit,fsmCommonAspect.GetScaledAnimationTime(0.6f));
                                nakedHFlagComponent.IsMeleeCrit = true;
                                break;
                        }
                        break;
                    case EFsmStateName.MeleeAttack:
                        var attackDamage = fsmCommonAspect.CombatComponent.ValueRO.AttackDamage;
                        FsmCombatDamage damage;
                        if (nakedHFlagComponent.IsMeleeCrit)
                        {
                            damage = new FsmCombatDamage
                            {
                                Damage = attackDamage * 2,
                                DamageType = EDamageType.Crit,
                                DamageCount = 1
                            };
                        }
                        else
                        {
                            damage = new FsmCombatDamage
                            {
                                Damage = attackDamage,
                                DamageType = EDamageType.Melee,
                                DamageCount = 1
                            };
                        }

                        fsmCommonAspect.TryAttackAimedTarget(damage);
                        fsmCommonAspect.SwitchState(EFsmStateName.Search,fsmCommonAspect.GetScaledAnimationTime(0.2f));
                        break;
                    case EFsmStateName.AfterDeath:
                        fsmCommonAspect.NormalAfterDeath(in AnimationNameComponent);
                        break;
                }
            }
        }
    }
}