using Unity.Entities;

namespace DOTS.Component.Combat.Skill
{
    public struct SkillBaseComponent : IComponentData
    {
        public Entity Owner;
    }
}