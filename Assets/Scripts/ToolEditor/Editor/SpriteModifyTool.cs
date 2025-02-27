using System.IO;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace ToolEditor.Editor
{
    public class SpriteModifyTool : OdinEditorWindow
    {
        [MenuItem("Tools/Sprite Modify Tool")]
        private static void OpenWindow()
        {
            GetWindow<SpriteModifyTool>();
        }
        [LabelText("扩展左")]
        public int extendL;
        [LabelText("扩展右")]
        public int extendR;
        [LabelText("扩展上")]
        public int extendT;
        [LabelText("扩展下")]
        public int extendB;
        [LabelText("目录")][FolderPath]
        public string directory;
        [Button("处理")]
        private void Handle()
        {
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            var findAssets = AssetDatabase.FindAssets("t:Texture2d", new string[] { directory });

            foreach (var guid in findAssets)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var texture2D = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var resultTexture = new Texture2D(texture2D.width + extendL + extendR, texture2D.height + extendT + extendB, TextureFormat.RGBA32, false);
                //初始化所有像素为透明
                for (var i = 0; i < resultTexture.width; i++)
                {
                    for (var j = 0; j < resultTexture.height; j++)
                    {
                        resultTexture.SetPixel(i, j, Color.clear);
                    }
                }
                //复制原图到新图，并扩展
                for (var i = 0; i < texture2D.width; i++)
                {
                    for (var j = 0; j < texture2D.height; j++)
                    {
                        resultTexture.SetPixel(i + extendL, j + extendB, texture2D.GetPixel(i, j));
                    }
                }
                resultTexture.Apply();

                var encodeToPNG = resultTexture.EncodeToPNG();
                File.WriteAllBytes(path, encodeToPNG);
                AssetDatabase.Refresh();
            }
        }
    }
}