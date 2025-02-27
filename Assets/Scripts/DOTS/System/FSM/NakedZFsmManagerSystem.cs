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
    public partial struct NakedZFsmManagerSystem : ISystem
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
            new NakedZFsmManagerSystemJob
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
        public partial struct NakedZFsmManagerSystemJob : IJobEntity
        {
            public AnimationNameComponent AnimationNameComponent;
            public Random Random;

            private void Execute(in NakedZFlagComponent _,FsmCommonAspect fsmCommonAspect)
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
                        fsmCommonAspect.SwitchState(EFsmStateName.MeleeAim,0.5f);
                        return;
                    }
                }

                switch (fsmStateComponent.CurFsmStateName)
                {
                    case EFsmStateName.Search:
                        var enterIdle = Random.NextInt(0,10);
                        if (enterIdle >= 9)
                        {
                            fsmCommonAspect.SwitchState(EFsmStateName.Idle,0);
                        }
                        else
                        {
                            fsmCommonAspect.NormalSearch(in AnimationNameComponent,false);
                        }
                        break;
                    case EFsmStateName.MeleeAim:
                        fsmCommonAspect.NormalMeleeAim(AnimationNameComponent.Idle,AnimationNameComponent.Melee,0.4f);
                        break;
                    case EFsmStateName.MeleeAttack:
                        var attackDamage = fsmCommonAspect.CombatComponent.ValueRO.AttackDamage;
                        fsmCommonAspect.TryAttackAimedTarget(new FsmCombatDamage
                        {
                            Damage = attackDamage,
                            DamageType = EDamageType.None,
                            DamageCount = 1
                        });
                        fsmCommonAspect.SwitchState(EFsmStateName.Search,fsmCommonAspect.GetScaledAnimationTime(0.2f));
                        break;
                    case EFsmStateName.Idle:
                        var nextInt = Random.NextInt(0,10);
                        if (nextInt >= 8)
                        {
                            fsmStateComponent.Animation.CurAnim = AnimationNameComponent.Idle2;
                            fsmCommonAspect.SwitchState(EFsmStateName.Idle,0.9f);
                        }
                        else
                        {
                            fsmStateComponent.Animation.CurAnim = AnimationNameComponent.Idle;
                            var nextFloat = Random.NextFloat(1,3);
                            fsmCommonAspect.SwitchState(EFsmStateName.Search,nextFloat);
                        }
                        break;
                }
            }
        }
    }
}