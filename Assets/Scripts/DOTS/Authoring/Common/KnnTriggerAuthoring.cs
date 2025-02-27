using DOTS.BufferElement;
using DOTS.Component.Common;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring.Common
{
    public enum EKnnShape
    {
        Fanned,
        Rect
    }
    public class KnnTriggerAuthoring : MonoBehaviour
    {
        public GameObject owner;
        public EFsmEventName fsmEvent;
        public EKnnShape shape;
        public float radius = 0.1f;
        [ShowIf("@shape == EKnnShape.Fanned")]
        public int angle = 180;
        [ShowIf("@shape == EKnnShape.Rect")]
        public Rect rect;
        public Vector3 offset;//偏移，固定方向不随实体翻转而改变
        
        public float updateInterval = 0.2f;
        private class KnnTriggerAuthoringBaker : Baker<KnnTriggerAuthoring>
        {
            public override void Bake(KnnTriggerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddBuffer<KnnTriggerBufferElement>(entity);
                var knnTriggerComponent = new KnnTriggerComponent
                {
                    Radius = authoring.radius,
                    Angle = authoring.angle,
                    Offset = authoring.offset,
                    Shape = authoring.shape,
                    Rect = authoring.rect,
                    UpdateInterval = authoring.updateInterval,
                    UpdateTimer = authoring.updateInterval
                };
                if (authoring.owner)
                {
                    var knnFlagAuthoring = authoring.owner.GetComponent<KnnFlagAuthoring>();
                    if (knnFlagAuthoring)
                    {
                        knnTriggerComponent.OwnerKnnFlag = knnFlagAuthoring.flag;
                        knnTriggerComponent.Owner = GetEntity(authoring.owner, TransformUsageFlags.None);
                        knnTriggerComponent.TriggerUpdateEventName = authoring.fsmEvent;
                    }
                }
                AddComponent(entity,knnTriggerComponent);
                //AddComponent(entity,new KnnTriggerConfigComponent());
            }
        }

        //根据rect画出KNN触发器
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            var trans = transform;
            var position = trans.position + offset;
            Gizmos.DrawWireSphere(position, radius);
            if (shape == EKnnShape.Fanned)
            {
                //画一个圆锥
                Gizmos.color = Color.green;
                var right = transform.right;
                var v1 = Quaternion.Euler(0, 0, angle / 2f) * right;
                var v2 = Quaternion.Euler(0, 0, -angle / 2f) * right;
                Gizmos.DrawLine(position, position + v1 * radius);
                Gizmos.DrawLine(position, position + v2 * radius);
            }else if (shape == EKnnShape.Rect)
            {
                //画一个矩形
                Gizmos.color = Color.green;
                var drawRect = transform.rotation.y == 0 ? 
                    new Rect(rect.x + position.x, rect.y + position.y, rect.width, rect.height) 
                    : new Rect(position.x - rect.x - rect.width, rect.y + position.y, rect.width, rect.height);
                var v1 = drawRect.position;
                var v2 = drawRect.position + new Vector2(rect.width, 0);
                var v3 = drawRect.position + new Vector2(rect.width, rect.height);
                var v4 = drawRect.position + new Vector2(0, rect.height);
                Gizmos.DrawLine(v1, v2);
                Gizmos.DrawLine(v2, v3);
                Gizmos.DrawLine(v3, v4);
                Gizmos.DrawLine(v4, v1);
            }

            
        }
    }
}