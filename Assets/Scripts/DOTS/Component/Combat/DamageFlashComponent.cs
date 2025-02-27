using Unity.Entities;

namespace DOTS.Component.Combat
{
    public struct DamageFlashComponent : IComponentData,IEnableableComponent
    {
        public float Duration;
    }
}