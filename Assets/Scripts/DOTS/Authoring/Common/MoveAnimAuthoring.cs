using DOTS.Component.Common;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DOTS.Authoring.Common
{
    public class MoveAnimAuthoring : MonoBehaviour
    {
        public float3 direction;
        public float speed;
        public float duration;
        private class MoveAnimAuthoringBaker : Baker<MoveAnimAuthoring>
        {
            public override void Bake(MoveAnimAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity,new MoveAnimComponent
                {
                    Direction = authoring.direction,
                    Speed = authoring.speed,
                    Duration = authoring.duration
                });
            }
        }
    }
}