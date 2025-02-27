using Unity.Entities;
using Unity.Mathematics;

namespace DOTS.Component.Common
{
    public struct MoveAnimComponent : IComponentData
    {
        public float3 Direction;
        public float Speed;
        public float Duration;
        public float Timer;
    }
}