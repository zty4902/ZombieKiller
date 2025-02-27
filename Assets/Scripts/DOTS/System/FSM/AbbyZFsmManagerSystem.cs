using System.Runtime.InteropServices;
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
using Unity.Transforms;

namespace DOTS.System.FSM
{
    [UpdateInGroup(typeof(FsmSystemGroup))]
    public partial struct AbbyZFsmManagerSystem : ISystem
    {
        private BufferLookup<KnnTriggerBufferElement> _knnTriggerBufferLookup;
        private ComponentLookup<LocalTransform> _localTransformLookup;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AnimationNameComponent>();
            _knnTriggerBufferLookup = state.GetBufferLookup<KnnTriggerBufferElement>();
            _localTransformLookup = state.GetComponentLookup<LocalTransform>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var entityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            _knnTriggerBufferLookup.Update(ref state);
            _localTransformLookup.Update(ref state);
            var random = SystemAPI.GetSingletonRW<RandomComponent>().ValueRW.GetRandom();
            new AbbyZFsmManagerSystemJob
            {
                AnimationNameComponent = SystemAPI.GetSingleton<AnimationNameComponent>(),
                Ecb = entityCommandBuffer.AsParallelWriter(),
                KnnTriggerBufferLookup = _knnTriggerBufferLookup,
                Random = random,
                LocalTransformLookup = _localTransformLookup
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
        public partial struct AbbyZFsmManagerSystemJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public AnimationNameComponent AnimationNameComponent;
            [ReadOnly]
            public BufferLookup<KnnTriggerBufferElement> KnnTriggerBufferLookup;
            [ReadOnly]
            public ComponentLookup<LocalTransform> LocalTransformLookup;
            public Random Random;

            private void Execute([EntityIndexInQuery]int index,Entity entity,FsmCommonAspect fsmCommonAspect
                ,ref AbbyZFlagComponent abbyZFlagComponent)
            {
                ref var fsmStateComponent = ref fsmCommonAspect.FsmStateComponent.ValueRW;
                if (fsmStateComponent.WaitTime > 0 || fsmCommonAspect.CombatComponent.ValueRO.IsDead)
                {
                    return;
                }
                if (fsmCommonAspect.HasFsmEventFlag(EFsmEventName.Skill1Ready) && fsmStateComponent.CurFsmStateName == EFsmStateName.Search)
                {
                    if (fsmCommonAspect.HasFsmEventFlag(EFsmEventName.EnemyInSkill1Range))
                    {
                        fsmStateComponent.CurFsmStateName = EFsmStateName.Dodge;
                    }
                    else
                    {
                        Ecb.SetComponentEnabled<KnnTriggerComponent>(index,abbyZFlagComponent.SpecialTriggerEntity, true);
                    }
                }
                if (fsmCommonAspect.HasFsmEventFlag(EFsmEventName.EnemyInMeleeRange) && fsmStateComponent.CurFsmStateName == EFsmStateName.Search)
                {
                    fsmStateComponent.CurFsmStateName = EFsmStateName.MeleeAim;
                }
                
                switch (fsmStateComponent.CurFsmStateName)
                {
                    case EFsmStateName.Create:
                        Ecb.SetComponentEnabled<KnnTriggerComponent>(index,abbyZFlagComponent.SpecialTriggerEntity, false);
                        break;
                    case EFsmStateName.Search:
                        fsmCommonAspect.NormalSearch(in AnimationNameComponent,false);
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
                    case EFsmStateName.Dodge://跃起
                        fsmCommonAspect.CombatComponent.ValueRW.Dodging = true;
                        Ecb.SetComponentEnabled<KnnTriggerComponent>(index,abbyZFlagComponent.SpecialTriggerEntity, false);
                        if (KnnTriggerBufferLookup.TryGetBuffer(abbyZFlagComponent.SpecialTriggerEntity,out var buffer))
                        {
                            var randomIndex = Random.NextInt(0,buffer.Length);
                            var randomTarget = buffer[randomIndex].Entity;
                            abbyZFlagComponent.CurrentTargetEntity = randomTarget;
                            fsmStateComponent.Animation.CurAnim = AnimationNameComponent.Special;
                            Ecb.SetBuffer<KnnTriggerBufferElement>(index,abbyZFlagComponent.SpecialTriggerEntity);
                            fsmCommonAspect.SwitchState(EFsmStateName.DodgeOver,fsmCommonAspect.GetScaledAnimationTime(0.7f));
                            
                            //fsmStateComponent.Combat.ReleaseSkill1(Entity.Null);
                            /*fsmStateComponent.Combat.Skill1Attack = true;
                            fsmStateComponent.Combat.SkillAttackTarget = Entity.Null;*/
                        }
                        else
                        {
                            fsmCommonAspect.SwitchState(EFsmStateName.Search,0.2f);
                        }

                        break;
                    case EFsmStateName.DodgeOver://跃起结束
                        fsmCommonAspect.CombatComponent.ValueRW.Dodging = false;
                        if (LocalTransformLookup.TryGetComponent(abbyZFlagComponent.CurrentTargetEntity,out var localTransform))
                        {
                            //fsmCommonAspect.LocalTransform.ValueRW.Position = localTransform.Position;
                            var selfTransform = fsmCommonAspect.LocalTransform.ValueRO;
                            Ecb.SetComponent(index,entity,selfTransform.WithPosition(localTransform.Position));
                        }
                        fsmCommonAspect.SwitchState(EFsmStateName.Skill1,fsmCommonAspect.GetScaledAnimationTime(0.6f));
                        break;
                    case EFsmStateName.Skill1:
                        //fsmStateComponent.Combat.Skill1Attack = true;
                        var knnTriggerBufferElements = Ecb.SetBuffer<KnnTriggerBufferElement>(index,abbyZFlagComponent.SpecialSkillTargetEntity);
                        knnTriggerBufferElements.Add(new KnnTriggerBufferElement
                            { Entity = abbyZFlagComponent.CurrentTargetEntity });
                        //fsmStateComponent.Combat.SkillAttackTarget = abbyZFlagComponent.SpecialSkillTargetEntity;
                        fsmStateComponent.Combat.ReleaseSkill1(abbyZFlagComponent.SpecialSkillTargetEntity);
                        fsmCommonAspect.SwitchState(EFsmStateName.Search,fsmCommonAspect.GetScaledAnimationTime(0.5f));
                        break;
                    case EFsmStateName.AfterDeath:
                        fsmCommonAspect.NormalAfterDeath(in AnimationNameComponent);
                        break;
                }
            }
        }
    }
}