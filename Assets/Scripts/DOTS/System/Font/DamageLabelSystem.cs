using DOTS.Component.Common;
using DOTS.Component.Font;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DOTS.System.Font
{
    public partial struct DamageLabelSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FontManagerComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var fontManagerComponent = SystemAPI.GetSingleton<FontManagerComponent>();
            var entityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            //var randomComponent = SystemAPI.GetSingletonRW<RandomComponent>();
            new DamageLabelSystemJob
            {
                Ecb = entityCommandBuffer.AsParallelWriter(),
                FontManagerComponent = fontManagerComponent,
                Random = Random.CreateFromIndex((uint)entityCommandBuffer.GetHashCode())
            }.ScheduleParallel(state.Dependency).Complete();
            entityCommandBuffer.Playback(state.EntityManager);
            entityCommandBuffer.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        public partial struct DamageLabelSystemJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public FontManagerComponent FontManagerComponent;
            public Random Random;
            private void Execute([EntityIndexInQuery]int index,Entity entity,in DamageLabelRequestComponent request)
            {
                var textLabelRequest = Ecb.Instantiate(index,FontManagerComponent.TextLabelEntity);
                Ecb.AddComponent(index,textLabelRequest,new TextLabelStringInfoComponent
                {
                    Text = request.Damage.ToString(),
                    ColorIndex = 0,
                    FontSize = 1
                });
                Ecb.AddComponent(index,textLabelRequest,LocalTransform.FromPosition(request.Position));
                var offsetX = Random.NextFloat(-0.6f,0.6f);
                var offsetDuration = Random.NextFloat(-0.2f,0.2f);
                Ecb.AddComponent(index,textLabelRequest,new MoveAnimComponent
                {
                    Direction = math.normalize(math.up() + new float3(offsetX,0,0)),
                    Speed = 0.5f,
                    Duration = 0.5f + offsetDuration
                });
                Ecb.AddComponent(index,textLabelRequest,new AutoDestroyComponent
                {
                    DestroyTime = 1f
                });
                Ecb.DestroyEntity(index,entity);
            }
        }
    }
}