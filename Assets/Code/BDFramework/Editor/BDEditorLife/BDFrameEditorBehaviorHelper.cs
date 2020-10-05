using System;
using System.Collections.Generic;

namespace BDFramework.Editor.EditorLife
{
    /// <summary>
    /// BDFrame各种事件帮助
    /// </summary>
    static public class BDFrameEditorBehaviorHelper
    {
        static private List<ABDFrameEditorBehavior> BDFrameBehaviorList = new List<ABDFrameEditorBehavior>();

        /// <summary>
        /// 初始化
        /// </summary>
        static public void Init()
        {
            var type = typeof(ABDFrameEditorBehavior);
            var types = type.Assembly.GetTypes();
            foreach (var t in types)
            {
                if (t.IsSubclassOf(type))
                {
                    var bdBehaviorInst = Activator.CreateInstance(t) as ABDFrameEditorBehavior;
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

        static public void OnBeginBuildAssetBundle()
        {
            foreach (var behavior in BDFrameBehaviorList)
            {
                behavior.OnBeginBuildAssetBundle();
            }
        }

        static public void OnEndBuildAssetBundle(string outputPath)
        {
            foreach (var behavior in BDFrameBehaviorList)
            {
                behavior.OnEndBuildAssetBundle(outputPath);
            }
        }
    }
}