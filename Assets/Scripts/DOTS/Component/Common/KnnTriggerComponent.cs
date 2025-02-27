using DOTS.Authoring.Common;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DOTS.Component.Common
{
    /*public struct KnnTriggerConfigComponent : IComponentData
    {
        public bool Enable;
    }*/
    public struct KnnTriggerComponent : IComponentData,IEnableableComponent
    {
        public Entity Owner;
        public EFsmEventName TriggerUpdateEventName;
        public int OwnerKnnFlag;
        //public bool Disable;
        public float Radius;
        public Rect Rect;
        public float3 Offset;
        public float Angle;
        public EKnnShape Shape;
        
        public float UpdateInterval;
        public float UpdateTimer;
    }
}