using System.Runtime.InteropServices;
using DOTS.BufferElement;
using DOTS.Component.Combat;
using DOTS.Component.Common;
using DOTS.Component.Font;
using DOTS.System.Combat.Skill;
using DOTS.SystemGroup;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace DOTS.System.Combat
{
    [UpdateInGroup(typeof(SkillHandleSystemGroup))][UpdateBefore(typeof(SkillBaseSystem))]
    public partial struct FsmCombatDamageSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RandomComponent>();
            state.RequireForUpdate<PrefabManagerComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var entityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            var prefabManagerComponent = SystemAPI.GetSingleton<PrefabManagerComponent>();
            //var fromIndex = Random.CreateFromIndex((uint)(SystemAPI.Time.ElapsedTime * 1000 % uint.MaxValue));
            var randomComponent = SystemAPI.GetSingletonRW<RandomComponent>();
            var random = randomComponent.ValueRW.GetRandom();
            new FsmCombatDamageHandleSystemJob
            {
                Ecb = entityCommandBuffer.AsParallelWriter(),
                PrefabManager = prefabManagerComponent,
                Random = random
            }.ScheduleParallel(state.Dependency).Complete();
            entityCommandBuffer.Playback(state.EntityManager);
            entityCommandBuffer.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        [BurstCompile]
        [StructLayout(LayoutKind.Auto)]
        public partial struct FsmCombatDamageHandleSystemJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public PrefabManagerComponent PrefabManager;
            public Random Random;
            private void Execute([EntityIndexInQuery]int index,Entity entity,ref CombatComponent combatComponent
                ,ref DynamicBuffer<CombatDamageBufferElement> damageBuffer,in LocalTransform localTransform)
            {
                if (combatComponent.IsDead || combatComponent.Dodging)
                {
                    damageBuffer.Clear();
                    return;
                }

                var burningDamage = false;
                //var totalDamage = 0;
                foreach (var combatDamageBufferElement in damageBuffer)
                {
                    if (combatDamageBufferElement.DamageType == EDamageType.Fire)
                    {
                        burningDamage = true;
                    }
                    combatComponent.CurHealth -= combatDamageBufferElement.Damage;
                    //totalDamage+= combatDamageBufferElement.Damage;
                    InstantiateBlood(index,combatDamageBufferElement.DamageType,localTransform);
                    
                    var damageLabelEntity = Ecb.CreateEntity(index);
                    Ecb.AddComponent(index,damageLabelEntity,new DamageLabelRequestComponent
                    {
                        Damage = combatDamageBufferElement.Damage,
                        Position = localTransform.Position + new float3(0,0.5f,0),
                    });
                    
                    Ecb.SetComponentEnabled<DamageFlashComponent>(index,entity,true);
                    Ecb.SetComponent(index,entity,new DamageFlashComponent
                    {
                        Duration = 0.2f
                    });
                }

                /*if (totalDamage > 0)
                {
                    
                }*/
                if (combatComponent.IsDead && burningDamage)
                {
                    combatComponent.BurningDeath = true;
                }
                damageBuffer.Clear();
            }
            private void InstantiateBlood(int index,EDamageType damageType,in LocalTransform localTransform)
            {
                var newLocalTransform = localTransform;
                Entity instantiate;
                var position = localTransform.Position;
                switch (damageType)
                {
                    case EDamageType.Gun:
                        instantiate = Ecb.Instantiate(index,PrefabManager.BloodShot);
                        var nextFloat = Random.NextFloat(0.1f,0.5f);
                        var localTransformPosition = position;
                        localTransformPosition.y += nextFloat;
                        Ecb.SetComponent(index,instantiate,LocalTransform.FromPosition(localTransformPosition));
                        break;
                    case EDamageType.Melee:
                        instantiate = Ecb.Instantiate(index,PrefabManager.BloodCommon);
                        var randomOffsetX = Random.NextFloat(-0.15f,0.15f);
                        var randomOffsetY = Random.NextFloat(-0.06f, 0.06f);
                        Ecb.SetComponent(index,instantiate,newLocalTransform.WithPosition(position + new float3(randomOffsetX,randomOffsetY,0)));
                        break;
                    case EDamageType.Crit:
                        instantiate = Ecb.Instantiate(index,PrefabManager.BloodSplash);
                        Ecb.SetComponent(index,instantiate,localTransform);
                        var critSmack = Ecb.Instantiate(index,PrefabManager.CritSmack);
                        Ecb.SetComponent(index,critSmack,newLocalTransform.WithPosition(position + new float3(0,0.5f,0)));
                        break;
                    case EDamageType.Fire:
                        instantiate = Ecb.Instantiate(index,PrefabManager.BloodFire);
                        var transformPosition = newLocalTransform.Position;
                        transformPosition.y += 0.2f;
                        Ecb.SetComponent(index,instantiate,newLocalTransform.WithPosition(transformPosition));
                        break;
                    case EDamageType.Smash:
                        instantiate = Ecb.Instantiate(index,PrefabManager.BloodSmallExplosion);
                        Ecb.SetComponent(index,instantiate,localTransform);
                        var critTwack = Ecb.Instantiate(index,PrefabManager.CritTwack);
                        Ecb.SetComponent(index,critTwack,newLocalTransform.WithPosition(position + new float3(0,0.5f,0)));
                        break;
                }
            }
        }
    }
}