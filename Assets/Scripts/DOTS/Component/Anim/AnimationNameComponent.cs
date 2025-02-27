using Unity.Entities;

namespace DOTS.Component.Anim
{
    public struct AnimationNameComponent : IComponentData
    {
        //根据EAnimationName生成成员
        public int Idle;
        public int Idle2;
        public int AfterDeath;
        public int AfterDeath2;
        public int Death;
        public int Death2;
        public int Move;
        public int Move2;
        public int MoveS;
        public int Aim;
        public int Dodge;
        public int Fire;
        public int Melee;
        public int MeleeS;
        public int Reload;
        public int Reload2;
        public int Super;
        public int Special;
        public int MeleeCrit;
        public int Melee2;
        public int Melee3;
        public int Disarmament;

    }
}