using System;
using System.Collections.Generic;
using DOTS.BufferElement;
using DOTS.Component.Spawn;
using Game.Player;
using Unity.Entities;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Test
{
    public class TestSpawnPanel : MonoBehaviour
    {
        public GameObject contentPanel;
        private int _toggleCount = 1;

        public void SetToggleCount1(bool isOn)
        {
            if (isOn)
            {
                _toggleCount = 1;
            }
        }

        public void SetToggleCount10(bool isOn)
        {
            if (isOn)
            {
                _toggleCount = 10;
            }
        }

        public void SetToggleCount100(bool isOn)
        {
            if (isOn)
            {
                _toggleCount = 100;
            }
        }

        public void HidePanel(bool isOn)
        {
            contentPanel.SetActive(!isOn);
        }

        public void SetToggleCount1000(bool isOn)
        {
            if (isOn)
            {
                _toggleCount = 1000;
            }
        }

        public void CreateCharacter(string prefabName)
        {
            if (Enum.TryParse(prefabName,true, out EPrefabName ePrefabName))
            {
                var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                var spawnManagerBufferElements = entityManager.CreateEntityQuery(typeof(SpawnQueueManagerComponent),typeof(SpawnRequestBufferElement))
                    .GetSingletonBuffer<SpawnRequestBufferElement>();
                spawnManagerBufferElements.Add(new SpawnRequestBufferElement
                {
                    Flag = prefabName.EndsWith("H") ? 0 : 1,
                    CharacterName = ePrefabName,
                    SpawnCount = _toggleCount,
                    PlayerId = Random.Range(0,PlayerManager.Instance.PlayerData.Count)
                });
            }

        }

        /*private void CreateCharacter(EPrefabName prefabName)
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var spawnManagerBufferElements = entityManager.CreateEntityQuery(typeof(SpawnManagerBufferElement))
                .GetSingletonBuffer<SpawnManagerBufferElement>();

            var entityQuery = entityManager.CreateEntityQuery(typeof(SpawnPointComponent));
            var entityArray = entityQuery.ToEntityArray(Allocator.Temp);
            var prefabManagerComponent = entityManager.CreateEntityQuery(typeof(PrefabManagerComponent))
                .GetSingleton<PrefabManagerComponent>();
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
            var hero = prefabName.ToString().EndsWith("H");
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

            var characterEntity = Entity.Null;
            foreach (var prefabItem in prefabs)
            {
                if (prefabItem.Name == prefabName)
                {
                    characterEntity = prefabItem.Prefab;
                    break;
                }
            }

            if (characterEntity == Entity.Null)
            {
                return;
            }

            for (var i = 0; i < _toggleCount; i++)
            {
                var range = Random.Range(0, localTransforms.Count);
                var localTransform = localTransforms[range];
                var randomX = Random.value * 40 - 20;
                var randomY = Random.value * 40 - 20;
                spawnManagerBufferElements.Add(new SpawnManagerBufferElement
                {
                    Prefab = characterEntity,
                    Position = localTransform.Position + new float3(randomX, randomY, 0),
                    PlayerId = Random.Range(0,5)
                });
            }

            entityArray.Dispose();
        }*/
    }
}