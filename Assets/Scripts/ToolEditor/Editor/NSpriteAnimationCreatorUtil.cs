using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NSprites;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace ToolEditor.Editor
{
    public class LeftSide
    {
        private float _topY;
        public float BottomY;
        public float LeftX;
        private readonly int _atlasWidth;

        public LeftSide(int atlasWidth)
        {
            _atlasWidth = atlasWidth;
        }

        public void ResetLeftX(int width, int height)
        {
            var countH = Mathf.CeilToInt(LeftX / width);
            var countV = Mathf.CeilToInt(_topY / height);
            var newBottomY = (countV + 1) * height;
            BottomY = math.max(BottomY, newBottomY);
            _topY = BottomY - height;
            LeftX = countH * width;
        }

        public void MoveNext(Vector2 size)
        {
            var newTopY = Mathf.CeilToInt(_topY / size.y) * size.y;
            var newX = 0f;
            if (newTopY < BottomY) //不新起一行
            {
                var rowCount = (int)(_atlasWidth / size.x);
                var count = Mathf.CeilToInt(LeftX / size.x);
                if (count == rowCount - 1) //x到头了，还得换行
                {
                    newTopY += size.y;
                }
                else
                {
                    newX = (count + 1) * size.x;
                }
            }

            LeftX = newX;
            _topY = newTopY;
            BottomY = newTopY + size.y;
        }

        public override string ToString()
        {
            return $"LeftX:{LeftX},TopY:{_topY},BottomY:{BottomY}";
        }
    }
    public class AnimationTextureItem
    {
        public int Index;
        public Texture2D Texture2D;
        public string AnimationName;
    }
    [HideReferenceObjectPicker]
    public class AnimationInfoItem
    {
        public string AnimationName;
        public int Width;
        public int Height;
        public int StartOffset;
        public int Count;
    }

    public static class NSpriteAnimationCreatorUtil
    {
        public static List<AnimationTextureItem> GetAnimationTextureItemList(string path)
        {
            var result = new List<AnimationTextureItem>();
            var directories = Directory.GetDirectories(path);
            foreach (var directory in directories)
            {
                var findAssets = AssetDatabase.FindAssets("t:Texture2D",new []{directory});
                var ps = findAssets.Select(AssetDatabase.GUIDToAssetPath).ToList();
                foreach (var s in ps)
                {
                    var texture2D = AssetDatabase.LoadAssetAtPath<Texture2D>(s);
                    result.Add(new AnimationTextureItem
                    {
                        Index = result.Count,
                        Texture2D = texture2D,
                        AnimationName = Path.GetFileName(directory)
                    });
                }
            }
            return result;
        }

        public static Dictionary<int, int> GetExtendSize(List<int> sizes)
        {
            if (sizes.Count == 0)
            {
                return null;
            }
            var sizeList = sizes.Distinct().OrderBy(x => x).ToList();
            var baseWidth = sizeList[0];
            var extendWidth = baseWidth;
            var result = new Dictionary<int, int>();
            foreach (var width in sizeList)
            {
                while (extendWidth < width)
                {
                    extendWidth += baseWidth;
                }
                result[width] = extendWidth;
            }
            return result;
        }
        // 求最大公约数
        private static int FindGcd(int a, int b)
        {
            while (b != 0)
            {
                int temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }
        // 求最小公倍数
        public static int FindLcm(IReadOnlyList<int> numbers)
        {
            var lcm = numbers[0];
            for (var i = 1; i < numbers.Count; i++)
            {
                lcm = (lcm * numbers[i]) / FindGcd(lcm, numbers[i]);
            }
            return lcm;
        }

        private static Texture2D GetTransparentTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            var color = new Color(0, 0, 0, 0);
            for (var i = 0; i < width; i++)
            {
                for (var j = 0; j < height; j++)
                {
                    texture.SetPixel(i, j, color);
                }
            }
            texture.Apply();
            return texture;
        }
        public static void SetSpriteSheet(string savePath)
        {
            var textureImporter = AssetImporter.GetAtPath(savePath) as TextureImporter;
            if (textureImporter)
            {
                textureImporter.textureType = TextureImporterType.Sprite;
                textureImporter.spriteImportMode = SpriteImportMode.Single;
                var textureImporterSettings = new TextureImporterSettings();
                textureImporter.ReadTextureSettings(textureImporterSettings);
                textureImporterSettings.spriteMeshType = SpriteMeshType.FullRect;
                textureImporter.SetTextureSettings(textureImporterSettings);
                textureImporter.SaveAndReimport();
            }
        }

        public static List<AnimationInfoItem> CreateAtlas(out Texture2D atlas,int atlasWidth,List<AnimationTextureItem> animationItemList
            ,Dictionary<int,int> widthExtendSize,Dictionary<int,int> heightExtendSize)
        {
            var curAtlas = GetTransparentTexture(atlasWidth, 4096);
            var result = new List<AnimationInfoItem>();
            var leftSide = new LeftSide(curAtlas.width);
            var curAnimationItem = animationItemList[0];
            for (var i = 0; i < animationItemList.Count; i++)
            {
                var item = animationItemList[i];
                var texture2D = item.Texture2D;
                if (curAnimationItem.AnimationName != item.AnimationName)
                {
                    var lastAnimationTextureItem = animationItemList[i-1];
                    result.Add(GetAnimationInfoItem(curAnimationItem,lastAnimationTextureItem,leftSide,widthExtendSize,heightExtendSize,curAtlas.width));
                    curAnimationItem = item;
                }
                var extendWidth = widthExtendSize[texture2D.width];
                var extendW = Mathf.FloorToInt((extendWidth - texture2D.width)/2f);
                var extendHeight = heightExtendSize[texture2D.height];
                var pixels = texture2D.GetPixels();
            
                leftSide.ResetLeftX(extendWidth,extendHeight);
                curAtlas.SetPixels((int)leftSide.LeftX + extendW,
                    curAtlas.height - (int)leftSide.BottomY, texture2D.width,
                    texture2D.height, pixels);
                leftSide.MoveNext(new Vector2(extendWidth,extendHeight));
            }
            result.Add(GetAnimationInfoItem(curAnimationItem,animationItemList[^1],leftSide,widthExtendSize,heightExtendSize,curAtlas.width));
            curAtlas.Apply();

            var realHeight = (int)leftSide.BottomY;
            atlas = new Texture2D(curAtlas.width, realHeight, TextureFormat.RGBA32, false);
            var atlasPixels = curAtlas.GetPixels(0, 4096 - realHeight, curAtlas.width, realHeight);
            atlas.SetPixels(0, 0, curAtlas.width, realHeight, atlasPixels);
            atlas.Apply();
            return result;
        }
        private static AnimationInfoItem GetAnimationInfoItem(AnimationTextureItem animationTextureItem,AnimationTextureItem lastAnimationTextureItem,
            LeftSide leftSide,Dictionary<int,int> widthExtendSize,Dictionary<int,int> heightExtendSize,int atlasWidth)
        {
            var texture2DWidth = widthExtendSize[animationTextureItem.Texture2D.width];
            var texture2DHeight = heightExtendSize[animationTextureItem.Texture2D.height];
            var xCount = (int)(leftSide.LeftX / texture2DWidth);
            var xMaxCount = atlasWidth / texture2DWidth;
            var yCount = (int)(leftSide.BottomY / texture2DHeight);
            var totalCount = (yCount - 1) * xMaxCount + xCount;
            var count = lastAnimationTextureItem.Index - animationTextureItem.Index + 1;
            var animationInfoItem = new AnimationInfoItem
            {
                AnimationName = animationTextureItem.AnimationName,
                StartOffset = totalCount - count,
                Count = count,
                Width = texture2DWidth,
                Height = texture2DHeight
            };
            return animationInfoItem;
        }

        public static void SaveAtlas(Texture2D atlas, string savePath)
        {
            var encodeToPNG = atlas.EncodeToPNG();
            File.WriteAllBytes(savePath,encodeToPNG);
            AssetDatabase.Refresh();
            SetSpriteSheet(savePath);
        }

        public static SpriteAnimation CreateSpriteAnimation(AnimationInfoItem animationInfoItem,string savePath,Sprite atlas)
        {
            SpriteAnimation spriteAnimation;
            if (File.Exists(savePath))
            {
                spriteAnimation = AssetDatabase.LoadAssetAtPath<SpriteAnimation>(savePath);
            }
            else
            {
                spriteAnimation = ScriptableObject.CreateInstance<SpriteAnimation>();
                AssetDatabase.CreateAsset(spriteAnimation, savePath);
            }

            spriteAnimation.SpriteSheet = atlas;
            var rowCount = atlas.texture.width / animationInfoItem.Width;
            var colCount = atlas.texture.height / animationInfoItem.Height;
            spriteAnimation.FrameCount = new int2(rowCount, colCount);
            spriteAnimation.FrameRange.Count = animationInfoItem.Count;
            spriteAnimation.FrameRange.Offset = animationInfoItem.StartOffset;
            spriteAnimation.FrameDurations = new float[animationInfoItem.Count];
            for (var i = 0; i < spriteAnimation.FrameDurations.Length; i++)
            {
                spriteAnimation.FrameDurations[i] = 0.1f;
            }
            EditorUtility.SetDirty(spriteAnimation);
            return spriteAnimation;
        }

        public static SpriteAnimationSet CreateSpriteAnimationSetAsset(Dictionary<string, SpriteAnimation> spriteAnimations, string animationSetPath)
        {
            SpriteAnimationSet spriteAnimationSet;
            if (File.Exists(animationSetPath))
            {
                spriteAnimationSet = AssetDatabase.LoadAssetAtPath<SpriteAnimationSet>(animationSetPath);
            }
            else
            {
                spriteAnimationSet = ScriptableObject.CreateInstance<SpriteAnimationSet>();
                AssetDatabase.CreateAsset(spriteAnimationSet, animationSetPath);
            }
            var animationsField = spriteAnimationSet.GetType().GetField("_animations",BindingFlags.Instance | BindingFlags.NonPublic);
            var namedAnimations = new SpriteAnimationSet.NamedAnimation[spriteAnimations.Count];
            
            var index = 0;
            foreach (var animation in spriteAnimations)
            {
                namedAnimations[index++] = new SpriteAnimationSet.NamedAnimation
                {
                    name = animation.Key.ToString(),
                    data = animation.Value
                };
            }

            if (animationsField != null) animationsField.SetValue(spriteAnimationSet, namedAnimations);
            EditorUtility.SetDirty(spriteAnimationSet);
            
            return spriteAnimationSet;
        }
    }
}