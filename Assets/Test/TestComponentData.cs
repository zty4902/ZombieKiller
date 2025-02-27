using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Serialization;

namespace DOTS.Component
{
    [Serializable]
    public struct AnimData
    {
        public int animIndex;
        public float2 scale;
    }
    public struct TestComponentData : IComponentData
    {
        public int LastAnim;
        public int CurAnim;

        public AnimData Move;
        public AnimData AfterDeath;
        public AnimData Aim;
        public AnimData Death;
        public AnimData Dodge;

    }
}