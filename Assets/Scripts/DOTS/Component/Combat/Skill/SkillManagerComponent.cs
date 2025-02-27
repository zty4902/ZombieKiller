using Unity.Entities;

namespace DOTS.Component.Combat.Skill
{
    public struct SkillItem
    {
        public Entity SkillEntity;
        public float SkillCd;
        public float CurrentSkillCd;
        public bool HolderSkill;
        public float HolderSkillInterval;
        public float HolderSkillTimer;
    }
    public struct SkillManagerComponent : IComponentData
    {
        public SkillItem Skill1;
    }
}