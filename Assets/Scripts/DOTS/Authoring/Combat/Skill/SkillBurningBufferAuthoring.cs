using DOTS.Component.Combat.Skill;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring.Combat.Skill
{
    public class SkillBurningBufferAuthoring : MonoBehaviour
    {
        public int burningDamage;
        public float burningDuration;
        private class SkillBurningBufferAuthoringBaker : Baker<SkillBurningBufferAuthoring>
        {
            public override void Bake(SkillBurningBufferAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity,new SkillBurningBufferComponent
                {
                    BurningDamage = authoring.burningDamage,
                    BurningDuration = authoring.burningDuration
                });
            }
        }
    }
}