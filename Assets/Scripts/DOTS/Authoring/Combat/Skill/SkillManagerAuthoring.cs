using DOTS.Component.Combat;
using DOTS.Component.Combat.Skill;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring.Combat.Skill
{
    public class SkillManagerAuthoring : MonoBehaviour
    {
        public GameObject skill1;
        public float skill1Cd;
        public float skill1CdTimer;
        public bool holderSkill1;
        [ShowIf("$holderSkill1")]
        public float holderSkillInterval;
        private class SkillBaseAuthoringBaker : Baker<SkillManagerAuthoring>
        {
            public override void Bake(SkillManagerAuthoring managerAuthoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var skillBaseComponent = new SkillManagerComponent
                {
                    Skill1 = new SkillItem
                    {
                        SkillEntity = GetEntity(managerAuthoring.skill1, TransformUsageFlags.Dynamic),
                        SkillCd = managerAuthoring.skill1Cd,
                        CurrentSkillCd = managerAuthoring.skill1CdTimer,
                        HolderSkill = managerAuthoring.holderSkill1,
                        HolderSkillInterval = managerAuthoring.holderSkillInterval
                    }

                };
                AddComponent(entity,skillBaseComponent);
            }
        }
    }
}