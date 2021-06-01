using System;
using System.Collections.Generic;
using BDFramework.Editor.Asset;

namespace BDFramework.Editor
{
    /// <summary>
    /// BDFrame各种事件帮助
    /// </summary>
    static public class BDEditorBehaviorHelper
    {
        static private List<ABDEditorBehavior> BDFrameBehaviorList = new List<ABDEditorBehavior>();
        /// <summary>
        /// 初始化
        /// </summary>
        static public void Init()
        {
            var type = typeof(ABDEditorBehavior);
            var types = BDFrameEditorLife.Types;
            foreach (var t in types)
            {
                if (t.IsSubclassOf(type))
                {
                    var bdBehaviorInst = Activator.CreateInstance(t) as ABDEditorBehavior;
                    BDFrameBehaviorList.Add(bdBehaviorInst);
                }
            }
        }

        static public void OnBeginBuildDLL()
        {
            foreach (var behavior in BDFrameBehaviorList)
            {
                behavior.OnBeginBuildDLL();
            }
        }

        static public void OnEndBuildDLL(string outputPath)
        {
            foreach (var behavior in BDFrameBehaviorList)
            {
                behavior.OnEndBuildDLL(outputPath);
            }
        }

        static public void OnBeginBuildSqlite()
        {
            foreach (var behavior in BDFrameBehaviorList)
            {
                behavior.OnBeginBuildSqlite();
            }
        }

        static public void OnEndBuildSqlite(string outputPath)
        {
            foreach (var behavior in BDFrameBehaviorList)
            {
                behavior.OnEndBuildSqlite(outputPath);
            }
        }

        static public void OnBeginBuildAssetBundle(BuildInfo  buildInfo)
        {
            foreach (var behavior in BDFrameBehaviorList)
            {
                behavior.OnBeginBuildAssetBundle(buildInfo);
            }
        }

        static public void OnEndBuildAssetBundle(string outputPath)
        {
            foreach (var behavior in BDFrameBehaviorList)
            {
                behavior.OnEndBuildAssetBundle(outputPath);
            }
        }
        
        static public void OnExportExcel(Type type)
        {
            foreach (var behavior in BDFrameBehaviorList)
            {
                behavior.OnExportExcel(type);
            }
        }

    }
}