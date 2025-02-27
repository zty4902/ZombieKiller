using Unity.Entities;

namespace DOTS.Component.Role
{
    public struct FlameThrowerHFlagComponent : IComponentData
    {
        public Entity Skill1TargetEntity;
        public float Skill1Duration;
        public float Skill1Timer;
    }
}