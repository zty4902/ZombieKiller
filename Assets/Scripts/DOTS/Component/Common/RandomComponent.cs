using Unity.Entities;
using Unity.Mathematics;

namespace DOTS.Component.Common
{
    public struct RandomComponent : IComponentData
    {
        private uint _seed;

        public void SetSeed(uint seed)
        {
            _seed = seed;
        }
        public Random GetRandom()
        {
            _seed = (_seed + 1) % uint.MaxValue;
            return Random.CreateFromIndex(_seed);
        }
    }
}