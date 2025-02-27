using DOTS.Component.Common;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring.Common
{
    public class RandomAuthoring : MonoBehaviour
    {
        private class RandomAuthoringBaker : Baker<RandomAuthoring>
        {
            public override void Bake(RandomAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var randomComponent = new RandomComponent();
                randomComponent.SetSeed((uint)UnityEngine.Random.Range(uint.MinValue, uint.MaxValue));
                AddComponent(entity,randomComponent);
            }
        }
    }
}