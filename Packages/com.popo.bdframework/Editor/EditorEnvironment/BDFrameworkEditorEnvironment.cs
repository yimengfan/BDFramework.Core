using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BDFramework.Core.Debugger;
using BDFramework.Mgr;
using BDFramework.ResourceMgr;
using BDFramework.Core.Tools;
using BDFramework.Editor.HotfixPipeline;
using BDFramework.Editor.Task;
using BDFramework.Hotfix.Reflection;
using BDFramework.ScreenView;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.Environment
{
    /// <summary>
    /// Editor下框架环境创建
    /// </summary>
   // [InitializeOnLoad]
    static public class BDFrameworkEditorEnvironment
    {
        /// <summary>
        /// 是否完成初始化
        /// </summary>
        static public bool IsInited { get; private set; } = false;

        /// <summary>
        /// 编辑器任务的
        /// </summary>
        static public EditorTask EditorTaskInstance { get; private set; } = null;
        [InitializeOnLoadMethod]
        static void BDFrameworkEditorEnvironmentInit()
        {
            //TODO 
            //一般情况下 打开unity.或者reloadAssembly 会重新初始化框架
            //但是ExitPlaymode后不会触发ReloadAssembly,所以有些静态对象会缓存
            //非播放模式，初始化框架编辑器
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                InitEditorEnvironment();
            }

            //防止重复注册事件
            EditorApplication.update -= EditorUpdate;
            EditorApplication.update += EditorUpdate;
            EditorApplication.update -= EditorUpdate_CheckGuideWindow;
            EditorApplication.update += EditorUpdate_CheckGuideWindow;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;

          
            
        }

        /// <summary>
        /// 代码编译完成后
        /// </summary>
        // [UnityEditor.Callbacks.DidReloadScripts(0)]
        // static void OnScriptReload()
        // {
        //     OnCodeBuildComplete();
        // }

        /// <summary>
        /// 退出播放模式
        /// </summary>
        /// <param name="state"></param>
        static private void OnPlayModeChanged(PlayModeStateChange state)
        {
            //非播放模式,初始化框架~
            switch (state)
            {
                case PlayModeStateChange.EnteredEditMode:
                {
                    InitEditorEnvironment();
                }
                    break;
            }
        }


        /// <summary>
        /// 初始化框架编辑器
        /// </summary>
        static public void InitEditorEnvironment()
        {
            //是否为batchmode
            if (Application.isBatchMode)
            {
                Debug.Log("BDFramework version:" + BDLauncher.Version);
            }

            //只有在非Playing的时候才初始化
            if (EditorApplication.isPlayingOrWillChangePlaymode || IsInited)
            {
                return;
            }

            try
            {
                //BD生命周期启动
                BDApplication.Init();
                BDEditorApplication.Init();
                //编辑器下加载初始化
                BResources.Load(AssetLoadPathType.Editor);
                //加载主工程的DLL Type
                var assemblyPath = BDApplication.Library + "/ScriptAssemblies/Assembly-CSharp.dll";
                var editorAssemlyPath = BDApplication.Library + "/ScriptAssemblies/Assembly-CSharp-Editor.dll";
                if (File.Exists(assemblyPath) && File.Exists(editorAssemlyPath))
                {
                    var gAssembly = Assembly.LoadFile(assemblyPath);
                    var eAssemlby = Assembly.LoadFile(editorAssemlyPath);
                    RegisterMainProjectAssembly(gAssembly, eAssemlby);
                }
                //Pipeline初始化
                HotfixPipelineTools.Init();
                //编辑器任务执行
                if (EditorTaskInstance == null)
                {
                    EditorTaskInstance = new EditorTask();
                    EditorTaskInstance.CollectEditorTaskMedthod();
                }
                EditorTaskInstance.OnUnityLoadOrCodeRecompiled();
                //最后，完成初始化
                IsInited = true;
                //  Debug.Log("框架编辑器环境初始化成功!");
            }
            catch (Exception e)
            {
                Debug.LogError("框架编辑器环境初始化失败!");
                Debug.LogError(e);
                throw;
            }
        }


        #region 主工程 Assembly

        /// <summary>
        /// 游戏逻辑的Assembly
        /// </summary>
        static public Type[] Types { get; private set; } = new Type[] { };

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
            typeList.AddRange(typeof(BDFrameworkEditorEnvironment).Assembly.GetTypes());
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
            ManagerInstHelper.Load(types);
        }

        #endregion

        /// <summary>
        /// 当主工程准备好
        /// </summary>
        public static void OnMainProjectReady()
        {
            RegisterEditorMgrbase(Types);
            //Editor的管理器初始化
            BDFrameworkPublishPipelineHelper.Init();
            //调试器启动
            DebuggerServerProcessManager.Inst.Start();
        }

        /// <summary>
        /// 编辑器的Update
        /// </summary>
        static public void EditorUpdate()
        {
            //编辑器任务的update
            EditorTaskInstance?.OnEditorUpdate();
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