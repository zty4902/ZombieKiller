using DOTS.BufferElement;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring.Combat.Skill
{
    public class SkillTargetAuthoring : MonoBehaviour
    {
        private class SkillTargetAuthoringBaker : Baker<SkillTargetAuthoring>
        {
            public override void Bake(SkillTargetAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddBuffer<KnnTriggerBufferElement>(entity);
            }
        }
    }
}