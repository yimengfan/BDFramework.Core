using System.Collections.Generic;
using System.IO;
using System.Linq;
using BDFramework.Core.Tools;
using UnityEditor;

namespace BDFramework.Editor.DevOps
{
    static public class DevOps_SVN
    {
        // [MenuItem("Assets/DevOps/SVN同步Git")]
        static void UpdateSVN2Git()
        {
            var sourcePath = BDApplication.EditorResourceRuntimePath;
            var targetPath = BDApplication.EditorResourceRuntimePath;

            // SVN2Git(sourcePath, targetPath);
        }


        /// <summary>
        /// 更新子目录
        /// </summary>
        /// <param name="subfolder"></param>
        public static void UpdateSubFolder(string subfolder)
        {
            var sourcePath = BDApplication.EditorResourceRuntimePath + "/" + subfolder;
            var targetPath = BDApplication.EditorResourceRuntimePath + "/"       + subfolder;
            //SVN2Git(sourcePath, targetPath);
        }


        /// <summary>
        /// 同步文件夹
        /// 这里只能同步Prefab不一致的问题
        /// 如果是png等资源，meta中有信息，且meta的资源id不一样
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="targetPath"></param>
        public static void CopyFloderAssts(string sourcePath, string targetPath)
        {
            var sourceDirects = Directory.GetDirectories(sourcePath, "*", SearchOption.TopDirectoryOnly).ToList();
            var targetDirects = Directory.GetDirectories(targetPath, "*", SearchOption.TopDirectoryOnly);
            //sourceDirects.Add(sourcePath);
            //
            foreach (var sourceDirect in sourceDirects)
            {
                var sourceFiles = Directory.GetFiles(sourceDirect, "*.*", SearchOption.AllDirectories)
                    .Where((s) => !s.EndsWith(".meta")).ToList();
                //目标路径
                var          targetDirect = sourceDirect.Replace(sourcePath, targetPath);
                List<string> targetFiles  = new List<string>();
                if (Directory.Exists(targetDirect))
                {
                    targetFiles = Directory.GetFiles(targetDirect, "*.*", SearchOption.AllDirectories)
                        .Where((s) => !s.EndsWith(".meta")).ToList();
                }

                //删除目标路径中，源目录不存在的部分
                foreach (var tf in targetFiles)
                {
                    var _tf = tf.Replace(targetDirect, "");
                    if (sourceFiles.Find((sf => sf.EndsWith(_tf))) == null)
                    {
                        File.Delete(tf);
                        EditorUtility.DisplayProgressBar("svn同步git", "【删除】" + tf, 1);
                    }

                    // else
                    // {
                    //     EditorUtility.DisplayProgressBar("svn同步git", "【处理】" + tf, 1);
                    // }
                }

                //复制源数据
                foreach (var sf in sourceFiles)
                {
                    var tf = sf.Replace(sourceDirect, targetDirect);
                    if (FileHelper.GetHashFromFile(sf) != FileHelper.GetHashFromFile(tf))
                    {
                        EditorUtility.DisplayProgressBar("svn同步git", "【拷贝新资源】 " + sf, 1);
                        //文件夹判断
                        var dir = Path.GetDirectoryName(tf);
                        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                        //拷贝
                        AssetDatabase.CopyAsset(sf, tf);
                        // var bytes = File.ReadAllBytes(sf);
                        // FileHelper.WriteAllBytes(tf, bytes);
                    }
                }

                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// 复制fileAssets
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="targetPath"></param>
        public static void CopyFileAssts(string sourcePath, string targetPath)
        {
            //
            var sourceFiles = Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories).Where((s) => !s.EndsWith(".meta")).ToList();
            //目标路径
            List<string> targetFiles  = new List<string>();
            if (Directory.Exists(targetPath))
            {
                targetFiles = Directory.GetFiles(targetPath, "*.*", SearchOption.AllDirectories).Where((s) => !s.EndsWith(".meta")).ToList();
            }

            //删除目标路径中，源目录不存在的部分
            foreach (var tf in targetFiles)
            {
                var sf = tf.Replace(targetPath, sourcePath);
                //源文档中不存在
                if (!File.Exists(sf))
                {
                    File.Delete(tf);
                    EditorUtility.DisplayProgressBar("svn同步git", "【删除】" + tf, 1);
                }
            }

            //复制源数据
            foreach (var sf in sourceFiles)
            {
                var tf = sf.Replace(sourcePath, targetPath);
                if (FileHelper.GetHashFromFile(sf) != FileHelper.GetHashFromFile(tf))
                {
                    EditorUtility.DisplayProgressBar("svn同步git", "【拷贝新资源】 " + sf, 1);
                    //文件夹判断
                    var dir = Path.GetDirectoryName(tf);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    //拷贝
                    AssetDatabase.CopyAsset(sf, tf);
                    // var bytes = File.ReadAllBytes(sf);
                    // FileHelper.WriteAllBytes(tf, bytes);
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }
    }
}