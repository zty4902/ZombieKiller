using System.Runtime.InteropServices;
using DOTS.Aspect;
using DOTS.BufferElement;
using DOTS.Component.Anim;
using DOTS.Component.Common;
using DOTS.Component.FSM;
using DOTS.SystemGroup;
using NSprites;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DOTS.System.FSM
{
    [UpdateInGroup(typeof(FsmSystemGroup),OrderLast = true)]
    public partial struct FallbackFsmManagerSystem : ISystem
    {
        private ComponentLookup<FsmStateRuntimeDataComponent> _fsmStateRuntimeDataComponentLookup;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PrefabManagerComponent>();
            state.RequireForUpdate<AnimationNameComponent>();
            _fsmStateRuntimeDataComponentLookup = state.GetComponentLookup<FsmStateRuntimeDataComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var animationNameComponent = SystemAPI.GetSingleton<AnimationNameComponent>();
            var entityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            var prefabManagerComponent = SystemAPI.GetSingleton<PrefabManagerComponent>();
            _fsmStateRuntimeDataComponentLookup.Update(ref state);
            new FallBackFsmManagerSystemJob
            {
                AnimationNameComponent = animationNameComponent,
                Ecb = entityCommandBuffer.AsParallelWriter(),
                DelTime = SystemAPI.Time.DeltaTime,
                PrefabManagerComponent = prefabManagerComponent,
                FsmStateRuntimeDataComponentLookup = _fsmStateRuntimeDataComponentLookup
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
        public partial struct FallBackFsmManagerSystemJob : IJobEntity
        {
            public AnimationNameComponent AnimationNameComponent;
            public EntityCommandBuffer.ParallelWriter Ecb;
            public PrefabManagerComponent PrefabManagerComponent;
            public float DelTime;
            [ReadOnly]
            public ComponentLookup<FsmStateRuntimeDataComponent> FsmStateRuntimeDataComponentLookup;

            private void Execute([EntityIndexInQuery]int index,Entity entity, FsmCommonAspect fsmCommonAspect
                ,ref DynamicBuffer<CombatDamageBufferElement> combatDamageBufferElements)
            {
                ref var fsmStateComponent = ref fsmCommonAspect.FsmStateComponent.ValueRW;
                if (fsmCommonAspect.HasFsmEventFlag(EFsmEventName.Dying)
                    && fsmStateComponent.CurFsmStateName!= EFsmStateName.Dying && fsmStateComponent.CurFsmStateName!= EFsmStateName.AfterDeath
                    && fsmStateComponent.CurFsmStateName!= EFsmStateName.Destroy && fsmStateComponent.CurFsmStateName!= EFsmStateName.BurningDeath)
                {
                    fsmStateComponent.CurFsmStateName = EFsmStateName.Dying;
                }
                else
                {
                    if (fsmStateComponent.CurFsmBufferEvent != EFsmBufferEventName.None && !fsmCommonAspect.HasFsmEventFlag(EFsmEventName.Dying))
                    {
                        //硬直
                        if (fsmCommonAspect.HasFsmBufferEventFlag(EFsmBufferEventName.Pause))
                        {
                            fsmCommonAspect.SwitchState(EFsmStateName.Pause, 0);
                        }
                        //火烧
                        if (fsmCommonAspect.HasFsmBufferEventFlag(EFsmBufferEventName.Burning))
                        {
                            if (fsmStateComponent.Buff.BurningDuration > fsmStateComponent.Buff.BurningTimer)
                            {
                                if (FsmStateRuntimeDataComponentLookup.TryGetComponent(entity,out var runtimeDataComponent))
                                {
                                    if (runtimeDataComponent.BuffFireEntity == Entity.Null)
                                    {
                                        var instantiate = Ecb.Instantiate(index,PrefabManagerComponent.BuffFire);
                                        Ecb.AddComponent(index,instantiate,new Parent
                                        {
                                            Value = entity
                                        });
                                        Ecb.SetComponent(index,instantiate,LocalTransform.Identity);
                                        var newFsmStateRuntimeData = runtimeDataComponent;
                                        newFsmStateRuntimeData.BuffFireEntity = instantiate;
                                        var renderColor = new float4(0, 0, 0, 0.5f);
                                        newFsmStateRuntimeData.RenderColor = renderColor;
                                        Ecb.SetComponent(index,entity,newFsmStateRuntimeData);
                                        Ecb.SetComponent(index,entity,new NsColor
                                        {
                                            value = renderColor
                                        });
                                    }
                                }
                                
                                var lastTime = (int)math.floor(fsmStateComponent.Buff.BurningTimer);
                                fsmStateComponent.Buff.BurningTimer += DelTime;
                                var nextTime = (int)math.floor(fsmStateComponent.Buff.BurningTimer);
                                if (lastTime != nextTime)
                                {
                                    combatDamageBufferElements.Add(new CombatDamageBufferElement
                                    {
                                        DamageType = EDamageType.Fire,
                                        Damage = fsmStateComponent.Buff.BurningDamage
                                    });
                                }
                            }
                            else
                            {
                                ClearBurningFireEntity(index, entity);
                                fsmCommonAspect.RemoveFsmBufferEventFlag(EFsmBufferEventName.Burning);
                                fsmStateComponent.Buff.BurningTimer = 0;
                                fsmStateComponent.Buff.BurningDuration = 0;
                            }
                        }
                    }
                    fsmStateComponent.CurFsmEvent = EFsmEventName.None;
                    //fsmStateComponent.CurFsmBufferEvent = EFsmBufferEventName.None;
                
                    if (fsmStateComponent.WaitTime > 0)
                    {
                        fsmStateComponent.WaitTime -= DelTime;
                        return;
                    }
                }

                switch (fsmStateComponent.CurFsmStateName)
                {
                    case EFsmStateName.Create:
                        fsmStateComponent.Animation.CurAnim = AnimationNameComponent.Idle;
                        //fsmStateComponent.Navigation.Layers = NavigationLayers.Default;
                        fsmCommonAspect.SwitchState(EFsmStateName.Search,1);
                        break;
                    case EFsmStateName.Dying:
                        fsmCommonAspect.NormalDying(in AnimationNameComponent);
                        break;
                    case EFsmStateName.BurningDeath:
                        var instantiate = Ecb.Instantiate(index,PrefabManagerComponent.BurningDeath);
                        Ecb.SetComponent(index,instantiate,fsmCommonAspect.LocalTransform.ValueRO);
                        fsmCommonAspect.SwitchState(EFsmStateName.Destroy, 0);
                        break;
                    case EFsmStateName.AfterDeath:
                        //fsmStateComponent.Animation.CurAnim = AnimationNameComponent.AfterDeath;
                        fsmCommonAspect.SwitchState(EFsmStateName.Destroy, 0);
                        break;
                    case EFsmStateName.Destroy:
                        ClearBurningFireEntity(index, entity);
                        Ecb.DestroyEntity(index,entity);
                        break;
                    case EFsmStateName.Pause:
                        fsmStateComponent.Animation.CurAnim = AnimationNameComponent.Idle;
                        fsmStateComponent.Navigation.Destination = fsmCommonAspect.LocalTransform.ValueRO.Position;
                        fsmCommonAspect.SwitchState(EFsmStateName.Search, 0.5f);
                        fsmCommonAspect.RemoveFsmBufferEventFlag(EFsmBufferEventName.Pause);
                        break;
                }
            }

            private void ClearBurningFireEntity(int index, Entity entity)
            {
                if (FsmStateRuntimeDataComponentLookup.TryGetComponent(entity,
                        out var runtimeDataComponent))
                {
                    if (runtimeDataComponent.BuffFireEntity != Entity.Null)
                    {
                        var renderColor = new float4(1,1,1,1);
                        Ecb.DestroyEntity(index,runtimeDataComponent.BuffFireEntity);
                        runtimeDataComponent.BuffFireEntity = Entity.Null;
                        runtimeDataComponent.RenderColor = renderColor;
                        Ecb.SetComponent(index,entity,runtimeDataComponent);
                        
                        Ecb.SetComponent(index,entity,new NsColor
                        {
                            value = renderColor
                        });
                    }
                }
            }
        }
    }
}