using DOTS.Component.Role;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring.Role
{
    public class AbbyZFlagAuthoring : MonoBehaviour
    {
        public GameObject specialTrigger;
        public GameObject specialSkillTarget;
        private class AbbyZFlagAuthoringBaker : Baker<AbbyZFlagAuthoring>
        {
            public override void Bake(AbbyZFlagAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                if (authoring.specialTrigger == null)
                {
                    return;
                }
                var abbyFlagComponent = new AbbyZFlagComponent
                {
                    SpecialTriggerEntity = GetEntity(authoring.specialTrigger, TransformUsageFlags.None),
                    SpecialSkillTargetEntity = GetEntity(authoring.specialSkillTarget, TransformUsageFlags.None)
                };
                AddComponent(entity,abbyFlagComponent);
            }
        }
    }
}