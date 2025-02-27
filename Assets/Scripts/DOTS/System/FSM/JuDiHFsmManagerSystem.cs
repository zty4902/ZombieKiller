using System.Runtime.InteropServices;
using DOTS.Aspect;
using DOTS.Component.Anim;
using DOTS.Component.Common;
using DOTS.Component.Role;
using DOTS.SystemGroup;
using JetBrains.Annotations;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace DOTS.System.FSM
{
    [UpdateInGroup(typeof(FsmSystemGroup))]
    public partial struct JuDiHFsmManagerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AnimationNameComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var randomComponent = SystemAPI.GetSingletonRW<RandomComponent>();
            new JuDiHFsmManagerSystemJob
            {
                AnimationNameComponent = SystemAPI.GetSingleton<AnimationNameComponent>(),
                Random = randomComponent.ValueRW.GetRandom(),
            }.ScheduleParallel(state.Dependency).Complete();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        [BurstCompile]
        [StructLayout(LayoutKind.Auto)]
        public partial struct JuDiHFsmManagerSystemJob : IJobEntity
        {
            public AnimationNameComponent AnimationNameComponent;
            public Random Random;
            [UsedImplicitly]
            private void Execute([EntityIndexInQuery]int index,ref JuDiHFlagComponent juDiHFlagComponent,FsmCommonAspect fsmCommonAspect)
            {
                ref var fsmStateComponent = ref fsmCommonAspect.FsmStateComponent.ValueRW;
                ref var combatComponent = ref fsmCommonAspect.CombatComponent.ValueRW;
                if (fsmStateComponent.WaitTime > 0 || fsmCommonAspect.CombatComponent.ValueRO.IsDead)
                {
                    return;
                }

                if (fsmCommonAspect.HasFsmEventFlag(EFsmEventName.EnemyInFireRange)
                    && fsmStateComponent.CurFsmStateName == EFsmStateName.Search)
                {
                    fsmStateComponent.CurFsmStateName = EFsmStateName.Aim;
                }
                
                if (fsmCommonAspect.HasFsmEventFlag(EFsmEventName.EnemyInMeleeRange)
                    && fsmStateComponent.CurFsmStateName is EFsmStateName.Search or EFsmStateName.Aim)
                {
                    if (fsmCommonAspect.HasFsmEventFlag(EFsmEventName.Skill1Ready))
                    {
                        fsmStateComponent.CurFsmStateName = EFsmStateName.MeleeAttack;
                    }
                }

                switch (fsmStateComponent.CurFsmStateName)
                {
                    case EFsmStateName.Search:
                        fsmCommonAspect.NormalSearch(in AnimationNameComponent,true);
                        break;
                    case EFsmStateName.Dodge:
                        combatComponent.Dodging = true;
                        fsmStateComponent.Animation.CurAnim = AnimationNameComponent.Dodge;
                        juDiHFlagComponent.LastDodgeDir = fsmCommonAspect.LocalTransform.ValueRO.Rotation.Equals(quaternion.identity) ? 1 : -1;
                        fsmCommonAspect.SwitchState(EFsmStateName.DodgeOver, fsmCommonAspect.GetScaledAnimationTime(0.6f));
                        break;
                    case EFsmStateName.DodgeOver:
                        //瞬移
                        combatComponent.Dodging = false;
                        fsmStateComponent.Animation.CurAnim = AnimationNameComponent.Idle;
                        fsmStateComponent.FlashMove.MoveOffset = new float3(-0.68f * juDiHFlagComponent.LastDodgeDir,0,0);
                        fsmCommonAspect.SwitchState(EFsmStateName.Search,0.4f);
                        break;
                    case EFsmStateName.Aim:
                        fsmCommonAspect.NormalAim(in AnimationNameComponent,fsmCommonAspect.GetScaledAnimationTime(0.5f));
                        break;
                    case EFsmStateName.FireAttack:
                        fsmCommonAspect.NormalFireAttack(in AnimationNameComponent,combatComponent.AttackDamage);
                        fsmCommonAspect.SwitchState(EFsmStateName.Search,fsmCommonAspect.GetScaledAnimationTime(0.5f));
                        break;
                    case EFsmStateName.MeleeAttack:
                        var dodge = Random.NextBool();
                        if (dodge)
                        {
                            fsmCommonAspect.SwitchState(EFsmStateName.Dodge,0.5f);
                        }
                        else
                        {
                            fsmStateComponent.Animation.CurAnim = AnimationNameComponent.Melee;
                            /*fsmStateComponent.Combat.Skill1Attack = true;
                            fsmStateComponent.Combat.SkillAttackTarget = combatComponent.MeleeTrigger;*/
                            fsmStateComponent.Combat.ReleaseSkill1(combatComponent.MeleeTrigger);
                            fsmCommonAspect.SwitchState(EFsmStateName.Search,fsmCommonAspect.GetScaledAnimationTime(1));
                        }
                        break;
                    case EFsmStateName.Reload:
                        fsmCommonAspect.NormalReload(in AnimationNameComponent);
                        fsmCommonAspect.SwitchState(EFsmStateName.Search, fsmCommonAspect.GetScaledAnimationTime(1.1f));
                        break;
                    case EFsmStateName.AfterDeath:
                        fsmCommonAspect.NormalAfterDeath(in AnimationNameComponent);
                        break;
                }
            }
        }
    }
}