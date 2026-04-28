using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BDFramework.Core.Tools
{
    /// <summary>
    /// 框架运行时应用上下文。
    /// Framework runtime application context.
    /// 该类型缓存 Unity 路径与平台信息，供资源、日志和构建流程共享。
    /// This type caches Unity paths and platform information shared by resource, logging, and build flows.
    /// </summary>
    static public class BApplication
    {
        static public bool IsPlaying { get; set; }

        /// <summary>
        /// 标记路径状态是否已经在安全线程完成初始化。
        /// Marks whether the cached path state has already been initialized on a safe thread.
        /// </summary>
        static private bool hasInitializedPathState;

        /// <summary>
        /// 定义 支持的平台
        /// </summary>
        /// <returns></returns>
        static RuntimePlatform[] _supportPlatform = new RuntimePlatform[]
        {
            RuntimePlatform.Android, RuntimePlatform.IPhonePlayer,
            /***********新增pc平台************/ RuntimePlatform.OSXPlayer, RuntimePlatform.WindowsPlayer
        };

        /// <summary>
        /// 定义 支持的平台
        /// </summary>
        /// <returns></returns>
        public static RuntimePlatform[] SupportPlatform
        {
            //返回一个副本
            get { return _supportPlatform.ToArray(); }
        }


        static BApplication()
        {
            TryInitializePathState(
                () => Application.dataPath,
                () => Application.persistentDataPath,
                () => Application.streamingAssetsPath,
                message => Debug.LogWarning(message),
                "StaticConstructor");
        }

        /// <summary>
        /// 在场景加载前于主线程重试一次路径初始化。
        /// Retry path initialization once on the main thread before scene loading.
        /// 某些 Player 启动路径会先在 loading thread 触发静态构造，导致 `Application.dataPath`
        /// 这类 Unity API 抛出“只能在主线程调用”的异常；这里把真正的安全初始化收敛到主线程重试。
        /// Some player startup paths hit the static constructor from the loading thread first, making Unity APIs
        /// such as `Application.dataPath` throw “main thread only”; this hook retries the real initialization on the main thread.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static private void InitializePathStateBeforeSceneLoad()
        {
            TryInitializePathState(
                () => Application.dataPath,
                () => Application.persistentDataPath,
                () => Application.streamingAssetsPath,
                message => Debug.LogWarning(message),
                "BeforeSceneLoad");
        }

        #region Unity3d路径重写

        /// <summary>
        /// Persistent
        /// </summary>
        static public string persistentDataPath { get; private set; }

        /// <summary>
        /// Streaming
        /// </summary>
        static public string streamingAssetsPath { get; private set; }


        /// <summary>
        ///  获取当前平台,不会返回Editor枚举
        ///  用以解决在editor模式 只会返回eitor runtime问题.
        ///   能在Editor下返回正常Runtime内容.
        /// </summary>
        static public RuntimePlatform RuntimePlatform
        {
            //注意这里 异常情况切平台（如打包ab 自动切到目标平台，到一半闪退）
            //会导致代码未编译，会返回老的宏~ 需要重新把BDFramework导入
            get
            {
#if UNITY_IOS
                return RuntimePlatform.IPhonePlayer;
#elif UNITY_ANDROID
                return RuntimePlatform.Android;
#elif UNITY_STANDALONE_WIN
                return RuntimePlatform.WindowsPlayer;
#elif UNITY_STANDALONE_OSX
                return RuntimePlatform.OSXPlayer;
#elif UNITY_WEBGL
                return RuntimePlatform.WebGLPlayer;
//以下不常用,不一定对
#elif UNITY_TVOS
                return RuntimePlatform.tvOS;
#elif UNITY_XBOXONE
                return RuntimePlatform.GameCoreXboxSeries;
#elif UNITY_PS4
                return RuntimePlatform.PS4;
#elif UNITY_PS5
               return   RuntimePlatform.PS5;
#elif UNITY_LUMIN
                return RuntimePlatform.Lumin; // Magic Leap OS 
#else
                return Application.platform;
#endif
            }
        }

        #endregion


        #region 自定义路径

        /// <summary>
        /// 项目根目录
        /// </summary>
        static public string ProjectRoot { get; private set; }

        /// <summary>
        /// BD Assets目录以外的workspace目录
        /// </summary>
        static public string BDWorkSpace { get; private set; }

        /// <summary>
        /// Library
        /// </summary>
        static public string Library { get; private set; }

        /// <summary>
        /// Package目录
        /// </summary>
        static public string Package { get; private set; }

        /// <summary>
        /// 资源的根目录
        /// </summary>
        static public string RuntimeResourceLoadPath { get; private set; }

        /// <summary>
        /// Editor的资源路径
        /// </summary>
        public static string EditorResourcePath { get; private set; }

        /// <summary>
        /// Editor的资源路径
        /// </summary>
        public static string EditorResourceRuntimePath { get; private set; }

        /// <summary>
        /// Devops 路径
        /// </summary>
        public static string DevOpsPath { get; private set; }

        /// <summary>
        /// Devops 代码路径
        /// </summary>
        public static string DevOpsCodePath { get; private set; }

        /// <summary>
        /// Devops 资源路径
        /// </summary>
        public static string DevOpsPublishAssetsPath { get; private set; }

        /// <summary>
        /// 发布包体路径
        /// </summary>
        public static string DevOpsPublishClientPackagePath { get; private set; }

        /// <summary>
        /// Devops 配置文件路径
        /// </summary>
        public static string DevOpsConfigPath { get; private set; }

        /// <summary>
        /// Devops CI路径
        /// </summary>
        public static string DevOpsCIPath { get; private set; }

        /// <summary>
        /// 编辑器缓存目录
        /// </summary>
        public static string BDEditorCachePath { get; private set; }

        #endregion

        /// <summary>
        /// 尝试读取 Unity 路径快照并初始化框架路径缓存。
        /// Try to read a Unity path snapshot and initialize the framework path cache.
        /// 该方法在 loading thread 触发 Unity “main thread only” 异常时会吞掉异常并返回 false，
        /// 以避免静态构造失败把整个类型永久毒化；随后由主线程重试完成真正初始化。
        /// This method catches Unity “main thread only” exceptions raised from the loading thread and returns false,
        /// avoiding a permanently poisoned static constructor; the main thread then retries and completes initialization.
        /// </summary>
        /// <param name="dataPathProvider">返回 Unity dataPath 的委托。Delegate that returns the Unity dataPath.</param>
        /// <param name="persistentDataPathProvider">返回 Unity persistentDataPath 的委托。Delegate that returns the Unity persistentDataPath.</param>
        /// <param name="streamingAssetsPathProvider">返回 Unity streamingAssetsPath 的委托。Delegate that returns the Unity streamingAssetsPath.</param>
        /// <param name="logWarning">输出延迟初始化告警的委托。Delegate that emits the deferred-initialization warning.</param>
        /// <param name="source">当前初始化尝试的来源标签。Source label of the current initialization attempt.</param>
        /// <returns>成功返回 true；若需要延迟到主线程重试则返回 false。Returns true on success; returns false when initialization must be retried on the main thread.</returns>
        static private bool TryInitializePathState(
            Func<string> dataPathProvider,
            Func<string> persistentDataPathProvider,
            Func<string> streamingAssetsPathProvider,
            Action<string> logWarning,
            string source)
        {
            if (hasInitializedPathState)
            {
                return true;
            }

            try
            {
                ApplyPathState(
                    dataPathProvider(),
                    persistentDataPathProvider,
                    streamingAssetsPathProvider);
                hasInitializedPathState = true;
                return true;
            }
            catch (UnityException ex)
            {
                logWarning?.Invoke($"[BApplication] {source} 阶段路径初始化延后，等待主线程重试: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 根据 Unity 路径快照应用框架路径缓存。
        /// Apply the framework path cache from a Unity path snapshot.
        /// </summary>
        /// <param name="dataPath">Unity 的 dataPath。Unity dataPath.</param>
        /// <param name="persistentDataPathProvider">返回 Unity persistentDataPath 的委托。Delegate that returns the Unity persistentDataPath.</param>
        /// <param name="streamingAssetsPathProvider">返回 Unity streamingAssetsPath 的委托。Delegate that returns the Unity streamingAssetsPath.</param>
        static private void ApplyPathState(
            string dataPath,
            Func<string> persistentDataPathProvider,
            Func<string> streamingAssetsPathProvider)
        {
            //自定义路径
            ProjectRoot = dataPath.Replace("/Assets", "");
            Library = ProjectRoot + "/Library";
            Package = ProjectRoot + "/Package";
            BDWorkSpace = ProjectRoot + "/BDWorkSpace";
            RuntimeResourceLoadPath = "Assets/Resource/Runtime";
            //Editor相关目录
            EditorResourcePath = "Assets/Resource_SVN";
            EditorResourceRuntimePath = $"{EditorResourcePath}/Runtime";
            BDEditorCachePath = $"{Library}/BDFrameCache";
            //DevOps路径
            //跟Assets同级
            DevOpsPath = $"DevOps";
            DevOpsPublishAssetsPath = $"{DevOpsPath}/PublishAssets";
            DevOpsPublishClientPackagePath = $"{DevOpsPath}/PublishPackages";
            DevOpsConfigPath = $"{DevOpsPath}/Config";
            DevOpsCIPath = $"{DevOpsPath}/CI";
            //Unity路径
#if UNITY_EDITOR
            persistentDataPath = ProjectRoot + "/.AppData";
#elif UNITY_STANDALONE_WIN|| UNITY_STANDALONE_OSX 
        persistentDataPath = dataPath + "/.AppData";
#else
        persistentDataPath = persistentDataPathProvider();
#endif
         
#if UNITY_EDITOR
            streamingAssetsPath = DevOpsPublishAssetsPath;
#else
        streamingAssetsPath = streamingAssetsPathProvider();
#endif
        }


        /// <summary>
        /// 获取当前平台Platform的路径
        /// </summary>
        /// <returns></returns>
        static public string GetRuntimePlatformPath()
        {
            return GetPlatformLoadPath(RuntimePlatform);
        }



        /// <summary>
        /// 平台资源的父路径
        /// 这里建议用BDApplication.platform进行传参
        /// </summary>
        public static string GetPlatformLoadPath(RuntimePlatform platform)
        {
            switch (platform)
            {
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                    return "windows";
                case RuntimePlatform.Android:
                    return "android";
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                    return "osx";
                case RuntimePlatform.IPhonePlayer:
                    return "ios";
            }

            return platform.ToString().Replace("Editor", "").ToLower();
        }


#if UNITY_EDITOR
        /// <summary>
        /// 定义支持的平台
        /// </summary>
        /// <returns></returns>
        static BuildTargetGroup[] _supportBuildTargetGroup = new BuildTargetGroup[]
        {
            BuildTargetGroup.Android, BuildTargetGroup.iOS,
            /***********新增pc平台************/ BuildTargetGroup.Standalone,
        };

        /// <summary>
        /// 定义 支持的平台
        /// </summary>
        /// <returns></returns>
        public static BuildTargetGroup[] SupportBuildTargetGroups
        {
            //返回一个副本
            get { return _supportBuildTargetGroup.ToArray(); }
        }

        #region Runtime目录支持

        /// <summary>
        /// 获取所有runtime的目录
        /// </summary>
        /// <returns></returns>
        public static List<string> GetAllRuntimeDirects()
        {
            //搜索所有资源
            var root = Application.dataPath;
            //获取根路径所有runtime
            var directories = Directory.GetDirectories(root, "*", SearchOption.TopDirectoryOnly).ToList();

            //ret
            List<string> retList = new List<string>();
            foreach (var dirt in directories)
            {
                //
                var _dirt = dirt + "/Runtime";
                if (Directory.Exists(_dirt))
                {
                    _dirt = _dirt.Replace("\\", "/").Replace(Application.dataPath, "Assets");
                    retList.Add(_dirt);
                }
            }

            return retList;
        }

        /// <summary>
        /// 获取所有资源
        /// </summary>
        /// <returns></returns>
        public static List<string> GetAllRuntimeAssetsPath()
        {
            List<string> allAssetsList = new List<string>();
            var directories = GetAllRuntimeDirects();
            //所有资源列表
            foreach (var dir in directories)
            {
                var rets = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories).Where((s) => !s.EndsWith(".meta"));
                allAssetsList.AddRange(rets);
            }


            for (int i = 0; i < allAssetsList.Count; i++)
            {
                var res = allAssetsList[i];
                allAssetsList[i] = res.Replace("\\", "/");
            }

            return allAssetsList;
        }

        #endregion

        #region BuildTarget 和RuntimePlatform互转

        /// <summary>
        /// 通过buildTarget获取platform
        /// </summary>
        /// <param name="buildTarget"></param>
        /// <returns></returns>
        public static string GetPlatformPath(BuildTarget buildTarget)
        {
            var platform = GetRuntimePlatform(buildTarget);
            return GetPlatformLoadPath(platform);
        }


        /// <summary>
        /// 获取BuildTarget
        /// </summary>
        /// <param name="platform"></param>
        /// <returns></returns>
        public static BuildTarget GetBuildTarget(RuntimePlatform platform)
        {
            //构建平台
            BuildTarget target = BuildTarget.Android;
            switch (platform)
            {
                case RuntimePlatform.Android:
                    target = BuildTarget.Android;
                    break;
                case RuntimePlatform.IPhonePlayer:
                    target = BuildTarget.iOS;
                    break;
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                {
                    target = BuildTarget.StandaloneWindows64;
                }
                    break;
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                {
                    target = BuildTarget.StandaloneOSX;
                }
                    break;
            }

            return target;
        }

        /// <summary>
        /// 获取TargetGroup
        /// </summary>
        /// <param name="platform"></param>
        /// <returns></returns>
        public static BuildTargetGroup GetBuildTargetGroup(RuntimePlatform platform)
        {
            //构建平台
            BuildTargetGroup target = BuildTargetGroup.Standalone;
            switch (platform)
            {
                case RuntimePlatform.Android:
                    target = BuildTargetGroup.Android;
                    break;
                case RuntimePlatform.IPhonePlayer:
                    target = BuildTargetGroup.iOS;
                    break;
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                {
                    target = BuildTargetGroup.Standalone;
                }
                    break;
            }

            return target;
        }

        /// <summary>
        /// 获取获取TargetGroup
        /// </summary>
        /// <param name="platform"></param>
        /// <returns></returns>
        public static BuildTargetGroup GetBuildTargetGroup(BuildTarget buildTarget)
        {
            //构建平台
            BuildTargetGroup target = BuildTargetGroup.Standalone;
            switch (buildTarget)
            {
                case BuildTarget.Android:
                {
                    target = BuildTargetGroup.Android;
                }
                    break;
                case BuildTarget.iOS:
                {
                    target = BuildTargetGroup.iOS;
                }
                    break;
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneOSX:
                {
                    target = BuildTargetGroup.Standalone;
                }
                    break;
            }

            return target;
        }

        /// <summary>
        /// 获取runtimeplatform
        /// </summary>
        /// <param name="buildTarget"></param>
        /// <returns></returns>
        public static RuntimePlatform GetRuntimePlatform(BuildTarget buildTarget)
        {
            var platform = RuntimePlatform.Android;
            switch (buildTarget)
            {
                case BuildTarget.Android:
                {
                    platform = RuntimePlatform.Android;
                }
                    break;
                case BuildTarget.iOS:
                {
                    platform = RuntimePlatform.IPhonePlayer;
                }
                    break;
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                {
                    platform = RuntimePlatform.WindowsPlayer;
                }
                    break;
                case BuildTarget.StandaloneOSX:
                {
                    platform = RuntimePlatform.OSXPlayer;
                }
                    break;
            }

            return platform;
        }

        #endregion

        #region 平台路径获取
        /// <summary>
        /// 平台资源的父路径
        /// </summary>
        public static string GetPlatformLoadPath(BuildTarget bt)
        {
            var platform = GetRuntimePlatform(bt);

            return GetPlatformLoadPath(platform);
        }
        /// <summary>
        /// 平台资源的父路径
        /// </summary>
        public static string GetPlatformLoadPath(string dir, BuildTarget bt)
        {
            var platform = GetRuntimePlatform(bt);
            var platpath = GetPlatformLoadPath(platform);
            return  Path.Combine(dir, platpath);
        }
        /// <summary>
        /// 获取发布资产平台目录
        /// </summary>
        /// <param name="platform"></param>
        /// <returns></returns>
        static public string GetPlatformDevOpsPublishAssetsPath(RuntimePlatform platform)
        {
            var path = GetPlatformLoadPath(platform);
            return Path.Combine(DevOpsPublishAssetsPath, path);
        }

        /// <summary>
        /// 获取发布包体平台目录
        /// </summary>
        /// <param name="platform"></param>
        /// <returns></returns>
        static public string GetPlatformDevOpsPublishPackagePath(RuntimePlatform platform)
        {
            var path = GetPlatformLoadPath(platform);
            return Path.Combine(DevOpsPublishClientPackagePath, path);
        }

        /// <summary>
        /// 获取发布资产平台目录
        /// </summary>
        /// <param name="bt"></param>
        /// <returns></returns>
        static public string GetPlatformDevOpsPublishAssetsPath(BuildTarget bt)
        {
            var path = GetPlatformPath(bt);
            return Path.Combine(DevOpsPublishAssetsPath, path);
        }

        /// <summary>
        /// 获取发布包体平台目录
        /// </summary>
        /// <param name="bt"></param>
        /// <returns></returns>
        static public string GetPlatformDevOpsPublishPackagePath(BuildTarget bt)
        {
            var path = GetPlatformPath(bt);
            return Path.Combine(DevOpsPublishClientPackagePath, path);
        }

        #endregion


#endif
    }
}
