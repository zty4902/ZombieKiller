using DOTS.Component;
using NSprites;
using Unity.Entities;

namespace DOTS.System
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)][RequireMatchingQueriesForUpdate]
    public partial class TestSystemBase : SystemBase
    {
        protected override void OnUpdate()
        {
            var singletonEntity = SystemAPI.GetSingletonEntity<TestComponentData>();
            var testComponentData = SystemAPI.GetComponent<TestComponentData>(singletonEntity);
            var curAnim = testComponentData.CurAnim;
            var lastAnim = testComponentData.LastAnim;
            if (curAnim != lastAnim)
            {
                testComponentData.LastAnim = curAnim;
                AnimData curAnimData = testComponentData.Move;
                switch (curAnim)
                {
                    case 0:
                        curAnimData = testComponentData.Move;
                        break;
                    case 1:
                        curAnimData = testComponentData.AfterDeath;
                        break;
                    case 2:
                        curAnimData = testComponentData.Aim;
                        break;
                    case 3:
                        curAnimData = testComponentData.Death;
                        break;
                    case 4:
                        curAnimData = testComponentData.Dodge;
                        break;
                }
                var animatorAspect = SystemAPI.GetAspect<AnimatorAspect>(singletonEntity);
                animatorAspect.SetAnimation(curAnimData.animIndex,SystemAPI.Time.ElapsedTime);
                var scale2D = SystemAPI.GetComponent<Scale2D>(singletonEntity);
                scale2D.value = curAnimData.scale;
                SystemAPI.SetComponent(singletonEntity,scale2D);
            }
            SystemAPI.SetComponent(singletonEntity,testComponentData);
        }
    }
}