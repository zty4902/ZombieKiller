using DOTS.BufferElement;
using Unity.Entities;

namespace DOTS.Component.Combat.Skill
{
    public struct SkillGunFireComponent : IComponentData
    {
        public bool IsFiring;
        public Entity GunSocketEntity;
        public float DamageInterval;
        public float DamageTimer;
        public int Damage;
        public EDamageType DamageType;
    }
}