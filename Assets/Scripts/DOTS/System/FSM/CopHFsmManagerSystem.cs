using DOTS.Aspect;
using DOTS.BufferElement;
using DOTS.Component.Anim;
using DOTS.Component.FSM;
using DOTS.Component.Role;
using DOTS.SystemGroup;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace DOTS.System.FSM
{
    [UpdateInGroup(typeof(FsmSystemGroup))]
    public partial struct CopHFsmManagerSystem : ISystem
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
            new CopHFsmManagerSystemJob
            {
                AnimationNameComponent = animationNameComponent
            }.ScheduleParallel(state.Dependency).Complete();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        [BurstCompile]
        public partial struct CopHFsmManagerSystemJob : IJobEntity
        {
            public AnimationNameComponent AnimationNameComponent;
            private void Execute(CopHFlagComponent _,FsmCommonAspect fsmCommonAspect)
            {
                ref var fsmStateComponent = ref fsmCommonAspect.FsmStateComponent.ValueRW;
                if (!fsmCommonAspect.CombatComponent.ValueRO.IsDead)
                {
                    if (fsmStateComponent.WaitTime > 0)
                    {
                        return;
                    }
                    if (fsmStateComponent.CurFsmStateName == EFsmStateName.Search && fsmCommonAspect.HasFsmEventFlag(EFsmEventName.EnemyInFireRange))
                    {
                        fsmStateComponent.CurFsmStateName = EFsmStateName.Aim;
                    }
                }

                switch (fsmStateComponent.CurFsmStateName)
                {
                    case EFsmStateName.Search:
                        fsmCommonAspect.NormalSearch(AnimationNameComponent, true);
                        break;
                    case EFsmStateName.Aim:
                        fsmCommonAspect.NormalAim(AnimationNameComponent, 0.4f);
                        break;
                    case EFsmStateName.FireAttack:
                        fsmCommonAspect.NormalFireAttack(AnimationNameComponent,new FsmCombatDamage
                        {
                            DamageType = EDamageType.Gun,
                            Damage = fsmCommonAspect.CombatComponent.ValueRO.AttackDamage,
                            DamageCount = 5
                        });
                        fsmCommonAspect.SwitchState(EFsmStateName.Search, fsmCommonAspect.GetScaledAnimationTime(0.8f));
                        break;
                    case EFsmStateName.Reload:
                        var position = fsmStateComponent.Combat.NearestEnemyTransform.Position;
                        var selfPosition = fsmCommonAspect.LocalTransform.ValueRO.Position;
                        if (math.distance(position, selfPosition) > 1)
                        {
                            fsmCommonAspect.CombatComponent.ValueRW.CurFireCount = 0;
                            fsmStateComponent.Animation.CurAnim = AnimationNameComponent.Reload2;
                            var reloadTime = fsmCommonAspect.GetScaledAnimationTime(0.9f*2);
                            fsmCommonAspect.SwitchState(EFsmStateName.Search, fsmCommonAspect.GetScaledAnimationTime(reloadTime));
                        }
                        else
                        {
                            fsmCommonAspect.CombatComponent.ValueRW.CurFireCount = fsmCommonAspect.CombatComponent.ValueRO.MaxFireCount - 1;
                            fsmStateComponent.Animation.CurAnim = AnimationNameComponent.Reload;
                            fsmCommonAspect.SwitchState(EFsmStateName.Search, fsmCommonAspect.GetScaledAnimationTime(1.2f));
                        }
                        break;
                    case EFsmStateName.AfterDeath:
                        fsmCommonAspect.NormalAfterDeath(AnimationNameComponent);
                        break;
                }
            }
        }
    }
}