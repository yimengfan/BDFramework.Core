using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;

using Debug = UnityEngine.Debug;
using System.Text.RegularExpressions;

namespace HybridCLR.Editor.Installer
{
    public enum InstallErrorCode
    {
        Ok,
    }




    public class InstallerController
    {
        private const string hybridclr_repo_path = "hybridclr_repo";

        private const string il2cpp_plus_repo_path = "il2cpp_plus_repo";

        public int MajorVersion => _curVersion.major;

        private readonly UnityVersion _curVersion;

        private readonly HybridclrVersionManifest _versionManifest;
        private readonly HybridclrVersionInfo _curDefaultVersion;

        public InstallerController()
        {
            _curVersion = ParseUnityVersion(Application.unityVersion);
            _versionManifest = GetHybridCLRVersionManifest();
            _curDefaultVersion = _versionManifest.versions.Find(v => v.unity_version == _curVersion.major.ToString());
        }

        private HybridclrVersionManifest GetHybridCLRVersionManifest()
        {
            string versionFile = $"{SettingsUtil.ProjectDir}/{SettingsUtil.HybridCLRDataPathInPackage}/hybridclr_version.json";
            return JsonUtility.FromJson<HybridclrVersionManifest>(File.ReadAllText(versionFile, Encoding.UTF8));
        }

        [Serializable]
        class VersionDesc
        {
            public string branch;

            //public string hash;
        }

        [Serializable]
        class HybridclrVersionInfo
        {
            public string unity_version;

            public VersionDesc hybridclr;

            public VersionDesc il2cpp_plus;
        }

        [Serializable]
        class HybridclrVersionManifest
        {
            public List<HybridclrVersionInfo> versions;
        }

        private class UnityVersion
        {
            public int major;
            public int minor1;
            public int minor2;

            public override string ToString()
            {
                return $"{major}.{minor1}.{minor2}";
            }
        }

        private static readonly Regex s_unityVersionPat = new Regex(@"(\d+)\.(\d+)\.(\d+)");

        public const int min2019_4_CompatibleMinorVersion = 40;
        public const int min2020_3_CompatibleMinorVersion = 21;
        public const int min2021_3_CompatibleMinorVersion = 0;

        private UnityVersion ParseUnityVersion(string versionStr)
        {
            var matches = s_unityVersionPat.Matches(versionStr);
            if (matches.Count == 0)
            {
                return null;
            }
            // 找最后一个匹配的
            Match match = matches[matches.Count - 1];
            // Debug.Log($"capture count:{match.Groups.Count} {match.Groups[1].Value} {match.Groups[2].Value}");
            int major = int.Parse(match.Groups[1].Value);
            int minor1 = int.Parse(match.Groups[2].Value);
            int minor2 = int.Parse(match.Groups[3].Value);
            return new UnityVersion { major = major, minor1 = minor1, minor2 = minor2 };
        }

        public string GetCurrentUnityVersionMinCompatibleVersionStr()
        {
            return GetMinCompatibleVersion(MajorVersion);
        }

        public string GetMinCompatibleVersion(int majorVersion)
        {
            switch(majorVersion)
            {
                case 2019: return $"2019.4.{min2019_4_CompatibleMinorVersion}";
                case 2020: return $"2020.3.{min2020_3_CompatibleMinorVersion}";
                case 2021: return $"2021.3.{min2021_3_CompatibleMinorVersion}";
                default: throw new Exception($"not support version:{majorVersion}");
            }
        }

        public bool IsComaptibleVersion()
        {
            UnityVersion version = _curVersion;
            switch (version.major)
            {
                case 2019:
                    {
                        if (version.major != 2019 || version.minor1 != 4)
                        {
                            return false;
                        }
                        return version.minor2 >= min2019_4_CompatibleMinorVersion;
                    }
                case 2020:
                    {
                        if (version.major != 2020 || version.minor1 != 3)
                        {
                            return false;
                        }
                        return version.minor2 >= min2020_3_CompatibleMinorVersion;
                    }
                case 2021:
                    { 
                        if (version.major != 2021 || version.minor1 != 3)
                        {
                            return false;
                        }
                        return version.minor2 >= min2021_3_CompatibleMinorVersion;
                    }
                default: throw new Exception($"not support il2cpp_plus branch:{version.major}");
            }
        }

        public string HybridclrLocalVersion => _curDefaultVersion.hybridclr.branch;

        public string Il2cppPlusLocalVersion => _curDefaultVersion.il2cpp_plus.branch;


        private string GetIl2CppPathByContentPath(string contentPath)
        {
            return $"{contentPath}/il2cpp";
        }


        public void InstallDefaultHybridCLR()
        {
            RunInitLocalIl2CppData(GetIl2CppPathByContentPath(EditorApplication.applicationContentsPath), _curVersion);
        }

        public bool HasInstalledHybridCLR()
        {
            return Directory.Exists($"{SettingsUtil.LocalIl2CppDir}/libil2cpp/hybridclr");
        }


        private string GetUnityIl2CppDllInstallLocation()
        {
#if UNITY_EDITOR_WIN
            return $"{SettingsUtil.LocalIl2CppDir}/build/deploy/net471/Unity.IL2CPP.dll";
#else
            return $"{SettingsUtil.LocalIl2CppDir}/build/deploy/il2cppcore/Unity.IL2CPP.dll";
#endif
        }

        private string GetUnityIl2CppDllModifiedPath(string curVersionStr)
        {
#if UNITY_EDITOR_WIN
            return $"{SettingsUtil.ProjectDir}/{SettingsUtil.HybridCLRDataPathInPackage}/ModifiedUnityAssemblies/{curVersionStr}/Unity.IL2CPP-Win.dll.bytes";
#else
            return $"{SettingsUtil.ProjectDir}/{SettingsUtil.HybridCLRDataPathInPackage}/ModifiedUnityAssemblies/{curVersionStr}/Unity.IL2CPP-Mac.dll.bytes";
#endif
        }

        void CloneBranch(string workDir, string repoUrl, string branch, string repoDir)
        {
            BashUtil.RemoveDir(repoDir);
            BashUtil.RunCommand(workDir, "git", new string[] {"clone", "-b", branch, "--depth", "1", repoUrl, repoDir});
        }

        private void RunInitLocalIl2CppData(string editorIl2cppPath, UnityVersion version)
        {
            if (!IsComaptibleVersion())
            {
                Debug.LogError($"il2cpp 版本不兼容，最小版本为 {GetCurrentUnityVersionMinCompatibleVersionStr()}");
                return;
            }
            string workDir = SettingsUtil.HybridCLRDataDir;
            Directory.CreateDirectory(workDir);
            //BashUtil.RecreateDir(workDir);

            string buildiOSDir = $"{workDir}/iOSBuild";
            BashUtil.RemoveDir(buildiOSDir);
            BashUtil.CopyDir($"{SettingsUtil.HybridCLRDataPathInPackage}/iOSBuild", buildiOSDir, true);

            // clone hybridclr
            string hybridclrRepoURL = HybridCLRSettings.Instance.hybridclrRepoURL;
            string hybridclrRepoDir = $"{workDir}/{hybridclr_repo_path}";
            CloneBranch(workDir, hybridclrRepoURL, _curDefaultVersion.hybridclr.branch, hybridclrRepoDir);

            // clone il2cpp_plus
            string il2cppPlusRepoURL = HybridCLRSettings.Instance.il2cppPlusRepoURL;
            string il2cppPlusRepoDir = $"{workDir}/{il2cpp_plus_repo_path}";
            CloneBranch(workDir, il2cppPlusRepoURL, _curDefaultVersion.il2cpp_plus.branch, il2cppPlusRepoDir);

            // create LocalIl2Cpp
            string localUnityDataDir = SettingsUtil.LocalUnityDataDir;
            BashUtil.RecreateDir(localUnityDataDir);

            // copy MonoBleedingEdge
            BashUtil.CopyDir($"{Directory.GetParent(editorIl2cppPath)}/MonoBleedingEdge", $"{localUnityDataDir}/MonoBleedingEdge", true);

            // copy il2cpp
            BashUtil.CopyDir(editorIl2cppPath, SettingsUtil.LocalIl2CppDir, true);

            // replace libil2cpp
            string dstLibil2cppDir = $"{SettingsUtil.LocalIl2CppDir}/libil2cpp";
            BashUtil.CopyDir($"{il2cppPlusRepoDir}/libil2cpp", dstLibil2cppDir, true);
            BashUtil.CopyDir($"{hybridclrRepoDir}/hybridclr", $"{dstLibil2cppDir}/hybridclr", true);

            // clean Il2cppBuildCache
            BashUtil.RemoveDir($"{SettingsUtil.ProjectDir}/Library/Il2cppBuildCache", true);

            if (version.major == 2019)
            {
                string curVersionStr = version.ToString();
                string srcIl2CppDll = GetUnityIl2CppDllModifiedPath(curVersionStr);
                if (File.Exists(srcIl2CppDll))
                {
                    string dstIl2CppDll = GetUnityIl2CppDllInstallLocation();
                    File.Copy(srcIl2CppDll, dstIl2CppDll, true);
                    Debug.Log($"copy {srcIl2CppDll} => {dstIl2CppDll}");
                }
                else
                {
                    Debug.LogError($"未找到当前版本:{curVersionStr} 对应的改造过的 Unity.IL2CPP.dll，打包出的程序将会崩溃");
                }
            }
            if (HasInstalledHybridCLR())
            {
                Debug.Log("安装成功");
            }
            else
            {
                Debug.LogError("安装失败");
            }
        }
    }
}
