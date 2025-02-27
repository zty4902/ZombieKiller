using DOTS.Component.Common;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace DOTS.System.Common
{
    public partial struct MoveAnimSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new MoveAnimSystemJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel(state.Dependency).Complete();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        [BurstCompile]
        public partial struct MoveAnimSystemJob : IJobEntity
        {
            public float DeltaTime;

            private void Execute(ref MoveAnimComponent moveAnimComponent,ref LocalTransform localTransform)
            {
                moveAnimComponent.Timer += DeltaTime;
                if (moveAnimComponent.Timer >= moveAnimComponent.Duration)
                {
                    return;
                }
                localTransform.Position += moveAnimComponent.Direction * moveAnimComponent.Speed * DeltaTime;
            }
        }
    }
}