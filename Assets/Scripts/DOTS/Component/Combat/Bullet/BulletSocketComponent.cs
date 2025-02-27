using Unity.Entities;
using Unity.Mathematics;

namespace DOTS.Component.Combat.Bullet
{
    public struct BulletSocketComponent : IComponentData,IEnableableComponent
    {
        public Entity BulletEntity;
        public float Interval;
        public float Timer;
        public float3 Offset;
        public float RandomOffsetY;
    }
}