using Unity.Entities;

namespace DOTS.Component.Combat
{
    public struct CombatComponent : IComponentData
    {
        public int CurHealth;
        public int MaxHealth;
        public int AttackDamage;
        public bool IsDead => CurHealth <= 0;
        public float FireRange;
        public Entity MeleeTrigger;
        /// <summary>
        /// 消耗完后装弹
        /// </summary>
        public int MaxFireCount;
        public int CurFireCount;

        /// <summary>
        /// 闪避中
        /// </summary>
        public bool Dodging;

        public bool BurningDeath;
    }
}