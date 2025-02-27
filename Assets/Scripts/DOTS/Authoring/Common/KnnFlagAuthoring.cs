using DOTS.Component.Common;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring.Common
{
    public class KnnFlagAuthoring : MonoBehaviour
    {
        public int flag;
        private class KnnFlagAuthoringBaker : Baker<KnnFlagAuthoring>
        {
            public override void Bake(KnnFlagAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddSharedComponent(entity,new KnnFlagComponent
                {
                    Flag = authoring.flag
                });
            }
        }
    }
}