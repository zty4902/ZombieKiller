using DOTS.Component.Font;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine.Rendering;

namespace DOTS.System.Font
{
    public partial class CharLabelSystem : SystemBase
    {
        private NativeHashMap<uint,CharLabelInfo> _customUvMap;
        protected override void OnCreate()
        {
            RequireForUpdate<FontManagerComponent>();
            RequireForUpdate<CharLabelComponent>();
            _customUvMap = new NativeHashMap<uint, CharLabelInfo>(0,Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            var singletonEntity = SystemAPI.GetSingletonEntity<FontManagerComponent>();
            if (_customUvMap.Count == 0)
            {
                var fontManagerComponent = SystemAPI.GetComponent<FontManagerComponent>(singletonEntity);
                var blobAssetReference = fontManagerComponent.CharLabelInfos;
                var valueLength = blobAssetReference.Value.Length;
                for (var i = 0; i < valueLength; i++)
                {
                    var item = blobAssetReference.Value[i];
                    _customUvMap.Add(item.Index,item);
                }
            }
            var stateEntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var renderMeshArray = stateEntityManager.GetSharedComponentManaged<RenderMeshArray>(singletonEntity);
            var entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

            var entityList = new NativeList<Entity>(Allocator.Temp);
            var customUvDataList = new NativeList<CharLabelInfo>(Allocator.Temp);
            var positionList = new NativeList<float3>(Allocator.Temp);
            //var fontSizeList = new NativeList<float2>(Allocator.Temp);
            foreach (var (charLabelComponent, localTransform,entity) in SystemAPI.Query<RefRW<CharLabelComponent>,RefRO<LocalTransform>>().WithEntityAccess())
            {
                entityList.Add(entity);
                positionList.Add(localTransform.ValueRO.Position);
                //fontSizeList.Add(charLabelComponent.ValueRO.FontSize);
                if (_customUvMap.TryGetValue(charLabelComponent.ValueRO.Character, out var uv))
                {
                    customUvDataList.Add(uv);
                }
            }

            for (var i = 0; i < entityList.Length; i++)
            {
                var entity = entityList[i];
                var charLabelInfo = customUvDataList[i];
                var position = positionList[i];
                //var fontSize = fontSizeList[i];
                var size = charLabelInfo.Size;
                DrawMesh(entity,stateEntityManager, renderMeshArray, charLabelInfo.CustomUv);
                /*stateEntityManager.AddComponentData(entity,new LocalToWorld
                {
                    Value = float4x4.TRS(position, quaternion.identity, new float3(size.x, size.y,1))
                });*/
                stateEntityManager.AddComponentData(entity, new PostTransformMatrix
                {
                    Value = float4x4.TRS(position, quaternion.identity, new float3(size.x, size.y,1))
                });
                stateEntityManager.SetComponentEnabled<CharLabelComponent>(entity,false);
            }
            entityCommandBuffer.Playback(stateEntityManager);
            entityCommandBuffer.Dispose();
            
            customUvDataList.Dispose();
            entityList.Dispose();
            positionList.Dispose();
            //fontSizeList.Dispose();
        }

        private void DrawMesh(Entity entity,EntityManager entityManager,RenderMeshArray renderMeshArray,float4 customUv)
        {
            var filterSettings = RenderFilterSettings.Default;
            filterSettings.ShadowCastingMode = ShadowCastingMode.Off;
            filterSettings.ReceiveShadows = false;

            var renderMeshDescription = new RenderMeshDescription
            {
                FilterSettings = filterSettings,
                LightProbeUsage = LightProbeUsage.Off,
            };
            RenderMeshUtility.AddComponents(
                entity,
                entityManager,
                renderMeshDescription,
                renderMeshArray,
                MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));
            entityManager.AddComponentData(entity, new CustomUvData()
            {
                Value = customUv
            });
        }

        protected override void OnDestroy()
        {
            _customUvMap.Dispose();
        }
    }
}