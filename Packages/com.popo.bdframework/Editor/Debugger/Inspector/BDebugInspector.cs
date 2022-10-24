using BDFramework.Core.Tools;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.Debugger
{
    [CustomEditor(typeof(BDebug))]
    public class BDebugInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var debug = target as BDebug;

            if (!Application.isPlaying)
            {
                if (debug.IsLog)
                {
                    //增加宏
                    foreach (var bt in BApplication.SupportBuildTargetGroups)
                    {
                        var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(bt);
                        if (!symbols.Contains(BDebug.ENABLE_BDEBUG))
                        {
                            string str = "";
                            if (!string.IsNullOrEmpty(symbols))
                            {
                                if (!str.EndsWith(";"))
                                {
                                    str = symbols + ";" + BDebug.ENABLE_BDEBUG;
                                }
                                else
                                {
                                    str = symbols + BDebug.ENABLE_BDEBUG;
                                }
                            }
                            else
                            {
                                str = BDebug.ENABLE_BDEBUG;
                            }


                            PlayerSettings.SetScriptingDefineSymbolsForGroup(bt, str);
                        }
                    }
                }
                else
                {
                    //移除宏
                    foreach (var bt in BApplication.SupportBuildTargetGroups)
                    {
                        var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(bt);
                        if (symbols.Contains(BDebug.ENABLE_BDEBUG + ";"))
                        {
                            symbols = symbols.Replace(BDebug.ENABLE_BDEBUG + ";", "");
                        }
                        else if (symbols.Contains(BDebug.ENABLE_BDEBUG))
                        {
                            symbols = symbols.Replace(BDebug.ENABLE_BDEBUG, "");
                        }
                        PlayerSettings.SetScriptingDefineSymbolsForGroup(bt, symbols);
                    }
                }
            }
        }
    }
}
