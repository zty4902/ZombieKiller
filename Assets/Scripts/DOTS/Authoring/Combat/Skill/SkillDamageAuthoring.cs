using DOTS.BufferElement;
using DOTS.Component.Combat.Skill;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring.Combat.Skill
{
    public class SkillDamageAuthoring : MonoBehaviour
    {
        public int damage;
        public EDamageType damageType;
        
        private class SkillDamageAuthoringBaker : Baker<SkillDamageAuthoring>
        {
            public override void Bake(SkillDamageAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity,new SkillDamageComponent
                {
                    Damage = authoring.damage,
                    DamageType = authoring.damageType,
                });
            }
        }
    }
}