using BDFramework.Editor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace BDFramework.Editor
{
    public class EditorWindow_BDFrameworkConfig : OdinEditorWindow
    {
        [MenuItem("BDFrameWork工具箱/框架设置", false, (int) BDEditorGlobalMenuItemOrderEnum.BDFrameworkSetting)]
        public static void Open()
        {
            var window = GetWindow<EditorWindow_BDFrameworkConfig>(false, "BDFrame设置");
            window.maxSize = window.minSize = new Vector2(600, 800);
            window.FrameWorkEditorSetting = BDEditorApplication.BDFrameWorkFrameEditorSetting;
            window.Show();
        }

        public void OnDestroy()
        {
            base.OnDestroy();
            if (FrameWorkEditorSetting != null)
                FrameWorkEditorSetting.Save();
        }

        [InlinePropertyAttribute]
        [Title("框架配置")]
        [LabelText("")]
        [LabelWidth(1)]
      
        public BDFrameWorkEditorSetting FrameWorkEditorSetting;

        [Button(ButtonSizes.Medium, Name = "保存")]
        [PropertySpace(20)]
        public void Save()
        {
            if (FrameWorkEditorSetting != null)
            {
                FrameWorkEditorSetting.Save();
            }
        }
    }
}