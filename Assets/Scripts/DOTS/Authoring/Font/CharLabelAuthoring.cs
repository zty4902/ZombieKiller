using DOTS.Component.Font;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace DOTS.Authoring.Font
{
    public class CharLabelAuthoring : MonoBehaviour
    {
        public char character;
        public Material material;
        public Mesh mesh;
        public Color color;
        private class CharLabelAuthoringBaker : Baker<CharLabelAuthoring>
        {
            public override void Bake(CharLabelAuthoring authoring)
            {
                var charLabelComponent = new CharLabelComponent
                {
                    Character = authoring.character,
                    //FontSize = 1
                };

                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent(entity, charLabelComponent);
                
                var renderMeshArray = new RenderMeshArray(new[] { authoring.material }, new[] { authoring.mesh });
                AddSharedComponentManaged(entity,renderMeshArray);
                AddComponent(entity,new CustomColorData
                {
                    Value = new float4(authoring.color.r, authoring.color.g, authoring.color.b, authoring.color.a)
                });
            }
        }
    }
}