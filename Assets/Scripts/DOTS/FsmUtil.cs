using DOTS.Component.FSM;
using Unity.Mathematics;

namespace DOTS
{
    public struct FsmUtil
    {
        /*public static void SwitchState(ref FsmStateComponent fsmStateComponent,in EFsmStateName curFsmStateName,in float waitTime)
        {
            //fsmStateComponent.LastFsmStateName = lastFsmStateName;
            fsmStateComponent.CurFsmStateName = curFsmStateName;
            fsmStateComponent.WaitTime = waitTime;
        }*/

        /*public static float3 GetAttackPoint(float3 enemyPos, float3 playerPos, float range)
        {
            var dir = enemyPos.x - playerPos.x;
            var xDist = math.abs(dir);
            var sign = math.sign(dir);
            enemyPos.x -= sign * math.min(range - 0.15f,xDist);
            return enemyPos;
        }*/

        /*public static bool HasFsmEventFlag(EFsmEventName eFlags, EFsmEventName eFlag)
        {
            return (eFlags & eFlag) != 0;
        }*/
    }
}