using System.Collections.Generic;
using System.Runtime.InteropServices;
using DOTS.Authoring.Common;
using DOTS.BufferElement;
using DOTS.Component.Combat;
using DOTS.Component.Common;
using DOTS.Component.FSM;
using DOTS.Component.Player;
using DOTS.SystemGroup;
using JetBrains.Annotations;
using KNN;
using KNN.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace DOTS.System.FSM.Handle
{
    public class KnnSystemData : IComponentData
    {
        public readonly Dictionary<int,List<Entity>> FlagBearers = new();
    }
    [StructLayout(LayoutKind.Auto)]
    [UpdateInGroup(typeof(FsmHandlerSystemGroup))]
    public partial struct FsmNearestEnemyHandleSystem : ISystem
    {
        private ComponentLookup<FsmStateComponent> _fsmStateLookup;
        private ComponentLookup<LocalTransform> _localTransformLookup;
        private ComponentLookup<CombatComponent> _combatLookup;
        private float _updateKnnTimer;
        private const float UpdateKnnInterval = 0.2f;
        private NativeHashSet<int> _handledPlayerIds;
        
        private int _updateIndex;
        public void OnCreate(ref SystemState state)
        {
            _fsmStateLookup = state.GetComponentLookup<FsmStateComponent>();
            _combatLookup = state.GetComponentLookup<CombatComponent>();
            _localTransformLookup = state.GetComponentLookup<LocalTransform>();
            _handledPlayerIds = new NativeHashSet<int>(100, Allocator.Persistent);

            var knnSystemData = new KnnSystemData();
            state.EntityManager.AddComponentData(state.SystemHandle, knnSystemData);
        }

        public void OnUpdate(ref SystemState state)
        {
            _updateKnnTimer += SystemAPI.Time.DeltaTime;
            if (_updateKnnTimer >= UpdateKnnInterval)
            {
                _updateKnnTimer = 0;
                UpdateKnn(ref state,_updateIndex);
                _updateIndex = (_updateIndex + 1) % 2;
            }

            //更新最近敌人的CombatComponent
            _combatLookup.Update(ref state);
            _localTransformLookup.Update(ref state);
            new FsmNearestEnemyCombatComponentUpdateJob
            {
                CombatLookup = _combatLookup,
                LocalTransformLookup = _localTransformLookup
            }.ScheduleParallel(state.Dependency).Complete();
            
            UpdateKnnSplit(ref state);
        }

        private void UpdateKnn(ref SystemState state,int updateIndex)
        {
            var hPosList = new NativeList<float3>(Allocator.TempJob);
            var hEntityList = new NativeList<Entity>(Allocator.TempJob);
            var zPosList = new NativeList<float3>(Allocator.TempJob);
            var zEntityList = new NativeList<Entity>(Allocator.TempJob);

            new KnnFlagCollectJob
            {
                HPosList = hPosList,
                HEntityList = hEntityList,
                ZPosList = zPosList,
                ZEntityList = zEntityList,
            }.Schedule(state.Dependency).Complete();
            if (hEntityList.Length == 0 || zEntityList.Length == 0)
            {
                hPosList.Dispose();
                hEntityList.Dispose();
                zPosList.Dispose();
                zEntityList.Dispose();
                ClearKnn(ref state);
                return;
            }
            var hPosArray = hPosList.AsArray();
            var hEntityArray = hEntityList.AsArray();
            var zPosArray = zPosList.AsArray();
            var zEntityArray = zEntityList.AsArray();
            KnnContainer hKnnContainer = default;
            KnnContainer zKnnContainer = default;
            if (updateIndex == 1)
            {
                hKnnContainer = new KnnContainer(hPosArray, true, Allocator.TempJob);
            }
            else
            {
                zKnnContainer = new KnnContainer(zPosArray, true, Allocator.TempJob);
            }
            

            UpdateNearestEnemy(ref state, hPosArray, zKnnContainer, zPosArray, hKnnContainer, hEntityArray,
                zEntityArray,_updateIndex);
            UpdateKnnTrigger(ref state,hKnnContainer, zKnnContainer, hEntityArray,zEntityArray,hPosArray,zPosArray,_updateIndex);

            hPosList.Dispose();
            hEntityList.Dispose();
            zPosList.Dispose();
            zEntityList.Dispose();
            if (_updateIndex == 1)
            {
                hKnnContainer.Dispose();
            }
            else
            {
                zKnnContainer.Dispose();
            }
        }


        public void OnDestroy(ref SystemState state)
        {
            _handledPlayerIds.Dispose();
        }

        [BurstCompile]
        [StructLayout(LayoutKind.Auto)]
        public partial struct KnnFlagCollectJob : IJobEntity
        {
            public NativeList<float3> HPosList;
            public NativeList<Entity> HEntityList;
            public NativeList<float3> ZPosList;
            public NativeList<Entity> ZEntityList;

            private void Execute(Entity entity, in KnnFlagComponent knnFlagComponent, in LocalTransform localTransform,in CombatComponent combatComponent)
            {
                if (combatComponent.IsDead)
                {
                    return;
                }
                if (knnFlagComponent.Flag == 0) //英雄
                {
                    HPosList.Add(localTransform.Position);
                    HEntityList.Add(entity);
                }
                else if (knnFlagComponent.Flag == 1) //僵尸
                {
                    ZPosList.Add(localTransform.Position);
                    ZEntityList.Add(entity);
                }
            }
        }

        #region Knn触发

        [BurstCompile][UsedImplicitly]
        private void UpdateKnnTrigger(ref SystemState _,KnnContainer hKnnContainer, KnnContainer zKnnContainer
            , NativeArray<Entity> hEntityArray,NativeArray<Entity> zEntityArray,NativeArray<float3> hPosArray,NativeArray<float3> zPosArray,int updateIndex)
        {
            var queryResult = new NativeList<int>(Allocator.TempJob);
            foreach (var (knnTriggerComponentRW,localTransform,buffer,entity) 
                     in SystemAPI.Query<RefRW<KnnTriggerComponent>,RefRO<LocalTransform>,DynamicBuffer<KnnTriggerBufferElement>>().WithEntityAccess())
            {
                var knnTriggerComponent = knnTriggerComponentRW.ValueRO;
                if (knnTriggerComponent.OwnerKnnFlag != updateIndex)
                {
                    continue;
                }
                knnTriggerComponentRW.ValueRW.UpdateTimer += UpdateKnnInterval * 2;
                if (knnTriggerComponentRW.ValueRW.UpdateTimer < knnTriggerComponent.UpdateInterval)
                {
                    continue;
                }
                knnTriggerComponentRW.ValueRW.UpdateTimer = 0;
                queryResult.Clear();
                buffer.Clear();
                KnnContainer knnContainer;
                NativeArray<Entity> entities;
                NativeArray<float3> positions;
                if (knnTriggerComponent.OwnerKnnFlag == 0)//英雄
                {
                    knnContainer = zKnnContainer;
                    entities = zEntityArray;
                    positions = zPosArray;
                }else if (knnTriggerComponent.OwnerKnnFlag == 1) //僵尸
                {
                    knnContainer = hKnnContainer;
                    entities = hEntityArray;
                    positions = hPosArray;
                }
                else
                {
                    return;
                }

                var hasParent = SystemAPI.HasComponent<Parent>(entity);
                var offsetTransform = localTransform.ValueRO;
                if (hasParent)
                {
                    var parent = SystemAPI.GetComponent<Parent>(entity);
                    if (SystemAPI.HasComponent<LocalTransform>(parent.Value))
                    {
                        var parentTransform = SystemAPI.GetComponent<LocalTransform>(parent.Value);
                        offsetTransform = parentTransform.TransformTransform(localTransform.ValueRO);
                    }
                }
                knnContainer.QueryRange(offsetTransform.Position+knnTriggerComponent.Offset,knnTriggerComponent.Radius,queryResult);
                if (knnTriggerComponent.Shape == EKnnShape.Fanned)
                {
                    foreach (var i in queryResult)
                    {
                        var pos = positions[i];
                        var zEntity = entities[i];
                    
                        var dir = pos - (offsetTransform.Position+knnTriggerComponent.Offset);
                        //求角度right 和 dir的夹角;
                        var dot = math.dot(offsetTransform.Right(),math.normalize(dir));
                        var angleRadians = math.acos(dot);
                        var angleDegrees = math.degrees(angleRadians);
                        if (angleDegrees * 2 <= knnTriggerComponent.Angle)
                        {
                            buffer.Add(new KnnTriggerBufferElement
                            {
                                Entity = zEntity
                            });
                        }
                    }
                    
                }else if (knnTriggerComponent.Shape == EKnnShape.Rect)
                {
                    foreach (var i in queryResult)
                    {
                        var pos = positions[i];
                        var zEntity = entities[i];
                        
                        var rect = knnTriggerComponent.Rect;
                        var position = offsetTransform.Position + knnTriggerComponent.Offset;
                        var drawRect = offsetTransform.Rotation.Equals(quaternion.identity) ? 
                            new Rect(rect.x + position.x, rect.y + position.y, rect.width, rect.height) 
                            : new Rect(position.x - rect.x - rect.width, rect.y + position.y, rect.width, rect.height);
                        
                        if (drawRect.Contains(new Vector2(pos.x, pos.y)))
                        {
                            buffer.Add(new KnnTriggerBufferElement
                            {
                                Entity = zEntity
                            });
                        }
                    }
                }

                
            }
            queryResult.Dispose();
        }
        
        #endregion
        #region 设置最近敌人信息

        private void ClearKnn(ref SystemState state)
        {
            var entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (fsmStateComponent, entity) in SystemAPI.Query<RefRW<FsmStateComponent>>().WithEntityAccess())
            {
                var fsmState = fsmStateComponent.ValueRO;
                fsmState.Combat.NearestEnemy = Entity.Null;
                fsmState.Combat.NearestEnemyTransform = LocalTransform.Identity;
                entityCommandBuffer.SetComponent(entity, fsmState);
                
            }
            foreach (var (_, entity) in SystemAPI.Query<DynamicBuffer<KnnTriggerBufferElement>>().WithEntityAccess())
            {
                entityCommandBuffer.SetBuffer<KnnTriggerBufferElement>(entity);
            }
            entityCommandBuffer.Playback(state.EntityManager);
            entityCommandBuffer.Dispose();
        }
        private void UpdateNearestEnemy(ref SystemState state, NativeArray<float3> hPosArray,
            KnnContainer zKnnContainer,
            NativeArray<float3> zPosArray, KnnContainer hKnnContainer, NativeArray<Entity> hEntityArray,
            NativeArray<Entity> zEntityArray,int updateIndex)
        {
            _fsmStateLookup.Update(ref state);
            if (updateIndex == 0)
            {
                var hNearestZ = new NativeArray<int>(hPosArray.Length, Allocator.TempJob);
                var queryKNearestBatchJob = new QueryKNearestBatchJob(zKnnContainer, hPosArray, hNearestZ);
                queryKNearestBatchJob.ScheduleBatch(hPosArray.Length, hPosArray.Length / 32, state.Dependency).Complete();
                
                var hEntityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob);
                if (hEntityArray.Length > 0)
                {
                    new FsmNearestEnemyHandleSystemJob
                    {
                        NearestArray = hNearestZ,
                        SelfEntityArray = hEntityArray,
                        EnemyArray = zEntityArray,
                        FsmStateLookup = _fsmStateLookup,
                        Ecb = hEntityCommandBuffer.AsParallelWriter(),
                    }.Schedule(hEntityArray.Length, 32, state.Dependency).Complete();
                    hEntityCommandBuffer.Playback(state.EntityManager);
                    hEntityCommandBuffer.Dispose();
                }
                hNearestZ.Dispose();
            }
            else
            {
                
                var zNearestH = new NativeArray<int>(zPosArray.Length, Allocator.TempJob);
                var queryKNearestBatchJob2 = new QueryKNearestBatchJob(hKnnContainer, zPosArray, zNearestH);
                queryKNearestBatchJob2.ScheduleBatch(zPosArray.Length, zPosArray.Length / 32, state.Dependency).Complete();
                
                var zEntityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob);
                if (zEntityArray.Length > 0)
                {
                    new FsmNearestEnemyHandleSystemJob
                    {
                        NearestArray = zNearestH,
                        SelfEntityArray = zEntityArray,
                        EnemyArray = hEntityArray,
                        FsmStateLookup = _fsmStateLookup,
                        Ecb = zEntityCommandBuffer.AsParallelWriter(),
                    }.Schedule(zEntityArray.Length, 32, state.Dependency).Complete();
                    zEntityCommandBuffer.Playback(state.EntityManager);
                    zEntityCommandBuffer.Dispose();
                }
                zNearestH.Dispose();
            }
        }

        [BurstCompile]
        private struct FsmNearestEnemyHandleSystemJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Entity> SelfEntityArray;
            [ReadOnly] public NativeArray<int> NearestArray;
            [ReadOnly] public NativeArray<Entity> EnemyArray;

            [ReadOnly] public ComponentLookup<FsmStateComponent> FsmStateLookup;

            //ecb
            public EntityCommandBuffer.ParallelWriter Ecb;

            public void Execute(int index)
            {
                var entity = SelfEntityArray[index];
                if (FsmStateLookup.TryGetComponent(entity, out var fsmStateComponent))
                {
                    var nearestEnemy = EnemyArray[NearestArray[index]];
                    fsmStateComponent.Combat.NearestEnemy = nearestEnemy;
                    Ecb.SetComponent(index, entity, fsmStateComponent);
                }
            }
        }

        [BurstCompile]
        [StructLayout(LayoutKind.Auto)]
        public partial struct FsmNearestEnemyCombatComponentUpdateJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<CombatComponent> CombatLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;

            private void Execute(ref FsmStateComponent fsmStateComponent)
            {
                if (CombatLookup.TryGetComponent(fsmStateComponent.Combat.NearestEnemy, out var combatComponent)
                    && LocalTransformLookup.TryGetComponent(fsmStateComponent.Combat.NearestEnemy,
                        out var localTransform))
                {
                    fsmStateComponent.Combat.NearestEnemyCombat = combatComponent;
                    fsmStateComponent.Combat.NearestEnemyTransform = localTransform;
                }
            }
        }

        #endregion
        #region 并查集划分

        [BurstCompile]
        private void UpdateKnnSplit(ref SystemState state)
        {
            var knnSystemData = state.EntityManager.GetComponentData<KnnSystemData>(state.SystemHandle);
            var playerSet = new NativeHashSet<int>(100,Allocator.TempJob);
            new PlayerComponentCollectJob
            {
                HashSet = playerSet
            }.Schedule(state.Dependency).Complete();
            var curPlayerId = 0;
            var defaultPlayerId = 0;
            playerSet.Remove(0);//0是无效id，防止意外放入
            foreach (var playerId in playerSet)
            {
                if (defaultPlayerId == 0)
                {
                    defaultPlayerId = playerId;
                }
                if (!_handledPlayerIds.Contains(playerId))
                {
                    _handledPlayerIds.Add(playerId);
                    curPlayerId = playerId;
                    break;
                }
            }
            
            var playerCount = playerSet.Count;
            playerSet.Dispose();
            if (playerCount == 0)
            {
                return;
            }
            if (curPlayerId == 0)
            {
                _handledPlayerIds.Clear();
                curPlayerId = defaultPlayerId;
            }

            if (!knnSystemData.FlagBearers.TryGetValue(curPlayerId, out var flagBearer))
            {
                flagBearer = new List<Entity>();
                knnSystemData.FlagBearers.Add(curPlayerId, flagBearer);
            }
            else
            {
                flagBearer.Clear();
            }
            var entities = new NativeList<Entity>(Allocator.TempJob);
            var positions = new NativeList<float3>(Allocator.TempJob);
            new PlayerRoleCollectJob
            {
                PlayerId = curPlayerId,
                EntityList = entities,
                PosList = positions
            }.Schedule(state.Dependency).Complete();

            var knnContainer = new KnnContainer(positions.AsArray(), true, Allocator.TempJob);
            var rangeQueryResults = new NativeArray<RangeQueryResult>(positions.Length, Allocator.TempJob);
            for (var i = 0; i < positions.Length; i++)
            {
                rangeQueryResults[i] = new RangeQueryResult(32, Allocator.TempJob);
            }
            var queryRangeBatchJob = new QueryRangeBatchJob(knnContainer, positions.AsArray(), 1, rangeQueryResults);
            queryRangeBatchJob.ScheduleBatch(positions.Length, positions.Length/32, state.Dependency).Complete();

            var parentArray = new NativeArray<int>(positions.Length, Allocator.TempJob);
            for (var i = 0; i < parentArray.Length; i++)
            {
                parentArray[i] = i;
            }
            for (var i = 0; i < positions.Length; i++)
            {
                if (parentArray[i] != i)
                {
                    continue;
                }
                var result = rangeQueryResults[i];
                for (var j = 0; j < result.Length; j++)
                {
                    var resultIndex = result[j];
                    SetParent(resultIndex,i,parentArray);
                }
            }

            var nativeHashMap = new NativeHashMap<int,float4>(12,Allocator.Temp);
            for (var i = 0; i < parentArray.Length; i++)
            {
                var root = FindRoot(i, parentArray);
                var pos = positions[i];
                if (nativeHashMap.TryGetValue(root, out var value))
                {
                    if (pos.x < value.x)
                    {
                        value.x = pos.x;
                    }
                    if (pos.y < value.y)
                    {
                        value.y = pos.y;
                    }

                    if (pos.x > value.z)
                    {
                        value.z = pos.x;
                    }
                    if (pos.y > value.w)
                    {
                        value.w = pos.y;
                    }
                    nativeHashMap[root] = value;
                }
                else
                {
                    nativeHashMap.TryAdd(root, new float4(pos.xyxy));
                }
            }

            var nativeArray = new NativeArray<int>(1,Allocator.TempJob);
            foreach (var kvPair in nativeHashMap)
            {
                var valueX = (kvPair.Value.x + kvPair.Value.z)/2;
                var valueY = (kvPair.Value.y + kvPair.Value.w)/2;
                knnContainer.QueryKNearest(new float3(valueX,valueY,0),nativeArray);
                var entity = entities[nativeArray[0]];
                flagBearer.Add(entity);
            }

            nativeArray.Dispose();
            
            parentArray.Dispose();
            foreach (var rangeQueryResult in rangeQueryResults)
            {
                rangeQueryResult.Dispose();
            }
            rangeQueryResults.Dispose();

            knnContainer.Dispose();
            entities.Dispose();
            positions.Dispose();
        }

        private int FindRoot(int i, NativeArray<int> parentArray)
        {
            var result = i;
            while (parentArray[result] != result)
            {
                result = parentArray[result];
            }
            return parentArray[result];
        }
        private void SetParent(int i, int parent, NativeArray<int> parentArray)
        {
            while (parentArray[i] != i)
            {
                var temp = parentArray[i];
                parentArray[i] = parent;
                i = temp;
            }
            parentArray[i] = parent;
        }
        [BurstCompile]
        public partial struct PlayerComponentCollectJob : IJobEntity
        {
            public NativeHashSet<int> HashSet;

            private void Execute(PlayerComponent playerComponent)
            {
                HashSet.Add(playerComponent.PlayerId);
            }
        }
        [BurstCompile]
        public partial struct PlayerRoleCollectJob : IJobEntity
        {
            public NativeList<Entity> EntityList;
            public NativeList<float3> PosList;
            public int PlayerId;

            private void Execute(PlayerComponent playerComponent,Entity entity,in LocalTransform localTransform)
            {
                if (playerComponent.PlayerId == PlayerId)
                {
                    EntityList.Add(entity);
                    PosList.Add(localTransform.Position);
                }
            }
        }
        #endregion
    }
}