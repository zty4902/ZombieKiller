using System;
using Unity.Entities;

namespace DOTS.Component.Common
{
    public struct KnnFlagComponent : ISharedComponentData,IEquatable<KnnFlagComponent>
    {
        public int Flag;

        public bool Equals(KnnFlagComponent other)
        {
            return Flag == other.Flag;
        }

        public override bool Equals(object obj)
        {
            return obj is KnnFlagComponent other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Flag;
        }
    }
}