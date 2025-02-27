using DOTS.Component.Combat.Bullet;
using DOTS.System.Combat.Bullet;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DOTS.Authoring.Combat.Bullet
{
    public class BulletSocketAuthoring : MonoBehaviour
    {
        public GameObject bulletPrefab;
        public float interval = 0.5f;
        public float3 offset;
        public float randomOffsetY = 0;
        private class BulletSocketAuthoringBaker : Baker<BulletSocketAuthoring>
        {
            public override void Bake(BulletSocketAuthoring authoring)
            {
                if (authoring.bulletPrefab == null)
                {
                    return;
                }
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var bulletSocketComponent = new BulletSocketComponent
                {
                    BulletEntity = GetEntity(authoring.bulletPrefab, TransformUsageFlags.Dynamic),
                    Interval = authoring.interval,
                    Offset = authoring.offset,
                    RandomOffsetY = authoring.randomOffsetY
                };
                AddComponent(entity,bulletSocketComponent);
            }
        }
    }
}