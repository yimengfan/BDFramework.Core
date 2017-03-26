using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
namespace com.putao.hotpudate
{
    public class IndexData
    {
        public string path;
        public string hash;

    }
    //索引文件的数据结构
    public class IndexFileData
    {
        public string mVersion = "null";
        public Dictionary<string, IndexData> dataMap = new Dictionary<string, IndexData>();
        /// <summary>
        /// 生成版本index对象
        /// </summary>
        /// <param name="data"></param>
        /// <param name="bytes"></param>
        static public IndexFileData Create(byte[] bytes)
        {
            IndexFileData data = new IndexFileData();
            //数据切割
            string _indexdata = System.Text.Encoding.Default.GetString(bytes);
            var lines = _indexdata.Split('\r', '\n');
            foreach (var _l in lines)
            {
                if (_l.Equals(""))
                {
                    continue;
                }
                var _block = _l.Split('|');
                if (_block.Length > 0)
                {
                    if (_block[0].Equals("Version"))
                    {
                        data.mVersion = _block[1];
                    }
                    else
                    {
                        IndexData _tempdata = new IndexData();
                        //数据段
                        foreach (var _data in _block)
                        {
                            var _d = _data.Split(':');
                            if (_d.Length > 0)
                            {

                                if (_d[0].Equals("F"))
                                {
                                    _tempdata.path = _d[1];
                                }
                                else if (_d[0].Equals("hash"))
                                {
                                    _tempdata.hash = _d[1];
                                }
                            }
                        }
                        if(_tempdata!=null)
                        data.dataMap[_tempdata.path] = _tempdata;
                     
                    }
                }

            }

            return data;
        }

        public bool ProductIndexFile(string path,string version = "0.01")
        {

            var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
            var fileList = new List<string>(files);

            string text = "Version|"+ version + "\r\n";
            foreach(var f in fileList)
            {
                var hash = Sha1.CalcSHA1String(File.ReadAllBytes(f));
                string temp = "F:" + f.Replace(path,"") + "|hash:" + hash + "\r\n";
                text += temp;
            }

            File.WriteAllText(path + "/Index.txt", text);
            return true;
        }
        public string ToString()
       {
           string text ="";
           text += ("Version|"+ this.mVersion + '\n'+'\r');
           foreach(var d in this.dataMap)
           {
               string temp = "F:" + d.Value.path + "|hash:" + d.Value.hash + '\n' + '\r';
               text += temp;
           }
           return text;
       }
    }
}
