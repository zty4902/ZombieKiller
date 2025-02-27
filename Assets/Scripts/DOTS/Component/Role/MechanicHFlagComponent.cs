using Unity.Entities;

namespace DOTS.Component.Role
{
    public struct MechanicHFlagComponent : IComponentData
    {
        public bool FirstCharge;
        public float CriticalRate;
        
        public bool CurrentCritical;
        public Entity MeleeSWeaponEntity;
    }
}