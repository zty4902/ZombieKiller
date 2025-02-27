using DOTS.Authoring.Combat;
using DOTS.Component.FSM;
using DOTS.Component.Role;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring.Role
{
    public class JuDiHFlagAuthoring : MonoBehaviour
    {
        private class JuDiHFlagAuthoringBaker : Baker<JuDiHFlagAuthoring>
        {
            public override void Bake(JuDiHFlagAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var juDiFlagComponent = new JuDiHFlagComponent();
                AddComponent(entity,juDiFlagComponent);
            }
        }
    }
}