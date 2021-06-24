using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BDFramework.Core.Tools;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BDFramework.Editor.Protocol
{
    public static class Protobuf2ClassTools
    {
        private static readonly string protoPath = BDApplication.ProjectRoot + "\\Assets\\Resource\\NetProtocol\\Protobuf\\";
        private static readonly string classPath = BDApplication.ProjectRoot + "\\Assets\\Code\\Game@hotfix\\NetProtocol\\Protobuf\\";
        private static readonly string execPath = BDApplication.ProjectRoot + "/Packages/com.popo.bdframework/Runtime/3rdGithub/NetProtocol/Tools/ProtoC.exe";
        
        private static readonly string cachePath = BDApplication.BDEditorCachePath + "/ProtoCache/";
        private static readonly string protoCachePath = cachePath + "Proto/";
        private static readonly string classCachePath = cachePath + "Class/";
        
        [MenuItem("BDFrameWork工具箱/4.网络协议/Protobuf->生成Class", false, (int) BDEditorMenuEnum.BuildPackage_NetProtocol_Proto2Class)]
        public static void ExecuteGenProtobuf()
        {
            //重新创建文件夹 确保不会产生冲突
            RebuildDirectory(classPath);
            RebuildDirectory(cachePath);

            var protoPaths = GetProtoFiles(protoPath);
            foreach (var toPath in protoPaths)
            {
                ReplaceNamespace(toPath);

                //拆分相对路径的 路径 文件名
                var relativePath = GetRelativePath(protoPath, toPath);
                var path = Path.GetDirectoryName(relativePath);
                var name = Path.GetFileName(relativePath);

                RunProtobufExe(name, path);
            }
            
            AssetDatabase.Refresh();
            Debug.Log("Protobuf 转换完成");
        }
        
        /// <summary>
        /// 缓存区替换proto命名空间
        /// </summary>
        private static void ReplaceNamespace(string filePath)
        {
            RebuildDirectory(protoCachePath);
            
            var fileName = Path.GetFileName(filePath);
            var @namespace = FindRightToLeft(fileName, ".", 2);
            var regex = new Regex(@"(?<=package ).*?(?=;)");

            var lines = File.ReadLines(filePath).ToArray();
            for (int i = 0; i < lines.Length; i++)
            {
                var package = regex.Match(lines[i]).Value;
                if (!string.IsNullOrEmpty(package))
                {
                    lines[i] = lines[i].Replace(package, @namespace);
                    break;
                }
            }
            
            var newPath = Path.Combine(protoCachePath, Path.GetFileName(filePath));
            File.WriteAllLines(newPath, lines);
        }

        /// <summary>
        /// 执行proto转换程序
        /// </summary>
        private static void RunProtobufExe(string fileName, string path)
        {
            RebuildDirectory(classCachePath);
            
            if (!string.IsNullOrEmpty(path)) path = path + "/";
            
            var args = $" --csharp_out={classCachePath} --proto_path={protoCachePath} {fileName}";
            Process process = new Process();
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.FileName = execPath;;
            process.StartInfo.Arguments = args;
            process.StartInfo.UseShellExecute = true;
            process.Start();
            process.WaitForExit();

            var newPath = classPath + path;
            ReplaceFileName(fileName, newPath);
        }

        /// <summary>
        /// 替换生成的class 文件名
        /// </summary>
        private static void ReplaceFileName(string fileName, string outputPath)
        {
            CheckDirectory(outputPath);
            
            var directoryInfo = new DirectoryInfo(classCachePath);
            var fileInfo = directoryInfo.GetFiles()[0];
            if (fileInfo.Exists)
            {
                var newName = fileName.Replace(".proto", ".cs");
                var newPath = Path.Combine(outputPath, newName);
                fileInfo.MoveTo(newPath);
            }
        }

        /// <summary>
        /// 从右往左搜索
        /// </summary>
        private static string FindRightToLeft(string str, string match, int count = 1)
        {
            string findStr = str;
            for (int i = 0; i < count; i++)
            {
                int startPos = findStr.LastIndexOf(match, StringComparison.Ordinal);
                findStr = findStr.Substring(0, startPos);
            }

            return findStr;
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
        /// 获取相对路径
        /// </summary>
        private static string GetRelativePath(string fromPath, string toPath)
        {
            Uri fromUri = new Uri(fromPath);
            Uri toUri = new Uri(toPath);

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            return Uri.UnescapeDataString(relativeUri.ToString());
        }

        /// <summary>
        /// 获取目录下所有proto
        /// </summary>
        private static List<string> GetProtoFiles(string folderPath)
        {
            List<string> protoList = new List<string>();
            if (Directory.Exists(folderPath))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(folderPath);
                FileInfo[] fileInfos = directoryInfo.GetFiles("*.proto", SearchOption.AllDirectories);
                foreach (var fileInfo in fileInfos)
                {
                    protoList.Add(fileInfo.FullName);
                }
            }

            return protoList;
        }
    }
}