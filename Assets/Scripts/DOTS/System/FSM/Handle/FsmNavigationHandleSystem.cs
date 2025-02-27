using System.Runtime.InteropServices;
using DOTS.Component.Anim;
using DOTS.Component.FSM;
using DOTS.SystemGroup;
using ProjectDawn.Navigation;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DOTS.System.FSM.Handle
{
    [UpdateInGroup(typeof(FsmHandlerSystemGroup))]
    public partial struct FsmNavigationHandleSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AnimationNameComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new FsmNavigationHandleSystemJob
            {
                AnimationNameComponent = SystemAPI.GetSingleton<AnimationNameComponent>()
            }.ScheduleParallel(state.Dependency).Complete();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        [BurstCompile]
        [StructLayout(LayoutKind.Auto)]
        public partial struct FsmNavigationHandleSystemJob : IJobEntity
        {
            public AnimationNameComponent AnimationNameComponent;
            private void Execute(in FsmStateComponent fsmStateComponent,ref AgentBody agentBody,ref Agent agent,ref AgentSeparation agentCollider,ref LocalTransform localTransform)
            {
                if (fsmStateComponent.Navigation.Disable)
                {
                    agent.Layers = NavigationLayers.None;
                    agentCollider.Layers = NavigationLayers.None;
                    agentBody.Stop();
                    return;
                }
                var targetPosition = fsmStateComponent.Navigation.TargetPosition;
                localTransform.Rotation = 
                    targetPosition.x - localTransform.Position.x < 0 ? quaternion.RotateY(math.PI) : quaternion.identity;
                if (fsmStateComponent.Animation.CurAnim != AnimationNameComponent.Move && fsmStateComponent.Animation.CurAnim != AnimationNameComponent.MoveS
                    && fsmStateComponent.Animation.CurAnim != AnimationNameComponent.Move2)
                {
                    agentBody.Stop();
                    return;
                }
                var navigationDestination = fsmStateComponent.Navigation.Destination;
                agentBody.SetDestination(navigationDestination);
            }
        }
    }
}