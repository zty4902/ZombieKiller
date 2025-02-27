using DOTS.Component.Anim;
using NSprites;
using Unity.Entities;
using Unity.Mathematics;

namespace DOTS.Aspect
{
    public readonly partial struct CustomAnimatorAspect : IAspect
    {
        public readonly AnimatorAspect AnimatorAspect;
        private readonly RefRW<Scale2D> _scale2D;
        private readonly RefRO<AnimationConfigComponent> _animationConfigComponent;
        public void SetAnimationWithScale(int animationName,double worldTime)
        {
            ref var blobArray = ref _animationConfigComponent.ValueRO.AnimationConfigArray.Value;
            float2 scale = default;
            for (var i = 0; i < blobArray.Length; i++)
            {
                var animationConfig = blobArray[i];
                var animationConfigAnimationIndex = animationConfig.AnimationIndex;
                if (animationConfigAnimationIndex == animationName)
                {
                    scale = animationConfig.Scale;
                }
            }
            if (scale is { x: >= 0, y: >= 0 })
            {
                _scale2D.ValueRW.value = scale;
            }
            AnimatorAspect.SetAnimation(animationName,worldTime);
        }
    }
}