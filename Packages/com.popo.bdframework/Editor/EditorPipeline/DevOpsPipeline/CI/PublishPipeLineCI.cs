using System;
using System.IO;
using BDFramework.Core.Tools;
using BDFramework.Editor.BuildPipeline;
using BDFramework.Editor.EditorPipeline.DevOps;
using BDFramework.Editor.Environment;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build.Player;
using UnityEngine;

namespace BDFramework.Editor.DevOps
{
    /// <summary>
    /// 构建相关的CI接口
    /// </summary>
    static public class PublishPipeLineCI
    {


        static PublishPipeLineCI()
        {
            //TODO : 初始化编辑器,必须
            if (Application.isBatchMode)
            {
                BDFrameworkEditorEnvironment.InitEditorEnvironment();
            }

        }




        #region 代码打包检查

        [CI(Des = "代码检查")]
        public static void CheckEditorCode()
        {
            CheckCode();
        }

        /// <summary>
        /// 检测代码
        /// </summary>
        /// <returns></returns>
        public static bool CheckCode()
        {
            //检查下打包前的代码错
            var setting = new ScriptCompilationSettings();
            setting.options = ScriptCompilationOptions.Assertions;
            setting.target = BuildTarget.Android;
            var ret = PlayerBuildInterface.CompilePlayerScripts(setting, BApplication.Library + "/BuildTest");
            if (ret.assemblies.Contains("Assembly-CSharp.dll"))
            {
                return true;
            }

            return false;
        }


        /// <summary>
        /// 构建dll
        /// </summary>
        public static void BuildDLL()
        {
          }

        #endregion

        #region 发布母包

        const string CLIENT_VERSION_ARG = "-clientVersion";

        static private string GetCommandLineArg(string argName)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], argName, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        static private string GetClientVersion()
        {
            var clientVersion = GetCommandLineArg(CLIENT_VERSION_ARG);
            if (!string.IsNullOrWhiteSpace(clientVersion))
            {
                return clientVersion.Trim();
            }

            return BuildTools_ClientPackage.GetDefaultClientVersion();
        }

        static private bool IsValidJdkPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                return false;
            }

#if UNITY_EDITOR_WIN
            return File.Exists(Path.Combine(path, "bin", "javac.exe"));
#else
            return File.Exists(Path.Combine(path, "bin", "javac"));
#endif
        }

        static private string FindJdkFromEnvironment()
        {
            var envNames = new[]
            {
                "UNITY_JDK_PATH",
                "UNITY_JDK",
                "JDK_HOME",
                "JAVA_HOME",
            };

            foreach (var envName in envNames)
            {
                var candidate = System.Environment.GetEnvironmentVariable(envName);
                if (IsValidJdkPath(candidate))
                {
                    Debug.Log($"【CI】使用环境变量 {envName} 提供的 JDK: {candidate}");
                    return candidate;
                }
            }

#if UNITY_EDITOR_WIN
            var roots = new[]
            {
                @"C:\Program Files\Java",
                @"C:\Program Files\OpenJDK",
                @"C:\Program Files\Microsoft",
                @"C:\Program Files\Android\Android Studio",
            };

            foreach (var root in roots)
            {
                if (!Directory.Exists(root))
                {
                    continue;
                }

                foreach (var candidate in Directory.GetDirectories(root))
                {
                    if (IsValidJdkPath(candidate))
                    {
                        Debug.Log($"【CI】使用自动探测到的 JDK: {candidate}");
                        return candidate;
                    }
                }

                if (IsValidJdkPath(root))
                {
                    Debug.Log($"【CI】使用自动探测到的 JDK: {root}");
                    return root;
                }
            }
#endif

            return null;
        }

        static private void EnsureAndroidJdkForBatchMode()
        {
            if (!Application.isBatchMode)
            {
                return;
            }

            if (IsValidJdkPath(AndroidExternalToolsSettings.jdkRootPath))
            {
                Debug.Log($"【CI】Unity Android JDK 已配置: {AndroidExternalToolsSettings.jdkRootPath}");
                return;
            }

            var detectedJdkPath = FindJdkFromEnvironment();
            if (IsValidJdkPath(detectedJdkPath))
            {
                AndroidExternalToolsSettings.jdkRootPath = detectedJdkPath;
                Debug.Log($"【CI】已为 Unity Android External Tools 配置 JDK: {AndroidExternalToolsSettings.jdkRootPath}");
                return;
            }

            Debug.LogWarning("【CI】未找到可用 JDK；如果 TeamCity Agent 已安装 JDK，请设置 JAVA_HOME/JDK_HOME/UNITY_JDK_PATH。");
        }

        /// <summary>
        /// 发布包体 AndroidDebug
        /// </summary>
        [CI(Des = "发布母包Android-Debug")]
        static public void PublishPackage_AndroidDebug()
        {
            BuildPackage(BuildTarget.Android, BuildTools_ClientPackage.BuildMode.Debug);
        }

        /// <summary>
        /// 发布包体 AndroidRelease
        /// </summary>
        [CI(Des = "发布母包Android-Release")]
        static public void PublishPackage_AndroidRelease()
        {
            BuildPackage(BuildTarget.Android, BuildTools_ClientPackage.BuildMode.Release);
        }

        /// <summary>
        /// 发布包体 iOSDebug
        /// </summary>
        [CI(Des = "发布母包iOS-Debug")]
        static public void PublishPackage_iOSDebug()
        {
            BuildPackage(BuildTarget.iOS, BuildTools_ClientPackage.BuildMode.Debug);
        }

        /// <summary>
        /// 发布包体 iOSRelease
        /// </summary>
        [CI(Des = "发布母包iOS-Release")]
        static public void PublishPackage_iOSRelease()
        {
            BuildPackage(BuildTarget.iOS, BuildTools_ClientPackage.BuildMode.Release);
        }

        /// <summary>
        /// 发布包体 WindowsDebug
        /// </summary>
        [CI(Des = "发布母包Windows-Debug")]
        static public void PublishPackage_WindowsDebug()
        {
            BuildPackage(BuildTarget.StandaloneWindows64, BuildTools_ClientPackage.BuildMode.Debug);
        }

        /// <summary>
        /// 发布包体 WindowsRelease
        /// </summary>
        [CI(Des = "发布母包Windows-Release")]
        static public void PublishPackage_WindowsRelease()
        {
            BuildPackage(BuildTarget.StandaloneWindows64, BuildTools_ClientPackage.BuildMode.Release);
        }

        /// <summary>
        /// BatchMode: 构建 Android Release 母包
        /// </summary>
        [CI(Des = "BatchMode构建母包Android-Release")]
        static public void BuildClientPackageAndroid()
        {
            BuildPackage(BuildTarget.Android, BuildTools_ClientPackage.BuildMode.Release);
        }

        /// <summary>
        /// BatchMode: 构建 iOS Release 母包
        /// </summary>
        [CI(Des = "BatchMode构建母包iOS-Release")]
        static public void BuildClientPackageIOS()
        {
            BuildPackage(BuildTarget.iOS, BuildTools_ClientPackage.BuildMode.Release);
        }

        /// <summary>
        /// BatchMode: 构建 Windows Release 母包
        /// </summary>
        [CI(Des = "BatchMode构建母包Windows-Release")]
        static public void BuildClientPackageWindows()
        {
            BuildPackage(BuildTarget.StandaloneWindows64, BuildTools_ClientPackage.BuildMode.Release);
        }


        /// <summary>
        /// 构建包体
        /// </summary>
        static private void BuildPackage(BuildTarget buildTarget, BuildTools_ClientPackage.BuildMode buildMode)
        {
            var clientVersion = GetClientVersion();
            Debug.Log($"【CI】BuildTarget:{buildTarget} BuildMode:{buildMode} ClientVersion:{clientVersion}");

            if (buildTarget == BuildTarget.Android)
            {
                EnsureAndroidJdkForBatchMode();
            }

            var ret = BuildTools_ClientPackage.Build(
                buildMode,
                true,
                BApplication.DevOpsPublishClientPackagePath,
                buildTarget,
                BuildTools_Assets.BuildPackageOption.BuildAll,
                clientVersion);

            if (!ret)
            {
                throw new Exception($"【CI】构建母包失败! Target:{buildTarget} Mode:{buildMode} ClientVersion:{clientVersion}");
            }
        }

        #endregion


        
    }
}
