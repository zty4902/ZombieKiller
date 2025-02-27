using Unity.Entities;

namespace DOTS.Component.Common
{
    public struct AutoDestroyComponent : IComponentData,IEnableableComponent
    {
        public float DestroyTime;
    }
}