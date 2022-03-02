using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using BDFramework.Core.Tools;
using ILRuntime.Runtime;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BDFramework.Editor.SVN
{
    /// <summary>
    /// SVN的处理器
    /// </summary>
    public class SVNProcessor
    {
        public string SVNURL { get; set; } = "";
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";

        /// <summary>
        /// 本地根目录
        /// </summary>
        public string LocalSVNRootPath { get; set; }

        private SVNProcessor(string svnurl, string user, string psw, string localpath)
        {
            this.SVNURL = svnurl;
            this.UserName = user;
            this.Password = psw;
            this.LocalSVNRootPath = localpath;
        }

        /// <summary>
        ///  svn处理器
        /// </summary>
        /// <param name="svnurl"></param>
        /// <param name="user"></param>
        /// <param name="psw"></param>
        static public SVNProcessor CreateSVNProccesor(string svnurl, string user, string psw, string localpath)
        {
            var svn = new SVNProcessor(svnurl, user, psw, localpath);


            return svn;
        }


        private string curWorkDirect = "";

        /// <summary>
        /// 检出一个仓库
        /// </summary>
        /// <param name="downloadpath"></param>
        public void CheckOut()
        {
            var cmd = $"co {this.SVNURL} --username {this.UserName} --password {this.Password} {this.LocalSVNRootPath}";
            this.ExecuteSVN(cmd);
        }

        /// <summary>
        /// 更新一个仓库
        /// </summary>
        /// <param name="downloadpath"></param>
        public void Update(string subFloder = "")
        {
            var cmd = "";
            if (string.IsNullOrEmpty(subFloder))
            {
                cmd = $"update {this.LocalSVNRootPath}  --username {this.UserName} --password {this.Password}";
            }
            else
            {
                cmd = $"update {this.LocalSVNRootPath}/{subFloder}  --username {this.UserName} --password {this.Password}";
            }

            this.ExecuteSVN(cmd);
        }


        /// <summary>
        /// 强制Revert
        /// </summary>
        public void RevertForce()
        {
            var cmd = $"revert --recursive  {this.LocalSVNRootPath}";
            this.ExecuteSVN(cmd);
        }

        /// <summary>
        /// 更新一个仓库
        /// </summary>
        /// <param name="downloadpath"></param>
        public void CleanUp()
        {
            var cmd = $"cleanup {this.LocalSVNRootPath}";
            this.ExecuteSVN(cmd);
        }

        /// <summary>
        /// 添加文件/文件夹，包含所有子目录
        /// </summary>
        /// <param name="file"></param>
        public void Add(params string[] paths)
        {
            foreach (var path in paths)
            {
                var cmd = $"add {path}";
                this.ExecuteSVN(cmd);
            }
        }

        /// <summary>
        /// 添加文件夹
        /// </summary>
        /// <param name="direct"></param>
        /// <param name="isIncludeAllFile"></param>
        public void AddFloder(string direct, bool isIncludeAllFile = false)
        {
            if (isIncludeAllFile)
            {
                this.Add(direct);
            }
            else
            {
                var cmd = $"add {direct}  --non-recursive";
                this.ExecuteSVN(cmd);
            }
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="path"></param>
        public void Delete(params string[] paths)
        {
            foreach (var path in paths)
            {
                var cmd = $"delete {path}";
                this.ExecuteSVN(cmd);
            }
        }


        /// <summary>
        /// 获取删除的文件
        /// </summary>
        /// <returns></returns>
        public string[] GetDeletedFiles()
        {
            var statusPath = this.LocalSVNRootPath + "/status.txt";
            var cmd = $"status {this.LocalSVNRootPath} {statusPath}";
            //
            this.ExecuteSVN(cmd);
            //
            string[] files = new string[] { };
            if (File.Exists(statusPath))
            {
                files = File.ReadLines(statusPath).Where(s => s.Contains("!")).ToArray();
                // File.Delete(statusPath);
            }

            return files;
        }
        
        /// <summary>
        /// 获取删除的文件
        /// </summary>
        /// <returns></returns>
        public string[] GetChangedFiles()
        {
            var statusPath = this.LocalSVNRootPath + "/status.txt";
            var cmd = $"status {this.LocalSVNRootPath} {statusPath}";
            //
            this.ExecuteSVN(cmd);
            //
            string[] files = new string[] { };
            if (File.Exists(statusPath))
            {
                files = File.ReadLines(statusPath).Where(s => s.Contains("!")).ToArray();
                // File.Delete(statusPath);
            }

            return files;
        }


        /// <summary>
        /// 提交
        /// </summary>
        public void Commit(string log = "")
        {
            var cmd = $"commit  {this.LocalSVNRootPath} -m  \"BDFramework CI自动提交!\n {log}\"";
            this.ExecuteSVN(cmd);
        }


        /// <summary>
        /// 命令列表
        /// </summary>
        private List<string> cmdList = new List<string>();

        /// <summary>
        /// 添加命令
        /// </summary>
        /// <param name="cmd"></param>
        private void AddSvnCmd(string cmd)
        {
            cmdList.Add(cmd);
        }

        /// <summary>
        /// 获取最新版本号
        /// </summary>
        /// <returns></returns>
        public string GetLeastVersion()
        {
            return "";
        }

            Process svnProcess = null;
        /// <summary>
        /// 执行SVN命令
        /// </summary>
        private void ExecuteSVN(string args)
        {
            svnProcess = new Process();
            //Exe包的路径
            var exePath = BDApplication.ProjectRoot + "/Packages/com.popo.bdframework/Editor/SVN/GreenSVN~/svn.exe";
            if (!File.Exists(exePath))
            {
                Debug.LogError("Svn.exe不存在!");
            }
   

            svnProcess.StartInfo.FileName = exePath;
            svnProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            svnProcess.StartInfo.CreateNoWindow = true;
            svnProcess.StartInfo.UseShellExecute = false;
            svnProcess.StartInfo.RedirectStandardInput = true;
            svnProcess.StartInfo.RedirectStandardOutput = true;
            svnProcess.StartInfo.RedirectStandardError = true;
            svnProcess.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            //日志
            svnProcess.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Debug.Log("[Svn]" + e.Data);
                }
            };
            svnProcess.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Debug.Log("[Error]" + e.Data);
                }
            };


            //执行
            Debug.Log("执行:\n" + args);
            svnProcess.StartInfo.Arguments = args;
            //开始
            svnProcess.Start();
            svnProcess.BeginOutputReadLine();
            svnProcess.BeginErrorReadLine();
            //
            svnProcess.WaitForExit();
            svnProcess.Close();
            svnProcess.Dispose();
        }

        private void ExecuteByCmd(string args = null)
        {
            //获取cmd内容
            if (string.IsNullOrEmpty(args))
            {
                for (int i = 0; i < this.cmdList.Count; i++)
                {
                    var cmd = this.cmdList[i];
                    if (i > 0)
                    {
                        args += ("\r\n" + cmd);
                    }
                    else
                    {
                        args = cmd;
                    }
                }

                this.cmdList.Clear();
            }
        }
    }
}