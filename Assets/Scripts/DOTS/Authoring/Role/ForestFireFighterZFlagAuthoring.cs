using DOTS.Component.Role;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring.Role
{
    public class ForestFireFighterZFlagAuthoring : MonoBehaviour
    {
        public GameObject skill1Target;
        private class ForestFireFighterZFlagAuthoringBaker : Baker<ForestFireFighterZFlagAuthoring>
        {
            public override void Bake(ForestFireFighterZFlagAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var forestFireFighterZFlagComponent = new ForestFireFighterZFlagComponent();
                if (authoring.skill1Target!= null)
                {
                    forestFireFighterZFlagComponent.Skill1Target =
                        GetEntity(authoring.skill1Target, TransformUsageFlags.None);
                }
                AddComponent(entity,forestFireFighterZFlagComponent);
            }
        }
    }
}