using Unity.Entities;

namespace DOTS.SystemGroup
{
    [UpdateInGroup(typeof(SimulationSystemGroup),OrderFirst = true)]
    public partial class FsmSystemGroup : ComponentSystemGroup { }
}