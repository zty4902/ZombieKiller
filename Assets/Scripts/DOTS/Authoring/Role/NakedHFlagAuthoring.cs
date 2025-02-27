using DOTS.Component.Role;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring.Role
{
    public class NakedHFlagAuthoring : MonoBehaviour
    {
        private class NakedHFlagAuthoringBaker : Baker<NakedHFlagAuthoring>
        {
            public override void Bake(NakedHFlagAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity,new NakedHFlagComponent());
            }
        }
    }
}