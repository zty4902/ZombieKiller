using DOTS.Component.Combat.Bullet;
using NSprites;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DOTS.System.Combat.Bullet
{
    [UpdateBefore(typeof(SpriteUVAnimationSystem))]
    public partial struct BulletFlyAnimSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new BulletFlyAnimSystemJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                WorldTime = SystemAPI.Time.ElapsedTime
            }.ScheduleParallel(state.Dependency).Complete();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        [BurstCompile]
        public partial struct BulletFlyAnimSystemJob : IJobEntity
        {
            public float DeltaTime;
            public double WorldTime;

            private void Execute(ref BulletFlyAnimComponent bulletFlyAnimComponent,ref LocalTransform localTransform,AnimatorAspect aspect)
            {
                if (!bulletFlyAnimComponent.IsFlying)
                {
                    bulletFlyAnimComponent.IsFlying = true;
                    aspect.ResetAnimation(WorldTime);
                }

                var turnRight = localTransform.Rotation.Equals(quaternion.identity);
                var turnFac = turnRight ? 1 : -1;
                localTransform.Position += bulletFlyAnimComponent.Direction * turnFac * bulletFlyAnimComponent.Speed * DeltaTime;
            }
        }
    }
}