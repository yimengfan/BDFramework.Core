using HybridCLR.Editor.Installer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Il2Cpp;
using UnityEditor.UnityLinker;
using UnityEngine;

namespace HybridCLR.Editor.BuildProcessors
{
    internal class CopyStrippedAOTAssemblies : IPostprocessBuildWithReport, IPreprocessBuildWithReport
#if !UNITY_2021_1_OR_NEWER
     , IIl2CppProcessor
#endif
    {

        public int callbackOrder => 0;

#if UNITY_2021_1_OR_NEWER
        public static string GetStripAssembliesDir2021(BuildTarget target)
        {
            string projectDir = SettingsUtil.ProjectDir;
            switch (target)
            {
                case BuildTarget.StandaloneWindows:
                    case BuildTarget.StandaloneWindows64:
                    return $"{projectDir}/Library/Bee/artifacts/WinPlayerBuildProgram/ManagedStripped";
                case BuildTarget.StandaloneLinux64:
                    return $"{projectDir}/Library/Bee/artifacts/LinuxPlayerBuildProgram/ManagedStripped";
                case BuildTarget.Android:
                    return $"{projectDir}/Library/Bee/artifacts/Android/ManagedStripped";
                case BuildTarget.iOS:
                    return $"{projectDir}/Library/Bee/artifacts/iOS/ManagedStripped";
                    case BuildTarget.WebGL:
                    return $"{projectDir}/Library/Bee/artifacts/WebGL/ManagedStripped";
                case BuildTarget.StandaloneOSX:
                    return $"{projectDir}/Library/Bee/artifacts/MacStandalonePlayerBuildProgram/ManagedStripped";
                case BuildTarget.PS4:
                    return $"{projectDir}/Library/Bee/artifacts/PS4PlayerBuildProgram/ManagedStripped";
                case BuildTarget.PS5:
                    return $"{projectDir}/Library/Bee/artifacts/PS5PlayerBuildProgram/ManagedStripped";
#if TUANJIE_2022
                case BuildTarget.WeixinMiniGame:
                    return $"{projectDir}/Library/Bee/artifacts/WeixinMiniGame/ManagedStripped";
                case BuildTarget.OpenHarmony:
                    return $"{projectDir}/Library/Bee/artifacts/OpenHarmonyPlayerBuildProgram/ManagedStripped";
#endif
                default: return "";
            }
        }
#else
        private string GetStripAssembliesDir2020(BuildTarget target)
        {
            string subPath = target == BuildTarget.Android ?
                "assets/bin/Data/Managed" :
                "Data/Managed/";
            return $"{SettingsUtil.ProjectDir}/Temp/StagingArea/{subPath}";
        }

        public void OnBeforeConvertRun(BuildReport report, Il2CppBuildPipelineData data)
        {            
            // 此回调只在 2020中调用
            CopyStripDlls(GetStripAssembliesDir2020(data.target), data.target);
        }
#endif

        public static void CopyStripDlls(string srcStripDllPath, BuildTarget target)
        {
            if (!SettingsUtil.Enable)
            {
                Debug.Log($"[CopyStrippedAOTAssemblies] disabled");
                return;
            }
            Debug.Log($"[CopyStrippedAOTAssemblies] CopyScripDlls. src:{srcStripDllPath} target:{target}");

            var dstPath = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);

            Directory.CreateDirectory(dstPath);

            foreach (var fileFullPath in Directory.GetFiles(srcStripDllPath, "*.dll"))
            {
                var file = Path.GetFileName(fileFullPath);
                Debug.Log($"[CopyStrippedAOTAssemblies] copy strip dll {fileFullPath} ==> {dstPath}/{file}");
                File.Copy($"{fileFullPath}", $"{dstPath}/{file}", true);
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
#if UNITY_2021_1_OR_NEWER
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            string srcStripDllPath = GetStripAssembliesDir2021(target);
            if (!string.IsNullOrEmpty(srcStripDllPath) && Directory.Exists(srcStripDllPath))
            {
                CopyStripDlls(srcStripDllPath, target);
            }
#endif
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            var dstPath = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
            BashUtil.RecreateDir(dstPath);
        }
    }
}
