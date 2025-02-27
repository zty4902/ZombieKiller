using DOTS.BufferElement;
using DOTS.Component.Anim;
using DOTS.Component.Combat;
using DOTS.Component.FSM;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DOTS.Aspect
{
    public readonly partial struct FsmCommonAspect : IAspect
    {
        public readonly RefRW<FsmStateComponent> FsmStateComponent;
        public readonly RefRO<LocalTransform> LocalTransform;
        public readonly RefRW<CombatComponent> CombatComponent;
        public readonly RefRO<AnimationConfigComponent> AnimationConfigComponent;
        
        public void SwitchState(in EFsmStateName curFsmStateName,in float waitTime)
        {
            //fsmStateComponent.LastFsmStateName = lastFsmStateName;
            FsmStateComponent.ValueRW.CurFsmStateName = curFsmStateName;
            FsmStateComponent.ValueRW.WaitTime = waitTime;
        }
        /// <summary>
        /// 没有敌人则等待，有敌人则导航到可攻击位置
        /// </summary>
        /// <param name="animationNameComponent"></param>
        /// <param name="isFirer"></param>
        public bool NormalSearch(in AnimationNameComponent animationNameComponent,bool isFirer)
        {
            if (FsmStateComponent.ValueRO.Combat.NearestEnemy == Entity.Null || FsmStateComponent.ValueRO.Combat.NearestEnemyCombat.IsDead)
            {
                FsmStateComponent.ValueRW.Animation.CurAnim = animationNameComponent.Idle;
                //FsmUtil.SwitchState(ref FsmStateComponent.ValueRW,EFsmStateName.Search, 0.5f);
                SwitchState(EFsmStateName.Search, 0.5f);
                return false;
            }
            var nearestEnemyPos = FsmStateComponent.ValueRO.Combat.NearestEnemyTransform.Position;

            if (isFirer)
            {
                var attackPoint = GetFireAttackPoint(nearestEnemyPos);
                FsmStateComponent.ValueRW.Navigation.Destination = attackPoint;
            }
            else
            {
                FsmStateComponent.ValueRW.Navigation.Destination = nearestEnemyPos;
            }

            FsmStateComponent.ValueRW.Navigation.TargetPosition =
                nearestEnemyPos;
                        
            FsmStateComponent.ValueRW.Animation.CurAnim = animationNameComponent.Move;
            SwitchState(EFsmStateName.Search, 0.5f);
            return true;
            //FsmUtil.SwitchState(ref FsmStateComponent.ValueRW,EFsmStateName.Search, 0.5f);
        }

        private float3 GetFireAttackPoint(float3 enemyPos)
        {
            var playerPos = LocalTransform.ValueRO.Position;
            var dir = enemyPos.x - playerPos.x;
            var xDist = math.abs(dir);
            var sign = math.sign(dir);
            enemyPos.x -= sign * math.min(CombatComponent.ValueRO.FireRange - 0.15f,xDist);
            return enemyPos;
        }

        public bool HasFsmEventFlag(EFsmEventName eFlag)
        {
            var eFlags = FsmStateComponent.ValueRO.CurFsmEvent;
            return (eFlags & eFlag) != 0;
        }
        public bool HasFsmBufferEventFlag(EFsmBufferEventName eFlag)
        {
            var eFlags = FsmStateComponent.ValueRO.CurFsmBufferEvent;
            return (eFlags & eFlag) != 0;
        }
        public void RemoveFsmBufferEventFlag(EFsmBufferEventName eFlag)
        {
            var eFlags = FsmStateComponent.ValueRW.CurFsmBufferEvent;
            eFlags &= ~eFlag;
            FsmStateComponent.ValueRW.CurFsmBufferEvent = eFlags;
        }
        public void NormalDying(in AnimationNameComponent animationNameComponent)
        {
            if (CombatComponent.ValueRO.BurningDeath)
            {
                SwitchState(EFsmStateName.BurningDeath,0);
            }
            else
            {
                FsmStateComponent.ValueRW.Animation.CurAnim = animationNameComponent.Death;
                FsmStateComponent.ValueRW.Navigation.Disable = true;
                SwitchState(EFsmStateName.AfterDeath,3);
            }

        }
        /// <summary>
        /// 恢复子弹数量，播放装弹动画
        /// </summary>
        /// <param name="animationNameComponent"></param>
        public void NormalReload(in AnimationNameComponent animationNameComponent)
        {
            CombatComponent.ValueRW.CurFireCount = 0;
            FsmStateComponent.ValueRW.Animation.CurAnim = animationNameComponent.Reload;
        }

        /// <summary>
        /// 播放枪击动画，造成伤害，子弹计数
        /// </summary>
        /// <param name="animationNameComponent"></param>
        /// <param name="damage"></param>
        public void NormalFireAttack(in AnimationNameComponent animationNameComponent,int damage)
        {
            ref var fsmStateComponent = ref FsmStateComponent.ValueRW;
            fsmStateComponent.Animation.ResetAnim = true;
            TryAttackAimedTarget(new FsmCombatDamage
            {
                DamageType = EDamageType.Gun,
                Damage = damage,
                DamageCount = 1
            });
            fsmStateComponent.Animation.CurAnim = animationNameComponent.Fire;
            CombatComponent.ValueRW.CurFireCount++;
        }
        /// <summary>
        /// 播放枪击动画，造成伤害，子弹计数
        /// </summary>
        /// <param name="animationNameComponent"></param>
        /// <param name="damage"></param>
        public void NormalFireAttack(in AnimationNameComponent animationNameComponent,FsmCombatDamage damage)
        {
            ref var fsmStateComponent = ref FsmStateComponent.ValueRW;
            fsmStateComponent.Animation.ResetAnim = true;
            TryAttackAimedTarget(damage);
            fsmStateComponent.Animation.CurAnim = animationNameComponent.Fire;
            CombatComponent.ValueRW.CurFireCount++;
        }
        /// <summary>
        /// 尝试近战攻击，成功则播放近战动画随后切换到近战攻击状态
        /// </summary>
        /// <param name="fallBackAnimation"></param>
        /// <param name="meleeAnimation"></param>
        /// <param name="meleeTime"></param>
        public bool NormalMeleeAim(int fallBackAnimation,int meleeAnimation,float meleeTime)
        {
            ref var fsmStateComponent = ref FsmStateComponent.ValueRW;
            if (fsmStateComponent.Combat.NearestEnemyCombat.Dodging || fsmStateComponent.Combat.NearestEnemyCombat.IsDead)
            {
                fsmStateComponent.Animation.CurAnim = fallBackAnimation;
                SwitchState(EFsmStateName.Search,0.4f);
                return false;
            }
            fsmStateComponent.Combat.CurAimedTarget = fsmStateComponent.Combat.NearestEnemy;
            fsmStateComponent.Animation.ResetAnim = true;
            fsmStateComponent.Animation.CurAnim = meleeAnimation;
            SwitchState(EFsmStateName.MeleeAttack,meleeTime);
            return true;
        }

        /// <summary>
        /// 子弹不足切换到装弹状态，否则随后切换到射击状态，播放瞄准动画
        /// </summary>
        /// <param name="animationNameComponent"></param>
        /// <param name="aimTime"></param>
        public void NormalAim(in AnimationNameComponent animationNameComponent,float aimTime)
        {
            var combatComponent = CombatComponent.ValueRO;
            
            if (combatComponent.CurFireCount >= combatComponent.MaxFireCount)
            {
                SwitchState(EFsmStateName.Reload,0f);
                return;
            }
            FsmStateComponent.ValueRW.Combat.CurAimedTarget = FsmStateComponent.ValueRO.Combat.NearestEnemy;
            FsmStateComponent.ValueRW.Animation.CurAnim = animationNameComponent.Aim;
            SwitchState(EFsmStateName.FireAttack, aimTime);

        }

        public float GetScaledAnimationTime(float animationTime)
        {
            return animationTime * AnimationConfigComponent.ValueRO.AnimScale;
        }

        public void NormalAfterDeath(in AnimationNameComponent animationNameComponent)
        {
            FsmStateComponent.ValueRW.Animation.CurAnim = animationNameComponent.AfterDeath;
            SwitchState(EFsmStateName.Destroy,6);
        }

        public Entity GetNearestAttackTarget()
        {
            var fsmCombatState = FsmStateComponent.ValueRO.Combat;
            if (fsmCombatState.CurAimedTarget == fsmCombatState.NearestEnemy)
            {
                return fsmCombatState.CurAimedTarget;
            }
            return Entity.Null;
        }

        public bool TryAttackAimedTarget(FsmCombatDamage fsmCombatDamage)
        {
            var nearestAttackTarget = GetNearestAttackTarget();
            if (nearestAttackTarget == Entity.Null)
            {
                return false;
            }

            FsmStateComponent.ValueRW.Combat.AttackAimedEnemy(fsmCombatDamage);
            return true;
        }
    }
}