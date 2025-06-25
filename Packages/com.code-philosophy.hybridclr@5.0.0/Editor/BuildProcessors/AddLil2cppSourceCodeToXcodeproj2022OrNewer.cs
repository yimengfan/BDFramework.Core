using HybridCLR.Editor.Installer;
using HybridCLR.Editor.Settings;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEngine;

#if UNITY_2022_2_OR_NEWER && UNITY_IOS

namespace HybridCLR.Editor.BuildProcessors
{
    public static class AddLil2cppSourceCodeToXcodeproj2022OrNewer
    {
        //[MenuItem("HybridCLR/Modfiyxcode")]
        //public static void Modify()
        //{
        //    OnPostProcessBuild(BuildTarget.iOS, $"{SettingsUtil.ProjectDir}/Build-iOS");
        //}

        [PostProcessBuild]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS || !HybridCLRSettings.Instance.enable)
                return;

            string pbxprojFile = $"{pathToBuiltProject}/Unity-iPhone.xcodeproj/project.pbxproj";
            RemoveExternalLibil2cppOption(pbxprojFile);
            CopyLibil2cppToXcodeProj(pathToBuiltProject);
        }

        private static void RemoveExternalLibil2cppOption(string pbxprojFile)
        {
            string pbxprojContent = File.ReadAllText(pbxprojFile, Encoding.UTF8);
            string removeBuildOption = @"--external-lib-il2-cpp=\""$PROJECT_DIR/Libraries/libil2cpp.a\""";
            if (!pbxprojContent.Contains(removeBuildOption))
            {
                //throw new BuildFailedException("modified project.pbxproj fail");
                Debug.LogError("[AddLil2cppSourceCodeToXcodeproj] modified project.pbxproj fail");
                return;
            }
            pbxprojContent = pbxprojContent.Replace(removeBuildOption, "");
            File.WriteAllText(pbxprojFile, pbxprojContent, Encoding.UTF8);
            Debug.Log($"[AddLil2cppSourceCodeToXcodeproj] remove il2cpp build option '{removeBuildOption}' from file '{pbxprojFile}'");
        }

        private static void CopyLibil2cppToXcodeProj(string pathToBuiltProject)
        {
            string srcLibil2cppDir = $"{SettingsUtil.LocalIl2CppDir}/libil2cpp";
            string destLibil2cppDir = $"{pathToBuiltProject}/Il2CppOutputProject/IL2CPP/libil2cpp";
            BashUtil.RemoveDir(destLibil2cppDir);
            BashUtil.CopyDir(srcLibil2cppDir, destLibil2cppDir, true);
        }
    }
}
#endif