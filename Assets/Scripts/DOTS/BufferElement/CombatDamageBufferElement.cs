using Unity.Entities;

namespace DOTS.BufferElement
{
    public enum EDamageType
    {
        None,
        Gun,
        Melee,
        Crit,
        Fire,
        Smash,
    }
    public struct CombatDamageBufferElement : IBufferElementData
    {
        public EDamageType DamageType;
        public int Damage;
    }
}