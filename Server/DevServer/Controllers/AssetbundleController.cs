using System;
using System.IO;
using System.Linq;
using DevServer.Logic.FileService;
using DevServer.Model;
using DevServer.Service.Sqlite;
using DevServer.Tools.Log;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DevServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AssetbundleController : ControllerBase
    {
        private AssetBundleFileService abService;

        public AssetbundleController(AssetBundleFileService abService)
        {
            this.abService = abService;
        }


        /// <summary>
        /// 获取最新版本
        /// </summary>
        /// <param name="projName"></param>
        /// <param name="platform"></param>
        /// <returns></returns>
        [HttpGet("GetLastVersion/{projName}/{platform}")]
        public Response GetLastVersion(string projName, string platform)
        {
            Response ret = new Response();
            //修改数据库最新的文件版本
            using (var db = new SqliteContext())
            {
               var ab = db.AssetBundleDatas.OrderBy((abd) => abd.Version)                          //
                    .LastOrDefault((ab) => ab.ProjName == projName && ab.Platform == platform); //
                if (ab == null)
                {
                    ret.Success(content: 0);
                }
                else
                {
                    ret.Success(content: ab.Version);
                }
            }

            return ret;
        }


        /// <summary>
        /// 获取所有文件列表
        /// </summary>
        /// <param name="projName"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        [HttpGet("GetLastUploadFiles/{projName}/{platform}")]
        public Response GetLastUploadFiles(string projName, string platform)
        {
            var ret = new Response();

            AssetBundleData ab = null;
            using (var db = new SqliteContext())
            {
                ab = db.AssetBundleDatas.OrderBy((abd) => abd.Version)                          //
                    .LastOrDefault((ab) => ab.ProjName == projName && ab.Platform == platform); //
            }


            if (ab == null)
            {
                ret.Fail(msg: "没有提交过");
            }
            else
            {
                var fs = abService.GetAllFile(ab.ProjName, ab.Version.ToString(), platform);
                if (fs == null)
                {
                    ret.Fail(msg: "文件不存在");
                }
                else
                {
                    ret.Success(content: fs); // = fs;
                }
            }

            return ret;
        }

        /// <summary>
        /// 上传
        /// </summary>
        /// <param name="projName"></param>
        /// <param name="version"></param>
        /// <param name="platform"></param>
        /// <param name="file"></param>
        [HttpPost("upload/{projName}/{version}/{platform}")]
        public Response UploadFile(string projName, int? version, string platform, IFormFile file)
        {
            var ret = new Response();
            if (string.IsNullOrEmpty(projName) || version == null || file == null)
            {
                ret.Fail(0, "参数出错!");

                return ret;
            }

            //增加一条记录
            using (var db = new SqliteContext())
            {
                var ab = db.AssetBundleDatas.FirstOrDefault((ab) => ab.ProjName    == projName  // 
                                                                    && ab.Platform == platform  //
                                                                    && ab.Version  == version); //
                if (ab == null)
                {
                    ab          = new AssetBundleData();
                    ab.ProjName = projName;
                    ab.Platform = platform;
                    ab.Version  = (int) version;
                    ab.Timer    = DateTime.Now.ToShortTimeString();
                    db.Add(ab);
                    db.SaveChanges();
                }
            }


            try
            {
                //保存文件
                abService.SaveFile(projName, platform, version.ToString(), file);
                ret.Success(msg: "成功!");
            }
            catch (Exception e)
            {
                ret.Fail(msg: e.Message);
            }

            return ret;
        }

        /// <summary>
        /// 下载
        /// </summary>
        /// <param name="filename"></param>
        [HttpGet("download/{projName}/{platform}/{filename}")]
        public IActionResult DownloadFile(string projName, string platform, string filename)
        {
            var version = 0;
            using (var db = new SqliteContext())
            {
                var ab = db.AssetBundleDatas.OrderBy((abd) => abd.Version)                      //
                    .LastOrDefault((ab) => ab.ProjName == projName && ab.Platform == platform); //

                if (ab != null)
                {
                    version = ab.Version;
                }
            }

            var fp = this.abService.GetFilePath(projName, version.ToString(), platform, filename);
            return File(new FileStream(fp, FileMode.Open), "application/octet-stream", filename);
        }
    }
}