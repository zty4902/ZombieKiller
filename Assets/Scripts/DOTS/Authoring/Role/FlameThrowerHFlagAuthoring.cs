using DOTS.Component.Role;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring.Role
{
    public class FlameThrowerHFlagAuthoring : MonoBehaviour
    {
        public GameObject skillTargetObject;
        public float skill1Duration;
        private class FlameThrowerHFlagAuthoringBaker : Baker<FlameThrowerHFlagAuthoring>
        {
            public override void Bake(FlameThrowerHFlagAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity,new FlameThrowerHFlagComponent
                {
                    Skill1TargetEntity = GetEntity(authoring.skillTargetObject,TransformUsageFlags.None),
                    Skill1Duration = authoring.skill1Duration,
                });
            }
        }
    }
}