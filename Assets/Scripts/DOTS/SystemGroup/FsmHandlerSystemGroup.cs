using Unity.Entities;

namespace DOTS.SystemGroup
{
    [UpdateInGroup(typeof(SimulationSystemGroup),OrderFirst = true)][UpdateAfter(typeof(FsmSystemGroup))]
    public partial class FsmHandlerSystemGroup : ComponentSystemGroup { }
}