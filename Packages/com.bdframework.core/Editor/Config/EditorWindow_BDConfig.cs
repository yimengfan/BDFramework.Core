using BDFramework.Editor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace BDFramework.Editor
{
    public class EditorWindow_BDConfig : OdinEditorWindow
    {
        [MenuItem("BDFrameWork工具箱/框架设置", false, (int)BDEditorMenuEnum.BDSetting)]
        public static void Open()
        {
            var window = GetWindow<EditorWindow_BDConfig>( false, "BDFrame设置");
            window.maxSize = window.minSize = new Vector2(600, 800);
            window.editorConfig = BDFrameEditorConfigHelper.EditorConfig;
            window.Show();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if(editorConfig!=null)
            editorConfig.Save();
        }
        
        [InlinePropertyAttribute]
        [Title("框架配置")]
        [LabelText("")]
        [LabelWidth(1)]
        public BDEditorConfig editorConfig;

        [Button(ButtonSizes.Medium, Name = "保存")]
        public void Save()
        {
            if (editorConfig != null)
            {
                editorConfig.Save();
            }
        }
    }
}