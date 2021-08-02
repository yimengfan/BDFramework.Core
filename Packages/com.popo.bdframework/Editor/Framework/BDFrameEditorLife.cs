using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BDFramework.Core.Debugger;
using BDFramework.Mgr;
using BDFramework.ResourceMgr;
using BDFramework.Core.Tools;
using BDFramework.Reflection;
using LitJson;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor
{
    /// <summary>
    /// 这个类用以编辑器环境下辅助BD生命周期的开发
    /// </summary>
    [InitializeOnLoad]
    static public class BDFrameEditorLife
    {

        /// <summary>
        /// 是否完成初始化
        /// </summary>
        static public bool IsInited { get; private set; } = false;
        static BDFrameEditorLife()
        {
            EditorApplication.playModeStateChanged += OnPlayExit;
            EditorApplication.update += EditorUpdate_CheckGuideWindow;
        }

        /// <summary>
        /// 代码编译完成后
        /// </summary>
        [UnityEditor.Callbacks.DidReloadScripts(0)]
        static void OnScriptReload()
        {
            OnCodeBuildComplete();
        }

        /// <summary>
        /// 退出播放模式
        /// </summary>
        /// <param name="state"></param>
        static private void OnPlayExit(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                InitBDFrameworkEditor();
            }
        }

        /// <summary>
        /// Editor代码刷新后执行
        /// </summary>
        static public void OnCodeBuildComplete()
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            //初始化框架编辑器
            InitBDFrameworkEditor();
        }

        /// <summary>
        /// 初始化框架编辑器
        /// </summary>
        static public void InitBDFrameworkEditor()
        {
            //BD生命周期启动
            BDApplication.Init();
            BDEditorApplication.Init();
            //编辑器下加载初始化
            BResources.Load(AssetLoadPath.Editor);
            //加载主工程的DLL Type
            var assemblyPath = BDApplication.Library + "/ScriptAssemblies/Assembly-CSharp.dll";
            var editorAssemlyPath = BDApplication.Library + "/ScriptAssemblies/Assembly-CSharp-Editor.dll";
            if (File.Exists(assemblyPath) && File.Exists(editorAssemlyPath))
            {
                var gAssembly = Assembly.LoadFile(assemblyPath);
                var eAssemlby = Assembly.LoadFile(editorAssemlyPath);
                RegisterMainProjectAssembly(gAssembly, eAssemlby);
            }

            
            
            
            
            //最后，完成初始化
            IsInited = true;
        }


        #region 主工程 Assembly

        /// <summary>
        /// 游戏逻辑的Assembly
        /// </summary>
        static public Type[] Types { get; set; } = new Type[] { };

        /// <summary>
        /// 外部注册主工程的Assembly
        /// </summary>
        /// <param name="gameLogicAssembly"></param>
        /// <param name="gameEditorAssembly"></param>
        static public void RegisterMainProjectAssembly(Assembly gameLogicAssembly, Assembly gameEditorAssembly)
        {
            //编辑器所有类
            List<Type> typeList = new List<Type>();
            typeList.AddRange(gameLogicAssembly.GetTypes());
            typeList.AddRange(gameEditorAssembly.GetTypes());
            //BD编辑器下所有的类
            typeList.AddRange(typeof(BDFrameEditorLife).Assembly.GetTypes());
            //BDRuntime下所有类
            typeList.AddRange(typeof(BDLauncher).Assembly.GetTypes());
            Types = typeList.ToArray();
            //
            OnMainProjectReady();
        }

        /// <summary>
        /// 注册所有管理器，让管理器在编辑器下生效
        /// </summary>
        static private void RegisterEditorMgrbase(Type[] types)
        {
            //编辑器下管理器注册
            List<IMgr> mgrs = new List<IMgr>();
            foreach (var t in types)
            {
                if (t != null && t.BaseType != null && t.BaseType.FullName != null &&
                    t.BaseType.FullName.Contains(".ManagerBase`2"))
                {
                    var i = t.BaseType.GetProperty("Inst").GetValue(null, null) as IMgr;
                    mgrs.Add(i);
                }
            }

            foreach (var type in types)
            {
                var attr = type.GetAttributeInILRuntime<ManagerAttribute>();
                if (attr == null)
                {
                    continue;
                }

                //1.类型注册到管理器
                foreach (var mgr in mgrs)
                {
                    mgr.CheckType(type, attr);
                }
            }
        }

        #endregion

        /// <summary>
        /// 当主工程准备好
        /// </summary>
        public static void OnMainProjectReady()
        {
            RegisterEditorMgrbase(Types);
            //Editor的管理器初始化
            BDEditorBehaviorHelper.Init();
            //调试器启动
            DebuggerServerProcessManager.Inst.Start();

        }


        /// <summary>
        /// 引导启动页面
        /// </summary>
        static public void EditorUpdate_CheckGuideWindow()
        {
            EditorApplication.update -= EditorUpdate_CheckGuideWindow;

            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorWindow_BDFrameworkStart.AutoOpen();
            }
        }
    }
}