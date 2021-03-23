using System.Collections.Generic;
using System.Linq;
using BDFramework.Core.Debugger;
using BDFramework.Mgr;
using BDFramework.ResourceMgr;
using BDFramework.Sql;
using Code.BDFramework.Core.Tools;
using Code.BDFramework.Editor;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.EditorLife
{
    /// <summary>
    /// 这个类用以编辑器环境下辅助BD生命周期的开发
    /// </summary>
    [InitializeOnLoad]
    static public class BDFrameEditorLife
    {
        static BDFrameEditorLife()
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
                InitBDEditorLife();
            }
        }
        
        /// <summary>
        /// Editor代码刷新后执行
        /// </summary>
        static public void OnCompileCode()
        {
            if (EditorApplication.isPlaying) return;
            InitBDEditorLife();
        }


        static public void InitBDEditorLife()
        {
            //Editor的管理器初始化
            RegisterEditorMgrInstance();
            //BD生命周期启动
            BDFrameEditorBehaviorHelper.Init();
            BDFrameEditorConfigHelper.Init();
            BDApplication.Init();
            //编辑器下加载初始化
            BResources.Load(AssetLoadPath.Editor);
            //调试器启动
            DebuggerServerProcessManager.Inst.Start();
            //TODO 
            //这一行还是不能加到框架层，应该还是：哪里用 哪里主动load，
            //然后用完了close（SqliteLoder.Close（））。
            //不然sql文件editor环境下一直被占用，很多麻烦事
            // SqliteLoder.Load(Application.streamingAssetsPath);
        }


        /// <summary>
        /// 注册所有管理器，让管理器在编辑器下生效
        /// </summary>
        static public void RegisterEditorMgrInstance()
        {
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
        }
    }
}