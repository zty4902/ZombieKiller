using DOTS.Component.Role;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring.Role
{
    public class BoomerZFlagAuthoring : MonoBehaviour
    {
        public GameObject deathPart;
        private class BoomerZFlagAuthoringBaker : Baker<BoomerZFlagAuthoring>
        {
            public override void Bake(BoomerZFlagAuthoring authoring)
            {
                var boomerZFlagComponent = new BoomerZFlagComponent();
                if (authoring.deathPart)
                {
                    boomerZFlagComponent.DeathPartEntity = GetEntity(authoring.deathPart,TransformUsageFlags.None);
;                }
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity,boomerZFlagComponent);
            }
        }
    }
}