using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using BDFramework.Core.Tools;
using BDFramework.Editor.Tools;
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
        public void Update(string workpath = "./")
        {
            var cmd = $"update {workpath}  --username {this.UserName} --password {this.Password}";

            this.ExecuteSVN(cmd);
        }


        /// <summary>
        /// 强制Revert
        /// </summary>
        public void RevertForce(string workpath = "./")
        {
            var cmd = $"revert --recursive  {workpath}";
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
            for (int i = 0; i < paths.Length; i++)
            {
                paths[i] = $"add {paths[i]}";
            }

            //批量添加cmd
            this.ExecuteSVN(paths);
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
            for (int i = 0; i < paths.Length; i++)
            {
                paths[i] = $"delete {paths[i]}";
            }

            //批量添加cmd
            this.ExecuteSVN(paths);
        }


        /// <summary>
        /// SVN状态
        /// </summary>
        public enum Status
        {
            /// <summary>
            /// 删除
            /// </summary>
            Deleted,

            /// <summary>
            /// 冲突
            /// </summary>
            Conflict
        }

        /// <summary>
        /// 获取Status
        /// </summary>
        /// <param name="path"></param>
        /// <param name="findStr"></param>
        /// <returns></returns>
        public string[] GetStatus(Status status, string workpath = "./")
        {
            //获取status
            var statusInfos = GetStatus(workpath);
            //获取文件信息
            string[] files = new string[] { };
            if (statusInfos!=null)
            {
                var findStr = "";
                switch (status)
                {
                    case Status.Deleted:
                        findStr = "!";
                        break;
                    case Status.Conflict:
                    {
                        findStr = "C";
                    }
                        break;
                }
                files = statusInfos.Where(s => s.StartsWith(findStr, StringComparison.OrdinalIgnoreCase)).ToArray();
            }

            return files;
        }

        /// <summary>
        /// 获取Status
        /// </summary>
        /// <param name="workpath"></param>
        /// <returns></returns>
        public string[] GetStatus(string workpath = "./")
        {
            // L    abc.c               # svn已经在.svn目录锁定了abc.c
            // M      bar.c               # bar.c的内容已经在本地修改过了
            // M     baz.c               # baz.c属性有修改，但没有内容修改
            // X      3rd_party           # 这个目录是外部定义的一部分
            // ?      foo.o               # svn并没有管理foo.o
            // !      some_dir            # svn管理这个，但它可能丢失或者不完整
            // ~      qux                 # 作为file/dir/link进行了版本控制，但类型已经改变
            // I      .screenrc           # svn不管理这个，配置确定要忽略它
            // A  +   moved_dir           # 包含历史的添加，历史记录了它的来历
            // M  +   moved_dir/README    # 包含历史的添加，并有了本地修改
            // D      stuff/fish.c        # 这个文件预定要删除
            // A      stuff/loot/bloo.h   # 这个文件预定要添加
            // C      stuff/loot/lump.c   # 这个文件在更新时发生冲突
            // R      xyz.c               # 这个文件预定要被替换
            // S  stuff/squawk        # 这个文件已经跳转到了分支

            
            var statusPath = this.LocalSVNRootPath + "/status.txt";
            var cmd = $"status \"{workpath}\" > \"{statusPath}\"";
            this.ExecuteSVN(cmd);
            if (File.Exists(statusPath))
            {
                return File.ReadLines(statusPath).ToArray();
                // File.Delete(statusPath);
            }

            return null;
        }

        #region 当前版本信息

        /// <summary>
        /// 获取版本
        /// </summary>
        /// <returns></returns>
        public string GetInfo(string workpath = "./", string svnurl = null)
        {
            var infoPath = this.LocalSVNRootPath + "/info.txt";
            string cmd = "";
            if (string.IsNullOrEmpty(svnurl))
            {
                cmd = $"info {workpath}  > \"{infoPath}\"";
            }
            else
            {
                cmd = $"info {workpath} {svnurl} > \"{infoPath}\"";
            }

            //
            this.ExecuteSVN(cmd);

            return File.ReadAllText(infoPath);
        }

        /// <summary>
        /// 获取当前版本
        /// </summary>
        /// <returns></returns>
        public string GetRevision(string workpath = "./")
        {
            var infos = GetInfo(workpath).Split('\n', '\r');

            foreach (var info in infos)
            {
                if (info.StartsWith("Revision:"))
                {
                    return info.Replace("Revision:", "");
                }
            }

            return "null";
        }


        /// <summary>
        /// 获取最新版本号
        /// </summary>
        /// <returns></returns>
        public string GetLeastVersion(string workpath = "./")
        {
            var infos = GetInfo(workpath).Split('\n', '\r');
            //获取远程仓库
            var response_url = "";
            foreach (var info in infos)
            {
                if (info.StartsWith("Repository Root:"))
                {
                    response_url = info.Replace("Repository Root:", "");
                    break;
                }
            }

            Debug.Log(response_url);
            //解析
            infos = GetInfo(response_url).Split('\n', '\r');

            foreach (var info in infos)
            {
                if (info.StartsWith("Revision:"))
                {
                    return info.Replace("Revision:", "");
                }
            }

            return "0";
        }

        #endregion

        /// <summary>
        /// 获取日志
        /// </summary>
        /// <param name="len">日志条数</param>
        public string GetLog(int len = 1)
        {
            var logPath = this.LocalSVNRootPath + "/log.txt";
            var cmd = $"log  -q -l {len}  > \"{logPath}\"";
            //
            this.ExecuteSVN(cmd);

            return File.ReadAllText(logPath, Encoding.UTF8);
        }


        /// <summary>
        /// 提交
        /// </summary>
        public void Commit(string workpath = "./", string log = "")
        {
            var cmd = $"commit  {workpath} -m  \"BDFramework CI自动提交!\n {log}\"";
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

        Process process = null;

        /// <summary>
        /// 执行SVN命令
        /// </summary>
        private void ExecuteSVN(params string[] args)
        {
            var out_utf8 = "cmd /c chcp 65001"; //chcp 65001";
            var cd_dir = $"cd /d \"{this.LocalSVNRootPath}\"";
            
            var svn_exe_path =$"{BApplication.ProjectRoot}/Packages/com.popo.bdframework/Editor/SVN/GreenSVN~/svn.exe";
            if (!File.Exists(svn_exe_path))
            {
                Debug.LogError("找不到svn.exe!");
                return;
            }
            
            var svn_dir = $"\"{svn_exe_path}\"";

            var argList = new List<string>() {out_utf8, cd_dir};
            foreach (var arg in args)
            {
                //添加svn命名
                argList.Add($"{svn_dir} " + arg);
            }

            //执行cmd
            CMDTools.RunCmd(argList.ToArray());
        }


    }
}
