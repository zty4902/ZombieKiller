using DOTS.Component.Combat.Skill.TargetFilter;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring.Combat.Skill.TargetFilter
{
    public class XClosestFilterAuthoring : MonoBehaviour
    {
        private class SkillTargetXClosestFilterBaker : Baker<XClosestFilterAuthoring>
        {
            public override void Bake(XClosestFilterAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity,new XClosestFilterComponent());
            }
        }
    }
}