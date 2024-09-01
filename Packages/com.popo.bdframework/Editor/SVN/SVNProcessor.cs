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
using LitJson;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BDFramework.Editor.SVN
{
    public class SvnOption
    {
        /// <summary>
        /// 移除未在版本控制的文件
        /// </summary>
        public static readonly string CleanUp_RemoveUnversioned= "--remove-unversioned";
        /// <summary>
        /// 移除已忽略的文件
        /// </summary>
        public static readonly string CleanUp_RemoveIgnored= "--remove-ignored";
    }
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

        /// <summary>
        /// 是否打印log
        /// </summary>
        public bool Islog { get; set; } = false;

        private SVNProcessor(string svnurl, string user, string psw, string localpath, bool islog)
        {
            this.SVNURL = svnurl;
            this.UserName = user;
            this.Password = psw;
            this.LocalSVNRootPath = Path.GetFullPath(localpath);
            this.Islog = islog;
            
            BDebug.Log($"SVN-账号:{user}，密码:{psw}");
        }


        /// <summary>
        ///  svn处理器
        /// </summary>
        /// <param name="svnurl"></param>
        /// <param name="user"></param>
        /// <param name="psw"></param>
        static public SVNProcessor CreateSVNProccesor(string svnurl, string user, string psw, string localpath, bool islog = true)
        {
// #if UNITY_EDITOR_WIN
//             var svn_exe_path = $"{BApplication.ProjectRoot}/Packages/com.popo.bdframework/Editor/SVN/GreenSVN~";
//             //设置环境变量
//             var value = System.Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine);
//             if (!value.Contains(svn_exe_path))
//             {
//                 Debug.Log("当前环境变量:" + value);
//
//                 var newValue = value + (";" + svn_exe_path);
//                 System.Environment.SetEnvironmentVariable("Path", newValue, EnvironmentVariableTarget.Machine);
//                 Debug.Log("设置svn环境变量:" + svn_exe_path);
//
//                 var value2 = System.Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine);
//                 if (value2.Contains(svn_exe_path))
//                 {
//                     Debug.Log("设置svn环境变量 成功!");
//                 }
//             }
// #endif
            var svn = new SVNProcessor(svnurl, user, psw, localpath, islog);
            return svn;
        }


        private string curWorkDirect = "";

        /// <summary>
        /// 获取登录的cmd
        /// </summary>
        /// <returns></returns>
        private string GetLoginCmd()
        {
            return $" --username {this.UserName} --password {this.Password} ";
        }
        /// <summary>
        /// 是否存在svn仓库
        /// </summary>
        /// <returns></returns>
        public bool IsExsitSvnStore()
        {
            var svnmark = IPath.Combine(this.LocalSVNRootPath, ".svn");
            if (Directory.Exists(svnmark))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 检出一个仓库
        /// </summary>
        /// <param name="checkOutTo">当检出一个局部仓库的时候，需要该字段</param>
        public void CheckOut(string checkOutTo = "./")
        {
            if (!IsExsitSvnStore())
            {
                var coPath = LocalSVNRootPath;
                if (checkOutTo != "./")
                {
                    coPath = $"{coPath}/{checkOutTo}";
                }

                var cmd = $"co {this.SVNURL} {GetLoginCmd()} \"{coPath}\"";
                this.ExecuteSVN(cmd);
            }
            else
            {
                Update(checkOutTo);
            }
        }

        /// <summary>
        /// 更新一个仓库
        /// </summary>
        /// <param name="downloadpath"></param>
        public void Update(string path = "./")
        {
            var cmd = $"update \"{path}\"  {GetLoginCmd()}";

            this.ExecuteSVN(cmd);
        }


        /// <summary>
        /// 强制Revert
        /// </summary>
        public void RevertForce(string path = "./")
        {
            var cmd = $"revert --recursive  \"{path}\"";
            this.ExecuteSVN(cmd);
        }

        /// <summary>
        /// 清理
        /// </summary>
        /// <param name="option">cleanup 参数</param>
        public void CleanUp(string option="")
        {
            var cmd = $"cleanup {option} \"{this.LocalSVNRootPath}\"";
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
                paths[i] = $"add \"{paths[i]}\"";
            }

            //批量添加cmd
            this.ExecuteSVN(paths);
        }

        /// <summary>
        /// 添加文件/文件夹，包含所有子目录
        /// </summary>
        /// <param name="file"></param>
        public void ForceAdd(params string[] paths)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                paths[i] = $"add \"{paths[i]}\" --force";
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
                var cmd = $"add \"{direct}\"  --non-recursive";
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
                paths[i] = $"rm \"{paths[i]}\"";
            }

            //批量添加cmd
            this.ExecuteSVN(paths);
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="path"></param>
        public void ForceDelete(params string[] paths)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                paths[i] = $"rm \"{paths[i]}\" --force";
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
            Conflict,

            /// <summary>
            /// 修改的文件
            /// </summary>
            Motify,

            /// <summary>
            /// 新文件
            /// </summary>
            NewFile,
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
            if (statusInfos != null)
            {
                var findStr = "";
                switch (status)
                {
                    case Status.Deleted:
                    {
                        findStr = "!";
                    }
                        break;
                    case Status.Conflict:
                    {
                        findStr = "C";
                    }
                        break;
                    case Status.NewFile:
                        findStr = "?";
                        break;
                    case Status.Motify:
                    {
                        findStr = "M";
                    }
                        break;
                }

                files = statusInfos.Where(s => s.StartsWith(findStr, StringComparison.OrdinalIgnoreCase)).Select((s) => { return s.Remove(0, 1).Trim(); }).ToArray();
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


            var statusPath = this.LocalSVNRootPath + "/../status.txt";
            var cmd = $"status  \"{workpath}\" > \"{statusPath}\"";
            this.ExecuteSVN(cmd);
            if (File.Exists(statusPath))
            {
                var ret = File.ReadLines(statusPath).ToArray();
                File.Delete(statusPath);
                return ret;
            }

            return null;
        }

        /// <summary>
        /// 从staus.txt中获取对应文件名
        /// </summary>
        /// <param name="status">文件状态</param>
        /// <param name="prefixPath">Svn下级指定目录</param>
        /// <returns></returns>
        public string[] GetFileNameByStatus(Status status, string prefixPath = "")
        {
            //获取status
            var statusInfos = GetStatus(status);
            //分割生成文件名
            List<string> fileName = new List<string>();
            foreach (var info in statusInfos)
            {
                var str = info.Split(' ');
                if (str.Last().StartsWith(prefixPath))
                {
                    fileName.Add(str.Last());
                }
            }

            return fileName.ToArray();
        }

        /// <summary>
        /// svn切换远端仓库
        /// </summary>
        /// <param name="newRepositoryUrl">新仓库地址</param>
        /// <param name="localSvnPath">本地仓库</param>
        public void Switch(string newRepositoryUrl, string localSvnPath = "./")
        {
            var cmd = $"switch  {newRepositoryUrl} {localSvnPath} {GetLoginCmd()}";

            this.ExecuteSVN(cmd);
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
                cmd = $"info {workpath}  > \"{infoPath}\" {GetLoginCmd()}";
            }
            else
            {
                cmd = $"info {workpath} {svnurl} > \"{infoPath}\" {GetLoginCmd()}";
            }

            //执行svn
            this.ExecuteSVN(cmd);
            if (File.Exists(infoPath))
            {
                var info = File.ReadAllText(infoPath);
                File.Delete(infoPath);
                return info;
            }

            return "";
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
            
            //解析
            infos = GetInfo(response_url).Split('\n', '\r');

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
        /// 获取分支的URL
        /// </summary>
        /// <param name="workpath"></param>
        public string GetRelativeUrl(string workpath = "./")
        {
            var infos = GetInfo(workpath).Split('\n', '\r');
            foreach (var info in infos)
            {
                if (info.StartsWith("Relative URL:"))
                {
                    return info.Replace("Relative URL:", "");
                }
            }

            return "null";
        }
        
        /// <summary>
        /// 获取当前 对应Url 的远端最新版本
        /// </summary>
        /// <returns></returns>
        public string GetLastRevision(string url = "")
        {
            var infos = GetInfo(string.IsNullOrEmpty(url)?SVNURL:url).Split('\n', '\r');

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
        /// 获取所有分支的名字/branches/"
        /// </summary>
        /// <param name="svnRepoUrl">仓库的url(PS:不需要包括分支)</param>
        /// <returns></returns>
        public string[] GetAllBranchesInfo(string svnRepoUrl="")
        {
            var infoPath = this.LocalSVNRootPath + "/info.txt";
            string cmd = "";
            if (string.IsNullOrEmpty(svnRepoUrl))
            {
                svnRepoUrl =SVNURL.Substring(0,SVNURL.IndexOf("/branches/", StringComparison.Ordinal))+"/branches/";
                cmd =$"ls {svnRepoUrl} --depth immediates > \"{infoPath}\" {GetLoginCmd()}";
            }
            else
            {
                cmd = $"ls {svnRepoUrl}/branches/ --depth immediates > \"{infoPath}\" {GetLoginCmd()}";
            }

            //执行svn
            this.ExecuteSVN(cmd);
            if (File.Exists(infoPath))
            {
                var info = File.ReadAllText(infoPath);
                File.Delete(infoPath);
                return info.Split(new char[]{'\n','\r'},StringSplitOptions.RemoveEmptyEntries);
            }

            return null;
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
        /// 提交文件夹内所有文件
        /// </summary>
        /// <param name="floder"></param>
        /// <param name="log"></param>
        public void CommitFolder(string floder = "./", string log = "Auto Commit")
        {
            
            //删除Resource_SVN表格
            var delFiles = this.GetFileNameByStatus(Status.Deleted, floder);
            this.Delete(delFiles);
            //添加Resource_SVN表格
            var addFiles = this.GetFileNameByStatus(Status.NewFile, floder);
            this.ForceAdd(addFiles.ToArray());
            //motify
            var motifyFiles = this.GetStatus(Status.Motify,floder);
            this.ForceAdd(motifyFiles);
        }
        

        /// <summary>
        /// 提交，因为cmd设置问题，中文log会让commit失败~
        /// </summary>
        public void Commit(string workpath = "./", string log = "Auto Commit")
        {
            var cmd = $"ci  \"{workpath}\" -m  \"{log}\" ";
            this.ExecuteSVN(cmd);
        }
        public void Commit_useaccount(string workpath = "./", string log = "Auto Commit")
        {
            var cmd = $"ci  \"{workpath}\" -m  \"{log}\" --username teamcity --password teamcity";
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
            if (!Directory.Exists(this.LocalSVNRootPath))
            {
                Directory.CreateDirectory(this.LocalSVNRootPath);
            }

            var argList = new List<string>();
#if UNITY_EDITOR_OSX
            var svn_exe_path = "/opt/homebrew/bin/svn";
            var cd_dir = $"cd \"{this.LocalSVNRootPath}\"";
#elif UNITY_EDITOR_WIN

            var svn_exe_path = $"{BApplication.ProjectRoot}/Packages/com.popo.bdframework/Editor/SVN/GreenSVN~/svn.exe";
            var out_utf8 = "cmd /c chcp 65001"; //chcp 65001";
            var cd_dir = $"cd /d \"{this.LocalSVNRootPath}\"";
            argList.Add(out_utf8);
#endif
            var svn_exe = "svn";
            argList.Add(cd_dir);
            
            //替换命令行
            foreach (var arg in args)
            {
                //添加svn命名
                argList.Add($"{svn_exe} {arg}");
            }
         
            if (!File.Exists(svn_exe_path))
            {
                Debug.LogError($"找不到svn执行文件，请安装：{svn_exe_path}!");
                Debug.Log($"执行命令行:{JsonMapper.ToJson(argList, true)}");
                return;
            }
            //执行cmd
            CMDTools.RunCmd(argList.ToArray(), svn_exe, svn_exe_path, this.Islog);
        }
    }
}
