using DOTS.BufferElement;
using DOTS.Component.Combat;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring.Combat
{
    public class CombatAuthoring : MonoBehaviour
    {
        public int health = 100;
        public int damage = 10;
        public float fireRange = 3;
        //public float meleeRange = 0.4f;
        [InfoBox("可以空，仅在战斗模块需要访问时赋值",VisibleIf = "@meleeTrigger == null")]
        public GameObject meleeTrigger;
        public int maxFireCount = 5;
        private class CombatAuthoringBaker : Baker<CombatAuthoring>
        {
            public override void Bake(CombatAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var combatComponent = new CombatComponent
                {
                    CurHealth = authoring.health,
                    MaxHealth = authoring.health,
                    AttackDamage = authoring.damage,
                    FireRange = authoring.fireRange,
                    //MeleeRange = authoring.meleeRange,
                    MaxFireCount = authoring.maxFireCount,
                };
                if (authoring.meleeTrigger)
                {
                    combatComponent.MeleeTrigger = GetEntity(authoring.meleeTrigger, TransformUsageFlags.Dynamic);
                }
                AddComponent(entity,combatComponent);
                AddBuffer<CombatDamageBufferElement>(entity);
                AddComponent(entity,new DamageFlashComponent());
            }
        }
    }
}