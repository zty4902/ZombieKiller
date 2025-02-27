using Unity.Entities;

namespace DOTS.SystemGroup
{
    [UpdateInGroup(typeof(SimulationSystemGroup))][UpdateAfter(typeof(FsmHandlerSystemGroup))]
    public partial class SkillTargetFilterHandleGroup : ComponentSystemGroup
    {
        
    }
}