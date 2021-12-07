using System;
using System.IO;
using System.Text;
using BDFramework.Core.Tools;
using BDFramework.ResourceMgr;
using BDFramework.VersionContrller;
using DotNetExtension;
using LitJson;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor
{
    static public class EditorAssetHelper
    {
        /// <summary>
        /// 资源转hash
        /// </summary>
        /// <param name="path"></param>
        /// <param name="uploadHttpApi"></param>
        static public void Assets2Hash(string path)
        {
            var plarforms = new RuntimePlatform[] {RuntimePlatform.Android, RuntimePlatform.IPhonePlayer, RuntimePlatform.WindowsPlayer};

            long timeStamp = DateTimeEx.GetTotalSeconds();
            foreach (var platform in plarforms)
            {
                var platformPath = IPath.Combine(path, BDApplication.GetPlatformPath(platform));
                if (Directory.Exists(platformPath))
                {
                    var outdir = PublishAssetsToHash(platformPath, platform, timeStamp.ToString());
                    //通知回调
                    BDFrameworkPublishPipelineHelper.ReadyPublishAssetsToServer(platform, outdir);
                }
            }
            

            File.WriteAllText(path + "/_Hash目录提交到服务器(并去除_Hash后缀)", "");
        }

        /// <summary>
        /// 文件转hash
        /// </summary>
        /// <param name="outRootPath"></param>
        /// <param name="platform"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        static public string PublishAssetsToHash(string outRootPath, RuntimePlatform platform, string version)
        {
            outRootPath = outRootPath.Replace("\\", "/");
            //
            var assets = Directory.GetFiles(outRootPath, "*", SearchOption.AllDirectories);
            var outputDir = outRootPath + "_Hash";
            //文件准备
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, true);
            }

            Directory.CreateDirectory(outputDir);
            //生成配置
            var config = new AssetConfig();
            config.Platfrom = BDApplication.GetPlatformPath(platform);
            config.Version = version;
            float count = 0;
            foreach (var asset in assets)
            {
                count++;
                EditorUtility.DisplayProgressBar(platform + " 资源处理",
                    string.Format("生成文件hash:{0}/{1}", count, assets.Length), count / assets.Length);
                var ext = Path.GetExtension(asset).ToLower();
                if (ext == ".manifest" || ext == ".meta")
                {
                    continue;
                }

                //
                var hash = GetMD5HashFromFile(asset);

                var localPath = asset.Replace("\\", "/").Replace(outRootPath + "/", "");

                var item = new AssetItem() {HashName = hash, LocalPath = localPath};

                config.Assets.Add(item);

                //开始拷贝
                try
                {
                    File.Copy(asset, IPath.Combine(outputDir, hash));
                }
                catch (Exception e)
                {
                    Debug.LogError("error file:" + asset);
                    Debug.LogError(e);
                    throw;
                }
            }

            //生成配置
            File.WriteAllText(IPath.Combine(outputDir, BResources.SERVER_ASSETS_VERSION_CONFIG), JsonMapper.ToJson(config));
            //
            return outputDir;
        }


        /// <summary>
        /// File2Md5
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static string GetMD5HashFromFile(string fileName)
        {
            try
            {
                FileStream file = new FileStream(fileName, FileMode.Open);
                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(file);
                file.Close();
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("GetMD5HashFromFile() fail,error:" + ex.Message);
            }
        }
    }
}
