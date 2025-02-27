using DOTS.BufferElement;
using DOTS.Component.Combat.Skill;
using DOTS.System.Combat.Skill;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring.Combat.Skill
{
    [UpdateBefore(typeof(SkillBaseSystem))]
    public class SkillGunFireAuthoring : MonoBehaviour
    {
        public GameObject gunSocketObject;
        public float damageInterval;
        public int damage;
        public EDamageType damageType;
        private class SkillGunFireAuthoringBaker : Baker<SkillGunFireAuthoring>
        {
            public override void Bake(SkillGunFireAuthoring authoring)
            {
                if (authoring.gunSocketObject == null)
                {
                    return;
                }

                var skillGunFireComponent = new SkillGunFireComponent
                {
                    GunSocketEntity = GetEntity(authoring.gunSocketObject, TransformUsageFlags.None),
                    IsFiring = true,
                    DamageInterval = authoring.damageInterval,
                    Damage = authoring.damage,
                    DamageType = authoring.damageType
                };
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, skillGunFireComponent);
            }
        }
    }
}