using DOTS.BufferElement;
using DOTS.Component.Combat;
using DOTS.Component.Combat.Bullet;
using DOTS.Component.Combat.Skill;
using DOTS.SystemGroup;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace DOTS.System.Combat.Skill
{
    [UpdateInGroup(typeof(SkillHandleSystemGroup))]
    public partial struct SkillGunFireSystem : ISystem
    {
        private ComponentLookup<CombatComponent> _combatLookup;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _combatLookup = state.GetComponentLookup<CombatComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var entityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            _combatLookup.Update(ref state);
            new SkillGunFireSystemJob
            {
                Ecb = entityCommandBuffer.AsParallelWriter(),
                DeltaTime = SystemAPI.Time.DeltaTime,
                CombatLookup = _combatLookup
            }.ScheduleParallel(state.Dependency).Complete();
            entityCommandBuffer.Playback(state.EntityManager);
            entityCommandBuffer.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        [BurstCompile]
        public partial struct SkillGunFireSystemJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public float DeltaTime;
            [ReadOnly]
            public ComponentLookup<CombatComponent> CombatLookup;

            private void Execute([EntityIndexInQuery]int index,ref SkillGunFireComponent skillGunFireComponent,in DynamicBuffer<KnnTriggerBufferElement> knnTriggerBuffer)
            {
                if (knnTriggerBuffer.Length == 0)
                {
                    if (skillGunFireComponent.IsFiring)
                    {
                        skillGunFireComponent.IsFiring = false;
                        Ecb.SetComponentEnabled<BulletSocketComponent>(index,skillGunFireComponent.GunSocketEntity,false);
                    }
                    return;
                }

                if (!skillGunFireComponent.IsFiring)
                {
                    skillGunFireComponent.IsFiring = true;
                    Ecb.SetComponentEnabled<BulletSocketComponent>(index,skillGunFireComponent.GunSocketEntity,true);
                }
                skillGunFireComponent.DamageTimer += DeltaTime;
                if (skillGunFireComponent.DamageTimer >= skillGunFireComponent.DamageInterval)
                {
                    skillGunFireComponent.DamageTimer = 0;
                    for (var i = 0; i < knnTriggerBuffer.Length; i++)
                    {
                        var entity = knnTriggerBuffer[i].Entity;
                        if (CombatLookup.TryGetComponent(entity,out var combatComponent) && !combatComponent.IsDead)
                        {
                            Ecb.AppendToBuffer(index,entity,new CombatDamageBufferElement
                            {
                                Damage = skillGunFireComponent.Damage,
                                DamageType = skillGunFireComponent.DamageType
                            });
                        }
                    }
                }
            }
        }
    }
}