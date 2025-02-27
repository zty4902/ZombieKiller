using System.Collections.Generic;
using DOTS.BufferElement;
using DOTS.Component.Common;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring.Common
{
    public class PrefabManagerAuthoring : SerializedMonoBehaviour
    {
        [OdinSerialize]
        public Dictionary<EPrefabName,GameObject> Characters = new();

        [TitleGroup("流血")][LabelText("通用流血")]
        public GameObject bloodCommon;
        [TitleGroup("流血")][LabelText("枪击流血")]
        public GameObject bloodShot;
        [TitleGroup("流血")][LabelText("喷溅流血")]
        public GameObject bloodSplash;
        [TitleGroup("流血")][LabelText("火焰燃烧")]
        public GameObject bloodFire;
        [TitleGroup("燃烧死亡")][LabelText("燃烧身亡僵尸")]
        public GameObject burningDeath;
        [TitleGroup("燃烧死亡")][LabelText("燃烧身亡物品")]
        public GameObject burningCorpse;
        [TitleGroup("爆炸")][LabelText("小爆炸")]
        public GameObject smallExplosion;
        [TitleGroup("爆炸")][LabelText("小血爆")]
        public GameObject bloodSmallExplosion;
        [TitleGroup("爆炸")][LabelText("大血爆")]
        public GameObject bloodExplosion;
        [TitleGroup("火焰")][LabelText("地面火焰")]
        public GameObject xmasFire;
        [TitleGroup("火焰")][LabelText("Buff火焰")]
        public GameObject buffFire;
        [TitleGroup("飘字")][LabelText("暴击Smack")]
        public GameObject critSmack;
        [TitleGroup("飘字")][LabelText("暴击Twack")]
        public GameObject critTwack;
        private class PrefabManagerAuthoringBaker : Baker<PrefabManagerAuthoring>
        {
            public override void Bake(PrefabManagerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var prefabEntityBufferElements = AddBuffer<PrefabEntityBufferElement>(entity);
                foreach (var kvp in authoring.Characters)
                {
                    prefabEntityBufferElements.Add(new PrefabEntityBufferElement
                    {
                        Prefab = GetEntity(kvp.Value, TransformUsageFlags.Dynamic),
                        PrefabName = kvp.Key,
                        Flag = kvp.Key.ToString().EndsWith("H") ? 0 : 1
                    });
                }

                var prefabManagerComponent = new PrefabManagerComponent
                {
                    BloodCommon = GetEntity(authoring.bloodCommon, TransformUsageFlags.None),
                    BloodShot = GetEntity(authoring.bloodShot, TransformUsageFlags.None),
                    BloodSplash = GetEntity(authoring.bloodSplash, TransformUsageFlags.None),
                    BloodFire = GetEntity(authoring.bloodFire, TransformUsageFlags.None),
                    BurningDeath = GetEntity(authoring.burningDeath, TransformUsageFlags.None),
                    BurningCorpse = GetEntity(authoring.burningCorpse, TransformUsageFlags.None),
                    SmallExplosion = GetEntity(authoring.smallExplosion, TransformUsageFlags.None),
                    XmasFire = GetEntity(authoring.xmasFire, TransformUsageFlags.None),
                    BloodExplosion = GetEntity(authoring.bloodExplosion, TransformUsageFlags.None),
                    BloodSmallExplosion = GetEntity(authoring.bloodSmallExplosion, TransformUsageFlags.None),
                    CritSmack = GetEntity(authoring.critSmack, TransformUsageFlags.None),
                    CritTwack = GetEntity(authoring.critTwack, TransformUsageFlags.None),
                    BuffFire = GetEntity(authoring.buffFire, TransformUsageFlags.None)
                };
                AddComponent(entity,prefabManagerComponent);
            }
        }
    }
}