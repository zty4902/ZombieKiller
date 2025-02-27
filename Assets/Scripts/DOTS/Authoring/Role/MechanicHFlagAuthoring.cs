using DOTS.Component.Role;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring.Role
{
    public class MechanicHFlagAuthoring : MonoBehaviour
    {
        [Range(0,1)][LabelText("暴击率")]
        public float criticalRate;
        [LabelText("近战武器")]
        public GameObject meleeSWeapon;
        private class MechanicHFlagAuthoringBaker : Baker<MechanicHFlagAuthoring>
        {
            public override void Bake(MechanicHFlagAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity,new MechanicHFlagComponent
                {
                    FirstCharge = true,
                    CriticalRate = authoring.criticalRate,
                    MeleeSWeaponEntity = GetEntity(authoring.meleeSWeapon,TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}