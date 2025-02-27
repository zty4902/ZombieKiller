using DOTS.Component.Role;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring.Role
{
    public class AbbyHFlagAuthoring : MonoBehaviour
    {
        public GameObject fireTrigger;
        public GameObject gunPrefab;
        public float fireDuration;
        [LabelText("可重装子弹次数")]
        public int maxReloadCount;
        private class AbbyHFlagAuthoringBaker : Baker<AbbyHFlagAuthoring>
        {
            public override void Bake(AbbyHFlagAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var abbyHFlagComponent = new AbbyHFlagComponent
                {
                    FireTriggerEntity = GetEntity(authoring.fireTrigger, TransformUsageFlags.None),
                    FireDuration = authoring.fireDuration,
                    MaxReloadCount = authoring.maxReloadCount,
                    GunPrefabEntity = GetEntity(authoring.gunPrefab, TransformUsageFlags.None)
                };
                AddComponent(entity,abbyHFlagComponent);
            }
        }
    }
}