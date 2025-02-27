using System.Collections.Generic;
using System.Linq;
using DOTS.Component.Anim;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;

namespace DOTS.Authoring.Anim
{
    public class AnimationConfigAuthoring : SerializedMonoBehaviour
    {
        [OdinSerialize]
        public Dictionary<string, float2> ScaleConfig;

        public float animScale = 1;
        private class AnimationConfigAuthoringBaker : Baker<AnimationConfigAuthoring>
        {
            public override void Bake(AnimationConfigAuthoring authoring)
            {
                if (authoring.ScaleConfig == null)
                {
                    return;
                }

                var hash128 = new Hash128
                {
                    Value = new uint4(GenerateUniqueUint(authoring.ScaleConfig))
                };
                if (!TryGetBlobAssetReference(hash128, out BlobAssetReference<BlobArray<AnimationConfig>> blobAssetReference))
                {
                    var blobBuilder = new BlobBuilder(Allocator.Temp);
                    ref var constructRoot = ref blobBuilder.ConstructRoot<BlobArray<AnimationConfig>>();
                    var blobBuilderArray = blobBuilder.Allocate(ref constructRoot, authoring.ScaleConfig.Count);
                    var index = 0;
                    foreach (var keyValuePair in authoring.ScaleConfig)
                    {
                        var animationConfig = new AnimationConfig
                        {
                            Scale = keyValuePair.Value,
                            AnimationIndex = Animator.StringToHash(keyValuePair.Key)
                        };
                        blobBuilderArray[index++] = animationConfig;
                    }
                    blobAssetReference = blobBuilder.CreateBlobAssetReference<BlobArray<AnimationConfig>>(Allocator.Persistent);
                    AddBlobAssetWithCustomHash(ref blobAssetReference, hash128);
                    blobBuilder.Dispose();
                }

                AddComponent(GetEntity(TransformUsageFlags.None),new AnimationConfigComponent()
                {
                    AnimationConfigArray = blobAssetReference,
                    AnimScale = authoring.animScale
                });

            }

            private static uint GenerateUniqueUint(Dictionary<string, float2> dictionary)
            {
                // Step 1: Sort the dictionary by key to ensure consistent order
                var sortedDictionary = dictionary.OrderBy(kv => kv.Key);

                // Step 2: Convert the dictionary to a string representation
                string dictionaryString = string.Join(";", sortedDictionary.Select(kv => $"{kv.Key}:{kv.Value.x},{kv.Value.y}"));

                // Step 3: Compute a hash from the string representation
                uint hash = ComputeHash(dictionaryString);

                return hash;
            }

            private static uint ComputeHash(string input)
            {
                // You can use any suitable hashing algorithm. Here we use a simple one for demonstration.
                uint hash = 2166136261;
                foreach (char c in input)
                {
                    hash = (hash ^ c) * 16777619;
                }
                return hash;
            }
        }
    }
}