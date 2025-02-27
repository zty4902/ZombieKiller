using DOTS.Component.Combat;
using DOTS.Component.Combat.Skill;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring.Combat.Skill
{
    public class SkillBeatOffAuthoring : MonoBehaviour
    {
        public float force = 0.5f;
        public float time = 0.3f;
        private class BeatOffAuthoringBaker : Baker<SkillBeatOffAuthoring>
        {
            public override void Bake(SkillBeatOffAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity,authoring.GetBeatOffComponent());
            }

        }
        public SkillBeatOffComponent GetBeatOffComponent()
        {
            return new SkillBeatOffComponent
            {
                Force = force,
                Time = time
            };
        }
    }
}