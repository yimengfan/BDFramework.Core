using System;
using System.Linq;
using BDFramework.Core.Tools;
using BDFramework.Editor.BuildPipeline;
using BDFramework.Editor.HotfixScript;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.EditorPipeline.BuildPipeline
{
    public class EditorWindow_BuildPipeline : OdinMenuEditorWindow
    {
        
        protected override OdinMenuTree BuildMenuTree()
        {
#if ODIN_INSPECTOR
            var tree = new OdinMenuTree(true);
            tree.DefaultMenuStyle.IconSize = 20.00f;
            var setting = BDEditorApplication.EditorSetting;
            if (setting == null)
            {
                return tree;
            }

            tree.Add("AssetsPipeline", null, EditorIcons.Airplane);
            tree.Add("AssetsPipeline/HotfixScript", new EditorWindow_BuildHotfixDll());
            //框架设置
            EditorWindow_BDFrameworkConfig win = new EditorWindow_BDFrameworkConfig();
            win.frameworkEditorSetting = BDEditorApplication.EditorSetting;
            tree.Add("框架Setting", win, EditorIcons.SettingsCog);

            
            //构建包体
            tree.Add("Build", null, EditorIcons.SmartPhone);
            tree.Add($"Build/{BApplication.GetPlatformLoadPath(RuntimePlatform.Android)}", new BuildAndroid(setting.Android, setting.AndroidDebug));
            tree.Add($"Build/{BApplication.GetPlatformLoadPath(RuntimePlatform.IPhonePlayer)}", new BuildIOS(setting.iOS, setting.iOSDebug));
            tree.Add($"Build/{BApplication.GetPlatformLoadPath(RuntimePlatform.WindowsPlayer)}", new BuildWindowsPlayer(setting.WindowsPlayer, setting.WindowsPlayerDebug));
            tree.Add($"Build/{BApplication.GetPlatformLoadPath(RuntimePlatform.OSXPlayer)}(待实现)", new BuildMacOSX());
            // tree.Add($"Build/{BApplication.GetPlatformPath(RuntimePlatform.OSXPlayer)}", new BuildAndroid());
            // tree.Add($"Build/{BApplication.GetPlatformPath(RuntimePlatform.WindowsPlayer)}", new BuildAndroid());
            // tree.Add("Test", EditorWindow.GetWindow<EditorWindow_BDFrameworkConfig>());
            //Player设置
            tree.Add("Player Settings", Resources.FindObjectsOfTypeAll<PlayerSettings>().FirstOrDefault());
            //tree.SortMenuItemsByName();
            //默认选择
            var selectMenuitem = tree.MenuItems.Find((m) => m.Name.Equals("Build"));
            if (selectMenuitem != null && selectMenuitem.ChildMenuItems.Count > 0)
            {
                var result = selectMenuitem.ChildMenuItems.Find((m) => m.Name.Equals(BApplication.GetPlatformLoadPath(BApplication.RuntimePlatform)));
                if (result == null)
                {
                    selectMenuitem = selectMenuitem.ChildMenuItems.Find((m) => m.Name.StartsWith(BApplication.GetPlatformLoadPath(BApplication.RuntimePlatform)));
                }
                else
                {
                    selectMenuitem = result;
                }
            }

            selectMenuitem.Select(true);

            return tree;
#else
            return null;
#endif
        }

        //保存设置
        protected override void OnDestroy()
        {
            BDEditorApplication.EditorSetting?.Save();
        }
    }
}
