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
    public class SkillCreator : OdinEditorWindow
    {
        //显示窗口
        [MenuItem("Tools/SkillCreator")]
        private static void OpenWindow()
        {
            var window = GetWindow<SkillCreator>();
            window.Show();
        }
        [FolderPath]
        public string foldPath = "";
        [ShowInInspector][ListDrawerSettings(DefaultExpandedState = true)]
        private Dictionary<int, int> _widthConfig = new();
        [ShowInInspector][ListDrawerSettings(DefaultExpandedState = true)]
        private Dictionary<int, int> _heightConfig = new();
        private List<AnimationTextureItem> _animationItemList = new();
        public int atlasWidth = 1024;
        [LabelText("最小公倍数")][ReadOnly]
        public int2 lcm;
        [ShowInInspector][ListDrawerSettings(DefaultExpandedState = true)]
        private List<AnimationInfoItem> _animationInfoItems = new();
        private Texture2D _curAtlas;
        private const string BaseAnimationSetPrefabPath = "Assets/Res/Skill/BaseAnimationSetPrefab.prefab";
        [Button("分析")]
        private void Analyze()
        {
            _widthConfig.Clear();
            _heightConfig.Clear();
            _animationItemList = NSpriteAnimationCreatorUtil.GetAnimationTextureItemList(foldPath);
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
            var directoryName = Path.GetDirectoryName(foldPath);
            var skillName = Path.GetFileName(directoryName);
            var saveFilePanel = EditorUtility.SaveFilePanel("保存文件", directoryName, $"{skillName}_skill_atlas.png", "png");
            if (string.IsNullOrEmpty(saveFilePanel))
            {
                return;
            }
            
            var atlasPath = saveFilePanel.Replace(Application.dataPath,"Assets");
            NSpriteAnimationCreatorUtil.SaveAtlas(_curAtlas, atlasPath);
            
            var atlas = AssetDatabase.LoadAssetAtPath<Sprite>(atlasPath);
            var spriteAnimations = new Dictionary<string, SpriteAnimation>();
            //生成技能预制体
            var skillPath = $"{directoryName}/{skillName}_prefab.prefab";
            GameObject skillTemplate;
            var loaded = File.Exists(skillPath);
            if (loaded)
            {
                skillTemplate = AssetDatabase.LoadAssetAtPath<GameObject>(skillPath);
            }
            else
            {
                skillTemplate = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(BaseAnimationSetPrefabPath));
            }
            if (skillTemplate == null)
            {
                return;
            }
            var animationConfig = skillTemplate.GetComponent<AnimationConfigAuthoring>();
            var spriteAnimated = skillTemplate.GetComponent<SpriteAnimatedRendererAuthoring>();
            animationConfig.ScaleConfig ??= new Dictionary<string, float2>();
            animationConfig.ScaleConfig.Clear();
            //创建动画信息文件
            foreach (var animationInfoItem in _animationInfoItems)
            {
                var savePath = $"{directoryName}/{skillName}_{animationInfoItem.AnimationName}_animation.asset";
                var spriteAnimation = NSpriteAnimationCreatorUtil.CreateSpriteAnimation(animationInfoItem, savePath,atlas);
                spriteAnimations[animationInfoItem.AnimationName] = spriteAnimation;
                animationConfig.ScaleConfig[animationInfoItem.AnimationName] = new float2(animationInfoItem.Width/100f,animationInfoItem.Height/100f);
                EditorUtility.SetDirty(spriteAnimation);
            }
            AssetDatabase.SaveAssets();
            var animationSetPath = $"{directoryName}/{skillName}_animation_set.asset";
            var spriteAnimationSet = NSpriteAnimationCreatorUtil.CreateSpriteAnimationSetAsset(spriteAnimations,animationSetPath);
            spriteAnimated.AnimationAuthoringModule.AnimationSet = spriteAnimationSet;
            AssetDatabase.SaveAssets();
            
            PrefabUtility.SaveAsPrefabAsset(skillTemplate,
                skillPath);
            if (!loaded)
            {
                DestroyImmediate(skillTemplate);
            }
            AssetDatabase.Refresh();
            
        }
    }
}