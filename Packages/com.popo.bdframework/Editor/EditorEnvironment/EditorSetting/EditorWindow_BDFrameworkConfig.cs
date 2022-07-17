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
            var window = GetWindow<EditorWindow_BDFrameworkConfig>(false, "BDFramework设置");
            window.maxSize = window.minSize = new Vector2(600, 800);
            window.frameworkEditorSetting = BDEditorApplication.BDFrameworkEditorSetting;
            window.Show();
        }

        public void OnDestroy()
        {
            base.OnDestroy();
            if (frameworkEditorSetting != null)
                frameworkEditorSetting.Save();
        }

        [InlinePropertyAttribute]
        [Title("框架配置")]
        [LabelText("")]
        [LabelWidth(1)]
        public BDFrameworkEditorSetting frameworkEditorSetting;

        
        [Button(ButtonSizes.Medium, Name = "保存")]
        [PropertySpace(20)]
        public void Save()
        {
            if (frameworkEditorSetting != null)
            {
                frameworkEditorSetting.Save();
            }
        }
    }
}