using DOTS.Component.Role;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring.Role
{
    public class BossZFlagAuthoring : MonoBehaviour
    {
        private class BossZFlagAuthoringBaker : Baker<BossZFlagAuthoring>
        {
            public override void Bake(BossZFlagAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity,new BossZFlagComponent());
            }
        }
    }
}