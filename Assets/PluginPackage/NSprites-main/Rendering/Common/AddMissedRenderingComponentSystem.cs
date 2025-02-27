using Unity.Burst;
using Unity.Entities;

namespace NSprites
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    //[UpdateBefore(typeof(SpriteRenderingSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.EntitySceneOptimizations)][RequireMatchingQueriesForUpdate]
    [BurstCompile]
    public partial class AddMissedRenderingComponentSystem : SystemBase
    {
        private EntityQuery _query;
        protected override void OnCreate()
        { 
            _query = SystemAPI.QueryBuilder()
                .WithAll<PropertyPointer>()
                .WithNoneChunkComponent<PropertyPointerChunk>()
                .WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.Default | EntityQueryOptions.IgnoreComponentEnabledState)
                .Build();
        }
        protected override void OnUpdate()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            entityManager.AddChunkComponentData(_query,new PropertyPointerChunk());
        }
    }
}