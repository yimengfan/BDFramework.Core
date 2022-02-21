using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BDFramework.Core.Tools;
using UnityEditor;

namespace BDFramework.Editor.Protocol
{
    public static class Protobuf2ClassTools
    {
        private static readonly string protoPath = BDApplication.ProjectRoot + "/Assets/Resource/NetProtocol/Protobuf/";
        private static readonly string classPath = BDApplication.ProjectRoot + "/Assets/Code/Game/NetProtocol/Protobuf/";
        private static readonly string cacheProtoPath = BDApplication.BDEditorCachePath + "/ProtoCache/Proto/";
        private static readonly string cacheScriptPath = BDApplication.BDEditorCachePath + "/ProtoCache/Script/";
        private static readonly string execPath = BDApplication.ProjectRoot + "/Packages/com.popo.bdframework/Runtime/3rdGithub/NetProtocol/Tools/ProtoC.exe";
        /// <summary>
        /// 全局父命名空间
        /// </summary>
        private static readonly string prefixName = "Com";
        
        [MenuItem("BDFrameWork工具箱/4.网络协议/Protobuf->生成Class", false, (int) BDEditorGlobalMenuItemOrderEnum.BuildPipeline_NetProtocol_Proto2Class)]
        public static void ExecuteGenProtobuf()
        {
            // 重新创建文件夹 确保不会产生冲突
            RebuildDirectory(classPath);
            RebuildDirectory(cacheProtoPath);
            RebuildDirectory(cacheScriptPath);

            CopyAllProtoToCachePath();
            // 生成主工程脚本
            ReplaceGenScript(prefixName);
            // 生成热更层脚本
            ReplaceGenScript(prefixName + ".hotfix", "@hotfix");
            
            AssetDatabase.Refresh();
            BDebug.LogError("Protobuf 转换完成");
        }

        /// <summary>
        /// 复制所有Proto文件到缓存目录
        /// </summary>
        private static void CopyAllProtoToCachePath()
        {
            var protoPaths = Directory.GetFiles(protoPath, "*.proto", SearchOption.AllDirectories);
            foreach (var filePath in protoPaths)
            {
                var fileName = Path.GetFileName(filePath);
                var newFilePath = Path.Combine(cacheProtoPath, fileName);
                File.Copy(filePath, newFilePath);
            }
        }
        
        /// <summary>
        /// 替换Proto命名空间并生成脚本
        /// </summary>
        private static void ReplaceGenScript(string replace, string suffix = "")
        {
            var copyProtoPath = Directory.GetFiles(cacheProtoPath, "*.proto", SearchOption.AllDirectories).ToList();
            foreach (var filePath in copyProtoPath)
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var newFilePath = $"{cacheProtoPath}/{fileName}.proto";
                
                var genFileName = fileName.Replace(".", "");
                var genFilePath = $"{cacheScriptPath}/{genFileName}.cs";
                
                ReplacePackageWriteText(newFilePath, replace);
                RunProtoc2Class(newFilePath, cacheProtoPath, cacheScriptPath);
                File.Move(genFilePath, $"{classPath}{fileName}{suffix}.cs");
            }
        }
        
        /// <summary>
        /// 正则匹配写入文件
        /// </summary>
        private static void ReplacePackageWriteText(string filePath, string replace)
        {
            var packageRegex = new Regex(@"(?<=package ).*?(?=;)");
            var lines = File.ReadLines(filePath).ToArray();
            for (int i = 0; i < lines.Length; i++)
            {
                var package = RegexReplace(packageRegex, lines[i], replace);
                if (!string.IsNullOrEmpty(package))
                {
                    lines[i] = package;
                    break;
                }
            }
            
            File.WriteAllLines(filePath, lines);
        }
        
        /// <summary>
        /// 正则匹配替换字符串
        /// </summary>
        private static string RegexReplace(Regex regex, string str, string replace)
        {
            var match =  regex.Match(str).Value;
            if (!string.IsNullOrEmpty(match))
            {
                var substring = match.Substring(prefixName.Length);
                return str.Replace(match, $"{replace}{substring}");
            }

            return null;
        }
        
        /// <summary>
        /// 执行proto转换程序
        /// </summary>
        private static void RunProtoc2Class(string filePath, string protoPath, string scriptPath)
        {
            var fileName = Path.GetFileName(filePath);

            var args = $" --csharp_out={scriptPath} --proto_path={protoPath} {fileName}";
            Process process = new Process();
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.FileName = execPath;;
            process.StartInfo.Arguments = args;
            process.StartInfo.UseShellExecute = true;
            process.Start();
            process.WaitForExit();
            process.Close();
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