using DOTS.BufferElement;
using Unity.Entities;

namespace DOTS.Component.Combat.Skill
{
    public struct SkillDamageComponent : IComponentData
    {
        public int Damage;
        public EDamageType DamageType;
    }
}