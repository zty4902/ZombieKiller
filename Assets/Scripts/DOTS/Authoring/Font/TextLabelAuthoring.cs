using DOTS.Component.Font;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring.Font
{
    public class TextLabelAuthoring : MonoBehaviour
    {
        public GameObject childCharLabelGameObject;
        private class TextLabelAuthoringBaker : Baker<TextLabelAuthoring>
        {
            public override void Bake(TextLabelAuthoring authoring)
            {
                if (authoring.childCharLabelGameObject == null)
                {
                    return;
                }
                var entity = GetEntity(TransformUsageFlags.None);
                var textLabelComponent = new TextLabelComponent
                {
                    ChildCharLabelEntity = GetEntity(authoring.childCharLabelGameObject.transform,
                        TransformUsageFlags.None)
                };
                AddComponent(entity,textLabelComponent);
            }
        }
    }
}