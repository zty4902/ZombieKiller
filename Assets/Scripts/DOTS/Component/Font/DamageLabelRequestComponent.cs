using Unity.Entities;
using Unity.Mathematics;

namespace DOTS.Component.Font
{
    public struct DamageLabelRequestComponent : IComponentData
    {
        public int Damage;
        public float3 Position;
    }
}