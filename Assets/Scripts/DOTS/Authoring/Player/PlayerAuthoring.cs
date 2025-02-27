using DOTS.Component.Player;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring.Player
{
    public class PlayerAuthoring : MonoBehaviour
    {
        public int playerId;
        private class PlayerAuthoringBaker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity,new PlayerComponent
                {
                    PlayerId = authoring.playerId
                });
            }
        }
    }
}