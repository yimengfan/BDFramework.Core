using System.IO;
using BDFramework.Core.Tools;
using BDFramework.Editor.HotfixScript;
using UnityEngine;

namespace BDFramework.Editor.WorkFlow
{
    /// <summary>
    /// 热更代码工作流
    /// </summary>
    static public class HotfixCodeWorkFlow
    {
        /// <summary>
        /// 热更代码发生改变
        /// </summary>
        static public void OnCodeChanged()
        {
            //获取最近修改的代码
            var codes = BDEditorApplication.GetLeastHotfixCodes();
            if (codes != null && codes.Length > 0) //修改过Hotfix
            {
                if (BDEditorApplication.EditorSetting.BuildHotfixDLLSetting.IsNeedAutoBuildDll())
                {
                    BuildTools_HotfixScript.BuildDLL(BApplication.streamingAssetsPath, BApplication.RuntimePlatform, Unity3dRoslynBuildTools.BuildMode.Debug, false);
                    Debug.Log("自动编译Hotfix.dll成功!");
                }
                else if (BDEditorApplication.EditorSetting.BuildHotfixDLLSetting.IsHotfixCodeOutofUnityAssets())
                {
                    MoveCodeToBDWorkSpace(codes);
                }
            }
        }


        /// <summary>
        /// 迁移代码到BDWorkSpace
        /// </summary>
        static public void MoveCodeToBDWorkSpace(string[] hotfixCodes)
        {
            var targetRoot = BApplication.BDWorkSpace + "/HotfixCode";
            
            foreach (var codePath in hotfixCodes)
            {
                var targetpath = Path.Combine(targetRoot, codePath);
                if (codePath.StartsWith("Assets")) //移动
                {
                    
                    FileHelper.Copy(codePath, targetpath,true);
                }
                else if (codePath.StartsWith("Package")) //拷贝&&覆盖
                {
                    FileHelper.Copy(codePath, targetpath, true);
                }
            }

            AddCSFileToHotfix();
        }
        
        /// <summary>
        /// 添加热更
        /// </summary>
        /// <param name="file"></param>
        static void AddCSFileToHotfix()
        {
            // var sln = Directory.GetFiles(BDApplication.ProjectRoot, "*.sln");
            // var slution = SolutionFile.Parse(sln[0]);
            // foreach (var project in slution.ProjectsInOrder)
            // {
            //     //project.
            // }
            
        }
    }
}