using DOTS.Component.Combat.Bullet;
using DOTS.Component.Common;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DOTS.System.Combat.Bullet
{
    public partial struct BulletSocketSystem : ISystem
    {
        private ComponentLookup<LocalTransform> _localTransformLookup;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _localTransformLookup = state.GetComponentLookup<LocalTransform>();
            state.RequireForUpdate<RandomComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var entityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            var random = SystemAPI.GetSingletonRW<RandomComponent>().ValueRW.GetRandom();
            _localTransformLookup.Update(ref state);
            new BulletSocketSystemJob
            {
                Ecb = entityCommandBuffer.AsParallelWriter(),
                DeltaTime = SystemAPI.Time.DeltaTime,
                Random = random,
                LocalTransformLookup = _localTransformLookup,
            }.ScheduleParallel(state.Dependency).Complete();
            entityCommandBuffer.Playback(state.EntityManager);
            entityCommandBuffer.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        [BurstCompile]
        public partial struct BulletSocketSystemJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public float DeltaTime;
            public Random Random;
            [ReadOnly]
            public ComponentLookup<LocalTransform> LocalTransformLookup;

            private void Execute([EntityIndexInQuery]int index,ref BulletSocketComponent bulletSocket,in LocalTransform localTransform,Parent parent)
            {
                if (bulletSocket.Timer < bulletSocket.Interval)
                {
                    bulletSocket.Timer += DeltaTime;
                    return;
                }
                bulletSocket.Timer = 0;


                if (LocalTransformLookup.TryGetComponent(parent.Value,out var parentLocalTransform))
                {
                    var nextFloat = Random.NextFloat(0,bulletSocket.RandomOffsetY);
                    var instantiate = Ecb.Instantiate(index,bulletSocket.BulletEntity);
                    var newLocalTransform = parentLocalTransform.TransformTransform(localTransform);
                    var localTransformPosition = newLocalTransform.Position + bulletSocket.Offset + new float3(0,nextFloat,0);
                    newLocalTransform = newLocalTransform.WithPosition(localTransformPosition);
                    Ecb.SetComponent(index,instantiate,newLocalTransform);
                }

            }
        }
    }
}