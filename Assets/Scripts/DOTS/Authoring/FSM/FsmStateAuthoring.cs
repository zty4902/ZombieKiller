using DOTS.Component.FSM;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DOTS.Authoring.FSM
{
    public class FsmStateAuthoring : MonoBehaviour
    {
        private class FsmStateAuthoringBaker : Baker<FsmStateAuthoring>
        {
            public override void Bake(FsmStateAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new FsmStateComponent());
                AddComponent(entity,new FsmStateRuntimeDataComponent
                {
                    RenderColor = new float4(1, 1, 1, 1)
                });
            }
        }
    }
}