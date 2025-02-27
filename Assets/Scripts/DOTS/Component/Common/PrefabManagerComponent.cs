using Unity.Collections;
using Unity.Entities;

namespace DOTS.Component.Common
{
    public struct PrefabItem
    {
        public EPrefabName Name;
        public Entity Prefab;
    }
    public struct PrefabManagerComponent : IComponentData
    {
        public Entity BloodCommon;
        public Entity BloodShot;
        public Entity BloodSplash;
        public Entity BloodFire;
        
        public BlobAssetReference<NativeList<PrefabItem>> Heroes;
        public BlobAssetReference<NativeList<PrefabItem>> Zombies;

        public Entity BurningDeath;
        public Entity BurningCorpse;
        
        public Entity SmallExplosion;
        public Entity XmasFire;
        public Entity BuffFire;
        public Entity BloodExplosion;
        public Entity BloodSmallExplosion;

        public Entity CritSmack;
        public Entity CritTwack;
    }
}