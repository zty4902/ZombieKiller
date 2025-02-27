using Unity.Collections;
using Unity.Entities;

namespace DOTS.Component.Font
{
    public struct TextLabelComponent : IComponentData,IEnableableComponent
    {
        public Entity ChildCharLabelEntity;
    }

    public struct TextLabelStringInfoComponent : IComponentData
    {
        public FixedString32Bytes Text;
        public int ColorIndex;
        public int FontSize;
    }
}