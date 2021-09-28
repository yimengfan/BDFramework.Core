using System;
using System.IO;
using System.Text;
using BDFramework.VersionContrller;
using LitJson;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.BuildPackage
{
    static public class FileHelper
    {
        /// <summary>
        /// 资源转hash
        /// </summary>
        /// <param name="path"></param>
        /// <param name="uploadHttpApi"></param>
        static public void Assets2Hash(string path)
        {
            var ios = IPath.Combine(path, "iOS");
            var android = IPath.Combine(path, "Android");
            var windows = IPath.Combine(path, "Windows");

            DateTime startTime = TimeZoneInfo.ConvertTime(new System.DateTime(1970, 1, 1), TimeZoneInfo.Utc,TimeZoneInfo.Local);  // 当地时区
            long timeStamp = (long)(DateTime.Now - startTime).TotalSeconds;
         
            if (Directory.Exists(ios))
            {
                File2Hash("iOS", timeStamp.ToString(), ios);
            }
            if (Directory.Exists(android))
            {
                File2Hash("Android", timeStamp.ToString(), android);
            }

            if (Directory.Exists(windows))
            {
                File2Hash("Windows", timeStamp.ToString(), windows);
            }

            EditorUtility.ClearProgressBar();

            File.WriteAllText(path + "/_Hash目录提交到服务器(并去除_Hash后缀)", "");
        }

        /// <summary>
        /// 文件转hash
        /// </summary>
        /// <param name="Platform"></param>
        /// <param name="version"></param>
        /// <param name="path"></param>
        /// <returns></returns>

        static public string File2Hash(string Platform, string version, string path)
        {
            path = path.Replace("\\", "/");
            //
            var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
            var tempDirect = path + "_Hash";

            //文件准备
            if (Directory.Exists(tempDirect))
            {
                Directory.Delete(tempDirect, true);
            }

            Directory.CreateDirectory(tempDirect);
            //生成配置
            var config = new AssetConfig();
            config.Platfrom = Platform;
            config.Version = version;
            float count = 0;
            foreach (var f in files)
            {
                count++;
                EditorUtility.DisplayProgressBar(Platform + " 资源处理",
                    string.Format("生成文件hash:{0}/{1}", count, files.Length), count / files.Length);
                var ext = Path.GetExtension(f).ToLower();
                if (ext == ".manifest" || ext == ".meta")
                {
                    continue;
                }

                //
                var hash = GetMD5HashFromFile(f);

                var localPath = f.Replace("\\", "/").Replace(path + "/", "");

                var item = new AssetItem() {HashName = hash, LocalPath = localPath};

                config.Assets.Add(item);

                //开始拷贝
                try
                {
                    File.Copy(f, IPath.Combine(tempDirect, hash));
                }
                catch (Exception e)
                {
                    Debug.LogError("error file:" + f);
                    Debug.LogError(e);
                    throw;
                }
            }

            //生成配置
            File.WriteAllText(IPath.Combine(tempDirect, Platform + "_VersionConfig.json"), JsonMapper.ToJson(config));
            //
            return tempDirect;
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