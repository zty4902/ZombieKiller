using Unity.Entities;
using Unity.Mathematics;

namespace DOTS.Component.FSM
{
    /// <summary>
    /// 运行时保存临时数据
    /// </summary>
    public struct FsmStateRuntimeDataComponent : IComponentData
    {
        public Entity BuffFireEntity;
        // 用于渲染的颜色
        public float4 RenderColor;
    }
}