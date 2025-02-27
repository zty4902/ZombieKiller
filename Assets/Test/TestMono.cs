using System.Collections.Generic;
using System.ComponentModel;
using DOTS.BufferElement;
using DOTS.Component.Common;
using DOTS.Component.Font;
using DOTS.Component.FSM;
using DOTS.Component.Spawn;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Test
{
    public class TestOptions
    {
        [Category("创建角色")][UsedImplicitly]
        public EPrefabName PrefabName {get;set;}
        [Category("创建角色")][UsedImplicitly]
        public int Count{get;set;}
        [Category("创建角色")][UsedImplicitly]
        public void CreateCharacter()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var spawnManagerBufferElements = entityManager.CreateEntityQuery(typeof(SpawnManagerBufferElement)).GetSingletonBuffer<SpawnManagerBufferElement>();

            var entityQuery = entityManager.CreateEntityQuery(typeof(SpawnPointComponent));
            var entityArray = entityQuery.ToEntityArray(Allocator.Temp);
            var prefabManagerComponent = entityManager.CreateEntityQuery(typeof(PrefabManagerComponent)).GetSingleton<PrefabManagerComponent>();
            var heroLocalTransformComponents = new List<LocalTransform>();
            var zombieLocalTransformComponents = new List<LocalTransform>();
            foreach (var entity in entityArray)
            {
                var localTransform = entityManager.GetComponentData<LocalTransform>(entity);
                var spawnPointComponent = entityManager.GetComponentData<SpawnPointComponent>(entity);
                if (spawnPointComponent.Flag == 0)
                {
                    heroLocalTransformComponents.Add(localTransform);
                }
                else
                {
                    zombieLocalTransformComponents.Add(localTransform);
                }
            }
            List<LocalTransform> localTransforms;
            NativeList<PrefabItem> prefabs;
            var hero = PrefabName.ToString().EndsWith("H");
            if (hero)
            {
                localTransforms = heroLocalTransformComponents;
                prefabs = prefabManagerComponent.Heroes.Value;
            }
            else
            {
                localTransforms = zombieLocalTransformComponents;
                prefabs = prefabManagerComponent.Zombies.Value;
            }
            Entity characterEntity = Entity.Null;
            foreach (var prefabItem in prefabs)
            {
                if (prefabItem.Name == PrefabName)
                {
                    characterEntity = prefabItem.Prefab;
                    break;
                }
            }

            if (characterEntity == Entity.Null)
            {
                return;
            }
            for (var i = 0; i < Count; i++)
            {
                var range = Random.Range(0, localTransforms.Count);
                var localTransform = localTransforms[range];
                var insideUnitCircle = Random.insideUnitCircle * 12;
                spawnManagerBufferElements.Add(new SpawnManagerBufferElement
                {
                    Prefab = characterEntity,
                    Position = localTransform.Position + new float3(insideUnitCircle.x, insideUnitCircle.y, 0),
                });
            }

            entityArray.Dispose();
        }
        
        public string TestLabelString {get;set;}
        public int FonsSize {get;set;}
        public void TestLabel()
        {
            /*var textLabelRequestComponent = new TextLabelRequestComponent
            {
                Text = TestLabelString,
                ColorIndex = 0,
                FontSize = FonsSize,
            };
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var entity = entityManager.CreateEntity();
            entityManager.AddComponentData(entity, textLabelRequestComponent);*/
        }
    }
    public class TestMono : MonoBehaviour
    {
        private TestOptions _options;
        private void Awake()
        {
            _options = new TestOptions();
            SRDebug.Instance.AddOptionContainer(_options);
        }

        private float _deltaTime;
        private float _timer;

        private void Update()
        {
            _deltaTime += (Time.deltaTime - _deltaTime) * 0.1f;
            _timer += Time.deltaTime;
            if (_timer > 1)
            {
                _timer = 0;
                UpdateEntityCount();
            }
        }

        private static int _playerIndex;
        private static int _entityCount;
        public void OnGUI()
        {
            //显示帧率
            GUILayout.Label("FPS:" + Mathf.RoundToInt(1/_deltaTime));
            //显示实体数量
            GUILayout.Label("entity数量："+_entityCount);
        }

        private static void UpdateEntityCount()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var fsmCombatStateArray = entityManager.CreateEntityQuery(typeof(FsmStateComponent)).ToEntityArray(Allocator.Temp);
            _entityCount = fsmCombatStateArray.Length;
            fsmCombatStateArray.Dispose();
        }
    }
}