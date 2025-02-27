using DOTS.Component.Combat.Bullet;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DOTS.Authoring.Combat.Bullet
{
    public class BulletFlyAnimAuthoring : MonoBehaviour
    {
        public float speed = 10f;
        public float3 direction = 0f;
        private class BulletFlyAnimAuthoringBaker : Baker<BulletFlyAnimAuthoring>
        {
            public override void Bake(BulletFlyAnimAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity,new BulletFlyAnimComponent
                {
                    Speed = authoring.speed,
                    Direction = authoring.direction
                });
            }
        }
    }
}