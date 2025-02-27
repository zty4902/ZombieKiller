using System.Runtime.InteropServices;
using DOTS.BufferElement;
using DOTS.Component.Common;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace DOTS.System.Common
{
    [StructLayout(LayoutKind.Auto)]
    public partial struct PrefabManagerSystem : ISystem
    {
        private Entity _prefabManagerEntity;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PrefabManagerComponent>();
            
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _prefabManagerEntity = SystemAPI.GetSingletonEntity<PrefabManagerComponent>();
            var prefabManagerComponent = SystemAPI.GetComponentRW<PrefabManagerComponent>(_prefabManagerEntity);
            var prefabEntityBufferElements = SystemAPI.GetBuffer<PrefabEntityBufferElement>(_prefabManagerEntity);
            //var heroes = new NativeList<Entity>(AllocatorManager.Persistent);
            //var zombies = new NativeList<Entity>(AllocatorManager.Persistent);
            var blobBuilderH = new BlobBuilder(Allocator.Temp);
            ref var heroes = ref blobBuilderH.ConstructRoot<NativeList<PrefabItem>>();
            heroes = new NativeList<PrefabItem>(Allocator.Domain);
            var heroesBlobAssetReference = blobBuilderH.CreateBlobAssetReference<NativeList<PrefabItem>>(Allocator.Domain);

            var blobBuilderZ = new BlobBuilder(Allocator.Temp);
            ref var zombies = ref blobBuilderZ.ConstructRoot<NativeList<PrefabItem>>();
            zombies = new NativeList<PrefabItem>(Allocator.Domain);
            var zombiesBlobAssetReference = blobBuilderZ.CreateBlobAssetReference<NativeList<PrefabItem>>(Allocator.Domain);
            
            prefabManagerComponent.ValueRW.Heroes = heroesBlobAssetReference;
            prefabManagerComponent.ValueRW.Zombies = zombiesBlobAssetReference;
            foreach (var prefabEntityBufferElement in prefabEntityBufferElements)
            {
                var flag = prefabEntityBufferElement.Flag;
                if (flag == 0)
                {
                    heroes.Add(new PrefabItem()
                    {
                        Prefab = prefabEntityBufferElement.Prefab,
                        Name = prefabEntityBufferElement.PrefabName
                    });
                }
                else
                {
                    zombies.Add(new PrefabItem()
                    {
                        Prefab = prefabEntityBufferElement.Prefab,
                        Name = prefabEntityBufferElement.PrefabName
                    });
                }
            }
            blobBuilderH.Dispose();
            blobBuilderZ.Dispose();
            
            state.Enabled = false;
        }
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}