using System.Runtime.InteropServices;
using DOTS.Aspect;
using DOTS.Component.FSM;
using DOTS.SystemGroup;
using Unity.Burst;
using Unity.Entities;

namespace DOTS.System.FSM.Handle
{
    [UpdateInGroup(typeof(FsmHandlerSystemGroup))]
    public partial struct FsmAnimationHandleSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new FsmAnimationHandleSystemJob
            {
                WorldTime = SystemAPI.Time.ElapsedTime
            }.ScheduleParallel(state.Dependency).Complete();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        [BurstCompile]
        [StructLayout(LayoutKind.Auto)]
        public partial struct FsmAnimationHandleSystemJob : IJobEntity
        {
            public double WorldTime;

            private void Execute(ref FsmStateComponent fsmStateComponent,CustomAnimatorAspect animatorAspect)
            {
                var change = fsmStateComponent.Animation.LastAnim != fsmStateComponent.Animation.CurAnim;
                if (change)
                {
                    fsmStateComponent.Animation.LastAnim = fsmStateComponent.Animation.CurAnim;
                    animatorAspect.SetAnimationWithScale(fsmStateComponent.Animation.CurAnim,WorldTime);
                }

                if (!change &&fsmStateComponent.Animation.ResetAnim)
                {
                    fsmStateComponent.Animation.ResetAnim = false;
                    animatorAspect.AnimatorAspect.ResetAnimation(WorldTime);
                }
            }
        }
    }
}