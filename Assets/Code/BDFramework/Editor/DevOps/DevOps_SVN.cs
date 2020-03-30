using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BDFramework.ResourceMgr;
using Code.BDFramework.Core.Tools;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.DevOps
{
    static  public class DevOps_SVN 
    {
      
        [MenuItem("BDFrameWork工具箱/DevOps/SVN同步Git")]
        static void UpdateSVN2Git()
        {
            var sourcePath = BApplication.EditorResourceLoadPath;
            var targetPath = BApplication.RuntimeResourceLoadPath;

            var sourceDirects = Directory.GetDirectories(sourcePath, "*", SearchOption.TopDirectoryOnly);
            var targetDirects = Directory.GetDirectories(targetPath, "*", SearchOption.TopDirectoryOnly);

            foreach (var sourceDirect in sourceDirects)
            {
              
                var sourceFiles = Directory.GetFiles(sourceDirect, "*.*", SearchOption.AllDirectories)
                    .Where((s)=>!s.EndsWith(".meta")).ToList();
                //目标路径
                var targetDirect = sourceDirect.Replace(sourcePath, targetPath);
                List<string> targetFiles= new List<string>();
                if(Directory.Exists(targetDirect))
                {
                    targetFiles = Directory.GetFiles(targetDirect, "*.*", SearchOption.AllDirectories)
                        .Where((s)=>!s.EndsWith(".meta")).ToList();
                } 
                //删除目标路径中，源目录不存在的部分
                foreach (var tf in targetFiles)
                {
                    var _tf = tf.Replace(targetDirect, "");
                    if(sourceFiles.Find((sf=>sf.EndsWith(_tf)))==null)
                    {
                        File.Delete(tf);
                        EditorUtility.DisplayProgressBar("svn同步git","【删除】" +tf,1);
                    }
                    else
                    {
                        EditorUtility.DisplayProgressBar("svn同步git","【处理】" +tf,1);
                    }

                  
                }
                //复制源数据
                foreach (var sf in sourceFiles)
                {
                    var tf = sf.Replace(sourceDirect, targetDirect);
                    if (FileHelper.GetHashFromFile(sf) != FileHelper.GetHashFromFile(tf))
                    {
                        EditorUtility.DisplayProgressBar("svn同步git","【拷贝新资源】 " +sf,1);

                        var bytes = File.ReadAllBytes(sf);
                        FileHelper.WriteAllBytes(tf,bytes);
                    }
                    
                    
                }

                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
                
            }
        }

       
    }
}
