using DOTS.Component.Role;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring.Role
{
    public class CopHFlagAuthoring : MonoBehaviour
    {
        private class CopHFlagAuthoringBaker : Baker<CopHFlagAuthoring>
        {
            public override void Bake(CopHFlagAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity,new CopHFlagComponent());
            }
        }
    }
}