using Unity.Entities;

namespace DOTS.Component.Role
{
    public struct AbbyZFlagComponent : IComponentData
    {
        public Entity SpecialTriggerEntity;
        public Entity CurrentTargetEntity;
        public Entity SpecialSkillTargetEntity;
    }
}