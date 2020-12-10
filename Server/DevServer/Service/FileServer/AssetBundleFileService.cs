using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DevServer.Service.Sqlite;
using DevServer.Tools.Log;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace DevServer.Logic.FileService
{
    public class AssetBundleFileService
    {
        private string fileRootPath;

        public AssetBundleFileService(IHostingEnvironment env)
        {
            if (string.IsNullOrWhiteSpace(env.WebRootPath))
            {
                this.fileRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/AssetBundleSever");
            }
            else
            {
                this.fileRootPath = env.WebRootPath + "/AssetBundleSever";
            }


            if (!Directory.Exists(this.fileRootPath))
            {
                Directory.CreateDirectory(this.fileRootPath);
            }

            Log.WriteLine("AssetBundle文件服务启动:" + this.fileRootPath);
        }

        /// <summary>
        /// 获取文件
        /// </summary>
        public string GetFilePath(string projName, string version, string platform, string filename)
        {
            var filePath = string.Format("{0}/{1}/{2}/{3}/{4}", this.fileRootPath, projName, version, platform, filename);
            return filePath;
        }

        /// <summary>
        /// 获取所有资源
        /// </summary>
        /// <param name="version"></param>
        public string[] GetAllFile(string projName, string version,string platform)
        {
            var root   = $"{this.fileRootPath}/{projName}";
           
            //只保留10个版本
            var ds = Directory.GetDirectories(root);
            if (ds.Length > 10)
            {
                using (var db = new SqliteContext())
                {
                    var ab = db.AssetBundleDatas.OrderBy((adb) => adb.Version).//排序
                        FirstOrDefault((abd) => abd.ProjName == projName && abd.Platform == platform);//取第一个
                    //删除最小版本
                    var del =    $"{root}/{ab.Version}";
                    db.Remove(ab);
                    db.SaveChanges();
                    
                    Directory.Delete(del,true);
                }
            }

            //
            var direct =$"{root}/{version}/{platform}";
            if (Directory.Exists(direct))
            {
                var fs = Directory.GetFiles(direct, "*", SearchOption.AllDirectories);
                return fs.Select((d) => Path.GetFileName(d)).ToArray();
            }

            return null;
        }

        /// <summary>
        /// 保存文件
        /// </summary>
        /// <param name="version"></param>
        /// <param name="file"></param>
        public void SaveFile(string projName, string platform, string version, IFormFile file)
        {
            var direct = string.Format("{0}/{1}/{2}/{3}", this.fileRootPath, projName, version, platform);
            if (!Directory.Exists(direct))
            {
                Directory.CreateDirectory(direct);
            }

            //
            var path = Path.Combine(direct, file.FileName);
            using (var stream = new FileStream(path, FileMode.Create))
            {
                file.CopyTo(stream);
            }
        }
    }
}