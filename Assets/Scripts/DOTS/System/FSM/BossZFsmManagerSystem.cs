using DOTS.Aspect;
using DOTS.Component.Anim;
using DOTS.Component.Role;
using DOTS.SystemGroup;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace DOTS.System.FSM
{
    [UpdateInGroup(typeof(FsmSystemGroup))]
    public partial struct BossZFsmManagerSystem : ISystem
    {
        private class SystemData : IComponentData
        {
            private CameraShake _cameraShake;

            public CameraShake CameraShake
            {
                get
                {
                    if (_cameraShake == null)
                    {
                        if (Camera.main != null) _cameraShake = Camera.main.transform.parent.GetComponent<CameraShake>();
                    }
                    return _cameraShake;
                }
            }
        }
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AnimationNameComponent>();
            state.EntityManager.AddComponentObject(state.SystemHandle, new SystemData());
        }

        public void OnUpdate(ref SystemState state)
        {
            var animationNameComponent = SystemAPI.GetSingleton<AnimationNameComponent>();
            var cameraShake = state.EntityManager.GetComponentObject<SystemData>(state.SystemHandle).CameraShake;
            
            foreach (var (_, fsmCommonAspect) in SystemAPI.Query<RefRO<BossZFlagComponent>, FsmCommonAspect>())
            {
                ref var fsmStateComponent = ref fsmCommonAspect.FsmStateComponent.ValueRW;
                if (!fsmCommonAspect.CombatComponent.ValueRO.IsDead)
                {
                    if (fsmStateComponent.WaitTime > 0)
                    {
                        return;
                    }

                    if (fsmCommonAspect.HasFsmEventFlag(EFsmEventName.EnemyInMeleeRange)
                        && fsmCommonAspect.HasFsmEventFlag(EFsmEventName.Skill1Ready)
                        && fsmStateComponent.CurFsmStateName is EFsmStateName.Search)
                    {
                        fsmCommonAspect.SwitchState(EFsmStateName.MeleeAim,0);
                    }
                }

                switch (fsmStateComponent.CurFsmStateName)
                {
                    case EFsmStateName.Search:
                        fsmCommonAspect.NormalSearch(in animationNameComponent,false);
                        break;
                    case EFsmStateName.MeleeAim:
                        fsmStateComponent.Animation.CurAnim = animationNameComponent.Melee;
                        fsmCommonAspect.SwitchState(EFsmStateName.MeleeAttack,fsmCommonAspect.GetScaledAnimationTime(1.8f));
                        break;
                    case EFsmStateName.MeleeAttack:
                        cameraShake.Shake(0.5f, 0.08f);
                        /*fsmStateComponent.Combat.Skill1Attack = true;
                        fsmStateComponent.Combat.SkillAttackTarget =
                            fsmCommonAspect.CombatComponent.ValueRO.MeleeTrigger;*/
                        fsmStateComponent.Combat.ReleaseSkill1(fsmCommonAspect.CombatComponent.ValueRO.MeleeTrigger);
                        fsmCommonAspect.SwitchState(EFsmStateName.Search,fsmCommonAspect.GetScaledAnimationTime(0.6f));
                        break;
                }
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}