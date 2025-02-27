using System;
using DOTS.BufferElement;
using DOTS.Component.Spawn;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DOTS.Authoring.Spawn
{
    public class SpawnPointAuthoring : MonoBehaviour
    {
        public int flag;
        public int2 spawnCount;
        public float spawnInterval;
        public float space;
        private class SpawnPointAuthoringBaker : Baker<SpawnPointAuthoring>
        {
            public override void Bake(SpawnPointAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity,new SpawnPointComponent
                {
                    Flag = authoring.flag,
                    SpawnCount = authoring.spawnCount,
                    SpawnInterval = authoring.spawnInterval,
                    Space = authoring.space,
                    SpawnTimer = authoring.spawnInterval
                });
                AddBuffer<SpawnRequestBufferElement>(entity);
            }
        }

        private void OnDrawGizmos()
        {
            var pos = transform.position;
            var halfRange = new Vector3(spawnCount.x,spawnCount.y,0) * space/2;
            Gizmos.color = Color.green;
            var v1 = pos - halfRange;
            var v2 = pos + new Vector3(halfRange.x,-halfRange.y,0);
            var v3 = pos + halfRange;
            var v4 = pos + new Vector3(-halfRange.x,halfRange.y,0);
            Gizmos.DrawLine(v1, v2);
            Gizmos.DrawLine(v2, v3);
            Gizmos.DrawLine(v3, v4);
            Gizmos.DrawLine(v4, v1);
        }
    }
}