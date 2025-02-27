using DOTS.Component.Common;
using DOTS.Component.Font;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using EntityCommandBuffer = Unity.Entities.EntityCommandBuffer;

namespace DOTS.System.Font
{
    public partial struct TextLabelSystem : ISystem
    {
        private ComponentLookup<AutoDestroyComponent> _autoDestroyLookup;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FontManagerComponent>();
            _autoDestroyLookup = state.GetComponentLookup<AutoDestroyComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var entityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            var fontManagerComponent = SystemAPI.GetSingleton<FontManagerComponent>();
            _autoDestroyLookup.Update(ref state);
            new TextLabelSystemJob
            {
                Ecb = entityCommandBuffer.AsParallelWriter(),
                FontManagerComponent = fontManagerComponent,
                AutoDestroyLookup = _autoDestroyLookup
            }.ScheduleParallel(state.Dependency).Complete();
            entityCommandBuffer.Playback(state.EntityManager);
            entityCommandBuffer.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        public partial struct TextLabelSystemJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public FontManagerComponent FontManagerComponent;
            [ReadOnly]
            public ComponentLookup<AutoDestroyComponent> AutoDestroyLookup;

            private void Execute([EntityIndexInQuery]int index,Entity entity,
                in TextLabelComponent textLabelComponent,in TextLabelStringInfoComponent textLabelStringInfoComponent,
                ref LocalTransform localTransform)
            {
                var textLength = textLabelStringInfoComponent.Text.Length;
                const float charWidth = 0.5f;
                var startX = - charWidth * textLength / 2;
                var color = FontManagerComponent.CharColors.Value[textLabelStringInfoComponent.ColorIndex];
                for (var i = 0; i < textLength; i++)
                {
                    var c = textLabelStringInfoComponent.Text.Value[i];
                    Entity charLabelEntity;
                    if (i == 0)
                    {
                        charLabelEntity = textLabelComponent.ChildCharLabelEntity;
                    }
                    else
                    {
                        charLabelEntity = Ecb.Instantiate(index,textLabelComponent.ChildCharLabelEntity);
                        if (AutoDestroyLookup.TryGetComponent(entity,out var autoDestroy))
                        {
                            Ecb.AddComponent(index,charLabelEntity,new AutoDestroyComponent
                            {
                                DestroyTime = autoDestroy.DestroyTime
                            });
                        }

                    }
                    Ecb.SetComponent(index,charLabelEntity,new CharLabelComponent { Character = c});
                    var x = startX + i * charWidth;
                    var offsetPosition = new float3(x, 0, 0);
                    Ecb.SetComponent(index,charLabelEntity,LocalTransform.FromPosition(offsetPosition));
                    Ecb.SetComponent(index,charLabelEntity,new CustomColorData
                    {
                        Value = color
                    });
                }
                Ecb.SetComponentEnabled<TextLabelComponent>(index,entity, false);
                localTransform.Scale = textLabelStringInfoComponent.FontSize / 10.0f ;
            }
        }
    }
}