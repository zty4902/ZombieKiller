using DOTS.Component.Common;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring.Common
{
    public class AutoDestroyAuthoring : MonoBehaviour
    {
        public float destroyTime = 5f;
        private class AutoDestroyAuthoringBaker : Baker<AutoDestroyAuthoring>
        {
            public override void Bake(AutoDestroyAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity,new AutoDestroyComponent
                {
                    DestroyTime = authoring.destroyTime
                });
            }
        }
    }
}