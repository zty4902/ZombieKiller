using ProjectDawn.Navigation;
using Unity.Entities;

namespace Test
{
    [RequireMatchingQueriesForUpdate]
    public partial class TestAgentSystemBase : SystemBase
    {
        protected override void OnUpdate()
        {
            foreach (var valueTuple in SystemAPI.Query<TestAgentComponentData,RefRW<AgentBody>>())
            {
                valueTuple.Item2.ValueRW.SetDestination(valueTuple.Item1.Destination);
            }
        }
    }
}