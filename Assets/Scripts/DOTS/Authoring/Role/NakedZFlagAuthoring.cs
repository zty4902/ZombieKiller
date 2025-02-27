using DOTS.Component.Role;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring.Role
{
    public class NakedZFlagAuthoring : MonoBehaviour
    {
        private class NakedZFlagAuthoringBaker : Baker<NakedZFlagAuthoring>
        {
            public override void Bake(NakedZFlagAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var nakedZFlagComponent = new NakedZFlagComponent();
                AddComponent(entity, nakedZFlagComponent);
            }
        }
    }
}