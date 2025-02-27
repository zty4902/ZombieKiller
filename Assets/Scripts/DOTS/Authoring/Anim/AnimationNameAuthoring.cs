using DOTS.Component.Anim;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring.Anim
{
    public class AnimationNameAuthoring : MonoBehaviour
    {
        private class AnimationNameAuthoringBaker : Baker<AnimationNameAuthoring>
        {
            public override void Bake(AnimationNameAuthoring authoring)
            {
                
                AddComponent(GetEntity(TransformUsageFlags.None),GetAnimationNameComponent());
            }
        }

        private static AnimationNameComponent GetAnimationNameComponent()
        {
            var animationNameComponent = new AnimationNameComponent
            {
                Melee = Animator.StringToHash(nameof(EAnimationName.Melee)),
                Melee2 = Animator.StringToHash(nameof(EAnimationName.Melee2)),
                Melee3 = Animator.StringToHash(nameof(EAnimationName.Melee3)),
                MeleeS = Animator.StringToHash(nameof(EAnimationName.MeleeS)),
                MeleeCrit = Animator.StringToHash(nameof(EAnimationName.MeleeCrit)),
                Fire = Animator.StringToHash(nameof(EAnimationName.Fire)),
                Aim = Animator.StringToHash(nameof(EAnimationName.Aim)),
                Move = Animator.StringToHash(nameof(EAnimationName.Move)),
                MoveS = Animator.StringToHash(nameof(EAnimationName.MoveS)),
                Death = Animator.StringToHash(nameof(EAnimationName.Death)),
                AfterDeath = Animator.StringToHash(nameof(EAnimationName.AfterDeath)),
                Idle = Animator.StringToHash(nameof(EAnimationName.Idle)),
                Idle2 = Animator.StringToHash(nameof(EAnimationName.Idle2)),
                Dodge = Animator.StringToHash(nameof(EAnimationName.Dodge)),
                Reload = Animator.StringToHash(nameof(EAnimationName.Reload)),
                Reload2 = Animator.StringToHash(nameof(EAnimationName.Reload2)),
                Super = Animator.StringToHash(nameof(EAnimationName.Super)),
                Special = Animator.StringToHash(nameof(EAnimationName.Special)),
                Disarmament = Animator.StringToHash(nameof(EAnimationName.Disarmament)),
                AfterDeath2 = Animator.StringToHash(nameof(EAnimationName.AfterDeath2)),
                Death2 = Animator.StringToHash(nameof(EAnimationName.Death2)),
                Move2 = Animator.StringToHash(nameof(EAnimationName.Move2)),
            };
            return animationNameComponent;
        }
    }
}