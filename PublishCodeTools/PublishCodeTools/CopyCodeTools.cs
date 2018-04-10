using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using LitJson;
using System.Collections;
namespace PublishCodeTools
{
   public static  class CopyCodeTools
   {
       public class CopyTo
       {
            public string Source;
            public string Target;
       }

        static public void Exec()
        {
            var path = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Config");
            if(!File.Exists(path))
            {
                var list = new List<CopyTo>()
                {
                    new CopyTo()
                    {
                        Source =  "C:/Test",
                        Target =  "C:/Test"
                    }
                };
                var c = JsonMapper.ToJson(list);
                File.WriteAllText(path ,c);
                Console.WriteLine("不存在配置文件，已为您生成，请根据需求修改!");
                return;
            }
            var content = File.ReadAllText(path);
            var datas = JsonMapper.ToObject<List<CopyTo>>(content);

            foreach (var d in datas)
            {
                if (Directory.Exists(d.Source) == false)
                {
                    Console.Write("目录不存在:" + d.Source);
                    return;
                }
                var files = Directory.GetFiles(d.Source, "*.*", SearchOption.AllDirectories);


                //删除目录
                foreach (var file in files)
                {
                    if (Path.GetExtension(file) == ".meta") continue;

                    var fileName = file.Replace(d.Source, "").Replace("\\", "/");
                    var targetFileName = d.Target + "/" + fileName;
                    //删除相同目录
                    var fs = fileName.Replace("\\", "/").Split('/');
                    var delDir = (d.Target + "/" + fs[1]).Replace("\\", "/");
                    if (Directory.Exists(delDir))
                    {
                        Directory.Delete(delDir, true);
                    }
                }

                //复制
                foreach (var file in files)
                {
                    if (Path.GetExtension(file) == ".meta") continue;

                    var fileName = file.Replace(d.Source, "").Replace("\\", "/");
                    var targetFileName = d.Target+"/"+ fileName;
                    //复制
                    var dir = Path.GetDirectoryName(targetFileName);
                    if(Directory.Exists(dir) == false)
                    {
                        Directory.CreateDirectory(dir);
                    }

                    Console.WriteLine(string.Format("拷贝:\n{0} \n到:\n{1}", file, targetFileName));
                    File.Copy(file, targetFileName);
                }                
            }

        }

   }
}
