using DOTS.BufferElement;
using DOTS.Component.Combat;
using ProjectDawn.Navigation;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DOTS.Component.FSM
{
    public struct FsmNavigationState
    {
        public float3 Destination;
        public float3 TargetPosition;
        public bool Disable;
    }
    public struct FsmAnimationState
    {
        /// <summary>
        /// 当前动画
        /// </summary>
        public int LastAnim;
        /// <summary>
        /// 上一个动画，自动维护
        /// </summary>
        public int CurAnim;
        public bool ResetAnim;
    }

    public struct FsmFlashMoveState
    {
        /// <summary>
        /// 偏移量
        /// </summary>
        public float3 MoveOffset;
    }

    public struct FsmCombatDamage
    {
        public EDamageType DamageType;
        public int Damage;
        /// <summary>
        /// 霰弹枪有多发子弹
        /// </summary>
        public int DamageCount;
    }
    public struct FsmCombatState
    {
        public Entity NearestEnemy;
        public CombatComponent NearestEnemyCombat;
        public Entity CurAimedTarget;
        public LocalTransform NearestEnemyTransform;
        
        //public bool FireAttack;
        public bool AimedEnemyAttack;
        public FsmCombatDamage CurDamage;
        /// <summary>
        /// 技能攻击目标
        /// </summary>
        public Entity SkillAttackTarget;
        public bool Skill1Attack;
        public bool ClearSkillTargetFlag;

        public void ReleaseSkill1(Entity target)
        {
            Skill1Attack = true;
            SkillAttackTarget = target;
        }

        public void AttackAimedEnemy(FsmCombatDamage damage)
        {
            AimedEnemyAttack = true;
            CurDamage = damage;
        }
        public void StopSkill1()
        {
            Skill1Attack = false;
            ClearSkillTargetFlag = true;
        }
    }

    public struct FsmBuffState
    {
        //燃烧buff
        public float BurningDuration;
        public float BurningTimer;
        public int BurningDamage;
        //public Entity BuffFireEntity;
    }
    public struct FsmStateComponent : IComponentData
    {
        //状态机
        //public EFsmStateName LastFsmStateName;
        public EFsmStateName CurFsmStateName;
        public EFsmEventName CurFsmEvent;
        public EFsmBufferEventName CurFsmBufferEvent;
        public float WaitTime;
        //动画
        public FsmAnimationState Animation;
        //瞬移
        public FsmFlashMoveState FlashMove;
        //战斗
        public FsmCombatState Combat;
        //移动
        public FsmNavigationState Navigation;
        //Buff
        public FsmBuffState Buff;
        
    }
}