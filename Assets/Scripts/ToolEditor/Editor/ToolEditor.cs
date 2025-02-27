using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace ToolEditor.Editor
{
    public class ToolEditor : OdinEditorWindow
    {
        //显示窗口
        [MenuItem("Tools/ToolEditor")]
        private static void OpenWindow()
        {
            var window = GetWindow<ToolEditor>();
            window.Show();
        }
        [InlineButton("ResetGlobalFrameDuration","设置")][LabelText("全局动画帧时间")]
        public float globalAnimationFrameDuration = 0.1f;
        [UsedImplicitly]
        private void ResetGlobalFrameDuration()
        {
            var findAssets = AssetDatabase.FindAssets("t:SpriteAnimation");
            foreach (var guid in findAssets)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var animation = AssetDatabase.LoadAssetAtPath<SpriteAnimation>(path);
                for (var i = 0; i < animation.FrameDurations.Length; i++)
                {
                    if (animation.FrameDurations[i] < 1)
                    {
                        animation.FrameDurations[i] = globalAnimationFrameDuration;
                    }
                }
                EditorUtility.SetDirty(animation);
            }
            AssetDatabase.SaveAssets();
        }

    }
}