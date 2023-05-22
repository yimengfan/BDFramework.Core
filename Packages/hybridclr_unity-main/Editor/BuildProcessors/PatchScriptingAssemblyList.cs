using HybridCLR.Editor.UnityBinFileReader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Il2Cpp;
using UnityEditor.UnityLinker;
using UnityEngine;
using UnityFS;

namespace HybridCLR.Editor.BuildProcessors
{
    public class PatchScriptingAssemblyList :
#if UNITY_ANDROID
        IPostGenerateGradleAndroidProject,
#endif
        IPostprocessBuildWithReport
#if !UNITY_2021_1_OR_NEWER && UNITY_WEBGL
     , IIl2CppProcessor
#endif
    {
        public int callbackOrder => 0;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            // 如果直接打包apk，没有机会在PostprocessBuild中修改ScriptingAssemblies.json。
            // 因此需要在这个时机处理
            // Unity有bug，偶然情况下会传入apk的路径，导致替换失败
            if (Directory.Exists(path))
            {
                PathScriptingAssembilesFile(path);
            }
            else
            {
                PathScriptingAssembilesFile($"{SettingsUtil.ProjectDir}/Library");
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            // 如果target为Android,由于已经在OnPostGenerateGradelAndroidProject中处理过，
            // 这里不再重复处理
#if !UNITY_ANDROID && !UNITY_WEBGL

            PathScriptingAssembilesFile(report.summary.outputPath);
#endif
        }

        public void PathScriptingAssembilesFile(string path)
        {
            if (!SettingsUtil.Enable)
            {
                Debug.Log($"[PatchScriptingAssemblyList] disabled");
                return;
            }
            Debug.Log($"[PatchScriptingAssemblyList]. path:{path}");
            if (!Directory.Exists(path))
            {
                path = Path.GetDirectoryName(path);
                Debug.Log($"[PatchScriptingAssemblyList] get path parent:{path}");
            }
#if UNITY_2020_1_OR_NEWER
            AddHotFixAssembliesToScriptingAssembliesJson(path);
#else
            AddHotFixAssembliesToBinFile(path);
#endif
        }

        private void AddHotFixAssembliesToScriptingAssembliesJson(string path)
        {
            Debug.Log($"[PatchScriptingAssemblyList]. path:{path}");
            /*
             * ScriptingAssemblies.json 文件中记录了所有的dll名称，此列表在游戏启动时自动加载，
             * 不在此列表中的dll在资源反序列化时无法被找到其类型
             * 因此 OnFilterAssemblies 中移除的条目需要再加回来
             */
            string[] jsonFiles = Directory.GetFiles(path, SettingsUtil.ScriptingAssembliesJsonFile, SearchOption.AllDirectories);

            if (jsonFiles.Length == 0)
            {
                //Debug.LogError($"can not find file {SettingsUtil.ScriptingAssembliesJsonFile}");
                return;
            }

            foreach (string file in jsonFiles)
            {
                var patcher = new ScriptingAssembliesJsonPatcher();
                patcher.Load(file);
                patcher.AddScriptingAssemblies(SettingsUtil.HotUpdateAssemblyFilesIncludePreserved);
                patcher.Save(file);
            }
        }

        private void AddHotFixAssembliesToBinFile(string path)
        {
            if (AddHotFixAssembliesToGlobalgamemanagers(path))
            {
                return;
            }
            if (AddHotFixAssembliesTodataunity3d(path))
            {
                return;
            }
            Debug.LogError($"[PatchScriptingAssemblyList] can not find file '{SettingsUtil.GlobalgamemanagersBinFile}' or '{SettingsUtil.Dataunity3dBinFile}' in '{path}'");
        }

        private bool AddHotFixAssembliesToGlobalgamemanagers(string path)
        {
            string[] binFiles = Directory.GetFiles(path, SettingsUtil.GlobalgamemanagersBinFile, SearchOption.AllDirectories);

            if (binFiles.Length == 0)
            {
                return false;
            }

            foreach (string binPath in binFiles)
            {
                var binFile = new UnityBinFile();
                binFile.Load(binPath);
                binFile.AddScriptingAssemblies(SettingsUtil.HotUpdateAssemblyFilesIncludePreserved);
                binFile.Save(binPath);
                Debug.Log($"[PatchScriptingAssemblyList] patch {binPath}");
            }
            return true;
        }

        private bool AddHotFixAssembliesTodataunity3d(string path)
        {
            string[] binFiles = Directory.GetFiles(path, SettingsUtil.Dataunity3dBinFile, SearchOption.AllDirectories);

            if (binFiles.Length == 0)
            {
                return false;
            }

            foreach (string binPath in binFiles)
            {
                var patcher = new Dataunity3dPatcher();
                patcher.ApplyPatch(binPath, SettingsUtil.HotUpdateAssemblyFilesIncludePreserved);
                Debug.Log($"[PatchScriptingAssemblyList] patch {binPath}");
            }
            return true;
        }

#if UNITY_WEBGL
        public void OnBeforeConvertRun(BuildReport report, Il2CppBuildPipelineData data)
        {
            PathScriptingAssembilesFile($"{SettingsUtil.ProjectDir}/Temp/StagingArea/Data");
        }
#endif
    }
}
