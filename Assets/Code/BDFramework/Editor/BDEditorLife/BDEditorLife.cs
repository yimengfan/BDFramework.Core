using System.Collections.Generic;
using System.Linq;
using BDFramework.Core.Debugger;
using BDFramework.Mgr;
using BDFramework.ResourceMgr;
using BDFramework.Sql;
using Code.BDFramework.Editor;
using SQLite4Unity3d;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.EditorLife
{
    [UnityEditor.InitializeOnLoad]
    static public class BDEditorLife
    {
        static BDEditorLife()
        {
            EditorApplication.delayCall += OnCompileCode;
            EditorApplication.playModeStateChanged += OnPlayExit;
        }


        /// <summary>
        /// 退出播放模式
        /// </summary>
        /// <param name="state"></param>
        static private void OnPlayExit(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                BDEditorInit();
            }
        }
        
        /// <summary>
        /// Editor代码刷新后执行
        /// </summary>
        static public void OnCompileCode()
        {
            if (EditorApplication.isPlaying) return;
            BDEditorInit();
        }


        static public void BDEditorInit()
        {
            #region 注册所以管理器，让管理器在编辑器下生效

            //项目所有类
            var types = typeof(BDLauncher).Assembly.GetTypes().ToList();
            //编辑器所有类
            var editorTypes = typeof(BDEditorMenuEnum).Assembly.GetTypes();
            types.AddRange(editorTypes);

            List<IMgr> mgrs = new List<IMgr>();
            foreach (var t in types)
            {
                if (t != null && t.BaseType != null && t.BaseType.FullName != null &&
                    t.BaseType.FullName.Contains(".ManagerBase`2"))
                {
                    var i = t.BaseType.GetProperty("Inst").GetValue(null, null) as IMgr;
                    mgrs.Add(i);
                    continue;
                }
            }

            foreach (var t in types)
            {
                foreach (var mgr in mgrs)
                {
                    mgr.CheckType(t);
                }
            }
            

            Debug.Log("BDFrameEditor:管理器注册完成");
            #endregion
            
            DebuggerServerProcessManager.Inst.Start();
            BDEditorHelper.Init();
            BResources.Load("");
            SqliteLoder.Load(Application.streamingAssetsPath);
        }
    }
}