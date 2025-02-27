using Unity.Entities;

namespace DOTS.Component.Font
{
    public struct CharLabelComponent : IComponentData,IEnableableComponent
    {
        public char Character;
        //public int FontSize;
    }
}