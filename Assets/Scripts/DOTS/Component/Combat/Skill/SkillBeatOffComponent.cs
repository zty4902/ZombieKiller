using Unity.Entities;

namespace DOTS.Component.Combat.Skill
{
    public struct SkillBeatOffComponent : IComponentData
    {
        //public bool Enable;
        public float Force;
        public float Time;
        public float AttackTimer;
        public Entity Target;
    }
}