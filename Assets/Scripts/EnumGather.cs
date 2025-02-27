using System;

public enum EFsmStateName
{
    Create,//创建
    Search,//更新最近敌人
    Dodge,//闪避
    DodgeOver,//闪避结束
    FireAttack,//远程攻击
    MeleeAttack,//近战
    Reload,//装弹
    Dying,//死亡
    AfterDeath,//死亡后
    Destroy,//销毁
    Aim,//远程瞄准
    MeleeAim,//近战瞄准
    Pause,//硬直
    Skill1,//技能1
    Idle,//待机
    BurningDeath,//燃烧死亡
    Disarmament,//解除装甲
    DisarmamentOver,//解除装甲结束
}
[Flags]
public enum EFsmEventName
{
    None = 0,
    EnemyInFireRange = 1,//敌人在远程攻击范围内
    EnemyInMeleeRange = 1<<1,//敌人在近战攻击范围内
    Dying = 1<<2,//死亡
    Skill1Ready = 1<<3,//技能1准备就绪
    EnemyInSkill1Range = 1<<4,//敌人在技能1瞄准范围内
}
[Flags]
public enum EFsmBufferEventName
{
    None = 0,
    Pause = 1,//硬直
    Burning = 1<<1,//火烧
}
public enum EAnimationName
{
    Idle,
    Idle2,
    AfterDeath,
    AfterDeath2,
    Death,
    Death2,
    Move,
    Move2,
    MoveS,
    Aim,
    Dodge,
    Fire,
    Melee,
    Melee2,
    Melee3,
    MeleeS,
    MeleeCrit,
    Reload,
    Reload2,
    Super,
    Special,
    Disarmament,
    Spawn,
}

public enum EPrefabName
{
    AbbyZ,
    JudiH,
    NakedH,
    NakedZ,
    BoosZ,
    FlameThrowerH,
    BoomerZ,
    MechanicH,
    AbbyH,
    ForestFireFighterZ,
    CopH,
}

/// <summary>
/// 弹幕消息类型
/// </summary>
public enum EPackMsgType
{
    //[Description("无")]
    无 = 0,       
    //[Description("消息")] 
    弹幕消息 = 1,
    //[Description("点赞")]
    点赞消息 = 2,
    //[Description("进房")]
    进直播间 = 3,
    //[Description("关注")]
    关注消息 = 4,
    //[Description("礼物")]
    礼物消息 = 5,
    //[Description("统计")]
    直播间统计 = 6,
    //[Description("粉团")]
    粉丝团消息 = 7,
    //[Description("分享")]
    直播间分享 = 8,
    //[Description("下播")]
    下播 = 9
}