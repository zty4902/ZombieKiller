using Unity.Entities;

namespace DOTS.SystemGroup
{
    [UpdateInGroup(typeof(SimulationSystemGroup))][UpdateAfter(typeof(SkillTargetFilterHandleGroup))]
    public partial class SkillHandleSystemGroup : ComponentSystemGroup
    {
        
    }
}