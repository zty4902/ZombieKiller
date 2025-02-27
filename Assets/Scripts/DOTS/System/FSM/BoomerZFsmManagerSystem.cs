using DOTS.Aspect;
using DOTS.Component.Anim;
using DOTS.Component.Common;
using DOTS.Component.Role;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace DOTS.System.FSM
{
    public partial struct BoomerZFsmManagerSystem : ISystem
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
            var randomComponent = SystemAPI.GetSingletonRW<RandomComponent>().ValueRW;
            var prefabManagerComponent = SystemAPI.GetSingleton<PrefabManagerComponent>();
            var entityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            new BoomerZFsmManagerSystemJob
            {
                AnimationNameComponent = animationNameComponent,
                Random = randomComponent.GetRandom(),
                BloodExplosionEntity = prefabManagerComponent.BloodExplosion,
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
        public partial struct BoomerZFsmManagerSystemJob : IJobEntity
        {
            public AnimationNameComponent AnimationNameComponent;
            public Random Random;
            public Entity BloodExplosionEntity;
            public EntityCommandBuffer.ParallelWriter Ecb;
            private void Execute([EntityIndexInQuery]int index,in BoomerZFlagComponent boomerZFlagComponent,FsmCommonAspect fsmCommonAspect)
            {
                ref var fsmStateComponent = ref fsmCommonAspect.FsmStateComponent.ValueRW;
                if (fsmCommonAspect.CombatComponent.ValueRO.IsDead)
                {
                    if (fsmStateComponent.CurFsmStateName != EFsmStateName.Destroy )
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
                }


                switch (fsmStateComponent.CurFsmStateName)
                {
                    case EFsmStateName.Search:
                        var nextInt = Random.NextInt(0,10);
                        if (nextInt <= 8)
                        {
                            fsmCommonAspect.NormalSearch(in AnimationNameComponent,false);
                        }
                        else
                        {
                            fsmCommonAspect.SwitchState(EFsmStateName.Idle,0);
                        }
                        break;
                    case EFsmStateName.Idle:
                        fsmStateComponent.Animation.CurAnim = AnimationNameComponent.Idle;
                        fsmCommonAspect.SwitchState(EFsmStateName.Search,2);
                        break;
                    case EFsmStateName.Dying:
                        fsmStateComponent.Animation.CurAnim = AnimationNameComponent.Death;
                        fsmStateComponent.Navigation.Disable = true;
                        fsmCommonAspect.SwitchState(EFsmStateName.Destroy,fsmCommonAspect.GetScaledAnimationTime(0.8f));

                        var instantiate = Ecb.Instantiate(index,BloodExplosionEntity);
                        Ecb.SetComponent(index,instantiate,fsmCommonAspect.LocalTransform.ValueRO);

                        var entity = Ecb.Instantiate(index,boomerZFlagComponent.DeathPartEntity);
                        Ecb.SetComponent(index,entity,fsmCommonAspect.LocalTransform.ValueRO);
                        break;
                }
            }
        }
    }
}