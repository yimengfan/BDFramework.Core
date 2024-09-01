using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BDFramework.Core.Tools;
using BDFramework.Editor.HotfixPipeline;
using UnityEditor;

namespace BDFramework.Editor.Protocol
{
    public static class Protobuf2ClassTools
    {
        public static readonly string ProtoPath = "Assets/Resource/NetProtocol/Protobuf/";
        private static readonly string classPath = "Assets/Code/Game/NetProtocol/Protobuf/";
        private static readonly string execPath = BApplication.ProjectRoot + "/Packages/com.popo.bdframework/3rdPlugins/Protobuf/Tools/ProtoC.exe";
        /// <summary>
        /// 全局父命名空间
        /// </summary>
        private static readonly string prefixName = "Com";
        
        [MenuItem("BDFrameWork工具箱/4.网络协议/Protobuf->生成Class", false, (int) BDEditorGlobalMenuItemOrderEnum.BuildPipeline_NetProtocol_Proto2Class)]
        public static void ExecuteGenProtobuf()
        {
            // 重新创建文件夹 确保不会产生冲突
            RebuildDirectory(classPath);

            var protoPathList = Directory.GetFiles(ProtoPath, "*.proto", SearchOption.AllDirectories);
            foreach (var filePath in protoPathList)
            {
                RunProtoc2Class(filePath);
                ReplaceScriptName(filePath);
            }
         
            AssetDatabase.Refresh();
            BDebug.LogError("Protobuf 转换完成");
        }

        /// <summary>
        /// 正则匹配是否存在Hotfix标志
        /// </summary>
        private static bool RegexHotfix(string filePath)
        {
            var regex = new Regex("(^.*)_namespace(.*$)");
            var hotfixRegex = new Regex("(^.*).Hotfix.(.*$)");
            using (var streamReader = new StreamReader(filePath))
            {
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    if (regex.Match(line).Success && hotfixRegex.Match(line).Success)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        
        /// <summary>
        /// 执行proto转换程序
        /// </summary>
        private static void RunProtoc2Class(string filePath)
        {
            if (!File.Exists(filePath))
            {
                BDebug.LogError($"proto2cs工具不存在! - {filePath}");
                return;
            }
            var fileName = Path.GetFileName(filePath);
            var args = $" --csharp_out={classPath} --proto_path={ProtoPath} {fileName}";
            var process = new Process();
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.FileName = execPath;;
            process.StartInfo.Arguments = args;
            process.StartInfo.UseShellExecute = true;
            process.Start();
            process.WaitForExit();
            process.Close();
        }

        /// <summary>
        /// 替换脚本名字
        /// </summary>
        private static void ReplaceScriptName(string filePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var genFileName = fileName.Replace(".", "");
            var genFilePath = $"{classPath}{genFileName}.cs";
            
            if (RegexHotfix(filePath))
            {
                fileName += "@hotfix";
                var newFilePath = $"{classPath}{fileName}.cs";
                File.Move(genFilePath, newFilePath);
            }
            else
            {
                var newFilePath = $"{classPath}{fileName}.cs";
                File.Move(genFilePath, newFilePath);
            }
        }
        
        /// <summary>
        /// 重新生成目录
        /// </summary>
        private static void RebuildDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
            Directory.CreateDirectory(path);
        }
    }
}