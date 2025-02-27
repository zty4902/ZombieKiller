using Unity.Entities;
using Unity.Mathematics;

namespace DOTS.Component.Combat.Bullet
{
    public struct BulletFlyAnimComponent : IComponentData
    {
        public float3 Direction;
        public float Speed;
        public bool IsFlying;
    }
}