using DOTS.Component.Combat;
using DOTS.Component.Combat.Skill;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring.Combat.Skill
{
    public class SkillAnimationAuthoring : MonoBehaviour
    {
        public GameObject animationPrefab;
        private class SkillAnimationAuthoringBaker : Baker<SkillAnimationAuthoring>
        {
            public override void Bake(SkillAnimationAuthoring authoring)
            {
                if (!authoring.animationPrefab)
                {
                    return;
                }
                var entity = GetEntity(TransformUsageFlags.None);
                var skillAnimationComponent = new SkillAnimationComponent
                {
                    AnimationEntity = GetEntity(authoring.animationPrefab,TransformUsageFlags.Dynamic)
                };
                AddComponent(entity,skillAnimationComponent);
            }
        }
    }
}