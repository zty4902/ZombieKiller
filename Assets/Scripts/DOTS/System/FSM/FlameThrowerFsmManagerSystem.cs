using DOTS.Aspect;
using DOTS.BufferElement;
using DOTS.Component.Anim;
using DOTS.Component.Common;
using DOTS.Component.FSM;
using DOTS.Component.Role;
using DOTS.SystemGroup;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace DOTS.System.FSM
{
    [UpdateInGroup(typeof(FsmSystemGroup))]
    public partial struct FlameThrowerFsmManagerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PrefabManagerComponent>();
            state.RequireForUpdate<AnimationNameComponent>();
            state.RequireForUpdate<RandomComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var animationNameComponent = SystemAPI.GetSingleton<AnimationNameComponent>();
            var prefabManagerComponent = SystemAPI.GetSingleton<PrefabManagerComponent>();
            var entityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            var randomComponent = SystemAPI.GetSingletonRW<RandomComponent>();
            new FlameThrowerFsmManagerSystemJob
            {
                AnimationNameComponent = animationNameComponent,
                DeltaTime = SystemAPI.Time.DeltaTime,
                Ecb = entityCommandBuffer.AsParallelWriter(),
                SmallExplosionEntity = prefabManagerComponent.SmallExplosion,
                Random = randomComponent.ValueRW.GetRandom(),
                XmasFireEntity = prefabManagerComponent.XmasFire
            }.ScheduleParallel(state.Dependency).Complete();
            entityCommandBuffer.Playback(state.EntityManager);
            entityCommandBuffer.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        [BurstCompile]
        public partial struct FlameThrowerFsmManagerSystemJob : IJobEntity
        {
            public AnimationNameComponent AnimationNameComponent;
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter Ecb;
            public Entity SmallExplosionEntity;
            public Random Random;
            public Entity XmasFireEntity;

            private void Execute([EntityIndexInQuery]int index,ref FlameThrowerHFlagComponent flameThrowerHFlagComponent,FsmCommonAspect fsmCommonAspect)
            {
                ref var fsmStateComponent = ref fsmCommonAspect.FsmStateComponent.ValueRW;

                if (fsmCommonAspect.CombatComponent.ValueRO.IsDead)
                {
                    if (fsmStateComponent.CurFsmStateName is EFsmStateName.AfterDeath or EFsmStateName.Destroy)
                    {
                        return;
                    }
                    fsmStateComponent.CurFsmStateName = EFsmStateName.Dying;//优先Fallback触发Dying，随后FallBack会切换到AfterDeath
                }
                else
                {
                    flameThrowerHFlagComponent.Skill1Timer += DeltaTime;
                    if (flameThrowerHFlagComponent.Skill1Timer >= flameThrowerHFlagComponent.Skill1Duration && fsmStateComponent.CurFsmStateName == EFsmStateName.FireAttack)
                    {
                        fsmStateComponent.Combat.StopSkill1();
                        fsmCommonAspect.SwitchState(EFsmStateName.Search,0);
                    }
                    if (fsmStateComponent.WaitTime > 0)
                    {
                        return;
                    }
                    if (fsmCommonAspect.HasFsmEventFlag(EFsmEventName.EnemyInSkill1Range) && fsmStateComponent.CurFsmStateName == EFsmStateName.Search)
                    {
                        if (fsmCommonAspect.HasFsmEventFlag(EFsmEventName.Skill1Ready))
                            fsmStateComponent.CurFsmStateName = EFsmStateName.Reload;
                        else
                            fsmStateComponent.CurFsmStateName = EFsmStateName.Idle;
                    }
                    if (fsmCommonAspect.HasFsmEventFlag(EFsmEventName.EnemyInMeleeRange) 
                        && (fsmStateComponent.CurFsmStateName == EFsmStateName.Search || fsmStateComponent.CurFsmStateName == EFsmStateName.Idle))
                    {
                        fsmStateComponent.CurFsmStateName = EFsmStateName.MeleeAim;
                    }
                }

                switch (fsmStateComponent.CurFsmStateName)
                {
                    case EFsmStateName.Search:
                        fsmCommonAspect.NormalSearch(in AnimationNameComponent,true);
                        break;
                    case EFsmStateName.MeleeAim:
                        fsmCommonAspect.NormalMeleeAim(AnimationNameComponent.Idle,AnimationNameComponent.Melee,fsmCommonAspect.GetScaledAnimationTime(0.4f));
                        break;
                    case EFsmStateName.MeleeAttack:
                        fsmCommonAspect.TryAttackAimedTarget(new FsmCombatDamage
                        {
                            Damage = fsmCommonAspect.CombatComponent.ValueRO.AttackDamage,
                            DamageType = EDamageType.Melee,
                            DamageCount = 1
                        });
                        fsmCommonAspect.SwitchState(EFsmStateName.Search,fsmCommonAspect.GetScaledAnimationTime(0.2f));
                        break;
                    case EFsmStateName.Reload:
                        fsmStateComponent.Animation.CurAnim = AnimationNameComponent.Reload;
                        fsmCommonAspect.SwitchState(EFsmStateName.Skill1,fsmCommonAspect.GetScaledAnimationTime(1.44f));
                        break;
                    case EFsmStateName.Skill1:
                        fsmStateComponent.Animation.CurAnim = AnimationNameComponent.Fire;
                        flameThrowerHFlagComponent.Skill1Timer = 0;
                        fsmStateComponent.Combat.ReleaseSkill1(flameThrowerHFlagComponent.Skill1TargetEntity);
                        fsmCommonAspect.SwitchState(EFsmStateName.FireAttack,0);
                        break;
                    case EFsmStateName.FireAttack:
                        //等待技能结束
                        if (!fsmCommonAspect.HasFsmEventFlag(EFsmEventName.EnemyInSkill1Range))//敌人死亡
                        {
                            fsmStateComponent.Combat.StopSkill1();
                            fsmCommonAspect.SwitchState(EFsmStateName.Search,0);
                        }
                        break;
                    case EFsmStateName.Idle:
                        fsmStateComponent.Animation.CurAnim = AnimationNameComponent.Idle;
                        fsmCommonAspect.SwitchState(EFsmStateName.Search,0.5f);
                        break;
                    case EFsmStateName.Dying:
                        fsmStateComponent.Combat.StopSkill1();

                        var instantiate = Ecb.Instantiate(index, SmallExplosionEntity);
                        Ecb.SetComponent(index,instantiate,fsmCommonAspect.LocalTransform.ValueRO);

                        for (var i = 0; i < 14; i++)
                        {
                            var nextFloat2 = Random.NextFloat2(-0.3f,0.3f);
                            var localTransformValueRO = fsmCommonAspect.LocalTransform.ValueRO;
                            localTransformValueRO.Position += new float3(nextFloat2.x,nextFloat2.y,0);

                            var entity = Ecb.Instantiate(index, XmasFireEntity);
                            Ecb.SetComponent(index,entity, localTransformValueRO);
                        }
                        break;
                }
            }
        }
    }
}