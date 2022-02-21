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
        private static readonly string protoPath = BDApplication.ProjectRoot + "/Assets/Resource/NetProtocol/Protobuf/";
        private static readonly string classPath = BDApplication.ProjectRoot + "/Assets/Code/Game/NetProtocol/Protobuf/";
        private static readonly string cachePath = BDApplication.BDEditorCachePath + "/ProtoCache/";
        private static readonly string execPath = BDApplication.ProjectRoot + "/Packages/com.popo.bdframework/Runtime/3rdGithub/NetProtocol/Tools/ProtoC.exe";
        private static readonly string prefixName = "Com";
        
        [MenuItem("BDFrameWork工具箱/4.网络协议/Protobuf->生成Class", false, (int) BDEditorGlobalMenuItemOrderEnum.BuildPipeline_NetProtocol_Proto2Class)]
        public static void ExecuteGenProtobuf()
        {
            // 重新创建文件夹 确保不会产生冲突
            RebuildDirectory(classPath);
            RebuildDirectory(cachePath);

            var protoPaths = GetProtoAndCopyFiles(protoPath);
            foreach (var toPath in protoPaths)
            {
                if (ReplaceNamespace(toPath))
                {
                    RunProtobufExe(toPath);
                }
            }
            
            AssetDatabase.Refresh();
            BDebug.Log("Protobuf 转换完成");
        }
        
        /// <summary>
        /// 缓存区替换proto命名空间
        /// </summary>
        private static bool ReplaceNamespace(string filePath)
        {
            var newFilePath = Path.Combine(cachePath, Path.GetFileName(filePath));
            var _namespace = Path.GetFileNameWithoutExtension(filePath);
            
            // 文件名是否符合命名规范(namespace.xxx.proto)
            if (_namespace.Contains(".response") || _namespace.Contains(".request"))
            {
                _namespace = _namespace.Replace(".response", "");
                _namespace = _namespace.Replace(".request", "");
                
                if (string.IsNullOrEmpty(_namespace))
                {
                    BDebug.Log($"{_namespace} 不符合命名规范!");
                    return false;
                }
                
                _namespace = $"{prefixName}.{_namespace}";
            }
            else
            {
                // 解决 因非url package 问题
                _namespace = prefixName;
            }
            
            // 替换proto包名 对应 生成的Class名
            var regex = new Regex(@"(?<=package ).*?(?=;)");
            var lines = File.ReadLines(filePath).ToArray();
            for (int i = 0; i < lines.Length; i++)
            {
                var package = regex.Match(lines[i]).Value;
                if (!string.IsNullOrEmpty(package))
                {
                    lines[i] = lines[i].Replace(package, _namespace);
                    break;
                }
            }
            
            File.WriteAllLines(newFilePath, lines);
            return true;
        }

        /// <summary>
        /// 执行proto转换程序
        /// </summary>
        private static void RunProtobufExe(string filePath)
        {
            var fileName = Path.GetFileName(filePath);

            var args = $" --csharp_out={cachePath} --proto_path={cachePath} {fileName}";
            Process process = new Process();
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.FileName = execPath;;
            process.StartInfo.Arguments = args;
            process.StartInfo.UseShellExecute = true;
            process.Start();
            process.WaitForExit();
            process.Close();
            
            // 提取相对路径
            var newPath = filePath.Replace(cachePath, "");
            newPath = newPath.Replace(fileName, "");
            ReplaceFileName(fileName, classPath + newPath);
        }

        /// <summary>
        /// 替换生成的class 文件名
        /// </summary>
        private static void ReplaceFileName(string fileName, string outputPath)
        {
            CheckDirectory(outputPath);
            
            var directoryInfo = new DirectoryInfo(cachePath);
            var fileInfos = directoryInfo.GetFiles("*.cs");
            if (fileInfos.Length > 0)
            {
                var fileInfo = fileInfos[0];
                if (fileInfo.Exists)
                {
                    //获取热更config
                    var config = HotfixPipelineTools.HotfixFileConfig.GetConfig("proto");
                    
                    var newName = fileName.Replace(".proto", ".cs");
                    //判断配置是否热更
                    if (config.IsHotfixFile(fileName))
                    {
                        newName = fileName.Replace(".proto", "@hotfix.cs");
                    }

                    var newPath = Path.Combine(outputPath, newName);
                    
                    fileInfo.MoveTo(newPath);
                }
            }
        }

        /// <summary>
        /// 重新生成目录
        /// </summary>
        private static void RebuildDirectory(string path)
        {
            var directoryInfo = new DirectoryInfo(path);
            if (directoryInfo.Exists)
            {
                directoryInfo.Delete(true);
            }
            Directory.CreateDirectory(path);
        }
        
        /// <summary>
        /// 创建目录
        /// </summary>
        private static void CheckDirectory(string path)
        { 
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        
        /// <summary>
        /// 获取目录下所有proto 并复制到缓存目录
        /// </summary>
        private static List<string> GetProtoAndCopyFiles(string folderPath)
        {
            List<string> protoList = new List<string>();
            if (Directory.Exists(folderPath))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(folderPath);
                FileInfo[] fileInfos = directoryInfo.GetFiles("*", SearchOption.AllDirectories);
                foreach (var fileInfo in fileInfos)
                {
                    var destFileName = cachePath + fileInfo.Name;
                    fileInfo.CopyTo(destFileName);
                    protoList.Add(destFileName);
                }
            }

            return protoList;
        }
    }
}