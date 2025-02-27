using DOTS.Component.FSM;
using DOTS.SystemGroup;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DOTS.System.FSM.Handle
{
    [UpdateInGroup(typeof(FsmHandlerSystemGroup))]
    public partial struct FsmFlashMoveHandleSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new FsmMoveHandleSystemJob().ScheduleParallel(state.Dependency).Complete();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        [BurstCompile]
        public partial struct FsmMoveHandleSystemJob : IJobEntity
        {
            private void Execute(ref FsmStateComponent fsmStateComponent,ref LocalTransform localTransform)
            {
                localTransform.Position += fsmStateComponent.FlashMove.MoveOffset;
                fsmStateComponent.FlashMove.MoveOffset = float3.zero;
            }
        }
    }
}