using Unity.Entities;
using Unity.Mathematics;

namespace DOTS.Component.Anim
{
    public struct AnimationConfig
    {
        public int AnimationIndex;
        public float2 Scale;
    }
    public struct AnimationConfigComponent : IComponentData
    {
        public BlobAssetReference<BlobArray<AnimationConfig>> AnimationConfigArray;
        public float AnimScale;
    }
}