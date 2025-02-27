using Unity.Entities;

namespace DOTS.Component.Combat.Skill
{
    public struct SkillBurningBufferComponent : IComponentData
    {
        public int BurningDamage;
        public float BurningDuration;
    }
}