using System.Collections.Generic;
using DOTS.System.FSM.Handle;
using Game.Common;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Player
{
    public class PlayerData
    {
        public int PlayerId;
        public string PlayerName;
    }

    public class PlayerFlagBearerData
    {
        public string FlagName;
        public GameObject FlagBearer;
        public Entity Entity;
    }
    public class PlayerManager : MonoBehaviourSingleton<PlayerManager> , IGameOverClear
    {
        public GameObject playerFlagBearerPrefab;
        public Transform flagBearerParent;
        private Camera _camera;
        
        public readonly Dictionary<int, PlayerData> PlayerData = new();
        private readonly Dictionary<int, List<PlayerFlagBearerData>> _playerFlagBearerDic = new();

        private const float FlagBearerUpdateInterval = 1f;
        private float _lastFlagBearerUpdateTimer;

        private readonly Stack<GameObject> _playerFlagBearersPool = new();

        private void Start()
        {
            _camera = Camera.main;
            string[] randomNames = {
                "归舟放鹤", "清影", "倦旅人", "星河", "似懂非懂", "念卿", "青竹酒", "步崖", "清欢百味", "忽如远行客",
                "浅笑离愁", "念雨尘封", "鳄鱼的笑脸", "霏琅咫天涯", "空水漫漫", "雨季悠离", "短发披肩", "溺鱼",
                "长夜深蓝", "紫藤花", "青柠微凉", "九日盛花", "怪妹妹", "罂粟幻灭", "芦苇少女", "萌呆淑女",
                "阳光刺痛眼眸", "指尖上的年轮", "百次凝眸", "深拥未栀", "皎、明月", "花蝶恋", "清风挽发", "高调的华丽",
                "惜灵静雅", "彼岸天涯", "南岸末阴", "lay挽歌", "夕顔如夢", "岛屿云烟", "倾墨使罄", "抠脚女神",
                "初心未变", "北方佳人", "紫色爱恋", "北海茫月", "冷月星空", "五月晴空", "秋之恋", "夜深、泪似海",
                "迷路的麋鹿", "花期如梦", "小姐来根烟", "飞韵无影", "糖果宝宝", "枫叶无情", "梦与时光遇", "俗世的流离",
                "北有亡夢", "裕火焚身", "傲娇萌娃", "浪迹天涯", "站台悲影", "梦醉西楼", "自幼可爱", "暴力野萝莉",
                "可爱一如往常", "打小我就淘i", "甛甛dē餹", "污萌少女", "萌眯", "牛奶煮萝莉", "空大萌妹",
                "软萌猫", "玛丽莲萌鹿", "氺粿餹ぎ", "软Q糖", "坏坏的丫头", "野区小公主╬Ψ", "呆呆宝", "雨妞"
            };
            for (var index = 1; index <= randomNames.Length; index++)
            {
                var randomName = randomNames[index-1];
                AddPlayer(new PlayerData { PlayerId = index, PlayerName = randomName });
            }
        }
        private void Update()
        {
            UpdatePlayerFlagBearersPos();
            
            _lastFlagBearerUpdateTimer += Time.deltaTime;
            if (_lastFlagBearerUpdateTimer >= FlagBearerUpdateInterval)
            {
                _lastFlagBearerUpdateTimer = 0f;
            }else
            {
                return;
            }
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var existingSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystem<FsmNearestEnemyHandleSystem>();
            var knnSystemData = entityManager.GetComponentData<KnnSystemData>(existingSystem);
            foreach (var flagBearer in knnSystemData.FlagBearers)
            {
                if (!_playerFlagBearerDic.TryGetValue(flagBearer.Key, out var playerFlagBearers))
                {
                    playerFlagBearers = new List<PlayerFlagBearerData>();
                    _playerFlagBearerDic[flagBearer.Key] = playerFlagBearers;
                }
                else
                {
                    for (var i = 0; i < playerFlagBearers.Count; i++)
                    {
                        if (!flagBearer.Value.Contains(playerFlagBearers[i].Entity))
                        {
                            RecycleFlagBearerObj(playerFlagBearers[i].FlagBearer);
                            playerFlagBearers.Remove(playerFlagBearers[i]);
                            i--;
                        }
                        else
                        {
                            flagBearer.Value.Remove(playerFlagBearers[i].Entity);
                        }
                    }
                }
                foreach (var entity in flagBearer.Value)
                {
                    if (entityManager.Exists(entity))
                    {
                        var playerFlagBearerData = new PlayerFlagBearerData
                        {
                            FlagName = PlayerData[flagBearer.Key].PlayerName,
                            FlagBearer = GetFlagBearerObj(),
                            Entity = entity
                        };
                        playerFlagBearers.Add(playerFlagBearerData);
                        var flagBearerCom = playerFlagBearerData.FlagBearer.GetComponent<FlagBearer>();
                        flagBearerCom.Refresh(playerFlagBearerData);
                    }
                }
            }
            
        }

        private void UpdatePlayerFlagBearersPos()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            //更新flagBearer的位置
            foreach (var kvp in _playerFlagBearerDic)
            {
                for (var i = 0; i < kvp.Value.Count; i++)
                {
                    var playerFlagBearerData = kvp.Value[i];
                    if (!entityManager.Exists(playerFlagBearerData.Entity))
                    {
                        kvp.Value.Remove(playerFlagBearerData);
                        RecycleFlagBearerObj(playerFlagBearerData.FlagBearer);
                        i--;
                        continue;
                    }

                    var localTransform = entityManager.GetComponentData<LocalTransform>(playerFlagBearerData.Entity);
                    var worldToScreenPoint = _camera.WorldToScreenPoint(localTransform.Position + new float3(0,0.7f,0));
                    playerFlagBearerData.FlagBearer.transform.position = worldToScreenPoint;
                }
            }
        }
        private GameObject GetFlagBearerObj()
        {
            var flagBearerObj = _playerFlagBearersPool.Count == 0 ? Instantiate(playerFlagBearerPrefab, flagBearerParent) : _playerFlagBearersPool.Pop();
            flagBearerObj.SetActive(true);
            return flagBearerObj;
        }

        private void RecycleFlagBearerObj(GameObject flagBearerObj)
        {
            flagBearerObj.SetActive(false);
            _playerFlagBearersPool.Push(flagBearerObj);
        }

        public void ClearGameOver()
        {
            _playerFlagBearerDic.Clear();
            PlayerData.Clear();
        }

        public void AddPlayer(PlayerData playerData)
        {
            PlayerData.Add(playerData.PlayerId, playerData);
        }
    }
}