using Unity.Entities;

namespace DOTS.Component.Role
{
    public struct AbbyHFlagComponent : IComponentData
    {
        public Entity FireTriggerEntity;
        public Entity GunPrefabEntity;
        public float FireDuration;
        public int MaxReloadCount;
        
        public bool IsDisarmament;
        public int CurReloadCount;
    }
}