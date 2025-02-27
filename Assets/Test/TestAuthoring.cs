using DOTS.Component;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring
{
    public class TestAuthoring : MonoBehaviour
    {
        public AnimData move;
        public AnimData afterDeath;
        public AnimData aim;
        public AnimData death;
        public AnimData dodge;
        private class TestAuthoringBaker : Baker<TestAuthoring>
        {
            public override void Bake(TestAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                authoring.move.animIndex = Animator.StringToHash("move");
                authoring.afterDeath.animIndex = Animator.StringToHash("afterDeath");
                authoring.aim.animIndex = Animator.StringToHash("aim");
                authoring.death.animIndex = Animator.StringToHash("death");
                authoring.dodge.animIndex = Animator.StringToHash("dodge");
                
                var testComponentData = new TestComponentData()
                {
                    Move = authoring.move,
                    AfterDeath = authoring.afterDeath,
                    Aim = authoring.aim,
                    Death = authoring.death,
                    Dodge = authoring.dodge
                };
                
                AddComponent(entity,testComponentData);
            }
        }
    }
}