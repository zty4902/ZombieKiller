using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace DOTS.Component.Font
{
    public struct CharLabelInfo
    {
        //public CustomUvData CustomUv;
        public uint Index;
        public float4 CustomUv;
        public float2 Size;
    }
    [MaterialProperty("_CustomUv")]
    public struct CustomUvData : IComponentData
    {
        public float4 Value;
    }
    [MaterialProperty("_Color")]
    public struct CustomColorData : IComponentData
    {
        public float4 Value;
    }
    public struct FontManagerComponent : IComponentData
    {
        //public BlobAssetReference<NativeHashMap<uint, CharLabelInfo>> CustomUvMap;
        public BlobAssetReference<BlobArray<CharLabelInfo>> CharLabelInfos;
        //public Entity CharLabelEntity;
        public Entity TextLabelEntity;
        public BlobAssetReference<BlobArray<float4>> CharColors;
    }
}