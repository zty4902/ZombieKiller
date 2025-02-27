using DOTS.BufferElement;
using DOTS.Component.Combat.Skill;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring.Combat.Skill
{
    public class SkillBaseAuthoring : MonoBehaviour
    {
        public GameObject owner;
        public bool addKnnTriggerBufferElement = true;
        private class SkillBaseComponentBaker : Baker<SkillBaseAuthoring>
        {
            public override void Bake(SkillBaseAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                if (authoring.addKnnTriggerBufferElement)
                {
                    AddBuffer<KnnTriggerBufferElement>(entity);
                }
                var skillBaseComponent = new SkillBaseComponent();
                if (authoring.owner)
                {
                    skillBaseComponent.Owner = GetEntity(authoring.owner, TransformUsageFlags.Dynamic);
                }
                AddComponent(entity,skillBaseComponent);
            }
        }
    }
}