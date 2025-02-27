using System.Collections.Generic;
using DOTS.Component.Font;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.TextCore;
using Hash128 = Unity.Entities.Hash128;

namespace DOTS.Authoring.Font
{
    public class FontManagerAuthoring : MonoBehaviour
    {
        public TMP_FontAsset fontAsset;
        public Material material;
        public Mesh mesh;
        
        //public GameObject charLabelPrefab;
        public GameObject textLabelPrefab;
        public List<Color> charLabelColors;
        private class FontManagerAuthoringBaker : Baker<FontManagerAuthoring>
        {
            public override void Bake(FontManagerAuthoring authoring)
            {
                if (authoring.fontAsset == null)
                {
                    return;
                }
                var tmpFontAsset = authoring.fontAsset;
                var fontAssetCharacterTable = tmpFontAsset.characterLookupTable;
                var customHash = new Hash128
                {
                    Value = new uint4((uint)fontAssetCharacterTable.GetHashCode(),(uint)tmpFontAsset.GetHashCode(),0,0)
                };
                if (!TryGetBlobAssetReference(customHash, out BlobAssetReference<BlobArray<CharLabelInfo>> blobAssetReference))
                {
                    var blobBuilder = new BlobBuilder(Allocator.Temp);
                    ref var charLabelInfoArray = ref blobBuilder.ConstructRoot<BlobArray<CharLabelInfo>>();
                    var blobBuilderArray = blobBuilder.Allocate(ref charLabelInfoArray,fontAssetCharacterTable.Count);
                    var fontAssetAtlasWidth = tmpFontAsset.atlasWidth;
                    var fontAssetAtlasHeight = tmpFontAsset.atlasHeight;
                    var index = 0;
                    foreach (var tmpCharacter in fontAssetCharacterTable)
                    {
                        var customUvData = new CharLabelInfo
                        {
                            CustomUv = GetCustomUv(tmpCharacter.Value.glyph, fontAssetAtlasWidth, fontAssetAtlasHeight),
                            Size = new float2(tmpCharacter.Value.glyph.metrics.width, tmpCharacter.Value.glyph.metrics.height)/100,
                            Index = tmpCharacter.Key
                        };
                        blobBuilderArray[index++] = customUvData;
                    }

                    blobAssetReference = blobBuilder.CreateBlobAssetReference<BlobArray<CharLabelInfo>>(Allocator.Persistent);
                    AddBlobAssetWithCustomHash(ref blobAssetReference,customHash);
                    blobBuilder.Dispose();

                }
                var fontManagerComponent = new FontManagerComponent
                {
                    CharLabelInfos = blobAssetReference
                };
                if (authoring.textLabelPrefab)
                {
                    fontManagerComponent.TextLabelEntity =
                        GetEntity(authoring.textLabelPrefab, TransformUsageFlags.None);
                }

                if (authoring.charLabelColors is { Count: > 0 })
                {
                    var colorHash = new Hash128((uint4)authoring.charLabelColors.GetHashCode());
                    if (!TryGetBlobAssetReference(colorHash,out BlobAssetReference<BlobArray<float4>> colorBlobAssetReference))
                    {
                        var colorBuilder = new BlobBuilder(Allocator.Temp);

                        ref var constructRoot = ref colorBuilder.ConstructRoot<BlobArray<float4>>();
                        var blobBuilderArray = colorBuilder.Allocate(ref constructRoot,authoring.charLabelColors.Count);
                        for (var i = 0; i < authoring.charLabelColors.Count; i++)
                        {
                            var charLabelColor = authoring.charLabelColors[i];
                            blobBuilderArray[i] = new float4(charLabelColor.r, charLabelColor.g, charLabelColor.b, charLabelColor.a);
                        }
                        colorBlobAssetReference = colorBuilder.CreateBlobAssetReference<BlobArray<float4>>(Allocator.Persistent);
                        AddBlobAssetWithCustomHash(ref colorBlobAssetReference,colorHash);
                        colorBuilder.Dispose();
                    }
                    fontManagerComponent.CharColors = colorBlobAssetReference;
                }
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity,fontManagerComponent);
                
                var renderMeshArray = new RenderMeshArray(new []{authoring.material},new []{authoring.mesh});
                AddSharedComponentManaged(entity,renderMeshArray);
            }
            
            private static float4 GetCustomUv(Glyph glyph,float atlasWidth, float atlasHeight)
            {
                float rectWidth = glyph.glyphRect.width;
                float rectHeight = glyph.glyphRect.height;
                float rx = glyph.glyphRect.x;
                float ry = glyph.glyphRect.y;

                var offsetX = rx / atlasWidth;
                var offsetY = ry / atlasHeight;
                var uvScaleX = (rx + rectWidth) / atlasWidth - offsetX;
                var uvScaleY = (ry + rectHeight) / atlasHeight - offsetY;

                return new float4(offsetX, offsetY, uvScaleX, uvScaleY);
            }
        }
    }
}