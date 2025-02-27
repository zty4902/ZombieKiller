using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DOTS.Authoring.Anim;
using NSprites;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace ToolEditor.Editor
{
    public class CharacterCreator : OdinEditorWindow
    {
        private const string CharacterTemplatePath = "Assets/Res/Character/CharacterTemplate.prefab";
        private Texture2D _curAtlas;
        private const string BasePath = "Assets/Res/Character/";
        [FolderPath(ParentFolder = BasePath)]
        public string foldPath = BasePath;
        public int atlasWidth = 1024;
        private List<AnimationTextureItem> _animationItemList = new();
        [ShowInInspector][ListDrawerSettings(DefaultExpandedState = true)]
        private Dictionary<int, int> _widthConfig = new();
        [ShowInInspector][ListDrawerSettings(DefaultExpandedState = true)]
        private Dictionary<int, int> _heightConfig = new();
        [LabelText("最小公倍数")][ReadOnly]
        public int2 lcm;
        [ShowInInspector][ListDrawerSettings(DefaultExpandedState = true)]
        private List<AnimationInfoItem> _animationInfoItems = new();
        [Button("分析")]
        private void Analyze()
        {
            _widthConfig.Clear();
            _heightConfig.Clear();
            _animationItemList = NSpriteAnimationCreatorUtil.GetAnimationTextureItemList(BasePath + foldPath);
            if (_animationItemList.Count == 0)
            {
                return;
            }
            
            _widthConfig =
                NSpriteAnimationCreatorUtil.GetExtendSize(_animationItemList.Select(t => t.Texture2D.width).ToList());
            _heightConfig = NSpriteAnimationCreatorUtil.GetExtendSize(_animationItemList.Select(t => t.Texture2D.height).ToList());
            var widthLcm = NSpriteAnimationCreatorUtil.FindLcm(_widthConfig.Values.ToList());
            var heightLcm = NSpriteAnimationCreatorUtil.FindLcm(_heightConfig.Values.ToList());
            atlasWidth = widthLcm;
            lcm = new int2(widthLcm,heightLcm);
        }


        //显示窗口
        [MenuItem("Tools/CharacterCreator")]
        private static void OpenWindow()
        {
            GetWindow<CharacterCreator>().Show();
        }
        [Button("生成")]
        private void GenAtlas()
        {
            if (_animationItemList.Count == 0)
            {
                return;
            }
            _animationInfoItems = NSpriteAnimationCreatorUtil.CreateAtlas(out var atlas,atlasWidth, _animationItemList, _widthConfig, _heightConfig);
            //_curAtlasHeight = (int)leftSide.BottomY;
            _curAtlas = atlas;

        }
        [Button("创建")]
        private void SaveAtlas()
        {
            if (_curAtlas == null)
            {
                return;
            }
            var characterName = Path.GetDirectoryName(foldPath);
            var saveFilePanel = EditorUtility.SaveFilePanel("保存文件", BasePath+Path.GetDirectoryName(foldPath), $"{characterName}_character_atlas.png", "png");
            if (string.IsNullOrEmpty(saveFilePanel))
            {
                return;
            }
            
            var atlasPath = saveFilePanel.Replace(Application.dataPath,"Assets");
            NSpriteAnimationCreatorUtil.SaveAtlas(_curAtlas, atlasPath);
            
            var atlas = AssetDatabase.LoadAssetAtPath<Sprite>(atlasPath);
            var spriteAnimations = new Dictionary<string, SpriteAnimation>();
            //生成角色预制体
            var characterPath = $"{BasePath}{characterName}/{characterName}_prefab.prefab";
            GameObject characterTemplate;
            var loaded = File.Exists(characterPath);
            if (loaded)
            {
                characterTemplate = AssetDatabase.LoadAssetAtPath<GameObject>(characterPath);
            }
            else
            {
                characterTemplate = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(CharacterTemplatePath));
            }
            if (characterTemplate == null)
            {
                return;
            }
            var animationConfig = characterTemplate.GetComponent<AnimationConfigAuthoring>();
            var spriteAnimated = characterTemplate.GetComponent<SpriteAnimatedRendererAuthoring>();
            animationConfig.ScaleConfig ??= new Dictionary<string, float2>();
            animationConfig.ScaleConfig.Clear();
            //创建动画信息文件
            foreach (var animationInfoItem in _animationInfoItems)
            {
                var savePath = $"{BasePath}{characterName}/{characterName}_{animationInfoItem.AnimationName}_animation.asset";
                if (Enum.TryParse(animationInfoItem.AnimationName,true,out EAnimationName animationName))
                {
                    var spriteAnimation = NSpriteAnimationCreatorUtil.CreateSpriteAnimation(animationInfoItem, savePath,atlas);
                    spriteAnimations[animationName.ToString()] = spriteAnimation;
                    animationConfig.ScaleConfig[animationName.ToString()] = new float2(animationInfoItem.Width/100f,animationInfoItem.Height/100f);
                    EditorUtility.SetDirty(spriteAnimation);
                }
                else
                {
                    Debug.Log($"无法解析动画名称{animationInfoItem.AnimationName}");
                }
            }
            AssetDatabase.SaveAssets();
            var animationSetPath = $"{BasePath}{characterName}/{characterName}_animation_set.asset";
            var spriteAnimationSet = NSpriteAnimationCreatorUtil.CreateSpriteAnimationSetAsset(spriteAnimations,animationSetPath);
            spriteAnimated.AnimationAuthoringModule.AnimationSet = spriteAnimationSet;
            AssetDatabase.SaveAssets();
            
            PrefabUtility.SaveAsPrefabAsset(characterTemplate,
                characterPath);
            if (!loaded)
            {
                DestroyImmediate(characterTemplate);
            }
            AssetDatabase.Refresh();
            
        }
    }
}
